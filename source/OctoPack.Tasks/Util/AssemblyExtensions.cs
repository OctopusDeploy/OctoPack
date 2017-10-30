using System;
using System.Collections.Generic;
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
        // Visual Studio runs msbuild with an unsual set of parameters "/nodemode:1 /nodeReuse:true" which cause msbuild to stay
        // running after the build process is finished. This means that if we load the assembly directly (e.g. Assemply.Load) then 
        // the assembly will be locked and no furthre re-builds will be possible.
        var copy = File.ReadAllBytes(assemblyFullPath);
        var assembly = Assembly.Load(copy);
        var nugetVersion = assembly.GetNuGetVersionFromGitVersionInformation();
        return nugetVersion;
    }

    private static string GetNuGetVersionFromGitVersionInformation(this Assembly assembly)
    {
        IEnumerable<Type> types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null); 
        }
        var gitVersionInformationType = types.FirstOrDefault(t => string.Equals(t.Name, "GitVersionInformation"));
        if (gitVersionInformationType == null)
            return null;
        var versionField = gitVersionInformationType.GetField("NuGetVersion");
        return (string)versionField.GetValue(null);
    }
}
