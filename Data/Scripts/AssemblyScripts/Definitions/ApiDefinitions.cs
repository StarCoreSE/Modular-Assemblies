using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
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
                ["GetAllParts"] = new Func<MyEntity[]>(GetAllParts),
                ["GetAllAssemblies"] = new Func<int[]>(GetAllAssemblies),
                ["GetMemberParts"] = new Func<int, MyEntity[]>(GetMemberParts),
                ["GetConnectedBlocks"] = new Func<MyEntity, bool, MyEntity[]>(GetConnectedBlocks),
                ["GetBasePart"] = new Func<int, MyEntity>(GetBasePart),
                ["IsDebug"] = new Func<bool>(IsDebug),
                ["GetContainingAssembly"] = new Func<MyEntity, int>(GetContainingAssembly),
                ["GetAssemblyGrid"] = new Func<int, IMyCubeGrid>(GetAssemblyGrid),
                ["AddOnAssemblyClose"] = new Action<Action<int>>(AddOnAssemblyClose),
                ["RemoveOnAssemblyClose"] = new Action<Action<int>>(RemoveOnAssemblyClose),
            };
        }

        private bool IsDebug()
        {
            return AssembliesSessionInit.DebugMode;
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
