using System;

namespace OctoPack.Tasks.Util
{
    public interface IFileInfo
    {
        string Extension { get; }
        DateTime LastAccessTimeUtc { get; }
        DateTime LastWriteTimeUtc { get; }
    }
}