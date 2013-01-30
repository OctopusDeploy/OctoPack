using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
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

        public CreateOctoPackPackage() : this(new OctopusPhysicalFileSystem())
        {
        }

        public CreateOctoPackPackage(IOctopusFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        [Required]
        public ITaskItem[] ContentFiles { get; set; }

        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string OutDir { get; set; }

        public string PackageVersion { get; set; }

        [Required]
        public string ProjectName { get; set; }

        [Required]
        public string PrimaryOutputAssembly { get; set; }

        public override bool Execute()
        {
            LogDiagnostics();

            var octopacking = CreateEmptyOutputDirectory("octopacking");
            var octopacked = CreateEmptyOutputDirectory("octopacked");

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
                where !file.EndsWith(".vshost.pdb", StringComparison.OrdinalIgnoreCase)
                select file;

            if (IsWebApplication())
            {
                LogMessage("Packaging an ASP.NET web application");
                LogMessage("Copy content files");
                Copy(content, ProjectDirectory, octopacking);

                LogMessage("Copy binary files to the bin folder");
                Copy(binaries, OutDir, Path.Combine(octopacking, "bin"));
            }
            else
            {
                LogMessage("Packaging a console or Window Service application");
                LogMessage("Copy binary files");
                Copy(binaries, OutDir, octopacking);
            }

            LogMessage("OutDir: " + OutDir);
            LogMessage("Package name: " + ProjectName);

            var specFilePath = GetOrCreateNuSpecFile(octopacking);

            RunNuGet(specFilePath, 
                octopacking,
                octopacked
                );

            CopyBuiltPackages(octopacked);

            LogMessage("OctoPack successful!");

            return true;
        }

        private void LogDiagnostics()
        {
            LogMessage("---Arguments---");
            LogMessage("Content files: " + (ContentFiles ?? new ITaskItem[0]).Length);
            LogMessage("ProjectDirectory: " + ProjectDirectory);
            LogMessage("OutDir: " + OutDir);
            LogMessage("PackageVersion: " + PackageVersion);
            LogMessage("ProjectName: " + ProjectName);
            LogMessage("PrimaryOutputAssembly: " + PrimaryOutputAssembly);
            LogMessage("---------------");
        }

        private string CreateEmptyOutputDirectory(string name)
        {
            var temp = Path.Combine(ProjectDirectory, "obj", name);
            LogMessage("Create directory: " + temp);
            fileSystem.PurgeDirectory(temp, DeletionOptions.TryThreeTimes);
            fileSystem.EnsureDirectoryExists(temp);
            fileSystem.EnsureDiskHasEnoughFreeSpace(temp);
            return temp;
        }

        private bool IsWebApplication()
        {
            return fileSystem.FileExists("web.config");
        }
        
        private string GetOrCreateNuSpecFile(string octopacking)
        {
            var specFileName = ProjectName + ".nuspec";

            if (fileSystem.FileExists(specFileName))
                Copy(new[] { Path.Combine(ProjectDirectory, specFileName) }, ProjectDirectory, octopacking);

            var specFilePath = Path.Combine(octopacking, specFileName);
            if (fileSystem.FileExists(specFilePath)) 
                return specFilePath;

            LogWarning("OCT001", string.Format("A NuSpec file named '{0}' was not found in the project root, so the file will be generated automatically. However, you should consider creating your own NuSpec file so that you can customize the description properly.", specFileName));

            var manifest =
                new XDocument(
                    new XElement(
                        "package",
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

        private void RunNuGet(string specFilePath, string octopacking, string octopacked)
        {
            var nuGetPath = Path.Combine(Path.GetDirectoryName(typeof(CreateOctoPackPackage).Assembly.FullLocalPath()), "NuGet.exe");
            var commandLine = "pack \"" + specFilePath + "\"  -NoPackageAnalysis -BasePath \"" + octopacking + "\" -OutputDirectory \"" + octopacked + "\"";
            if (!string.IsNullOrWhiteSpace(PackageVersion))
            {
                commandLine += " -Version " + PackageVersion;
            }

            LogMessage("NuGet.exe path: " + nuGetPath);
            LogMessage("Running NuGet.exe with command line arguments: " + commandLine);

            var exitCode = SilentProcessRunner.ExecuteCommand(
                nuGetPath, 
                commandLine,
                octopacking, 
                output => LogMessage(output),
                error => LogError("OCTONUGET", error));

            if (exitCode != 0)
            {
                throw new Exception(string.Format("There was an error calling NuGet. Please see the output above for more details. Command line: '{0}' {1}", nuGetPath, commandLine));
            }
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

        private void CopyBuiltPackages(string packageOutput)
        {
            foreach (var file in fileSystem.EnumerateFiles(packageOutput, "*.nupkg"))
            {
                LogMessage("Packaged file: " + file);

                Copy(new[] { file }, packageOutput, OutDir);

                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEAMCITY_VERSION")))
                {
                    LogMessage("##teamcity[publishArtifacts '" + file + "']");
                }
            }

            LogMessage("Packages have been copied to: " + OutDir);
        }
    }
}
