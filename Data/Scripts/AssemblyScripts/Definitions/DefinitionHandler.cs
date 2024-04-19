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

        public List<ModularDefinition> ModularDefinitions = new List<ModularDefinition>();

        public void Init()
        {
            I = this;

            ModularLog.Log("DefinitionHandler loading...");

            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, DefMessageHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(InboundMessageId, ActionMessageHandler);
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, true);

            MyAPIGateway.Session.OnSessionReady += CheckValidDefinitions;
            ModularLog.Log("Init DefinitionHandler.cs");
        }

        public void Unload()
        {
            I = null;

            ModularLog.Log("DefinitionHandler closing...");

            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, DefMessageHandler);
            MyAPIGateway.Utilities.UnregisterMessageHandler(InboundMessageId, ActionMessageHandler);
        }

        public void DefMessageHandler(object o)
        {
            try
            {
                byte[] message = o as byte[];

                if (message == null)
                    return;

                DefinitionContainer baseDefArray = null;
                try
                {
                    baseDefArray = MyAPIGateway.Utilities.SerializeFromBinary<DefinitionContainer>(message);
                }
                catch
                {
                    // ignored
                }

                if (baseDefArray != null)
                {
                    ModularLog.Log($"Received {baseDefArray.PhysicalDefs.Length} definitions.");
                    foreach (var def in baseDefArray.PhysicalDefs)
                    {
                        var modDef = ModularDefinition.Load(def);
                        if (modDef == null)
                            continue;

                        bool isDefinitionValid = true;
                        // Check for duplicates
                        foreach (var definition in ModularDefinitions)
                        {
                            if (definition.Name != modDef.Name)
                                continue;

                            ModularLog.Log($"Duplicate DefinitionName in definition {modDef.Name}! Skipping load...");
                            MyAPIGateway.Utilities.ShowMessage("ModularAssemblies", $"Duplicate DefinitionName in definition {modDef.Name}! Skipping load...");
                            isDefinitionValid = false;
                        }
                        if (isDefinitionValid)
                            ModularDefinitions.Add(modDef);
                    }
                }
                else
                {
                    ModularLog.Log($"Invalid definition container!");
                }
            }
            catch (Exception ex) { ModularLog.Log($"Exception in DefinitionMessageHandler: {ex}"); }
        }

        public void ActionMessageHandler(object o)
        {
            try
            {
                var message = o as byte[];
                if (message == null) return;

                FunctionCall functionCall = null;
                try
                {
                    functionCall = MyAPIGateway.Utilities.SerializeFromBinary<FunctionCall>(message);
                }
                catch { }

                if (functionCall != null)
                {
                    //ModularLog.Log($"ModularAssemblies: Recieved action of type {functionCall.ActionId}.");

                    PhysicalAssembly wep = AssemblyPartManager.I.AllPhysicalAssemblies[functionCall.PhysicalAssemblyId];
                    if (wep == null)
                    {
                        ModularLog.Log($"Invalid PhysicalAssembly!");
                        return;
                    }

                    // TODO: Remove
                    //object[] Values = functionCall.Values.Values();

                    switch (functionCall.ActionId)
                    {
                        default:
                            // Fill in here if necessary.
                            break;
                    }
                }
                else
                {
                    ModularLog.Log($"functionCall null!");
                }
            }
            catch (Exception ex) { ModularLog.Log($"Exception in ActionMessageHandler: {ex}"); }
        }

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
