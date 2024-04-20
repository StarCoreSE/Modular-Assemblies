using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugUtils;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions
{
    internal class ApiDefinitions
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;

        internal ApiDefinitions()
        {
            ModApiMethods = new Dictionary<string, Delegate>
            {
                // Global assembly methods
                ["GetAllParts"] = new Func<IMyCubeBlock[]>(GetAllParts), // Returns a IMyCubeBlock array of all CubeBlocks with Assembly parts.
                ["GetAllAssemblies"] = new Func<int[]>(GetAllAssemblies), // Returns an int array of all Assembly IDs.

                // Per-assembly methods
                ["GetMemberParts"] = new Func<int, IMyCubeBlock[]>(GetMemberParts), // Returns a IMyCubeBlock array of all CubeBlocks within a given Assembly ID.
                ["GetBasePart"] = new Func<int, IMyCubeBlock>(GetBasePart),
                ["GetAssemblyGrid"] = new Func<int, IMyCubeGrid>(GetAssemblyGrid), // Returns the IMyCubeGrid an assembly ID is contained in.
                ["AddOnAssemblyClose"] = new Action<Action<int>>(AddOnAssemblyClose), // Registers an Action<AssemblyId> triggered on assembly removal.
                ["RemoveOnAssemblyClose"] = new Action<Action<int>>(RemoveOnAssemblyClose), // De-registers an Action<AssemblyId> triggered on assembly removal.
                ["RecreateAssembly"] = new Action<int>(RecreateAssembly),

                // Per-part methods
                ["GetConnectedBlocks"] = new Func<IMyCubeBlock, string, bool, IMyCubeBlock[]>(GetConnectedBlocks),
                ["GetContainingAssembly"] = new Func<IMyCubeBlock, string, int>(GetContainingAssembly),
                ["RecreateConnections"] = new Action<IMyCubeBlock, string>(RecreateConnections),

                // Definition methods
                ["RegisterDefinitions"] = new Func<byte[], string[]>(DefinitionHandler.I.RegisterDefinitions), // Tries to register a new definition.
                ["UnregisterDefinition"] = new Func<string, bool>(DefinitionHandler.I.UnregisterDefinition), // Tries to de-register a definition.
                ["GetAllDefinitions"] = new Func<string[]>(() => DefinitionHandler.I.ModularDefinitionsMap.Keys.ToArray()), // Returns a list of all definition names.
                ["RegisterOnPartAdd"] = new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.RegisterOnPartAdd),
                ["RegisterOnPartRemove"] = new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.RegisterOnPartRemove),
                ["RegisterOnPartDestroy"] = new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.RegisterOnPartDestroy),
                ["UnregisterOnPartAdd"] = new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.UnregisterOnPartAdd),
                ["UnregisterOnPartRemove"] = new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.UnregisterOnPartRemove),
                ["UnregisterOnPartDestroy"] = new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.UnregisterOnPartDestroy),

                // Global methods
                ["IsDebug"] = new Func<bool>(() => AssembliesSessionInit.DebugMode), // Returns whether debug mode is enabled or not.
                ["LogWriteLine"] = new Action<string>(ModularLog.Log), // Writes a new line in the Modular Assemblies debug log.
                ["AddChatCommand"] = new Action<string, string, Action<string[]>, string>(CommandHandler.AddCommand), // Registers a command for Modular Assemblies' command handler.
                ["RemoveChatCommand"] = new Action<string>(CommandHandler.RemoveCommand), // Removes a command from Modular Assemblies' command handler.
            };
        }

        #region Global Assembly Methods

        private IMyCubeBlock[] GetAllParts()
        {
            List<IMyCubeBlock> parts = new List<IMyCubeBlock>();
            foreach (var definitionBlockSet in AssemblyPartManager.I.AllAssemblyParts.Values)
                foreach (var block in definitionBlockSet.Keys)
                    if (block.FatBlock != null)
                        parts.Add(block.FatBlock);
            return parts.ToArray();
        }

        private int[] GetAllAssemblies()
        {
            return AssemblyPartManager.I.AllPhysicalAssemblies.Keys.ToArray();
        }

        #endregion

        #region Per-Assembly Methods

        private IMyCubeBlock[] GetMemberParts(int assemblyId)
        {
            PhysicalAssembly wep;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out wep))
                return Array.Empty<IMyCubeBlock>();

            List<IMyCubeBlock> parts = new List<IMyCubeBlock>();
            foreach (var part in wep.ComponentParts)
                if (part.Block.FatBlock != null)
                    parts.Add(part.Block.FatBlock);

            return parts.ToArray();
        }

        private IMyCubeBlock GetBasePart(int assemblyId)
        {
            PhysicalAssembly wep;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out wep))
                return null;

            return null;  //wep.basePart?.block?.FatBlock as IMyCubeBlock;
        }

        private IMyCubeGrid GetAssemblyGrid(int assemblyId)
        {
            PhysicalAssembly wep;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out wep))
                return null;

            return wep.ComponentParts[0].Block.CubeGrid;
        }

        private void AddOnAssemblyClose(Action<int> action)
        {
            AssemblyPartManager.I.OnAssemblyClose += action;
        }

        private void RemoveOnAssemblyClose(Action<int> action)
        {
            AssemblyPartManager.I.OnAssemblyClose -= action;
        }

        private void RecreateAssembly(int assemblyId)
        {
            PhysicalAssembly assembly = AssemblyPartManager.I.AllPhysicalAssemblies.GetValueOrDefault(assemblyId, null);
            if (assembly == null)
                return;

            foreach (var part in assembly.ComponentParts)
            {
                part.PartRemoved();
                AssemblyPartManager.I.QueueConnectionCheck(part);
            }
        }

        #endregion

        #region Per-Part Methods

        private IMyCubeBlock[] GetConnectedBlocks(IMyCubeBlock block, string definitionName, bool useCached)
        {
            ModularDefinition definition = DefinitionHandler.TryGetDefinition(definitionName);
            if (block == null || definition == null)
                return Array.Empty<IMyCubeBlock>();

            AssemblyPart wep;
            if (!AssemblyPartManager.I.AllAssemblyParts[definition].TryGetValue(block.SlimBlock, out wep) || wep.ConnectedParts == null)
                return Array.Empty<IMyCubeBlock>();

            List<IMyCubeBlock> parts = new List<IMyCubeBlock>();
            if (useCached)
            {
                foreach (var part in wep.ConnectedParts)
                    if (part.Block.FatBlock != null)
                        parts.Add(part.Block.FatBlock);
            }
            else
            {
                foreach (var part in wep.GetValidNeighbors(true))
                    if (part.FatBlock != null)
                        parts.Add(part.FatBlock);
            }

            return parts.ToArray();
        }

        private int GetContainingAssembly(IMyCubeBlock block, string definitionName)
        {
            ModularDefinition definition = DefinitionHandler.TryGetDefinition(definitionName);
            if (definition == null)
                return -1;

            foreach (var partKvp in AssemblyPartManager.I.AllAssemblyParts[definition])
            {
                if (partKvp.Key != block.SlimBlock)
                    continue;
                return partKvp.Value.MemberAssembly.AssemblyId;
            }
            return -1;
        }

        private void RecreateConnections(IMyCubeBlock block, string definitionName)
        {
            ModularDefinition definition = DefinitionHandler.TryGetDefinition(definitionName);
            if (definition == null)
                return;

            AssemblyPart part = AssemblyPartManager.I.AllAssemblyParts[definition].GetValueOrDefault(block.SlimBlock, null);
            if (part != null)
            {
                part.PartRemoved();
                AssemblyPartManager.I.QueueConnectionCheck(part);
            }
        }

        #endregion
    }
}
