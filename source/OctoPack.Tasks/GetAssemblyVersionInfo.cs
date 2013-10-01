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

        public GetAssemblyVersionInfo()
        {

        }

        public override bool Execute()
        {
            if (AssemblyFiles.Length <= 0)
            {
                return false;
            }
            List<ITaskItem> infos = new List<ITaskItem>();
            foreach (ITaskItem assemblyFile in AssemblyFiles)
            {
                LogMessage(String.Format("Assembly: {0}", assemblyFile.ItemSpec), MessageImportance.Normal);
                infos.Add(CreateTaskItemFromFileVersionInfo(FileVersionInfo.GetVersionInfo(assemblyFile.ItemSpec)));
            }
            AssemblyVersionInfo = infos.ToArray();
            return true;
        }

        private static TaskItem CreateTaskItemFromFileVersionInfo(FileVersionInfo info)
        {
            var properties =
                from property in info.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                select new
                {
                    Name = property.Name,
                    Value = property.GetValue(info, null)
                };

            Hashtable metadata = new Hashtable();
            foreach (var property in properties)
            {
                metadata.Add(property.Name, property.Value.ToString());
            }
            return new TaskItem(info.ProductName, metadata);
        }
    }
}
