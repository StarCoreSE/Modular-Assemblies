using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Debug;
using VRage;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, Priority = 0)]
    internal class ApiHandler : MySessionComponentBase
    {
        private const long Channel = 8774;
        public static readonly Vector2I ModVersion = new Vector2I(0, 1); // Mod version, API version
        private readonly IReadOnlyDictionary<string, Delegate> _apiDefinitions = new ApiDefinitions().ModApiMethods;
        private MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>> _endpointTuple;

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
        public override void LoadData()
        {
            _endpointTuple = new MyTuple<Vector2I, IReadOnlyDictionary<string, Delegate>>(ModVersion, _apiDefinitions);

            MyAPIGateway.Utilities.RegisterMessageHandler(Channel, HandleMessage);

            IsReady = true;
            try
            {
                MyAPIGateway.Utilities.SendModMessage(Channel, _apiDefinitions);
            }
            catch (Exception ex)
            {
                ModularLog.Log($"Exception in Api Load: {ex}"); 
            }
            ModularLog.Log($"ModularDefinitionsAPI v{ModVersion.Y} initialized.");
        }


        /// <summary>
        /// Unloads all API endpoints and detaches events.
        /// </summary>
        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, HandleMessage);

            IsReady = false;
            MyAPIGateway.Utilities.SendModMessage(Channel, new Dictionary<string, Delegate>());

            ModularLog.Log("ModularDefinitionsAPI unloaded.");
        }
    }
}
