using System;
using System.Collections.Generic;
using Modular_Assemblies.AssemblyScripts.DebugUtils;
using Modular_Assemblies.AssemblyScripts.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Modular_Assemblies.AssemblyScripts.AssemblyComponents
{
    /// <summary>
    ///     Creates and manages all AssemblyParts and PhysicalAssemblies.
    /// </summary>
    public class AssemblyPartManager
    {
        public static AssemblyPartManager I;

        private readonly HashSet<AssemblyPart> QueuedConnectionChecks = new HashSet<AssemblyPart>();

        public Dictionary<IMyCubeGrid, GridAssemblyLogic> AllGridLogics =
            new Dictionary<IMyCubeGrid, GridAssemblyLogic>();

        /// <summary>
        ///     Every single PhysicalAssembly in the world.
        /// </summary>
        public Dictionary<int, PhysicalAssembly> AllPhysicalAssemblies = new Dictionary<int, PhysicalAssembly>();

        public int CreatedPhysicalAssemblies = 0;

        public Action<int> OnAssemblyClose;

        /// <summary>
        ///     GameLogicComponents aren't instantly added, so we need a queue for transferring assembly parts.
        /// </summary>
        internal Dictionary<MyCubeGrid, AssemblySerializer.AssemblyStorage[]> QueuedAssemblyTransfers =
            new Dictionary<MyCubeGrid, AssemblySerializer.AssemblyStorage[]>();

        public void QueueConnectionCheck(AssemblyPart part)
        {
            lock (QueuedConnectionChecks)
            {
                QueuedConnectionChecks.Add(part);
            }
        }

        public void UnQueueConnectionCheck(AssemblyPart part)
        {
            lock (QueuedConnectionChecks)
            {
                QueuedConnectionChecks.Remove(part);
            }
        }

        public void Init()
        {
            ModularLog.Log("AssemblyPartManager loading...");

            I = this;
        }

        public void Unload()
        {
            I = null; // important for avoiding this object to remain allocated in memory
            AllPhysicalAssemblies.Clear();
            OnAssemblyClose = null;

            ModularLog.Log("AssemblyPartManager closing...");
        }

        public void UpdateAfterSimulation()
        {
            // Queue partadds to account for world load/grid pasting
            ProcessQueuedConnectionChecks();

            foreach (var assembly in AllPhysicalAssemblies.Values) assembly.Update();
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