using System;
using System.Collections.Generic;
using System.Linq;
using Modular_Assemblies.AssemblyScripts.DebugUtils;
using Modular_Assemblies.AssemblyScripts.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Modular_Assemblies.AssemblyScripts.AssemblyComponents
{
    public class GridAssemblyLogic
    {
        public Dictionary<ModularDefinition, Dictionary<IMySlimBlock, AssemblyPart>> AllAssemblyParts =
            new Dictionary<ModularDefinition, Dictionary<IMySlimBlock, AssemblyPart>>();

        private MyCubeGrid Grid;

        /// <summary>
        ///     List of all AssemblyParts moved by Grid split. Used to transfer to new Grid logic.
        /// </summary>
        private readonly Dictionary<int, AssemblySerializer.AssemblyStorage> SplitAssemblies =
            new Dictionary<int, AssemblySerializer.AssemblyStorage>();

        private void LoadStorage(AssemblySerializer.AssemblyStorage storage, ref List<MyCubeBlock> queuedBlockChecks)
        {
            try
            {
                var newParts = new List<AssemblyPart>();
                var def = DefinitionHandler.TryGetDefinition(storage.DefinitionName);
                if (def == null)
                    throw new Exception($"Invalid stored definition \"{storage.DefinitionName}\"!");

                foreach (var block in queuedBlockChecks)
                {
                    if (!storage.IsBlockValid(block))
                        continue;
                    newParts.Add(new AssemblyPart(block.SlimBlock, def));
                }

                if (newParts.Count == 0)
                    throw new Exception("Invalid part collection!");

                var newAssembly =
                    new PhysicalAssembly(AssemblyPartManager.I.CreatedPhysicalAssemblies, newParts[0], def);

                for (int i = 1; i < newParts.Count; i++)
                    newAssembly.AddPart(newParts[i]);

                newAssembly.Properties = storage.AssemblyProperties();
                //ModularLog.Log("Loaded Properties:");
                //foreach (var property in newAssembly.Properties)
                //    ModularLog.Log($"|   {property.Key}: {property.Value}");
            }
            catch (Exception ex)
            {
                ModularLog.LogException(ex, typeof(GridAssemblyLogic));
            }
        }

        private void OnGridSplit(MyCubeGrid thisGrid, MyCubeGrid newGrid)
        {
            try
            {
                var storages = SplitAssemblies.Values.ToArray();

                AssemblyPartManager.I.QueuedAssemblyTransfers.Add(newGrid, storages);

                SplitAssemblies.Clear();
            }
            catch (Exception ex)
            {
                ModularLog.LogException(ex, typeof(GridAssemblyLogic));
            }
        }

        private void OnBlockAdd(IMySlimBlock block)
        {
            if (block?.FatBlock == null)
                return;

            try
            {
                foreach (var modularDefinition in DefinitionHandler.I.ModularDefinitions)
                {
                    if (!modularDefinition.IsBlockAllowed(block) || AllAssemblyParts[modularDefinition].ContainsKey(block))
                        continue;

                    var w = new AssemblyPart(block, modularDefinition);
                    // No further init work is needed.
                    // Not returning because a part can have multiple assemblies.
                }
            }
            catch (Exception e)
            {
                ModularLog.LogException(e, typeof(GridAssemblyLogic));
            }
        }

        private void OnBlockRemove(IMySlimBlock block)
        {
            if (block?.FatBlock == null)
                return;

            try
            {
                AssemblyPart part;
                foreach (var definitionPartSet in AllAssemblyParts.Values)
                    if (definitionPartSet.TryGetValue(block, out part))
                    {
                        if ((block.IsMovedBySplit || block.CubeGrid.WillRemoveBlockSplitGrid(block)) &&
                            part.MemberAssembly?.ComponentParts != null)
                        {
                            if (!SplitAssemblies.ContainsKey(part.MemberAssembly.AssemblyId))
                                SplitAssemblies.Add(part.MemberAssembly.AssemblyId,
                                    new AssemblySerializer.AssemblyStorage(part.MemberAssembly, true));
                        }

                        part.PartRemoved();
                        AllAssemblyParts[part.AssemblyDefinition].Remove(block);

                        if (block.IsMovedBySplit && part.MemberAssembly?.ComponentParts != null)
                            AssemblyPartManager.I.UnQueueConnectionCheck(part);
                    }
            }
            catch (Exception ex)
            {
                ModularLog.LogException(ex, typeof(GridAssemblyLogic));
            }
        }

        #region Base Methods

        public void UpdateOnceBeforeFrame(MyCubeGrid grid)
        {
            Grid = grid;
            if (Grid.Physics == null)
                return;

            try
            {
                AssemblyPartManager.I.AllGridLogics.Add(Grid, this);

                foreach (var definition in DefinitionHandler.I.ModularDefinitions)
                    AllAssemblyParts.Add(definition, new Dictionary<IMySlimBlock, AssemblyPart>());

                Grid.OnBlockAdded += OnBlockAdd;
                Grid.OnBlockRemoved += OnBlockRemove;
                Grid.OnGridSplit += OnGridSplit;

                var existingBlocks = Grid.GetFatBlocks().ToList();
                if (AssemblyPartManager.I.QueuedAssemblyTransfers.ContainsKey(Grid))
                {
                    foreach (var storage in AssemblyPartManager.I.QueuedAssemblyTransfers[Grid])
                        LoadStorage(storage, ref existingBlocks);
                    AssemblyPartManager.I.QueuedAssemblyTransfers.Remove(Grid);
                }
                else
                {
                    string storageStr;
                    if (Grid.Storage == null)
                        Grid.Storage = new MyModStorageComponent();
                    else if (Grid.Storage.TryGetValue(AssembliesSessionInit.ModStorageGuid, out storageStr) &&
                             !string.IsNullOrEmpty(storageStr))
                        foreach (var storage in AssemblySerializer.DeserializeGrid(Convert.FromBase64String(storageStr)))
                            LoadStorage(storage, ref existingBlocks);
                }

                foreach (var block in existingBlocks)
                    OnBlockAdd(block.SlimBlock);
            }
            catch (Exception ex)
            {
                ModularLog.LogException(ex, typeof(GridAssemblyLogic));
            }
        }

        public void UpdateSlow()
        {
            try
            {
                if (Grid?.Physics == null || Grid.Storage == null)
                    return;
        
                var serialized = AssemblySerializer.SerializeGrid(Grid);
        
                Grid.Storage.SetValue(AssembliesSessionInit.ModStorageGuid, Convert.ToBase64String(serialized));
            }
            catch (Exception ex)
            {
                ModularLog.LogException(ex, typeof(GridAssemblyLogic));
            }
        }

        public void Close()
        {
            if (Grid.Physics == null)
                return;

            var toRemove = new List<AssemblyPart>();
            var toRemoveAssemblies = new HashSet<PhysicalAssembly>();
            foreach (var definitionPartSet in AllAssemblyParts.Values)
            {
                foreach (var partKvp in definitionPartSet)
                {
                    toRemove.Add(partKvp.Value);
                    if (partKvp.Value.MemberAssembly != null)
                        toRemoveAssemblies.Add(partKvp.Value.MemberAssembly);
                }
            }

            foreach (var deadAssembly in toRemoveAssemblies)
                deadAssembly.Close();
            foreach (var deadPart in toRemove)
                AllAssemblyParts[deadPart.AssemblyDefinition].Remove(deadPart.Block);

            AssemblyPartManager.I.AllGridLogics.Remove(Grid);
        }

        #endregion
    }
}