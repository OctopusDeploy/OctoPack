//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0"
#tool "nuget:?package=NUnit.Runners&version=2.6.4"
#addin "Cake.FileHelpers&version=3.2.0"

using Path = System.IO.Path;
using IO = System.IO;
using Cake.Common.Xml;
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var buildDir = "./build/";
var artifactsDir = "./artifacts/";

GitVersion gitVersionInfo;
string nugetVersion;


///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });

    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);

    nugetVersion = gitVersionInfo.NuGetVersion;

    Information("Building OctoPack v{0}", nugetVersion);
    Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
    CleanDirectory(artifactsDir);
    CleanDirectories("./source/**/bin");
    CleanDirectories("./source/**/obj");
    CleanDirectories("./source/**/TestResults");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
        DotNetCoreRestore("source");
    });


Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
        MSBuild("./source/OctoPack.sln", new MSBuildSettings
        {
            Configuration = configuration,
            ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
        });
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
        var assemblies = GetFiles("./source/**/bin/**/*Tests.dll");
        foreach (var assembly in assemblies) {
                Information("Running test {0}", assembly.FullPath);
                NUnit(assembly.FullPath);
        }
    });

Task("Package")
    .IsDependentOn("Test")
    .Does(() => {
        CreateDirectory(buildDir);
        CreateDirectory(Path.Combine(buildDir, "content"));
        CreateDirectory(Path.Combine(buildDir, "content", "net35"));
        CreateDirectory(Path.Combine(buildDir, "content", "net40"));
        CreateDirectory(Path.Combine(buildDir, "content", "netcore45"));
        CreateDirectory(artifactsDir);

        CopyDirectory($"./source/OctoPack.Tasks/bin/{configuration}", Path.Combine(buildDir, "build"));
        CopyDirectory(@"./source/build", Path.Combine(buildDir, "build"));
        CopyDirectory(@"./source/tools", Path.Combine(buildDir, "tools"));
        CopyFileToDirectory(@".\source\OctoPack.nuspec", buildDir);
        CopyFileToDirectory(@".\license.txt", buildDir);

        NuGetPack(Path.Combine(buildDir, "OctoPack.nuspec"), new NuGetPackSettings {
            Version = nugetVersion,
            OutputDirectory = artifactsDir,
            NoPackageAnalysis = true
        });
    });

Task("Default")
    .IsDependentOn("Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
