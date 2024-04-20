using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugUtils;
using VRage;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions
{
    internal class ApiHandler
    {
        private const long Channel = 8774;
        private readonly IReadOnlyDictionary<string, Delegate> _apiDefinitions;
        private readonly MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>> _endpointTuple;

        /// <summary>
        /// Is the API ready?
        /// </summary>
        public bool IsReady { get; private set; }

        private void HandleMessage(object o)
        {
            if ((o as string) == "ApiEndpointRequest")
            {
                MyAPIGateway.Utilities.SendModMessage(Channel, _endpointTuple);
                ModularLog.Log("ModularDefinitionsAPI sent definitions.");
            }
            else
                ModularLog.Log($"ModularDefinitionsAPI ignored message {o as string}.");
        }

        /// <summary>
        /// Registers for API requests and updates any pre-existing clients.
        /// </summary>
        public ApiHandler()
        {
            _apiDefinitions = new ApiDefinitions().ModApiMethods;
            _endpointTuple = new MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>>(AssembliesSessionInit.ModVersion, _apiDefinitions);

            MyAPIGateway.Utilities.RegisterMessageHandler(Channel, HandleMessage);

            IsReady = true;
            try
            {
                MyAPIGateway.Utilities.SendModMessage(Channel, _endpointTuple);
            }
            catch (Exception ex)
            {
                ModularLog.Log($"Exception in Api Load: {ex}"); 
            }
            ModularLog.Log($"ModularDefinitionsAPI v{AssembliesSessionInit.ModVersion.Y} initialized.");
        }


        /// <summary>
        /// Unloads all API endpoints and detaches events.
        /// </summary>
        public void Unload()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, HandleMessage);

            IsReady = false;
            // Clear API client's endpoints
            MyAPIGateway.Utilities.SendModMessage(Channel, new MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>>(AssembliesSessionInit.ModVersion, null));

            ModularLog.Log("ModularDefinitionsAPI unloaded.");
        }
    }
}
