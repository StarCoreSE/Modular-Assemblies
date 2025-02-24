﻿using System;
using System.IO;
using Sandbox.ModAPI;

namespace Modular_Assemblies.AssemblyScripts.DebugUtils
{
    /// <summary>
    ///     Standard logging class. Outputs to %AppData%\Roaming\Space Engineers\Storage\
    /// </summary>
    public class ModularLog
    {
        private static ModularLog I;
        private readonly TextWriter _writer;

        private ModularLog()
        {
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage("ModularAssemblies.log");
            _writer = MyAPIGateway.Utilities
                .WriteFileInGlobalStorage(
                    "ModularAssemblies.log"); // Only creating one debug.log to avoid clutter. Might change in the future.
            _writer.WriteLine(
                $"     Modular Assemblies v{AssembliesSessionInit.ModVersion.X} - Debug Log\n===========================================\n");
            _writer.Flush();
        }

        public static void Log(string message)
        {
            I?._Log(message);
        }

        public static void LogException(Exception ex, Type callingType, string prefix = "")
        {
            I?._LogException(ex, callingType, prefix);
        }

        public static void Init()
        {
            Close();
            I = new ModularLog();
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
