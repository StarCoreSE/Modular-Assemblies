using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    /// <summary>
    ///     Attached to every part in a AssemblyDefinition.
    /// </summary>
    public class AssemblyPart
    {
        private PhysicalAssembly _memberAssembly;
        public ModularDefinition AssemblyDefinition;
        public IMySlimBlock Block;

        public HashSet<AssemblyPart> ConnectedParts = new HashSet<AssemblyPart>();
        public bool IsBaseBlock;

        public int PrevAssemblyId = -1;

        public AssemblyPart(IMySlimBlock block, ModularDefinition AssemblyDefinition)
        {
            Block = block;
            this.AssemblyDefinition = AssemblyDefinition;

            IsBaseBlock = AssemblyDefinition.BaseBlockSubtype == Block.BlockDefinition.Id.SubtypeName;

            if (AssemblyPartManager.I.AllAssemblyParts[AssemblyDefinition].ContainsKey(block))
                return;

            AssemblyPartManager.I.AllAssemblyParts[AssemblyDefinition].Add(block, this);

            AssemblyPartManager.I.QueueConnectionCheck(this);
        }

        public PhysicalAssembly MemberAssembly
        {
            get { return _memberAssembly; }
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

        public void DoConnectionCheck(bool cascadingUpdate = false, HashSet<AssemblyPart> visited = null)
        {
            if (visited == null)
                visited = new HashSet<AssemblyPart>();

            if (!visited.Add(this))
                return;

            ConnectedParts = GetValidNeighborParts();

            // If no neighbors AND (is base block OR base block not defined), create assembly.
            if (ConnectedParts.Count == 0 && (AssemblyDefinition.BaseBlockSubtype == null || IsBaseBlock))
            {
                _memberAssembly = new PhysicalAssembly(AssemblyPartManager.I.CreatedPhysicalAssemblies, this,
                    AssemblyDefinition);
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

            var assemblies = new HashSet<PhysicalAssembly>();
            foreach (var neighbor in ConnectedParts)
            {
                if (neighbor.MemberAssembly != null) assemblies.Add(neighbor.MemberAssembly);
                neighbor.ConnectedParts = neighbor.GetValidNeighborParts();
            }

            // Double-checking for null assemblies
            if (assemblies.Count == 0 && (AssemblyDefinition.BaseBlockSubtype == null || IsBaseBlock))
            {
                _memberAssembly = new PhysicalAssembly(AssemblyPartManager.I.CreatedPhysicalAssemblies, this,
                    AssemblyDefinition);
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

            var largestAssembly = MemberAssembly;
            foreach (var assembly in assemblies)
                if (assembly.ComponentParts.Count > (largestAssembly?.ComponentParts.Count ?? -1))
                {
                    largestAssembly?.MergeWith(assembly);
                    largestAssembly = assembly;
                }
                else
                {
                    assembly.MergeWith(largestAssembly);
                }

            largestAssembly?.AddPart(this);

            // Trigger cascading update
            if (IsBaseBlock || cascadingUpdate)
                //debug notification begone
                //MyAPIGateway.Utilities.ShowNotification("" + GetValidNeighborParts().Count);
                foreach (var neighbor in GetValidNeighborParts())
                    if (neighbor.MemberAssembly == null)
                        neighbor.DoConnectionCheck(true, visited);
        }

        public void PartRemoved(bool notifyMods = true)
        {
            var assemblyId = MemberAssembly?.AssemblyId ?? -1;
            MemberAssembly?.RemovePart(this);
            foreach (var neighbor in ConnectedParts)
                neighbor.ConnectedParts.Remove(this);

            if (notifyMods)
            {
                AssemblyDefinition.OnPartRemove?.Invoke(assemblyId, Block.FatBlock, IsBaseBlock);
                if (Block.Integrity <= 0)
                    AssemblyDefinition.OnPartDestroy?.Invoke(assemblyId, Block.FatBlock, IsBaseBlock);
            }
        }

        /// <summary>
        ///     Returns attached (as per AssemblyPart) neighbor blocks.
        /// </summary>
        /// <returns></returns>
        public List<IMySlimBlock> GetValidNeighbors(bool MustShareAssembly = false)
        {
            var neighbors = new List<IMySlimBlock>();
            Block.GetNeighbours(neighbors);

            neighbors.RemoveAll(nBlock => !AssemblyDefinition.DoesBlockConnect(Block, nBlock));

            if (MustShareAssembly)
                neighbors.RemoveAll(nBlock =>
                {
                    AssemblyPart part;
                    if (!AssemblyPartManager.I.AllAssemblyParts[AssemblyDefinition].TryGetValue(nBlock, out part))
                        return true;
                    return part.MemberAssembly != MemberAssembly;
                });

            return neighbors;
        }

        /// <summary>
        ///     Returns attached (as per AssemblyPart) neighbor blocks's parts.
        /// </summary>
        /// <returns></returns>
        public HashSet<AssemblyPart> GetValidNeighborParts(bool MustShareAssembly = false)
        {
            var validNeighbors = new List<AssemblyPart>();
            foreach (var nBlock in GetValidNeighbors())
            {
                AssemblyPart nBlockPart;
                if (!AssemblyPartManager.I.AllAssemblyParts[AssemblyDefinition].TryGetValue(nBlock, out nBlockPart))
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
            foreach (var part in ConnectedParts) part.GetAllConnectedParts(ref connectedParts);
        }
    }
}