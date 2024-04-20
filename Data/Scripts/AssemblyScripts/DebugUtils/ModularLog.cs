using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugUtils
{
    /// <summary>
    /// Standard logging class. Outputs to %AppData%\Roaming\Space Engineers\Storage\
    /// </summary>
    public class ModularLog
    {
        private readonly TextWriter _writer;
        private static ModularLog I;

        public static void Log(string message)
        {
            I._Log(message);
        }

        public static void LogException(Exception ex, Type callingType, string prefix = "")
        {
            I._LogException(ex, callingType, prefix);
        }

        public static void Init()
        {
            Close();
            I = new ModularLog();
        }

        private ModularLog()
        {
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage("ModularAssemblies.log");
            _writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage("ModularAssemblies.log"); // Only creating one debug.log to avoid clutter. Might change in the future.
            _writer.WriteLine($"Modular Assemblies v{AssembliesSessionInit.ModVersion.X} - Debug Log\n===========================================\n");
            _writer.Flush();
        }

        public static void Close()
        {
            if (I != null)
            {
                Log("Closing log writer.");
                I._writer.Close();
            }
            I = null;
        }

        private void _Log(string message)
        {
            _writer.WriteLine($"{DateTime.UtcNow:HH:mm:ss}: {message}");
            _writer.Flush();
        }

        private void _LogException(Exception ex, Type callingType, string prefix = "")
        {
            if (ex == null)
            {
                _Log("Null exception! CallingType: " + callingType.FullName);
                return;
            }

            _Log(prefix + $"Exception in {callingType.FullName}! {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
        }
    }
}
