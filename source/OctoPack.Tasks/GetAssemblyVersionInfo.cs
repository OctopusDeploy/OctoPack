using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OctoPack.Tasks
{
    public class GetAssemblyVersionInfo : AbstractTask
    {

        /// <summary>
        /// Specifies the files the retrieve info from.
        /// </summary>
        [Required]
        public ITaskItem[] AssemblyFiles { get; set; }

        /// <summary>
        /// Contains the retrieved version info
        /// </summary>
        [Output]
        public ITaskItem[] AssemblyVersionInfo { get; set; }

        public override bool Execute()
        {
            if (AssemblyFiles.Length <= 0)
            {
                return false;
            }
            
            var infos = new List<ITaskItem>();
            foreach (var assemblyFile in AssemblyFiles)
            {
                LogMessage(String.Format("Get version info from assembly: {0}", assemblyFile), MessageImportance.Normal);

                infos.Add(CreateTaskItemFromFileVersionInfo(assemblyFile.ItemSpec));
            }
            AssemblyVersionInfo = infos.ToArray();
            return true;
        }

        private static TaskItem CreateTaskItemFromFileVersionInfo(string path)
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            var currentAssemblyName = AssemblyName.GetAssemblyName(info.FileName);

            var assemblyVersion = currentAssemblyName.Version;

            Version assemblyFileVersion = null;
            if (!string.IsNullOrWhiteSpace(info.FileVersion))
            {
                Version.TryParse(info.FileVersion, out assemblyFileVersion);
            }

            // select version number in this order
            // 1) Informational Product Version (as it can be 1.x.x-demo)
            // 2) File Version
            // 3) Assembly Version

            string selectedVersion;
            if (!string.IsNullOrWhiteSpace(info.ProductVersion))
            {
                selectedVersion = info.ProductVersion;
            }
            else if (assemblyFileVersion != null
                && assemblyFileVersion > assemblyVersion)
            {
                selectedVersion = assemblyFileVersion.ToString();
            }
            else
            {
                selectedVersion = assemblyVersion.ToString();
            }

            return new TaskItem(info.FileName, new Hashtable
            {
                {"Version", selectedVersion},
            });
        }
    }
}
