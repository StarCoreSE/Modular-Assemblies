﻿using Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    /// <summary>
    /// Creates and manages all AssemblyParts and PhysicalAssemblies.
    /// </summary>
    public class AssemblyPartManager
    {
        public static AssemblyPartManager I;

        /// <summary>
        /// Every single AssemblyPart in the world.
        /// </summary>
        public Dictionary<IMySlimBlock, AssemblyPart> AllAssemblyParts = new Dictionary<IMySlimBlock, AssemblyPart>();

        /// <summary>
        /// Every single PhysicalAssembly in the world.
        /// </summary>
        public Dictionary<int, PhysicalAssembly> AllPhysicalAssemblies = new Dictionary<int, PhysicalAssembly>();
        public int CreatedPhysicalAssemblies = 0;

        private HashSet<IMySlimBlock> QueuedBlockAdds = new HashSet<IMySlimBlock>();
        private HashSet<AssemblyPart> QueuedConnectionChecks = new HashSet<AssemblyPart>();
        private Dictionary<AssemblyPart, PhysicalAssembly> QueuedAssemblyChecks = new Dictionary<AssemblyPart, PhysicalAssembly>();

        public Action<int> OnAssemblyClose;

        public void QueueBlockAdd(IMySlimBlock block) => QueuedBlockAdds.Add(block);
        public void QueueConnectionCheck(AssemblyPart part)
        {
            QueuedConnectionChecks.Add(part);
        }
        public void QueueAssemblyCheck(AssemblyPart part, PhysicalAssembly assembly)
        {
            if (!QueuedAssemblyChecks.ContainsKey(part))
                QueuedAssemblyChecks.Add(part, assembly);
        }

        public void Init()
        {
            MyLog.Default.WriteLineAndConsole("Modular Assemblies: AssemblyPartManager loading...");

            I = this;

            // None of this should run on client.
            //if (!MyAPIGateway.Multiplayer.IsServer)
            //    return;

            MyAPIGateway.Entities.OnEntityAdd += OnGridAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnGridRemove;
        }

        public void Unload()
        {
            I = null; // important for avoiding this object to remain allocated in memory
            AllAssemblyParts.Clear();
            AllPhysicalAssemblies.Clear();
            OnAssemblyClose = null;

            // None of this should run on client.
            //if (!MyAPIGateway.Multiplayer.IsServer)
            //    return;

            MyLog.Default.WriteLineAndConsole("Modular Assemblies: AssemblyPartManager closing...");

            MyAPIGateway.Entities.OnEntityAdd -= OnGridAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnGridRemove;
        }

        public void UpdateAfterSimulation()
        {
            // Queue gridadds to account for world load/grid pasting
            foreach (var queuedBlock in QueuedBlockAdds.ToList())
            {
                OnBlockAdd(queuedBlock);
                QueuedBlockAdds.Remove(queuedBlock);
            }

            // Queue partadds to account for world load/grid pasting
            foreach (var queuedPart in QueuedConnectionChecks.ToList())
            {
                queuedPart.DoConnectionCheck();
                QueuedConnectionChecks.Remove(queuedPart);
            }

            // Queue assembly pathing to account for world load/grid pasting
            foreach (var queuedAssembly in QueuedAssemblyChecks.Keys.ToList())
            {
                //QueuedAssemblyChecks[queuedAssembly].RecursiveAssemblyChecker(queuedAssembly);
                QueuedAssemblyChecks.Remove(queuedAssembly);
            }

            foreach (var assembly in AllPhysicalAssemblies.Values)
                assembly.Update();

            if (Assemblies_SessionInit.DebugMode)
            {
                MyAPIGateway.Utilities.ShowNotification("Assemblies: " + AllPhysicalAssemblies.Count + " | Parts: " + AllAssemblyParts.Count, 1000 / 60);
                MyAPIGateway.Utilities.ShowNotification($"Definitions: {DefinitionHandler.I.ModularDefinitions.Count}", 1000 / 60);
            }
        }

        private void OnGridAdd(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;

            IMyCubeGrid grid = (IMyCubeGrid) entity;

            // Exclude projected and held grids
            if (grid.Physics == null)
                return;

            grid.OnBlockAdded += OnBlockAdd;
            grid.OnBlockRemoved += OnBlockRemove;

            List<IMySlimBlock> existingBlocks = new List<IMySlimBlock>();
            grid.GetBlocks(existingBlocks);
            foreach (var block in existingBlocks)
                QueuedBlockAdds.Add(block);
        }

        private void OnBlockAdd(IMySlimBlock block)
        {
            try
            {
                foreach (var modularDefinition in DefinitionHandler.I.ModularDefinitions)
                {
                    if (!modularDefinition.IsBlockAllowed(block))
                        return;

                    AssemblyPart w = new AssemblyPart(block, modularDefinition);
                    // No further init work is needed.
                    // Not returning because a part can have multiple assemblies.
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Handled exception in Modular Assemblies.AssemblyPartManager.OnBlockAdd()!\n" + e.ToString());
            }
        }

        private void OnGridRemove(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;

            IMyCubeGrid grid = (IMyCubeGrid)entity;

            // Exclude projected and held grids
            if (grid.Physics == null)
                return;

            grid.OnBlockAdded -= OnBlockAdd;
            grid.OnBlockRemoved -= OnBlockRemove;

            List<AssemblyPart> toRemove = new List<AssemblyPart>();
            HashSet<PhysicalAssembly> toRemoveAssemblies = new HashSet<PhysicalAssembly>();
            foreach (var partKvp in AllAssemblyParts)
            {
                if (partKvp.Key.CubeGrid == grid)
                {
                    toRemove.Add(partKvp.Value);
                    if (partKvp.Value.MemberAssembly != null)
                        toRemoveAssemblies.Add(partKvp.Value.MemberAssembly);
                }
            }
            foreach (var deadAssembly in toRemoveAssemblies)
                deadAssembly.Close();
            foreach (var deadPart in toRemove)
                AllAssemblyParts.Remove(deadPart.Block);
        }

        private void OnBlockRemove(IMySlimBlock block)
        {
            AssemblyPart part;
            if (AllAssemblyParts.TryGetValue(block, out part))
            {
                part.PartRemoved();
                AllAssemblyParts.Remove(block);
            }
        }
    }
}
