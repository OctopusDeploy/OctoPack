using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
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
        private readonly XName xmlPackageElement = XName.Get("package", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd");

        public CreateOctoPackPackage() : this(new OctopusPhysicalFileSystem())
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
        /// The list of content files in the project. For web applications, these files will be included in the final package.
        /// </summary>
        [Required]
        public ITaskItem[] ContentFiles { get; set; }

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

        public override bool Execute()
        {
            try
            {
                LogDiagnostics();

                FindNuGet();

                var octopacking = CreateEmptyOutputDirectory("octopacking");
                var octopacked = CreateEmptyOutputDirectory("octopacked");

                var specFilePath = GetOrCreateNuSpecFile(octopacking);
                var specFile = OpenNuSpecFile(specFilePath);

                AddReleaseNotes(specFile);

                OutDir = fileSystem.GetFullPath(OutDir);

                var content =
                    from file in ContentFiles
                    where !string.Equals(Path.GetFileName(file.ItemSpec), "packages.config", StringComparison.OrdinalIgnoreCase)
                    where !string.Equals(Path.GetFileName(file.ItemSpec), "web.debug.config", StringComparison.OrdinalIgnoreCase)
                    select Path.Combine(ProjectDirectory, file.ItemSpec);

                var binaries =
                    from file in fileSystem.EnumerateFilesRecursively(OutDir)
                    where !file.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)
                    where !file.EndsWith(".vshost.exe", StringComparison.OrdinalIgnoreCase)
                    where !file.EndsWith(".vshost.exe.config", StringComparison.OrdinalIgnoreCase)
                    where !file.EndsWith(".vshost.exe.manifest", StringComparison.OrdinalIgnoreCase)
                    where !file.EndsWith(".vshost.pdb", StringComparison.OrdinalIgnoreCase)
                    where file.IndexOf("\\_publishedwebsites", StringComparison.OrdinalIgnoreCase) < 0
                    select file;

                if (IsWebApplication())
                {
                    LogMessage("Packaging an ASP.NET web application");

                    LogMessage("Add content files", MessageImportance.Normal);
                    AddFiles(specFile, content, ProjectDirectory);

                    LogMessage("Add binary files to the bin folder", MessageImportance.Normal);
                    AddFiles(specFile, binaries, OutDir, "bin");
                }
                else
                {
                    LogMessage("Packaging a console or Window Service application");

                    LogMessage("Add binary files", MessageImportance.Normal);
                    AddFiles(specFile, binaries, OutDir);
                }

                SaveNuSpecFile(specFilePath, specFile);

                RunNuGet(specFilePath,
                    octopacking,
                    octopacked
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
            var specFileName = string.IsNullOrWhiteSpace(NuSpecFileName) ? ProjectName + ".nuspec" : NuSpecFileName;

            if (fileSystem.FileExists(specFileName))
                Copy(new[] { Path.Combine(ProjectDirectory, specFileName) }, ProjectDirectory, octopacking);

            var specFilePath = Path.Combine(octopacking, specFileName);
            if (fileSystem.FileExists(specFilePath))
                return specFilePath;

            LogWarning("OCT001", string.Format("A NuSpec file named '{0}' was not found in the project root, so the file will be generated automatically. However, you should consider creating your own NuSpec file so that you can customize the description properly.", specFileName));

            var manifest =
                new XDocument(
                    new XElement(
                        xmlPackageElement,
                        new XElement(
                            "metadata",
                            new XElement("id", ProjectName),
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

            var package = nuSpec.Element(xmlPackageElement);
            if (package == null) throw new Exception(string.Format("The NuSpec file does not contain a <package> XML element. The NuSpec file appears to be invalid."));

            var metadata = package.Element("metadata");
            if (metadata == null) throw new Exception(string.Format("The NuSpec file does not contain a <metadata> XML element. The NuSpec file appears to be invalid."));

            metadata.SetElementValue("releaseNotes", notes);
        }

        private void AddFiles(XContainer nuSpec, IEnumerable<string> sourceFiles, string sourceBaseDirectory, string targetDirectory = "")
        {
            var package = nuSpec.Element(xmlPackageElement);
            if (package == null) throw new Exception("The NuSpec file does not contain a <package> XML element. The NuSpec file appears to be invalid.");

            var files = package.Element("files");
            if (files == null)
            {
                files = new XElement("files");
                package.Add(files);
            }

            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = fileSystem.GetPathRelativeTo(sourceFile, sourceBaseDirectory);
                var destination = Path.Combine(targetDirectory, relativePath);

                LogMessage("Including file: " + sourceFile, MessageImportance.Normal);

                files.Add(new XElement("file",
                    new XAttribute("src", sourceFile),
                    new XAttribute("target", destination)
                    ));
            }
        }

        private void SaveNuSpecFile(string specFilePath, XDocument document)
        {
            fileSystem.OverwriteFile(specFilePath, document.ToString());
        }

        private bool IsWebApplication()
        {
            return fileSystem.FileExists("web.config");
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

        private void RunNuGet(string specFilePath, string octopacking, string octopacked)
        {
            var commandLine = "pack \"" + specFilePath + "\"  -NoPackageAnalysis -BasePath \"" + octopacking + "\" -OutputDirectory \"" + octopacked + "\"";
            if (!string.IsNullOrWhiteSpace(PackageVersion))
            {
                commandLine += " -Version " + PackageVersion;
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

                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEAMCITY_VERSION")))
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
