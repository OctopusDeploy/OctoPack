using System;
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


    public static string GetNugetVersionFromGitVersionInformation(this Assembly assembly)
    {
        var types = assembly.GetTypes();
        var gitVersionInformationType = types.FirstOrDefault(t => string.Equals(t.Name, "GitVersionInformation"));
        if (gitVersionInformationType == null)
            return null;
        var versionField = gitVersionInformationType.GetField("NuGetVersion");
        return (string)versionField.GetValue(null);
    }
}