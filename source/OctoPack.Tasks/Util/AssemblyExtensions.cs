using System;
using System.IO;
using System.Linq;
using System.Reflection;

// ReSharper disable CheckNamespace
public static class AssemblyExtensions
    // ReSharper restore CheckNamespace
{
    public static string FullLocalPath(this Assembly assembly)
    {
        var codeBase = assembly.CodeBase;
        var uri = new UriBuilder(codeBase);
        var root = Uri.UnescapeDataString(uri.Path);
        root = root.Replace("/", "\\");
        return root;
    }


    public static string GetFileVersion(this Assembly assembly)
    {
        return assembly.GetCustomAttributes(true).OfType<AssemblyFileVersionAttribute>().First().Version;
    }


    public static string GetNuGetVersionFromGitVersionInformation(string assemblyFullPath)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => OnAssemblyResolve(assemblyFullPath, s, a);

        // Visual Studio runs msbuild with an unsual set of parameters "/nodemode:1 /nodeReuse:true" which cause msbuild to stay
        // running after the build process is finished. This means that if we load the assembly directly (e.g. Assemply.Load) then 
        // the assembly will be locked and no furthre re-builds will be possible.
        var copy = File.ReadAllBytes(assemblyFullPath);
        var assembly = Assembly.Load(copy);
        var nugetVersion = assembly.GetNuGetVersionFromGitVersionInformation();
        return nugetVersion;
    }

    private static Assembly OnAssemblyResolve(string path, object sender, ResolveEventArgs args)
    {
        var assemblyBasePath = Path.Combine(new FileInfo(path).DirectoryName, new AssemblyName(args.Name).Name);
        var assemblyPath = assemblyBasePath + ".dll";
        if (!File.Exists(assemblyPath))
            assemblyPath = assemblyBasePath + ".exe";

        if (!File.Exists(assemblyPath))
            return null;

        return Assembly.Load(File.ReadAllBytes(assemblyPath));
    }


    private static string GetNuGetVersionFromGitVersionInformation(this Assembly assembly)
    {
        var types = assembly.GetTypes();
        var gitVersionInformationType = types.FirstOrDefault(t => string.Equals(t.Name, "GitVersionInformation"));
        if (gitVersionInformationType == null)
            return null;
        var versionField = gitVersionInformationType.GetField("NuGetVersion");
        return (string)versionField.GetValue(null);
    }
}