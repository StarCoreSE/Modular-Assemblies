using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    /// <summary>
    /// Attached to every part in a AssemblyDefinition.
    /// </summary>
    public class AssemblyPart
    {
        public IMySlimBlock Block;

        public PhysicalAssembly MemberAssembly
        {
            get
            {
                return memberAssembly;
            }
            set
            {
                if (value != memberAssembly)
                {
                    memberAssembly?.RemovePart(this);
                    memberAssembly = value;
                }
            }
        }
        private PhysicalAssembly memberAssembly = null;

        public List<AssemblyPart> ConnectedParts = new List<AssemblyPart>();
        public ModularDefinition AssemblyDefinition;

        public int prevAssemblyId = -1;

        public AssemblyPart(IMySlimBlock block, ModularDefinition AssemblyDefinition)
        {
            this.Block = block;
            this.AssemblyDefinition = AssemblyDefinition;

            //MyAPIGateway.Utilities.ShowNotification("Placed valid AssemblyPart");

            if (AssemblyPartManager.I.AllAssemblyParts.ContainsKey(block))
                return;

            AssemblyPartManager.I.AllAssemblyParts.Add(block, this);

            AssemblyPartManager.I.QueueConnectionCheck(this);
        }

        public void DoConnectionCheck()
        {
            List<AssemblyPart> neighbors = GetValidNeighborParts();

            // If no neighbors, create assembly.
            if (neighbors.Count == 0)
            {
                memberAssembly = new PhysicalAssembly(AssemblyPartManager.I.CreatedPhysicalAssemblies, this, AssemblyDefinition);
                return;
            }
            
            HashSet<PhysicalAssembly> assemblies = new HashSet<PhysicalAssembly>();
            foreach (var neighbor in neighbors)
            {
                if (neighbor.MemberAssembly != null)
                {
                    assemblies.Add(neighbor.MemberAssembly);
                    neighbor.ConnectedParts.Add(this);
                }
            }

            // Double-checking for null assemblies
            if (assemblies.Count == 0)
            {
                memberAssembly = new PhysicalAssembly(AssemblyPartManager.I.CreatedPhysicalAssemblies, this, AssemblyDefinition);
                return;
            }
             
            PhysicalAssembly largestAssembly = null;
            foreach (var assembly in assemblies)
            {
                if (assembly.ComponentParts.Count > (largestAssembly?.ComponentParts.Count ?? -1))
                {
                    largestAssembly?.MergeWith(assembly);
                    largestAssembly = assembly;
                }
                else
                {
                    assembly.MergeWith(largestAssembly);
                }
            }
            largestAssembly?.AddPart(this);
            
            ConnectedParts = neighbors;
            MyAPIGateway.Utilities.ShowNotification("NeighborCount: " + ConnectedParts.Count);
        }

        public void PartRemoved()
        {
            MyAPIGateway.Utilities.ShowNotification("PremNeighborCount: " + ConnectedParts.Count);
            MemberAssembly?.RemovePart(this);
            foreach (var neighbor in ConnectedParts)
                neighbor.ConnectedParts.Remove(this);
            MyAPIGateway.Utilities.ShowNotification("RemNeighborCount: " + ConnectedParts.Count);
        }

        /// <summary>
        /// Returns attached (as per AssemblyPart) neighbor blocks.
        /// </summary>
        /// <returns></returns>
        public List<IMySlimBlock> GetValidNeighbors(bool MustShareAssembly = false)
        {
            List<IMySlimBlock> neighbors = new List<IMySlimBlock>();
            Block.GetNeighbours(neighbors);

            neighbors.RemoveAll(nBlock => !AssemblyDefinition.DoesBlockConnect(Block, nBlock, true));

            if (MustShareAssembly)
                neighbors.RemoveAll(nBlock =>
                {
                    AssemblyPart part;
                    if (!AssemblyPartManager.I.AllAssemblyParts.TryGetValue(nBlock, out part))
                        return true;
                    return part.MemberAssembly != this.MemberAssembly;
                });

            return neighbors;
        }

        /// <summary>
        /// Returns attached (as per AssemblyPart) neighbor blocks's parts.
        /// </summary>
        /// <returns></returns>
        public List<AssemblyPart> GetValidNeighborParts(bool MustShareAssembly = false)
        {
            List<AssemblyPart> validNeighbors = new List<AssemblyPart>();
            foreach (var nBlock in GetValidNeighbors())
            {
                AssemblyPart nBlockPart;
                if (!AssemblyPartManager.I.AllAssemblyParts.TryGetValue(nBlock, out nBlockPart))
                    continue;

                if (!MustShareAssembly || nBlockPart.MemberAssembly == MemberAssembly)
                    validNeighbors.Add(nBlockPart);
            }

            return validNeighbors;
        }

        public void GetAllConnectedParts(ref HashSet<AssemblyPart> connectedParts)
        {
            // If a block has already been added, return.
            if (!connectedParts.Add(this))
                return;
            foreach (var part in ConnectedParts)
            {
                part.GetAllConnectedParts(ref connectedParts);
            }
        }
    }
}