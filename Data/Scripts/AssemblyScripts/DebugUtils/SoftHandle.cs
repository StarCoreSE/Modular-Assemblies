using System;
using Sandbox.ModAPI;

namespace Modular_Assemblies.AssemblyScripts.DebugUtils
{
    public static class SoftHandle
    {
        public static void RaiseException(string message, Exception ex = null, Type callingType = null,
            ulong callerId = ulong.MaxValue)
        {
            if (message == null)
                return;

            if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification("Minor Exception: " + message);
            var soft = new Exception(message, ex);
            ModularLog.LogException(soft, callingType ?? typeof(SoftHandle),
                callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "");
        }

        public static void RaiseException(Exception exception, Type callingType = null, ulong callerId = ulong.MaxValue)
        {
            if (exception == null)
                return;
            if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification("Minor Exception: " + exception.Message);
            ModularLog.LogException(exception, callingType ?? typeof(SoftHandle),
                callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "");
        }

        public static string FirstLine(this string str)
        {
            int idx = str.IndexOf(Environment.NewLine, StringComparison.Ordinal);
            if (idx == -1)
                return str;
            return str.Substring(0, idx).Trim();
        }
    }
}