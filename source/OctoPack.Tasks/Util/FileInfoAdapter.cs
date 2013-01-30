using System;
using System.IO;

namespace OctoPack.Tasks.Util
{
    public class FileInfoAdapter : IFileInfo
    {
        readonly FileInfo info;

        public FileInfoAdapter(FileInfo info)
        {
            this.info = info;
        }

        public string Extension { get { return info.Extension; } }
        public DateTime LastAccessTimeUtc { get { return info.LastAccessTimeUtc; } }
        public DateTime LastWriteTimeUtc { get { return info.LastWriteTimeUtc; } }
    }
}