using System.Text.RegularExpressions;

public static class VersionExtensions
{
    const string SemanticVersionPattern = @"^(?<semanticVersion>(\d+(\.\d+){0,3}" // Major Minor Patch
     + @"(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?)" // Pre-release identifiers
     + @"(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?)$"; // Build Metadata

    public static bool IsSemanticVersion(this string versionString)
    {
        var match = Regex.Match(versionString, SemanticVersionPattern);

        var versionMatch = match.Groups["semanticVersion"];

        return versionMatch.Success;
    }
}