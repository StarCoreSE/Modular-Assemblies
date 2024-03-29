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
        public IMySlimBlock block;
        public PhysicalAssembly memberAssembly = null;
        public List<AssemblyPart> ConnectedParts = new List<AssemblyPart>();
        public ModularDefinition AssemblyDefinition;

        public int prevAssemblyId = -1;

        public AssemblyPart(IMySlimBlock block, ModularDefinition AssemblyDefinition)
        {
            this.block = block;
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

            PhysicalAssembly largestAssembly = neighbors[0].memberAssembly;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.memberAssembly.ComponentParts.Count > largestAssembly.ComponentParts.Count)
                {
                    largestAssembly.MergeWith(neighbor.memberAssembly);
                    largestAssembly = neighbor.memberAssembly;
                }
                else
                {
                    neighbor.memberAssembly.MergeWith(largestAssembly);
                }
                neighbor.ConnectedParts.Add(this);
            }
            largestAssembly.AddPart(this);

            ConnectedParts = neighbors;
        }

        public void PartRemoved()
        {
            memberAssembly.RemovePart(this);
        }

        /// <summary>
        /// Returns attached (as per AssemblyPart) neighbor blocks.
        /// </summary>
        /// <returns></returns>
        public List<IMySlimBlock> GetValidNeighbors(bool MustShareAssembly = false)
        {
            List<IMySlimBlock> neighbors = new List<IMySlimBlock>();
            block.GetNeighbours(neighbors);

            neighbors.RemoveAll(nBlock => !AssemblyDefinition.DoesBlockConnect(nBlock, nBlock, true));

            if (MustShareAssembly)
                neighbors.RemoveAll(nBlock =>
                {
                    AssemblyPart part;
                    if (!AssemblyPartManager.I.AllAssemblyParts.TryGetValue(nBlock, out part))
                        return true;
                    return part.memberAssembly != this.memberAssembly;
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
                if (AssemblyPartManager.I.AllAssemblyParts.TryGetValue(nBlock, out nBlockPart))
                {
                    if (!MustShareAssembly || nBlockPart.memberAssembly == memberAssembly)
                        validNeighbors.Add(nBlockPart);
                }
            }

            return validNeighbors;
        }

        public void GetAllConnectedParts(ref HashSet<AssemblyPart> connectedParts)
        {
            connectedParts.Add(this);
            foreach (var part in ConnectedParts)
            {
                GetAllConnectedParts(ref connectedParts);
            }
        }
    }
}