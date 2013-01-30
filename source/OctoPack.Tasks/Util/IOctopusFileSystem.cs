using System;
using System.Collections.Generic;
using System.IO;

namespace OctoPack.Tasks.Util
{
    public interface IOctopusFileSystem
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
        void DeleteFile(string path);
        void DeleteFile(string path, DeletionOptions options);
        void DeleteDirectory(string path);
        IEnumerable<string> EnumerateDirectories(string parentDirectoryPath);
        IEnumerable<string> EnumerateDirectoriesRecursively(string parentDirectoryPath);
        IEnumerable<string> EnumerateFiles(string parentDirectoryPath, params string[] searchPatterns);
        IEnumerable<string> EnumerateFilesRecursively(string parentDirectoryPath, params string[] searchPatterns);
        long GetFileSize(string path);
        string ReadFile(string path);
        void AppendToFile(string path, string contents);
        void OverwriteFile(string path, string contents);
        Stream OpenFile(string path, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read);
        Stream OpenFile(string path, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read);
        Stream CreateTemporaryFile(string extension, out string path);
        void CopyDirectory(string sourceDirectory, string targetDirectory, int overwriteFileRetryAttempts = 3);
        void CopyFile(string sourceFile, string targetFile, int overwriteFileRetryAttempts = 3);
        void PurgeDirectory(string targetDirectory, DeletionOptions options);
        void PurgeDirectory(string targetDirectory, Predicate<IFileInfo> filter, DeletionOptions options);
        void EnsureDirectoryExists(string directoryPath);
        void EnsureDiskHasEnoughFreeSpace(string directoryPath);
        void EnsureDiskHasEnoughFreeSpace(string directoryPath, long requiredSpaceInBytes);
        string GetFullPath(string relativeOrAbsoluteFilePath);
        void OverwriteAndDelete(string originalFile, string temporaryReplacement);
        string GetPathRelativeTo(string fullPath, string relativeTo);
    }
}
