using System;
using System.Collections.Generic;
using System.Linq;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugUtils;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using static Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions.DefinitionDefs;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions
{
    /// <summary>
    ///     Handles all communication about definitions.
    /// </summary>
    internal class DefinitionHandler
    {
        private const int OutboundMessageId = 8771;
        public static DefinitionHandler I;

        internal ApiHandler ApiHandler;

        public Dictionary<string, ModularDefinition>
            ModularDefinitionsMap = new Dictionary<string, ModularDefinition>();

        /// <summary>
        ///     An array of all valid block subtypes, generated at session init.
        /// </summary>
        internal string[] ValidBlockSubtypes = Array.Empty<string>();

        public ICollection<ModularDefinition> ModularDefinitions => ModularDefinitionsMap.Values;

        public void Init()
        {
            I = this;

            ApiHandler = new ApiHandler();
            ModularLog.Log("DefinitionHandler loading...");
            MyAPIGateway.Session.OnSessionReady += CheckValidDefinitions;
        }

        public void Unload()
        {
            I = null;
            ModularLog.Log("DefinitionHandler closing...");
            MyAPIGateway.Session.OnSessionReady -= CheckValidDefinitions;
            ApiHandler.Unload();
        }

        /// <summary>
        ///     Registers a serialized definition.
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public string[] RegisterDefinitions(byte[] serialized)
        {
            if (serialized == null)
                return Array.Empty<string>();

            try
            {
                var definitionSet = MyAPIGateway.Utilities.SerializeFromBinary<ModularDefinitionContainer>(serialized);
                return RegisterDefinitions(definitionSet);
            }
            catch (Exception ex)
            {
                ModularLog.Log($"Exception in DefinitionHandler.RegisterDefinitions: {ex}");
            }

            return Array.Empty<string>();
        }

        /// <summary>
        ///     Registers a deserialized definition.
        /// </summary>
        /// <param name="modularDefinitionSet"></param>
        /// <returns></returns>
        public string[] RegisterDefinitions(ModularDefinitionContainer modularDefinitionSet)
        {
            try
            {
                if (modularDefinitionSet == null)
                {
                    ModularLog.Log("Invalid definition container!");
                    return Array.Empty<string>();
                }

                ModularLog.Log($"Received {modularDefinitionSet.PhysicalDefs.Length} definitions.");
                var newValidDefinitions = new List<string>();

                foreach (var def in modularDefinitionSet.PhysicalDefs)
                {
                    var modDef = ModularDefinition.Load(def);
                    if (modDef == null)
                        continue;

                    var isDefinitionValid = true;
                    // Check for duplicates
                    if (ModularDefinitionsMap.ContainsKey(modDef.Name))
                    {
                        ModularLog.Log($"Duplicate DefinitionName for definition {modDef.Name}! Skipping load...");
                        MyAPIGateway.Utilities.ShowMessage("ModularAssemblies",
                            $"Duplicate DefinitionName in definition {modDef.Name}! Skipping load...");
                        isDefinitionValid = false;
                    }

                    if (!isDefinitionValid)
                        continue;

                    ModularDefinitionsMap.Add(modDef.Name, modDef);
                    AssemblyPartManager.I.AllAssemblyParts.Add(modDef, new Dictionary<IMySlimBlock, AssemblyPart>());
                    newValidDefinitions.Add(modDef.Name);

                    if (AssembliesSessionInit.IsSessionInited)
                    {
                        CheckDefinitionValid(modDef);
                        AssemblyPartManager.I
                            .RegisterExistingBlocks(
                                modDef); // We only want to do this if blocks already exist in the world.
                    }
                }

                return newValidDefinitions.ToArray();
            }
            catch (Exception ex)
            {
                ModularLog.Log($"Exception in DefinitionHandler.RegisterDefinitions: {ex}");
            }

            return Array.Empty<string>();
        }

        /// <summary>
        ///     Removes a definition and destroys all assemblies referencing it.
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

        public static ModularDefinition TryGetDefinition(string definitionName)
        {
            return I.ModularDefinitionsMap.GetValueOrDefault(definitionName, null);
        }

        public void RegisterOnPartAdd(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            var definition = ModularDefinitionsMap.GetValueOrDefault(definitionName, null);

            if (definition == null)
                return;

            definition.OnPartAdd += action;
        }

        public void RegisterOnPartRemove(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            var definition = ModularDefinitionsMap.GetValueOrDefault(definitionName, null);

            if (definition == null)
                return;

            definition.OnPartRemove += action;
        }

        public void RegisterOnPartDestroy(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            var definition = ModularDefinitionsMap.GetValueOrDefault(definitionName, null);

            if (definition == null)
                return;

            definition.OnPartDestroy += action;
        }

        public void UnregisterOnPartAdd(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            var definition = ModularDefinitionsMap.GetValueOrDefault(definitionName, null);

            if (definition == null || action == null)
                return;

            definition.OnPartAdd -= action;
        }

        public void UnregisterOnPartRemove(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            var definition = ModularDefinitionsMap.GetValueOrDefault(definitionName, null);

            if (definition == null || action == null)
                return;

            definition.OnPartRemove -= action;
        }

        public void UnregisterOnPartDestroy(string definitionName, Action<int, IMyCubeBlock, bool> action)
        {
            var definition = ModularDefinitionsMap.GetValueOrDefault(definitionName, null);

            if (definition == null || action == null)
                return;

            definition.OnPartDestroy -= action;
        }

        private void CheckValidDefinitions()
        {
            // Get all block definition subtypes
            var defs = MyDefinitionManager.Static.GetAllDefinitions();
            var validSubtypes = new List<string>();
            foreach (var def in defs)
            {
                var blockDef = def as MyCubeBlockDefinition;

                if (blockDef != null) validSubtypes.Add(def.Id.SubtypeName);
            }

            ValidBlockSubtypes = validSubtypes.ToArray();

            foreach (var def in ModularDefinitions.ToList())
                CheckDefinitionValid(def);
        }

        private void CheckDefinitionValid(ModularDefinition modDef)
        {
            foreach (var subtypeId in modDef.AllowedBlocks)
            {
                if (ValidBlockSubtypes.Contains(subtypeId))
                    continue;
                ModularLog.Log(
                    $"Invalid SubtypeId \"{subtypeId}\" in definition {modDef.Name}! Unexpected behavior may occur.");
                MyAPIGateway.Utilities.ShowMessage("ModularAssemblies",
                    $"Invalid SubtypeId [{subtypeId}] in definition {modDef.Name}! Unexpected behavior may occur.");
            }
        }
    }
}