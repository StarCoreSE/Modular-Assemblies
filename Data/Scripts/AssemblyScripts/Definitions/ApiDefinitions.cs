using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Debug;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions
{
    internal class ApiDefinitions
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;

        internal ApiDefinitions()
        {
            ModApiMethods = new Dictionary<string, Delegate>()
            {
                // Global assembly methods
                ["GetAllParts"] = new Func<MyEntity[]>(GetAllParts), // Returns a MyEntity array of all CubeBlocks with Assembly parts.
                ["GetAllAssemblies"] = new Func<int[]>(GetAllAssemblies), // Returns an int array of all Assembly IDs.

                // Per-assembly methods
                ["GetMemberParts"] = new Func<int, MyEntity[]>(GetMemberParts), // Returns a MyEntity array of all CubeBlocks within a given Assembly ID.
                ["GetBasePart"] = new Func<int, MyEntity>(GetBasePart),
                ["GetAssemblyGrid"] = new Func<int, IMyCubeGrid>(GetAssemblyGrid), // Returns the IMyCubeGrid an assembly ID is contained in.
                ["AddOnAssemblyClose"] = new Action<Action<int>>(AddOnAssemblyClose), // Registers an Action<AssemblyId> triggered on assembly removal.
                ["RemoveOnAssemblyClose"] = new Action<Action<int>>(RemoveOnAssemblyClose), // De-registers an Action<AssemblyId> triggered on assembly removal.
                // TODO: RecreateAssembly - Destroys assembly and makes all contained blocks queue for search.
                // TODO: Replace stupid dumb definition actions with nice fancy API methods.

                // Per-part methods
                ["GetConnectedBlocks"] = new Func<MyEntity, bool, MyEntity[]>(GetConnectedBlocks),
                ["GetContainingAssembly"] = new Func<MyEntity, int>(GetContainingAssembly),
                // TODO: RecreateConnections - Destroys connections and queues for search.

                // Global methods
                ["IsDebug"] = new Func<bool>(() => AssembliesSessionInit.DebugMode), // Returns whether debug mode is enabled or not.
                ["LogWriteLine"] = new Action<string>(ModularLog.Log), // Writes a new line in the Modular Assemblies debug log.
                ["AddChatCommand"] = new Action<string, string, Action<string[]>, string>(CommandHandler.AddCommand), // Registers a command for Modular Assemblies' command handler.
                ["RemoveChatCommand"] = new Action<string>(CommandHandler.RemoveCommand), // Removes a command from Modular Assemblies' command handler.
            };
        }

        private MyEntity[] GetAllParts()
        {
            List<MyEntity> parts = new List<MyEntity>();
            foreach (var block in AssemblyPartManager.I.AllAssemblyParts.Keys)
                if (block.FatBlock != null)
                    parts.Add((MyEntity)block.FatBlock);
            return parts.ToArray();
        }

        private int[] GetAllAssemblies()
        {
            return AssemblyPartManager.I.AllPhysicalAssemblies.Keys.ToArray();
        }

        private MyEntity[] GetMemberParts(int assemblyId)
        {
            PhysicalAssembly wep;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out wep))
                return Array.Empty<MyEntity>();

            List<MyEntity> parts = new List<MyEntity>();
            foreach (var part in wep.ComponentParts)
                if (part.Block.FatBlock != null)
                    parts.Add((MyEntity)part.Block.FatBlock);

            return parts.ToArray();
        }

        private MyEntity[] GetConnectedBlocks(MyEntity blockEntity, bool useCached)
        {
            var block = blockEntity as IMyCubeBlock;
            if (block == null)
                return Array.Empty<MyEntity>();

            AssemblyPart wep;
            if (!AssemblyPartManager.I.AllAssemblyParts.TryGetValue(block.SlimBlock, out wep) || wep.ConnectedParts == null)
                return Array.Empty<MyEntity>();

            List<MyEntity> parts = new List<MyEntity>();
            if (useCached)
            {
                foreach (var part in wep.ConnectedParts)
                    if (part.Block.FatBlock != null)
                        parts.Add((MyEntity)part.Block.FatBlock);
            }
            else
            {
                foreach (var part in wep.GetValidNeighbors(true))
                    if (part.FatBlock != null)
                        parts.Add((MyEntity)part.FatBlock);
            }

            return parts.ToArray();
        }

        private MyEntity GetBasePart(int assemblyId)
        {
            PhysicalAssembly wep;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out wep))
                return null;

            return null;  //wep.basePart?.block?.FatBlock as MyEntity;
        }

        private int GetContainingAssembly(MyEntity blockEntity)
        {
            IMySlimBlock block = blockEntity as IMySlimBlock;
            foreach (var partKvp in AssemblyPartManager.I.AllAssemblyParts)
            {
                if (partKvp.Value != block)
                    continue;
                return partKvp.Value.MemberAssembly.AssemblyId;
            }
            return -1;
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
    }
}
