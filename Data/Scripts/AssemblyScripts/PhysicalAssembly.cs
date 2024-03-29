using Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugDraw;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    /// <summary>
    /// The collection of AssemblyParts attached to a modular assembly base.
    /// </summary>
    public class PhysicalAssembly
    {
        //public AssemblyPart basePart;
        public List<AssemblyPart> ComponentParts = new List<AssemblyPart>();
        public ModularDefinition AssemblyDefinition;
        public int AssemblyId = -1;

        private Color color;

        public void Update()
        {
            if (Assemblies_SessionInit.I.DebugMode)
            {
                foreach (var part in ComponentParts)
                {
                    DebugDrawManager.AddGridPoint(part.block.Position, part.block.CubeGrid, color, 0f);
                    foreach (var conPart in part.ConnectedParts)
                        DebugDrawManager.AddLine(DebugDrawManager.GridToGlobal(part.block.Position, part.block.CubeGrid), DebugDrawManager.GridToGlobal(conPart.block.Position, part.block.CubeGrid), color, 0f);
                }
                MyAPIGateway.Utilities.ShowNotification($"Assembly {AssemblyId} Parts: {ComponentParts.Count}", 1000 / 60);
            }
        }

        public PhysicalAssembly(int id, AssemblyPart basePart, ModularDefinition AssemblyDefinition)
        {
            //this.basePart = basePart;
            this.AssemblyDefinition = AssemblyDefinition;
            this.AssemblyId = id;
            AssemblyPartManager.I.CreatedPhysicalAssemblies++;

            if (AssemblyPartManager.I.AllPhysicalAssemblies.ContainsKey(id))
                throw new Exception("Duplicate assembly ID!");
            AssemblyPartManager.I.AllPhysicalAssemblies.Add(id, this);

            Random r = new Random();
            color = new Color(r.Next(255), r.Next(255), r.Next(255));

            AddPart(basePart);
            AssemblyPartManager.I.QueueAssemblyCheck(basePart, this);
        }

        public void AddPart(AssemblyPart part)
        {
            if (ComponentParts.Contains(part))
                return;

            ComponentParts.Add(part);
            part.memberAssembly = this;
            if (part.prevAssemblyId != AssemblyId)
                DefinitionHandler.I.SendOnPartAdd(AssemblyDefinition.Name, AssemblyId, part.block.FatBlock.EntityId, /*part == basePart*/ false);
            part.prevAssemblyId = AssemblyId;
        }

        public void RemovePart(AssemblyPart part)
        {
            MyAPIGateway.Utilities.ShowNotification("NeighborCount: " + part.GetValidNeighbors().Count);
            ComponentParts.Remove(part);
            if (ComponentParts.Count == 0)
            {
                Close();
                return;
            }

            List<AssemblyPart> neighbors = part.ConnectedParts;

            foreach (var neighbor in neighbors)
                neighbor.ConnectedParts.Remove(part);

            if (neighbors.Count == 1)
                return;

            List<HashSet<AssemblyPart>> partLoops = new List<HashSet<AssemblyPart>>();
            foreach (var neighbor in neighbors)
            {
                HashSet<AssemblyPart> connectedParts = new HashSet<AssemblyPart>();
                neighbor.GetAllConnectedParts(ref connectedParts);

                if (connectedParts.Count == ComponentParts.Count - 1)
                    continue;

                partLoops.Add(connectedParts);
            }

            MyAPIGateway.Utilities.ShowNotification("LoopCount: " + partLoops.Count + " of " + neighbors.Count);
        }

        public void Close()
        {
            foreach (var part in ComponentParts)
                if (part.memberAssembly == this)
                    part.memberAssembly = null;

            ComponentParts.Clear();
            //basePart = null;
            AssemblyPartManager.I.AllPhysicalAssemblies.Remove(AssemblyId);
        }

        public void MergeWith(PhysicalAssembly assembly)
        {
            if (assembly == null || assembly == this)
                return;

            foreach (var part in ComponentParts)
            {
                assembly.AddPart(part);
            }
            Close();
        }
    }
}
