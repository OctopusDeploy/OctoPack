namespace OctoPack
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;

    using Microsoft.Extensions.CommandLineUtils;

    using Octopus.Client;

    public static class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.Command("pack", Pack);
            app.Command("push", Push);

            app.HelpOption("-?|-h|--help");

            if (args.Length == 0)
            {
                Console.WriteLine("octo " + GetAssemblyInformationalVersion());
                app.ShowHelp();
                return 0;
            }

            try
            {
                return app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return -1;
            }
        }

        public static void Pack(CommandLineApplication command)
        {
            command.Description = "Create a zip file from a folder";

            var idOption = command.Option(
                "--id",
                "The ID of the package; e.g MyWebSite",
                CommandOptionType.SingleValue);

            var versionOption = command.Option(
                "--version",
                "The version of the package, must be a valid SemVer",
                CommandOptionType.SingleValue);

            var sourceDirOption = command.Option(
                "-s|--source-dir",
                "Source directory",
                CommandOptionType.SingleValue);

            var destinationDirOption = command.Option(
                "-d|--destination-dir",
                "Destination directory",
                CommandOptionType.SingleValue);

            var verboseOption = command.Option(
                "-v|--verbose",
                "[Optional] Output debug info",
                CommandOptionType.NoValue);

            command.HelpOption("-?|-h|--help");

            command.OnExecute(
                () =>
                {
                    var sourceDirectoryName = sourceDirOption.Value();
                    var id = idOption.Value();
                    var version = versionOption.Value();
                    var destinationDirectoryName = destinationDirOption.Value();
                    if (string.IsNullOrWhiteSpace(sourceDirectoryName)
                        || string.IsNullOrWhiteSpace(id)
                        || string.IsNullOrWhiteSpace(version)
                        || string.IsNullOrWhiteSpace(destinationDirectoryName))
                    {
                        command.ShowHelp();
                        return -1;
                    }

                    var destinationArchiveFileName = Path.Combine(destinationDirectoryName, $"{id}.{version}.zip");
                    var verbose = verboseOption.Value();

                    if (verbose == "on")
                    {
                        Console.WriteLine($"id {id}");
                        Console.WriteLine($"version {version}");
                        Console.WriteLine($"source {sourceDirectoryName}");
                        Console.WriteLine($"destinationDir {destinationDirectoryName}");
                        Console.WriteLine($"destination {destinationArchiveFileName}");
                        Console.Out.Flush();
                    }

                    ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);

                    return 0;
                });
        }

        public static void Push(CommandLineApplication command)
        {
            command.Description = "Push a nupkg or zip file to octopus";

            var packageOption = command.Option(
                "--package",
                "Package to push",
                CommandOptionType.SingleValue);

            var serverOption = command.Option(
                "--server",
                "The base URL for your octopus server - e.g. http://your-octopus/",
                CommandOptionType.SingleValue);

            var apiKeyOption = command.Option(
                "--api-key",
                "Your API key",
                CommandOptionType.SingleValue);

            var verboseOption = command.Option(
                "-v|--verbose",
                "[Optional] Output debug info",
                CommandOptionType.NoValue);

            command.HelpOption("-?|-h|--help");

            command.OnExecute(
                async () =>
                {
                    var uriString = serverOption.Value();
                    var apiKey = apiKeyOption.Value();
                    var package = packageOption.Value();

                    if (string.IsNullOrWhiteSpace(uriString)
                        || string.IsNullOrWhiteSpace(apiKey)
                        || string.IsNullOrWhiteSpace(package))
                    {
                        command.ShowHelp();
                        return -1;
                    }

                    var server = new Uri(uriString).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);

                    var endpoint = new OctopusServerEndpoint(server, apiKey);
                    var client = await OctopusAsyncClient.Create(endpoint);
                    var repo = new OctopusAsyncRepository(client);

                    var verbose = verboseOption.Value();

                    if (verbose == "on")
                    {
                        Console.WriteLine($"server {server}");
                        Console.WriteLine($"apiKey {apiKey}");
                        Console.WriteLine($"package {package}");
                        Console.Out.Flush();
                    }

                    using (var fileStream = File.OpenRead(package))
                    {
                        await repo.BuiltInPackageRepository.PushPackage(Path.GetFileName(package), fileStream);
                    }

                    return 0;
                });
        }

        private static string GetAssemblyInformationalVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attribute?.InformationalVersion;
        }
    }
}