using Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugDraw;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRage.Game.ModAPI;
using VRageMath;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    /// <summary>
    /// The collection of AssemblyParts attached to a modular assembly base.
    /// </summary>
    public class PhysicalAssembly
    {
        public AssemblyPart BasePart = null;
        public List<AssemblyPart> ComponentParts = new List<AssemblyPart>();
        public ModularDefinition AssemblyDefinition;
        public int AssemblyId = -1;
        public bool IsClosing = false;

        private Color color;

        public void Update()
        {
            if (AssembliesSessionInit.DebugMode)
            {
                foreach (var part in ComponentParts)
                {
                    DebugDrawManager.AddGridPoint(part.Block.Position, part.Block.CubeGrid, color, 0f);
                    foreach (var conPart in part.ConnectedParts)
                        DebugDrawManager.AddLine(DebugDrawManager.GridToGlobal(part.Block.Position, part.Block.CubeGrid), DebugDrawManager.GridToGlobal(conPart.Block.Position, part.Block.CubeGrid), color, 0f);
                }
                //DebugDrawManager.AddGPS($"ASM {AssemblyId} Parts: {ComponentParts.Count}", ComponentParts[0].Block.FatBlock.GetPosition(), 1/60f);
            }
        }

        public PhysicalAssembly(int id, AssemblyPart basePart, ModularDefinition AssemblyDefinition)
        {
            if (AssemblyDefinition.BaseBlockSubtype != null)
                BasePart = basePart;
            this.AssemblyDefinition = AssemblyDefinition;
            this.AssemblyId = id;
            AssemblyPartManager.I.CreatedPhysicalAssemblies++;

            if (AssemblyPartManager.I.AllPhysicalAssemblies.ContainsKey(id))
                throw new Exception("Duplicate assembly ID!");
            AssemblyPartManager.I.AllPhysicalAssemblies.Add(id, this);

            
            color = new Color(AssembliesSessionInit.I.Random.Next(255), AssembliesSessionInit.I.Random.Next(255), AssembliesSessionInit.I.Random.Next(255));

            AddPart(basePart);
        }

        public void AddPart(AssemblyPart part)
        {
            if (ComponentParts.Contains(part) || part.Block == null)
                return;

            ComponentParts.Add(part);
            part.MemberAssembly = this;
            if (part.PrevAssemblyId != AssemblyId)
                DefinitionHandler.I.SendOnPartAdd(AssemblyDefinition.Name, AssemblyId, part.Block.FatBlock.EntityId, /*part == basePart*/ ComponentParts.Count == 1);
            part.PrevAssemblyId = AssemblyId;
        }

        public void RemovePart(AssemblyPart part)
        {
            if (!ComponentParts.Remove(part))
                return;

            HashSet<AssemblyPart> neighbors = part.ConnectedParts;

            foreach (var neighbor in neighbors)
            {
                neighbor.ConnectedParts = neighbor.GetValidNeighborParts();
            }

            if (ComponentParts.Count == 0 || part == BasePart)
            {
                Close();
                return;
            }

            if (neighbors.Count == 1)
                return;

            List<HashSet<AssemblyPart>> partLoops = new List<HashSet<AssemblyPart>>();
            foreach (var neighbor in neighbors)
            {
                HashSet<AssemblyPart> connectedParts = new HashSet<AssemblyPart>();
                neighbor.GetAllConnectedParts(ref connectedParts);

                partLoops.Add(connectedParts);
            }

            if (partLoops.Count <= 1)
                return;

            // Split apart, keeping this assembly as the largest loop.
            HashSet<AssemblyPart> largestLoop = partLoops[0];
            foreach (var loop in partLoops)
            {
                if (loop.Count > largestLoop.Count)
                    largestLoop = loop;
            }
            foreach (var componentPart in ComponentParts.ToArray())
            {
                if (!largestLoop.Contains(componentPart))
                {
                    ComponentParts.Remove(componentPart);
                    componentPart.MemberAssembly = null;
                    componentPart.ConnectedParts.Clear();
                    AssemblyPartManager.I.QueueConnectionCheck(componentPart);
                }
            }
        }

        public void Close()
        {
            IsClosing = true;
            AssemblyPartManager.I.OnAssemblyClose?.Invoke(AssemblyId);
            if (ComponentParts != null)
            {
                foreach (var part in ComponentParts)
                {
                    //nullcheck for good luck :^)
                    if (part?.MemberAssembly != this)
                        continue;

                    part.MemberAssembly = null;
                    part.ConnectedParts.Clear();
                }
            }
            ComponentParts = null;
            //basePart = null;
            AssemblyPartManager.I.AllPhysicalAssemblies.Remove(AssemblyId);
        }

        

        public void MergeWith(PhysicalAssembly assembly)
        {
            if (assembly == null || assembly == this)
                return;

            foreach (var part in ComponentParts.ToArray())
            {
                assembly.AddPart(part);
            }
            Close();
        }
    }
}
