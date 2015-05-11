using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.Build.Utilities;
using OctoPack.Tasks.Util;

namespace OctoPack.Tasks
{
    /// <summary>
    /// An MSBuild task that creates an Octopus Deploy package containing only the appropriate files - 
    /// for example, an ASP.NET website will contain only the content files, assets, binaries and configuration
    /// files. C# files won't be included. Other project types (console applications, Windows Services, etc.) will 
    /// only contain the binaries. 
    /// </summary>
    public class CreateOctoPackPackage : AbstractTask
    {
        private readonly IOctopusFileSystem fileSystem;
        private readonly HashSet<string> seenBefore = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] knownProjectFileExtensions = new[] { ".csproj", ".vbproj", ".sqlproj" };


        public CreateOctoPackPackage()
            : this(new OctopusPhysicalFileSystem())
        {
        }

        public CreateOctoPackPackage(IOctopusFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Allows the name of the NuSpec file to be overridden. If empty, defaults to <see cref="ProjectName"/>.nuspec.
        /// </summary>
        public string NuSpecFileName { get; set; }

        /// <summary>
        /// Appends the value to <see cref="ProjectName"/> when generating the Id of the Nuget Package
        /// </summary>
        public string AppendToPackageId { get; set; }

        /// <summary>
        /// The list of content files in the project. For web applications, these files will be included in the final package.
        /// </summary>
        [Required]
        public ITaskItem[] ContentFiles { get; set; }

        /// <summary>
        /// The list of written files in the project. This should mean all binaries produced from the build.
        /// </summary>
        [Required]
        public ITaskItem[] WrittenFiles { get; set; }

        /// <summary>
        /// The projects root directory; set to <code>$(MSBuildProjectDirectory)</code> by default.
        /// </summary>
        [Required]
        public string ProjectDirectory { get; set; }

        /// <summary>
        /// The directory in which the built files were written to.
        /// </summary>
        [Required]
        public string OutDir { get; set; }

        /// <summary>
        /// Whether TypeScript (.ts) files should be included.
        /// </summary>
        public bool IncludeTypeScriptSourceFiles { get; set; }

        /// <summary>
        /// The NuGet package version. If not set via an MSBuild property, it will be empty in which case we'll use the version in the NuSpec file or 1.0.0.
        /// </summary>
        public string PackageVersion { get; set; }

        /// <summary>
        /// The name of the project; by default will be set to $(MSBuildProjectName). 
        /// </summary>
        [Required]
        public string ProjectName { get; set; }

        /// <summary>
        /// The path to the primary DLL/executable being produced by the project.
        /// </summary>
        [Required]
        public string PrimaryOutputAssembly { get; set; }

        /// <summary>
        /// Allows release notes to be attached to the NuSpec file when building.
        /// </summary>
        public string ReleaseNotesFile { get; set; }

        public string AppConfigFile { get; set; }

        /// <summary>
        /// Used to output the list of built packages.
        /// </summary>
        [Output]
        public ITaskItem[] Packages { get; set; }

        /// <summary>
        /// The path to NuGet.exe.
        /// </summary>
        [Output]
        public string NuGetExePath { get; set; }

        /// <summary>
        /// The version of the dac file, for database projects.
        /// </summary>
        public string DacVersion { get; set; }

        public bool EnforceAddingFiles { get; set; }

        public bool PublishPackagesToTeamCity { get; set; }

        /// <summary>
        /// Extra arguments to pass along to nuget.
        /// </summary>
        public string NuGetArguments { get; set; }

        /// <summary>
        /// Properties to pass along to nuget
        /// </summary>
        [Output]
        public string NuGetProperties { get; set; }

        /// <summary>
        /// Whether to suppress the warning about having scripts at the root
        /// </summary>
        public bool IgnoreNonRootScripts { get; set; }

        public override bool Execute()
        {
            try
            {
                LogDiagnostics();

                FindNuGet();

                WrittenFiles = WrittenFiles ?? new ITaskItem[0];
                LogMessage("Written files: " + WrittenFiles.Length);

                var octopacking = CreateEmptyOutputDirectory("octopacking");
                var octopacked = CreateEmptyOutputDirectory("octopacked");

                var specFilePath = GetOrCreateNuSpecFile(octopacking);
                var specFile = OpenNuSpecFile(specFilePath);

                UpdatePackageIdWithAppendValue(specFile);
                AddReleaseNotes(specFile);

                OutDir = fileSystem.GetFullPath(OutDir);

                if (SpecAlreadyHasFiles(specFile) && EnforceAddingFiles == false)
                {
                    LogMessage("Files will not be added because the NuSpec file already contains a <files /> section with one or more elements and option OctoPackEnforceAddingFiles was not specified.", MessageImportance.High);
                }

                if (SpecAlreadyHasFiles(specFile) == false || EnforceAddingFiles)
                {
                    var content =
                        from file in ContentFiles
                        where !string.Equals(Path.GetFileName(file.ItemSpec), "packages.config", StringComparison.OrdinalIgnoreCase)
                        select file;

                    var binaries =
                        from file in WrittenFiles
                        select file;

                    if (IsWebApplication())
                    {
                        LogMessage("Packaging an ASP.NET web application (Web.config detected)");

                        LogMessage("Add content files", MessageImportance.Normal);
                        AddFiles(specFile, content, ProjectDirectory);

                        LogMessage("Add binary files to the bin folder", MessageImportance.Normal);
                        AddFiles(specFile, binaries, ProjectDirectory, relativeTo: OutDir, targetDirectory: "bin");
                    }
                    else if (IsDatabaseApplication())
                    {
                        LogMessage("Packaging a database project (.sqlproj detected)");
                        AddFiles(specFile, binaries, ProjectDirectory, relativeTo: OutDir);
                    }
                    else
                    {
                        LogMessage("Packaging a console or Window Service application (no Web.config detected)");

                        LogMessage("Add binary files", MessageImportance.Normal);
                        AddFiles(specFile, binaries, ProjectDirectory, relativeTo: OutDir);
                    }
                }

                SaveNuSpecFile(specFilePath, specFile);

                RunNuGet(specFilePath,
                    octopacking,
                    octopacked,
                    ProjectDirectory
                    );

                CopyBuiltPackages(octopacked);

                LogMessage("OctoPack successful");

                return true;
            }
            catch (Exception ex)
            {
                LogError("OCT" + ex.GetType().Name.GetHashCode(), ex.Message);
                LogError("OCT" + ex.GetType().Name.GetHashCode(), ex.ToString());
                return false;
            }
        }

        private void LogDiagnostics()
        {
            LogMessage("---Arguments---", MessageImportance.Low);
            LogMessage("Content files: " + (ContentFiles ?? new ITaskItem[0]).Length, MessageImportance.Low);
            LogMessage("ProjectDirectory: " + ProjectDirectory, MessageImportance.Low);
            LogMessage("OutDir: " + OutDir, MessageImportance.Low);
            LogMessage("PackageVersion: " + PackageVersion, MessageImportance.Low);
            LogMessage("ProjectName: " + ProjectName, MessageImportance.Low);
            LogMessage("PrimaryOutputAssembly: " + PrimaryOutputAssembly, MessageImportance.Low);
            LogMessage("NugetArguments: " + NuGetArguments, MessageImportance.Low);
            LogMessage("NugetProperties: " + NuGetProperties, MessageImportance.Low);
            LogMessage("---------------", MessageImportance.Low);
        }

        private string CreateEmptyOutputDirectory(string name)
        {
            var temp = Path.Combine(ProjectDirectory, "obj", name);
            LogMessage("Create directory: " + temp, MessageImportance.Low);
            fileSystem.PurgeDirectory(temp, DeletionOptions.TryThreeTimes);
            fileSystem.EnsureDirectoryExists(temp);
            fileSystem.EnsureDiskHasEnoughFreeSpace(temp);
            return temp;
        }

        private string GetOrCreateNuSpecFile(string octopacking)
        {

            var packageId = RemoveTrailing(ProjectName, knownProjectFileExtensions);
            var specFileName = NuSpecFileName;
            if (string.IsNullOrWhiteSpace(specFileName))
            {
                specFileName = packageId + ".nuspec";
                //specFileName =  + ".nuspec";
            }

            if (fileSystem.FileExists(specFileName))
                Copy(new[] { Path.Combine(ProjectDirectory, specFileName) }, ProjectDirectory, octopacking);

            var specFilePath = Path.Combine(octopacking, specFileName);
            if (fileSystem.FileExists(specFilePath))
                return specFilePath;

            //  var packageId = RemoveTrailing(ProjectName, ".csproj", ".vbproj");

            LogMessage(string.Format("A NuSpec file named '{0}' was not found in the project root, so the file will be generated automatically. However, you should consider creating your own NuSpec file so that you can customize the description properly.", specFileName));

            var manifest =
                new XDocument(
                    new XElement(
                        "package",
                        new XElement(
                            "metadata",
                            new XElement("id", packageId),
                            new XElement("version", PackageVersion),
                            new XElement("authors", Environment.UserName),
                            new XElement("owners", Environment.UserName),
                            new XElement("licenseUrl", "http://example.com"),
                            new XElement("projectUrl", "http://example.com"),
                            new XElement("requireLicenseAcceptance", "false"),
                            new XElement("description", "The " + ProjectName + " deployment package, built on " + DateTime.Now.ToShortDateString()),
                            new XElement("releaseNotes", "")
                            )));

            manifest.Save(specFilePath);
            return specFilePath;
        }

        private string RemoveTrailing(string specFileName, params string[] extensions)
        {
            foreach (var extension in extensions)
            {
                if (specFileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    specFileName = specFileName.Substring(0, specFileName.Length - extension.Length).TrimEnd('.');
                }
            }

            return specFileName;
        }

        private XDocument OpenNuSpecFile(string specFilePath)
        {
            var xml = fileSystem.ReadFile(specFilePath);
            return XDocument.Parse(xml);
        }

        private void AddReleaseNotes(XContainer nuSpec)
        {
            if (string.IsNullOrWhiteSpace(ReleaseNotesFile))
            {
                return;
            }

            ReleaseNotesFile = fileSystem.GetFullPath(ReleaseNotesFile);

            if (!fileSystem.FileExists(ReleaseNotesFile))
            {
                LogWarning("OCT901", string.Format("The release notes file: {0} does not exist or could not be found. Release notes will not be added to the package.", ReleaseNotesFile));
                return;
            }

            LogMessage("Adding release notes from file: " + ReleaseNotesFile);

            var notes = fileSystem.ReadFile(ReleaseNotesFile);

            var package = nuSpec.ElementAnyNamespace("package");
            if (package == null) throw new Exception(string.Format("The NuSpec file does not contain a <package> XML element. The NuSpec file appears to be invalid."));

            var metadata = package.ElementAnyNamespace("metadata");
            if (metadata == null) throw new Exception(string.Format("The NuSpec file does not contain a <metadata> XML element. The NuSpec file appears to be invalid."));

            var releaseNotes = metadata.ElementAnyNamespace("releaseNotes");
            if (releaseNotes == null)
            {
                metadata.Add(new XElement("releaseNotes", notes));
            }
            else
            {
                releaseNotes.Value = notes;
            }
        }

        private void UpdatePackageIdWithAppendValue(XContainer nuSpec)
        {
            if (string.IsNullOrWhiteSpace(AppendToPackageId))
            {
                return;
            }

            var package = nuSpec.ElementAnyNamespace("package");
            if (package == null) throw new Exception(string.Format("The NuSpec file does not contain a <package> XML element. The NuSpec file appears to be invalid."));

            var metadata = package.ElementAnyNamespace("metadata");
            if (metadata == null) throw new Exception(string.Format("The NuSpec file does not contain a <metadata> XML element. The NuSpec file appears to be invalid."));

            var packageId = metadata.ElementAnyNamespace("id");
            if (packageId == null) throw new Exception(string.Format("The NuSpec file does not contain a <id> XML element. The NuSpec file appears to be invalid."));

            packageId.Value = string.Format("{0}.{1}", packageId.Value, AppendToPackageId.Trim());
        }


        private void AddFiles(XContainer nuSpec, IEnumerable<ITaskItem> sourceFiles, string sourceBaseDirectory, string targetDirectory = "", string relativeTo = "")
        {

            var package = nuSpec.ElementAnyNamespace("package");
            if (package == null) throw new Exception("The NuSpec file does not contain a <package> XML element. The NuSpec file appears to be invalid.");

            var files = package.ElementAnyNamespace("files");
            if (files == null)
            {
                files = new XElement("files");
                package.Add(files);
            }

            if (!string.IsNullOrWhiteSpace(relativeTo) && Path.IsPathRooted(relativeTo))
            {
                relativeTo = fileSystem.GetPathRelativeTo(relativeTo, sourceBaseDirectory);
            }

            foreach (var sourceFile in sourceFiles)
            {

                var destinationPath = sourceFile.ItemSpec;
                var link = sourceFile.GetMetadata("Link");
                if (!string.IsNullOrWhiteSpace(link))
                {
                    destinationPath = link;
                }

                if (!Path.IsPathRooted(destinationPath))
                {
                    destinationPath = fileSystem.GetFullPath(Path.Combine(sourceBaseDirectory, destinationPath));
                }

                if (Path.IsPathRooted(destinationPath))
                {
                    destinationPath = fileSystem.GetPathRelativeTo(destinationPath, sourceBaseDirectory);
                }

                if (!string.IsNullOrWhiteSpace(relativeTo))
                {
                    if (destinationPath.StartsWith(relativeTo, StringComparison.OrdinalIgnoreCase))
                    {
                        destinationPath = destinationPath.Substring(relativeTo.Length);
                    }
                }

                destinationPath = Path.Combine(targetDirectory, destinationPath);

                var sourceFilePath = Path.Combine(sourceBaseDirectory, sourceFile.ItemSpec);

                sourceFilePath = Path.GetFullPath(sourceFilePath);

                if (!fileSystem.FileExists(sourceFilePath))
                {
                    LogMessage("The source file '" + sourceFilePath + "' does not exist, so it will not be included in the package", MessageImportance.High);
                    continue;
                }

                if (seenBefore.Contains(sourceFilePath))
                {
                    continue;
                }

                seenBefore.Add(sourceFilePath);

                var fileName = Path.GetFileName(destinationPath);
                if (string.Equals(fileName, "app.config", StringComparison.OrdinalIgnoreCase))
                {
                    if (fileSystem.FileExists(AppConfigFile))
                    {
                        var configFileName = Path.GetFileName(AppConfigFile);
                        destinationPath = Path.GetDirectoryName(destinationPath);
                        destinationPath = Path.Combine(destinationPath, configFileName);
                        files.Add(new XElement("file",
                                new XAttribute("src", AppConfigFile),
                                new XAttribute("target", destinationPath)
                                ));

                        LogMessage("Added file: " + destinationPath, MessageImportance.Normal);
                    }
                    continue;
                }

                if (new[] { "Deploy.ps1", "DeployFailed.ps1", "PreDeploy.ps1", "PostDeploy.ps1" }.Any(f => string.Equals(f, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    var isNonRoot = destinationPath.Contains('\\') || destinationPath.Contains('/');
                    if (isNonRoot && !IgnoreNonRootScripts)
                    {
                        LogWarning("OCTNONROOT", "As of Octopus Deploy 2.4, PowerShell scripts that are not at the root of the package will not be executed. The script '" + destinationPath + "' lives in a subdirectory, so it will not be executed. If you want Octopus to execute this script, move it to the root of your project. If you don't want it to be executed, you can ignore this warning, or suppress it by setting the MSBuild property OctoPackIgnoreNonRootScripts=true");
                    }
                }

                var isTypeScript = string.Equals(Path.GetExtension(sourceFilePath), ".ts", StringComparison.OrdinalIgnoreCase);
                if (isTypeScript)
                {
                    if (IncludeTypeScriptSourceFiles)
                    {
                        files.Add(new XElement("file",
                            new XAttribute("src", sourceFilePath),
                            new XAttribute("target", destinationPath)
                            ));

                        LogMessage("Added file: " + destinationPath, MessageImportance.Normal);
                    }

                    var changedSource = Path.ChangeExtension(sourceFilePath, ".js");
                    var changedDestination = Path.ChangeExtension(destinationPath, ".js");
                    if (fileSystem.FileExists(changedSource))
                    {
                        files.Add(new XElement("file",
                            new XAttribute("src", changedSource),
                            new XAttribute("target", changedDestination)
                            ));

                        LogMessage("Added file: " + changedDestination, MessageImportance.Normal);
                    }
                }
                else
                {
                    files.Add(new XElement("file",
                        new XAttribute("src", sourceFilePath),
                        new XAttribute("target", destinationPath)
                        ));

                    LogMessage("Added file: " + destinationPath, MessageImportance.Normal);
                }
            }
        }

        private static bool SpecAlreadyHasFiles(XDocument nuSpec)
        {
            var package = nuSpec.ElementAnyNamespace("package");
            if (package == null) throw new Exception("The NuSpec file does not contain a <package> XML element. The NuSpec file appears to be invalid.");

            var files = package.ElementAnyNamespace("files");
            return files != null && files.Elements().Any();
        }

        private void SaveNuSpecFile(string specFilePath, XDocument document)
        {
            fileSystem.OverwriteFile(specFilePath, document.ToString());
        }

        private bool IsWebApplication()
        {
            return fileSystem.FileExists("web.config");
        }

        private bool IsDatabaseApplication()
        {
            // If a $(DacVersion) build property value is defined, or if it's a .sqlproj file, then this is a database project.
            string dbProjectName = ProjectName + ".sqlproj";
            return fileSystem.FileExists(dbProjectName) || !string.IsNullOrWhiteSpace(DacVersion);
        }

        private void Copy(IEnumerable<string> sourceFiles, string baseDirectory, string destinationDirectory)
        {
            foreach (var source in sourceFiles)
            {
                var relativePath = fileSystem.GetPathRelativeTo(source, baseDirectory);
                var destination = Path.Combine(destinationDirectory, relativePath);

                LogMessage("Copy file: " + source, importance: MessageImportance.Normal);

                var relativeDirectory = Path.GetDirectoryName(destination);
                fileSystem.EnsureDirectoryExists(relativeDirectory);

                fileSystem.CopyFile(source, destination);
            }
        }

        private void FindNuGet()
        {
            if (string.IsNullOrWhiteSpace(NuGetExePath) || !fileSystem.FileExists(NuGetExePath))
            {
                var nuGetPath = Path.Combine(Path.GetDirectoryName(typeof(CreateOctoPackPackage).Assembly.FullLocalPath()), "NuGet.exe");
                NuGetExePath = nuGetPath;
            }
        }

        private void RunNuGet(string specFilePath, string octopacking, string octopacked, string projectDirectory)
        {
            var commandLine = "pack \"" + specFilePath + "\"  -NoPackageAnalysis -BasePath \"" + projectDirectory + "\" -OutputDirectory \"" + octopacked + "\"";
            if (!string.IsNullOrWhiteSpace(PackageVersion))
            {
                commandLine += " -Version " + PackageVersion;
            }

            if (!string.IsNullOrWhiteSpace(NuGetProperties))
            {
                commandLine += " -Properties " + NuGetProperties;
            }

            if (!string.IsNullOrWhiteSpace(NuGetArguments))
            {
                commandLine += " " + NuGetArguments;
            }

            LogMessage("NuGet.exe path: " + NuGetExePath, MessageImportance.Low);
            LogMessage("Running NuGet.exe with command line arguments: " + commandLine, MessageImportance.Low);

            var exitCode = SilentProcessRunner.ExecuteCommand(
                NuGetExePath,
                commandLine,
                octopacking,
                output => LogMessage(output),
                error => LogError("OCTONUGET", error));

            if (exitCode != 0)
            {
                throw new Exception(string.Format("There was an error calling NuGet. Please see the output above for more details. Command line: '{0}' {1}", NuGetExePath, commandLine));
            }
        }

        private void CopyBuiltPackages(string packageOutput)
        {
            var packageFiles = new List<ITaskItem>();

            foreach (var file in fileSystem.EnumerateFiles(packageOutput, "*.nupkg"))
            {
                LogMessage("Packaged file: " + file, MessageImportance.Low);

                var fullPath = Path.Combine(packageOutput, file);
                packageFiles.Add(CreateTaskItemFromPackage(fullPath));

                Copy(new[] { file }, packageOutput, OutDir);

                if (PublishPackagesToTeamCity && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEAMCITY_VERSION")))
                {
                    LogMessage("##teamcity[publishArtifacts '" + file + "']");
                }
            }

            LogMessage("Packages have been copied to: " + OutDir, MessageImportance.Low);

            Packages = packageFiles.ToArray();
        }

        private static TaskItem CreateTaskItemFromPackage(string packageFile)
        {
            var metadata = new Hashtable
            {
                {"Name", Path.GetFileName(packageFile)}
            };

            return new TaskItem(packageFile, metadata);
        }
    }
}
