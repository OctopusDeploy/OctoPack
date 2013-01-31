using Microsoft.Build.Framework;

namespace OctoPack.Tasks
{
    public abstract class AbstractTask : ITask
    {
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }
        
        public abstract bool Execute();

        protected void LogMessage(string message, MessageImportance importance = MessageImportance.High)
        {
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, "OctoPack", "OctoPack", importance));
        }

        protected void LogWarning(string code, string message)
        {
            BuildEngine.LogWarningEvent(new BuildWarningEventArgs("OctoPack", code, null, 0, 0, 0, 0, message, "OctoPack", "OctoPack"));
        }

        protected void LogError(string code, string message)
        {
            BuildEngine.LogErrorEvent(new BuildErrorEventArgs("OctoPack", code, null, 0, 0, 0, 0, message, "OctoPack", "OctoPack"));
        }
    }
}