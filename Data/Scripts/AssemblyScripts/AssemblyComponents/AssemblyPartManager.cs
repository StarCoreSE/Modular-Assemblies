using System;
using System.Collections.Generic;
using System.Linq;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugUtils;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    /// <summary>
    ///     Creates and manages all AssemblyParts and PhysicalAssemblies.
    /// </summary>
    public class AssemblyPartManager
    {
        public static AssemblyPartManager I;

        /// <summary>
        ///     Every single AssemblyPart in the world, mapped to definitions.
        /// </summary>
        public Dictionary<ModularDefinition, Dictionary<IMySlimBlock, AssemblyPart>> AllAssemblyParts =
            new Dictionary<ModularDefinition, Dictionary<IMySlimBlock, AssemblyPart>>();

        /// <summary>
        ///     Every single PhysicalAssembly in the world.
        /// </summary>
        public Dictionary<int, PhysicalAssembly> AllPhysicalAssemblies = new Dictionary<int, PhysicalAssembly>();

        public int CreatedPhysicalAssemblies = 0;

        public Action<int> OnAssemblyClose;

        private readonly HashSet<IMySlimBlock> QueuedBlockAdds = new HashSet<IMySlimBlock>();
        private readonly HashSet<AssemblyPart> QueuedConnectionChecks = new HashSet<AssemblyPart>();

        public void QueueBlockAdd(IMySlimBlock block)
        {
            QueuedBlockAdds.Add(block);
        }

        public void QueueConnectionCheck(AssemblyPart part)
        {
            QueuedConnectionChecks.Add(part);
        }

        public void Init()
        {
            ModularLog.Log("AssemblyPartManager loading...");

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

            ModularLog.Log("AssemblyPartManager closing...");

            MyAPIGateway.Entities.OnEntityAdd -= OnGridAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnGridRemove;
        }

        public void UpdateAfterSimulation()
        {
            // Queue gridadds to account for world load/grid pasting
            ProcessQueuedBlockAdds();
            // Queue partadds to account for world load/grid pasting
            ProcessQueuedConnectionChecks();

            foreach (var assembly in AllPhysicalAssemblies.Values) assembly.Update();

            if (AssembliesSessionInit.DebugMode)
            {
                var partCount = 0;
                foreach (var definition in AllAssemblyParts)
                    partCount += definition.Value.Count;
                MyAPIGateway.Utilities.ShowNotification(
                    $"Assemblies: {AllPhysicalAssemblies.Count} | Parts: {partCount}", 1000 / 60);
                MyAPIGateway.Utilities.ShowNotification($"Definitions: {DefinitionHandler.I.ModularDefinitions.Count}",
                    1000 / 60);
            }
        }

        private void ProcessQueuedBlockAdds()
        {
            HashSet<IMySlimBlock> queuedBlocks;
            lock (QueuedBlockAdds)
            {
                queuedBlocks = QueuedBlockAdds.ToHashSet();
                QueuedBlockAdds.Clear();
            }

            foreach (var queuedBlock in queuedBlocks) OnBlockAdd(queuedBlock);
        }

        private void ProcessQueuedConnectionChecks()
        {
            HashSet<AssemblyPart> queuedParts;
            lock (QueuedConnectionChecks)
            {
                queuedParts = new HashSet<AssemblyPart>(QueuedConnectionChecks);
                QueuedConnectionChecks.Clear();
            }

            foreach (var queuedPart in queuedParts) queuedPart.DoConnectionCheck();
        }

        private void OnGridAdd(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;

            var grid = (IMyCubeGrid)entity;

            // Exclude projected and held grids
            if (grid.Physics == null)
                return;

            grid.OnBlockAdded += OnBlockAdd;
            grid.OnBlockRemoved += OnBlockRemove;

            var existingBlocks = new List<IMySlimBlock>();
            grid.GetBlocks(existingBlocks);
            foreach (var block in existingBlocks)
                QueuedBlockAdds.Add(block);
        }

        private void OnBlockAdd(IMySlimBlock block)
        {
            if (block == null)
                return;
            try
            {
                foreach (var modularDefinition in DefinitionHandler.I.ModularDefinitions)
                {
                    if (!modularDefinition.IsBlockAllowed(block))
                        return;

                    var w = new AssemblyPart(block, modularDefinition);
                    // No further init work is needed.
                    // Not returning because a part can have multiple assemblies.
                }
            }
            catch (Exception e)
            {
                ModularLog.Log("Handled exception in AssemblyPartManager.OnBlockAdd()!\n" + e);
            }
        }

        private void OnGridRemove(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;

            var grid = (IMyCubeGrid)entity;

            // Exclude projected and held grids
            if (grid.Physics == null)
                return;

            grid.OnBlockAdded -= OnBlockAdd;
            grid.OnBlockRemoved -= OnBlockRemove;

            var toRemove = new List<AssemblyPart>();
            var toRemoveAssemblies = new HashSet<PhysicalAssembly>();
            foreach (var definitionPartSet in AllAssemblyParts.Values)
            foreach (var partKvp in definitionPartSet)
                if (partKvp.Key.CubeGrid == grid)
                {
                    toRemove.Add(partKvp.Value);
                    if (partKvp.Value.MemberAssembly != null)
                        toRemoveAssemblies.Add(partKvp.Value.MemberAssembly);
                }

            foreach (var deadAssembly in toRemoveAssemblies)
                deadAssembly.Close();
            foreach (var deadPart in toRemove)
                AllAssemblyParts[deadPart.AssemblyDefinition].Remove(deadPart.Block);
        }

        private void OnBlockRemove(IMySlimBlock block)
        {
            if (block == null)
                return;
            AssemblyPart part;
            foreach (var definitionPartSet in AllAssemblyParts.Values)
                if (definitionPartSet.TryGetValue(block, out part))
                {
                    part.PartRemoved();
                    AllAssemblyParts[part.AssemblyDefinition].Remove(block);
                }
        }

        /// <summary>
        ///     Assigns all valid existing blocks an assembly part. Very slow operation, use sparingly.
        /// </summary>
        /// <param name="definition"></param>
        public void RegisterExistingBlocks(ModularDefinition definition)
        {
            if (definition == null)
                return;

            // Iterate through all entities and pick out grids
            var allEntities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(allEntities, entity => entity is IMyCubeGrid);

            // Parallel iterate through all grids and check all blocks for definition
            MyAPIGateway.Parallel.ForEach(allEntities, entity =>
            {
                foreach (var block in ((IMyCubeGrid)entity).GetFatBlocks<IMyCubeBlock>())
                    if (definition.AllowedBlocks.Contains(block.BlockDefinition.SubtypeId))
                    {
                        var w = new AssemblyPart(block.SlimBlock, definition);
                    }
            });
        }
    }
}