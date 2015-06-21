using System;
using System.Reflection;

// ReSharper disable CheckNamespace
public static class AssemblyExtensions
// ReSharper restore CheckNamespace
{
    public static string FullLocalPath(Assembly assembly)
    {
        var codeBase = assembly.CodeBase;
        var uri = new UriBuilder(codeBase);
        var root = Uri.UnescapeDataString(uri.Path);
        root = root.Replace("/", "\\");
        return root;
    }

    public static string GetFileVersion(Assembly assembly)
    {
	    string version = null;
	    var attributes = assembly.GetCustomAttributes(true);
	    foreach (var attribute in attributes)
	    {
		    var versionAttribute = attribute as AssemblyFileVersionAttribute;
		    if (versionAttribute != null)
		    {
			    version = versionAttribute.Version;
			    break;
		    }
	    }

	    return version;
    }
}
