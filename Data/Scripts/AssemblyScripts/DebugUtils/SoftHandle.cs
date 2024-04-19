using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Debug
{
    public class SoftHandle
    {
        public static void RaiseException(string message, Exception ex = null, Type callingType = null, ulong callerId = ulong.MaxValue)
        {
            if (message == null)
                return;

            if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification("Minor Exception: " + message);
            Exception soft = new Exception(message, ex);
            ModularLog.LogException(soft, callingType ?? typeof(SoftHandle), callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "");
        }

        public static void RaiseException(Exception exception, Type callingType = null, ulong callerId = ulong.MaxValue)
        {
            if (exception == null)
                return;
            if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification("Minor Exception: " + exception.Message);
            ModularLog.LogException(exception, callingType ?? typeof(SoftHandle), callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "");
        }
    }
}
