using Modular_Assemblies.AssemblyScripts.DebugUtils;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace Modular_Assemblies.AssemblyScripts.AssemblyComponents
{
    /// <summary>
    ///     The collection of AssemblyParts attached to a modular assembly base.
    /// </summary>
    public class PhysicalAssembly
    {
        public readonly IMyCubeGrid Grid;
        public Dictionary<IMySlimBlock, AssemblyPart> _componentParts = new Dictionary<IMySlimBlock, AssemblyPart>();
        public IReadOnlyDictionary<IMySlimBlock, AssemblyPart> ComponentParts => _componentParts;

        public ModularDefinition AssemblyDefinition;
        public int AssemblyId = -1;
        public AssemblyPart BasePart;

        private readonly Color Color = new Color(AssembliesSessionInit.I.Random.Next(255),
            AssembliesSessionInit.I.Random.Next(255), AssembliesSessionInit.I.Random.Next(255));

        public bool IsClosing;

        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        public PhysicalAssembly(int id, AssemblyPart basePart, ModularDefinition AssemblyDefinition)
        {
            if (AssemblyDefinition.BaseBlockSubtypes.Length > 0)
                BasePart = basePart;
            this.AssemblyDefinition = AssemblyDefinition;
            AssemblyId = id;
            Grid = basePart.Block.CubeGrid;

            if (AssemblyPartManager.I.AllPhysicalAssemblies.ContainsKey(id))
                throw new Exception("Duplicate assembly ID!");
            AssemblyPartManager.I.AllPhysicalAssemblies.Add(id, this);
            AssemblyPartManager.I.CreatedPhysicalAssemblies++;

            AddPart(basePart);
        }

        public object GetProperty(string propertyName)
        {
            return Properties.GetValueOrDefault(propertyName, null);
        }

        public void SetProperty(string propertyName, object value)
        {
            if (value == null)
                Properties.Remove(propertyName);
            else
                Properties[propertyName] = value;
        }

        public void Update()
        {
            if (_componentParts.Count == 0)
                Close();

            if (AssembliesSessionInit.DebugMode)
            {
                foreach (var part in _componentParts.Values)
                {
                    DebugDrawManager.AddGridPoint(part.Block.Position, part.Block.CubeGrid, Color, 0f);
                    foreach (var conPart in part.ConnectedParts)
                    {
                        DebugDrawManager.AddLine(
                            DebugDrawManager.GridToGlobal(part.Block.Position, part.Block.CubeGrid),
                            DebugDrawManager.GridToGlobal(conPart.Block.Position, part.Block.CubeGrid), Color, 0f);
                    }
                }
            }
                
        }

        public void AddPart(AssemblyPart part)
        {
            if (part.Block == null || _componentParts.ContainsKey(part.Block))
                return;

            _componentParts.Add(part.Block, part);
            part.MemberAssembly = this;
            if (part.PrevAssemblyId != AssemblyId)
            {
                // invoke on next game tick to avoid Issues
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    try
                    {
                        part.AssemblyDefinition.OnPartAdd?.Invoke(AssemblyId, part.Block.FatBlock, part.IsBaseBlock);
                    }
                    catch (Exception ex)
                    {
                        ModularLog.LogException(ex, typeof(PhysicalAssembly));
                        MyAPIGateway.Utilities.ShowMessage("Modular Assemblies",
                            $"Exception caught {ex.StackTrace.FirstLine()} - check logs for more info.");
                    }
                });
            }
            part.PrevAssemblyId = AssemblyId;
        }

        public void RemovePart(AssemblyPart part)
        {
            if (part == null)
                return;

            if (!_componentParts?.Remove(part.Block) ?? true)
                return;

            var neighbors = part.ConnectedParts;

            foreach (var neighbor in neighbors) neighbor.ConnectedParts = neighbor.GetValidNeighborParts();

            if (_componentParts.Count == 0 || part == BasePart)
            {
                Close();
                return;
            }

            if (neighbors.Count == 1)
                return;

            var partLoops = new List<HashSet<AssemblyPart>>();
            foreach (var neighbor in neighbors)
            {
                var connectedParts = new HashSet<AssemblyPart>();
                neighbor.GetAllConnectedParts(ref connectedParts);

                partLoops.Add(connectedParts);
            }

            if (partLoops.Count <= 1)
                return;
            // Split apart, keeping this assembly as the largest loop.
            var largestLoop = partLoops[0];
            foreach (var loop in partLoops)
            {
                if (loop.Count > largestLoop.Count)
                    largestLoop = loop;
            }

            foreach (var componentPart in _componentParts.Values.ToArray())
            {
                if (largestLoop.Contains(componentPart))
                    continue;

                if (!_componentParts.Remove(componentPart.Block))
                    continue;
                componentPart.RemoveAssemblyUnsafe();
                // invoke on next game tick to avoid Issues
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    try
                    {
                        componentPart.AssemblyDefinition.OnPartRemove?.Invoke(AssemblyId, componentPart.Block.FatBlock,
                            componentPart.IsBaseBlock);
                    }
                    catch (Exception ex)
                    {
                        ModularLog.LogException(ex, typeof(PhysicalAssembly));
                        MyAPIGateway.Utilities.ShowMessage("Modular Assemblies",
                            $"Exception caught {ex.StackTrace.FirstLine()} - check logs for more info.");
                    }
                });
                AssemblyPartManager.I.QueueConnectionCheck(componentPart);
            }
        }

        public void Close()
        {
            IsClosing = true;
            try
            {
                AssemblyPartManager.I.OnAssemblyClose?.Invoke(AssemblyId);
                AssemblyDefinition.OnAssemblyClose?.Invoke(AssemblyId);
            }
            catch (Exception ex)
            {
                ModularLog.LogException(ex, typeof(PhysicalAssembly));
                MyAPIGateway.Utilities.ShowMessage("Modular Assemblies",
                    $"Exception caught {ex.StackTrace.FirstLine()} - check logs for more info.");
            }

            if (_componentParts != null)
            {
                foreach (var part in _componentParts.Values)
                {
                    //nullcheck for good luck :^)
                    if (part?.MemberAssembly != this)
                        continue;

                    part.MemberAssembly = null;
                    part.ConnectedParts.Clear();
                }
            }

            _componentParts = null;
            //basePart = null;
            AssemblyPartManager.I.AllPhysicalAssemblies.Remove(AssemblyId);
        }


        public void MergeWith(PhysicalAssembly assembly)
        {
            if (assembly == null || assembly == this || _componentParts == null)
                return;

            // TODO: Add definition action for merging dictionaries
            if ((bool?)GetProperty("ExistingProperties") ?? false) assembly.Properties = Properties;

            Properties.Clear();

            foreach (var part in _componentParts.Values.ToArray())
                assembly.AddPart(part);
            Close();
        }
    }
}