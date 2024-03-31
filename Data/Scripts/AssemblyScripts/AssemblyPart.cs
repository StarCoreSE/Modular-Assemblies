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
        public bool IsBaseBlock = false;

        public PhysicalAssembly MemberAssembly
        {
            get
            {
                return _memberAssembly;
            }
            set
            {
                if (value != _memberAssembly)
                {
                    if (!_memberAssembly?.IsClosing ?? false)
                        _memberAssembly.RemovePart(this);
                    _memberAssembly = value;
                }
            }
        }
        private PhysicalAssembly _memberAssembly = null;

        public HashSet<AssemblyPart> ConnectedParts = new HashSet<AssemblyPart>();
        public ModularDefinition AssemblyDefinition;

        public int PrevAssemblyId = -1;

        public AssemblyPart(IMySlimBlock block, ModularDefinition AssemblyDefinition)
        {
            this.Block = block;
            this.AssemblyDefinition = AssemblyDefinition;

            IsBaseBlock = AssemblyDefinition.BaseBlockSubtype == Block.BlockDefinition.Id.SubtypeName;

            if (AssemblyPartManager.I.AllAssemblyParts.ContainsKey(block))
                return;

            AssemblyPartManager.I.AllAssemblyParts.Add(block, this);

            AssemblyPartManager.I.QueueConnectionCheck(this);
        }

        public void DoConnectionCheck(bool cascadingUpdate = false)
        {
            ConnectedParts = GetValidNeighborParts();
            if (Assemblies_SessionInit.DebugMode)
                MyAPIGateway.Utilities.ShowMessage("Fusion Systems", "AddNeighbors: " + ConnectedParts.Count);

            // If no neighbors AND (is base block OR base block not defined), create assembly.
            if (ConnectedParts.Count == 0 && (AssemblyDefinition.BaseBlockSubtype == null || IsBaseBlock))
            {
                _memberAssembly = new PhysicalAssembly(AssemblyPartManager.I.CreatedPhysicalAssemblies, this, AssemblyDefinition);
                // Trigger cascading update
                if (IsBaseBlock || cascadingUpdate)
                {
                    MyAPIGateway.Utilities.ShowNotification("" + GetValidNeighborParts().Count);
                    foreach (var neighbor in GetValidNeighborParts())
                        if (neighbor.MemberAssembly == null)
                            neighbor.DoConnectionCheck(true);
                }
                return;
            }
            
            HashSet<PhysicalAssembly> assemblies = new HashSet<PhysicalAssembly>();
            foreach (var neighbor in ConnectedParts)
            {
                if (neighbor.MemberAssembly != null)
                {
                    assemblies.Add(neighbor.MemberAssembly);
                }
                neighbor.ConnectedParts = neighbor.GetValidNeighborParts();
            }

            // Double-checking for null assemblies
            if (assemblies.Count == 0 && (AssemblyDefinition.BaseBlockSubtype == null || IsBaseBlock))
            {
                _memberAssembly = new PhysicalAssembly(AssemblyPartManager.I.CreatedPhysicalAssemblies, this, AssemblyDefinition);
                // Trigger cascading update
                if (IsBaseBlock || cascadingUpdate)
                {
                    MyAPIGateway.Utilities.ShowNotification("" + GetValidNeighborParts().Count);
                    foreach (var neighbor in GetValidNeighborParts())
                        if (neighbor.MemberAssembly == null)
                            neighbor.DoConnectionCheck(true);
                }
                return;
            }
            
            PhysicalAssembly largestAssembly = MemberAssembly;
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
            
            // Trigger cascading update
            if (IsBaseBlock || cascadingUpdate)
            {
                MyAPIGateway.Utilities.ShowNotification("" + GetValidNeighborParts().Count);
                foreach (var neighbor in GetValidNeighborParts())
                    if (neighbor.MemberAssembly == null)
                        neighbor.DoConnectionCheck(true);
            }
        }

        public void PartRemoved()
        {
            MemberAssembly?.RemovePart(this);
            foreach (var neighbor in ConnectedParts)
                neighbor.ConnectedParts.Remove(this);
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
        public HashSet<AssemblyPart> GetValidNeighborParts(bool MustShareAssembly = false)
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

            return validNeighbors.ToHashSet();
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