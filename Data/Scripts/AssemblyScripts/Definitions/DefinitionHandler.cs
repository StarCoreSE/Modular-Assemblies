using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Debug;
using VRage;
using VRage.Game.Components;
using VRage.Profiler;
using VRage.Utils;
using static Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions.DefinitionDefs;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions
{
    /// <summary>
    /// Handles all communication about definitions.
    /// </summary>
    internal class DefinitionHandler
    {
        public static DefinitionHandler I;
        const int DefinitionMessageId = 8772;
        const int InboundMessageId = 8773;
        const int OutboundMessageId = 8771;

        public Dictionary<string, ModularDefinition> ModularDefinitionsMap = new Dictionary<string, ModularDefinition>();
        public ICollection<ModularDefinition> ModularDefinitions => ModularDefinitionsMap.Values;

        public void Init()
        {
            I = this;

            ModularLog.Log("DefinitionHandler loading...");
            MyAPIGateway.Session.OnSessionReady += CheckValidDefinitions;
        }

        public void Unload()
        {
            I = null;
            ModularLog.Log("DefinitionHandler closing...");
            MyAPIGateway.Session.OnSessionReady -= CheckValidDefinitions;
        }

        public string[] RegisterDefinitions(byte[] serialized)
        {
            if (serialized == null)
                return Array.Empty<string>();

            try
            {
                DefinitionContainer definitionSet = MyAPIGateway.Utilities.SerializeFromBinary<DefinitionContainer>(serialized);
                return RegisterDefinitions(definitionSet);
            }
            catch (Exception ex)
            {
                ModularLog.Log($"Exception in DefinitionHandler.RegisterDefinitions: {ex}");
            }

            return Array.Empty<string>();
        }

        public string[] RegisterDefinitions(DefinitionContainer definitionSet)
        {
            try
            {
                if (definitionSet == null)
                {
                    ModularLog.Log($"Invalid definition container!");
                    return Array.Empty<string>();
                }

                ModularLog.Log($"Received {definitionSet.PhysicalDefs.Length} definitions.");
                List<string> newValidDefinitions = new List<string>();

                foreach (var def in definitionSet.PhysicalDefs)
                {
                    var modDef = ModularDefinition.Load(def);
                    if (modDef == null)
                        continue;

                    bool isDefinitionValid = true;
                    // Check for duplicates
                    if (ModularDefinitionsMap.ContainsKey(modDef.Name))
                    {
                        ModularLog.Log($"Duplicate DefinitionName for definition {modDef.Name}! Skipping load...");
                        MyAPIGateway.Utilities.ShowMessage("ModularAssemblies", $"Duplicate DefinitionName in definition {modDef.Name}! Skipping load...");
                        isDefinitionValid = false;
                    }

                    if (!isDefinitionValid)
                        continue;

                    ModularDefinitionsMap.Add(modDef.Name, modDef);
                    AssemblyPartManager.I.AllAssemblyParts.Add(modDef, new Dictionary<IMySlimBlock, AssemblyPart>());
                    newValidDefinitions.Add(modDef.Name);
                }

                return newValidDefinitions.ToArray();
            }
            catch (Exception ex) { ModularLog.Log($"Exception in DefinitionHandler.RegisterDefinitions: {ex}"); }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Removes a definition and destroys all assemblies referencing it.
        /// </summary>
        /// <param name="definitionName"></param>
        /// <returns></returns>
        public bool UnregisterDefinition(string definitionName)
        {
            if (!ModularDefinitionsMap.ContainsKey(definitionName))
                return false;

            foreach (var assembly in AssemblyPartManager.I.AllPhysicalAssemblies.Values)
            {
                if (assembly.AssemblyDefinition.Name != definitionName)
                    continue;

                assembly.Close();
            }

            AssemblyPartManager.I.AllAssemblyParts.Remove(ModularDefinitionsMap[definitionName]);
            ModularDefinitionsMap.Remove(definitionName);
            return true;
        }

        public static ModularDefinition TryGetDefinition(string definitionName) => I.ModularDefinitionsMap.GetValueOrDefault(definitionName, null);


        public void SendOnPartAdd(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                BlockEntityId,
                IsBaseBlock
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnPartAdd,
                DefinitionName = DefinitionName,
                PhysicalAssemblyId = PhysicalAssemblyId,
                Values = Values,
            });
        }

        public void SendOnPartRemove(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                BlockEntityId,
                IsBaseBlock
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnPartRemove,
                DefinitionName = DefinitionName,
                PhysicalAssemblyId = PhysicalAssemblyId,
                Values = Values,
            });
        }

        public void SendOnPartDestroy(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                BlockEntityId,
                IsBaseBlock
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnPartDestroy,
                DefinitionName = DefinitionName,
                PhysicalAssemblyId = PhysicalAssemblyId,
                Values = Values,
            });
        }

        private void SendFunc(FunctionCall call)
        {
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, MyAPIGateway.Utilities.SerializeToBinary(call));
            //ModularLog.Log($"ModularAssemblies: Sending function call [id {call.ActionId}] to [{call.DefinitionName}].");
        }

        private void CheckValidDefinitions()
        {
            // Get all block definition subtypes
            var defs = MyDefinitionManager.Static.GetAllDefinitions();
            List<string> validSubtypes = new List<string>();
            foreach (var def in defs)
            {
                var blockDef = def as MyCubeBlockDefinition;

                if (blockDef != null)
                {
                    validSubtypes.Add(def.Id.SubtypeName);
                }
            }
            foreach (var def in ModularDefinitions.ToList())
                CheckDefinitionValid(def, validSubtypes);
        }

        private void CheckDefinitionValid(ModularDefinition modDef, List<string> validSubtypes)
        {
            foreach (var subtypeId in modDef.AllowedBlocks)
            {
                if (!validSubtypes.Contains(subtypeId))
                {
                    ModularLog.Log($"Invalid SubtypeId [{subtypeId}] in definition {modDef.Name}! Unexpected behavior may occur.");
                    MyAPIGateway.Utilities.ShowMessage("ModularAssemblies", $"Invalid SubtypeId [{subtypeId}] in definition {modDef.Name}! Unexpected behavior may occur.");
                }
            }
        }
    }
}
