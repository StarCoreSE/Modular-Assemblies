using System;
using System.Collections.Generic;
using System.Reflection;
using Modular_Assemblies.AssemblyScripts.DebugUtils;
using VRageMath;

namespace Modular_Assemblies.AssemblyScripts.AssemblyComponents
{
    /// <summary>
    ///     The collection of AssemblyParts attached to a modular assembly base.
    /// </summary>
    public class PhysicalAssembly
    {
        private List<AssemblyPart> _componentParts = new List<AssemblyPart>();
        public ModularDefinition AssemblyDefinition;
        public int AssemblyId = -1;
        public AssemblyPart BasePart;

        private readonly Color Color = new Color(AssembliesSessionInit.I.Random.Next(255),
            AssembliesSessionInit.I.Random.Next(255), AssembliesSessionInit.I.Random.Next(255));

        public bool IsClosing;

        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        public PhysicalAssembly(int id, AssemblyPart basePart, ModularDefinition AssemblyDefinition)
        {
            if (AssemblyDefinition.BaseBlockSubtype != null)
                BasePart = basePart;
            this.AssemblyDefinition = AssemblyDefinition;
            AssemblyId = id;

            if (AssemblyPartManager.I.AllPhysicalAssemblies.ContainsKey(id))
                throw new Exception("Duplicate assembly ID!");
            AssemblyPartManager.I.AllPhysicalAssemblies.Add(id, this);
            AssemblyPartManager.I.CreatedPhysicalAssemblies++;

            AddPart(basePart);
        }

        public AssemblyPart[] ComponentParts = Array.Empty<AssemblyPart>();

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
                foreach (var part in _componentParts)
                {
                    DebugDrawManager.AddGridPoint(part.Block.Position, part.Block.CubeGrid, Color, 0f);
                    foreach (var conPart in part.ConnectedParts)
                        DebugDrawManager.AddLine(
                            DebugDrawManager.GridToGlobal(part.Block.Position, part.Block.CubeGrid),
                            DebugDrawManager.GridToGlobal(conPart.Block.Position, part.Block.CubeGrid), Color, 0f);
                }
        }

        public void AddPart(AssemblyPart part)
        {
            if (_componentParts.Contains(part) || part.Block == null)
                return;

            _componentParts.Add(part);
            ComponentParts = _componentParts?.ToArray();
            part.MemberAssembly = this;
            if (part.PrevAssemblyId != AssemblyId)
                part.AssemblyDefinition.OnPartAdd?.Invoke(AssemblyId, part.Block.FatBlock, part.IsBaseBlock);
            part.PrevAssemblyId = AssemblyId;
        }

        public void RemovePart(AssemblyPart part)
        {
            if (part == null)
                return;

            if (!_componentParts?.Remove(part) ?? true)
                return;

            ComponentParts = _componentParts?.ToArray();

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

            foreach (var componentPart in _componentParts.ToArray())
            {
                if (largestLoop.Contains(componentPart))
                    continue;

                if (!_componentParts.Remove(componentPart))
                    continue;
                componentPart.RemoveAssemblyUnsafe();
                componentPart.AssemblyDefinition.OnPartRemove?.Invoke(AssemblyId, componentPart.Block.FatBlock, componentPart.IsBaseBlock);
                AssemblyPartManager.I.QueueConnectionCheck(componentPart);
            }

            ComponentParts = _componentParts?.ToArray();
        }

        public void Close()
        {
            IsClosing = true;
            AssemblyPartManager.I.OnAssemblyClose?.Invoke(AssemblyId);
            if (_componentParts != null)
                foreach (var part in _componentParts)
                {
                    //nullcheck for good luck :^)
                    if (part?.MemberAssembly != this)
                        continue;

                    part.MemberAssembly = null;
                    part.ConnectedParts.Clear();
                    part.AssemblyDefinition.OnPartRemove?.Invoke(AssemblyId, part.Block.FatBlock, part.IsBaseBlock);
                }

            _componentParts = null;
            ComponentParts = null;
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

            foreach (var part in _componentParts.ToArray()) assembly.AddPart(part);
            Close();
        }
    }
}