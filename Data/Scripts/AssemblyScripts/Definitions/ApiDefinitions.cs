using Modular_Assemblies.AssemblyScripts.AssemblyComponents;
using Modular_Assemblies.AssemblyScripts.Commands;
using Modular_Assemblies.AssemblyScripts.DebugUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Modular_Assemblies.AssemblyScripts.Definitions
{
    internal class ApiDefinitions
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;

        internal ApiDefinitions()
        {
            ModApiMethods = new Dictionary<string, Delegate>
            {
                // Global assembly methods
                ["GetAllParts"] =
                    new Func<IMyCubeBlock[]>(
                        GetAllParts), // Returns a IMyCubeBlock array of all CubeBlocks with Assembly parts.
                ["GetAllAssemblies"] = new Func<int[]>(GetAllAssemblies), // Returns an int array of all Assembly IDs.

                // Per-assembly methods
                ["GetMemberParts"] =
                    new Func<int, IMyCubeBlock[]>(
                        GetMemberParts), // Returns a IMyCubeBlock array of all CubeBlocks within a given Assembly ID.
                ["GetBasePart"] = new Func<int, IMyCubeBlock>(GetBasePart),
                ["GetAssemblyGrid"] =
                    new Func<int, IMyCubeGrid>(
                        GetAssemblyGrid), // Returns the IMyCubeGrid an assembly ID is contained in.
                ["AddOnAssemblyClose"] =
                    new Action<Action<int>>(
                        AddOnAssemblyClose), // Registers an Action<AssemblyId> triggered on assembly removal.
                ["RemoveOnAssemblyClose"] =
                    new Action<Action<int>>(
                        RemoveOnAssemblyClose), // De-registers an Action<AssemblyId> triggered on assembly removal.
                ["RecreateAssembly"] = new Action<int>(RecreateAssembly),
                ["GetAssemblyProperty"] = new Func<int, string, object>(GetAssemblyProperty),
                ["SetAssemblyProperty"] = new Action<int, string, object>(SetAssemblyProperty),
                ["ListAssemblyProperties"] = new Func<int, string[]>(ListAssemblyProperties),

                // Per-part methods
                // Note - next three are obsolete but included to not break existing mods.
                ["GetConnectedBlocks"] = new Func<IMyCubeBlock, string, bool, IMyCubeBlock[]>(GetConnectedBlocks),
                ["GetContainingAssembly"] = new Func<IMyCubeBlock, string, int>(GetContainingAssembly),
                ["RecreateConnections"] = new Action<IMyCubeBlock, string>(RecreateConnections),
                ["GetConnectedBlocksMulti"] = new Func<IMyCubeBlock, int, bool, IMyCubeBlock[]>(GetConnectedBlocksMulti),
                ["GetContainingAssemblyMulti"] = new Func<IMyCubeBlock, string, int[]>(GetContainingAssemblyMulti),
                ["RecreateConnectionsMulti"] = new Action<IMyCubeBlock, int>(RecreateConnectionsMulti),
                ["GetGridConnectingPositions"] = new Func<IMyCubeBlock, string, Vector3I[]>(GetGridConnectingPositions),
                ["GetLocalConnectingPositions"] = new Func<IMyCubeBlock, string, Vector3I[]>(GetLocalConnectingPositions),

                // Definition methods
                ["RegisterDefinitions"] =
                    new Func<byte[], string[]>(DefinitionHandler.I
                        .RegisterDefinitions), // Tries to register a new definition.
                ["UnregisterDefinition"] =
                    new Func<string, bool>(DefinitionHandler.I
                        .UnregisterDefinition), // Tries to de-register a definition.
                ["GetAllDefinitions"] =
                    new Func<string[]>(() =>
                        DefinitionHandler.I.ModularDefinitionsMap.Keys
                            .ToArray()), // Returns a list of all definition names.
                ["RegisterOnPartAdd"] =
                    new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.RegisterOnPartAdd),
                ["RegisterOnPartRemove"] =
                    new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.RegisterOnPartRemove),
                ["RegisterOnPartDestroy"] =
                    new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.RegisterOnPartDestroy),
                ["RegisterOnAssemblyClose"] = new Action<string, Action<int>>(DefinitionHandler.I.RegisterOnAssemblyClose),
                ["UnregisterOnAssemblyClose"] = new Action<string, Action<int>>(DefinitionHandler.I.UnregisterOnAssemblyClose),
                ["UnregisterOnPartAdd"] =
                    new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.UnregisterOnPartAdd),
                ["UnregisterOnPartRemove"] =
                    new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.UnregisterOnPartRemove),
                ["UnregisterOnPartDestroy"] =
                    new Action<string, Action<int, IMyCubeBlock, bool>>(DefinitionHandler.I.UnregisterOnPartDestroy),

                // Global methods
                ["IsDebug"] =
                    new Func<bool>(() =>
                        AssembliesSessionInit.DebugMode), // Returns whether debug mode is enabled or not.
                ["LogWriteLine"] =
                    new Action<string>(ModularLog.Log), // Writes a new line in the Modular Assemblies debug log.
                ["AddChatCommand"] =
                    new Action<string, string, Action<string[]>, string>(CommandHandler
                        .AddCommand), // Registers a command for Modular Assemblies' command handler.
                ["RemoveChatCommand"] =
                    new Action<string>(CommandHandler
                        .RemoveCommand) // Removes a command from Modular Assemblies' command handler.
            };
        }

        #region Global Assembly Methods

        private IMyCubeBlock[] GetAllParts()
        {
            var parts = new HashSet<IMyCubeBlock>();
            foreach (var gridLogic in AssemblyPartManager.I.AllGridLogics.Values)
            foreach (var definitionBlockSet in gridLogic.AllAssemblyParts.Values)
            foreach (var block in definitionBlockSet.Keys)
                if (block.FatBlock != null)
                    parts.Add(block.FatBlock);
            return parts.ToArray();
        }

        private int[] GetAllAssemblies()
        {
            return AssemblyPartManager.I.AllPhysicalAssemblies.Keys.ToArray();
        }

        #endregion

        #region Per-Assembly Methods

        private IMyCubeBlock[] GetMemberParts(int assemblyId)
        {
            PhysicalAssembly asm;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out asm))
                return Array.Empty<IMyCubeBlock>();

            var parts = new List<IMyCubeBlock>();
            foreach (var part in asm.ComponentParts)
                if (part.Key.FatBlock != null)
                    parts.Add(part.Key.FatBlock);

            return parts.ToArray();
        }

        private IMyCubeBlock GetBasePart(int assemblyId)
        {
            PhysicalAssembly wep;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out wep))
                return null;

            return null; //wep.basePart?.block?.FatBlock as IMyCubeBlock;
        }

        private IMyCubeGrid GetAssemblyGrid(int assemblyId)
        {
            PhysicalAssembly asm;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out asm))
                return null;

            return asm.Grid;
        }

        private void AddOnAssemblyClose(Action<int> action)
        {
            AssemblyPartManager.I.OnAssemblyClose += action;
        }

        private void RemoveOnAssemblyClose(Action<int> action)
        {
            AssemblyPartManager.I.OnAssemblyClose -= action;
        }

        private void RecreateAssembly(int assemblyId)
        {
            var assembly = AssemblyPartManager.I.AllPhysicalAssemblies.GetValueOrDefault(assemblyId, null);
            if (assembly == null)
                return;

            foreach (var part in assembly.ComponentParts.Values)
            {
                part.PartRemoved();
                AssemblyPartManager.I.QueueConnectionCheck(part);
            }
        }

        private object GetAssemblyProperty(int assemblyId, string propertyName)
        {
            var assembly = AssemblyPartManager.I.AllPhysicalAssemblies.GetValueOrDefault(assemblyId, null);

            return assembly?.GetProperty(propertyName);
        }

        private void SetAssemblyProperty(int assemblyId, string propertyName, object value)
        {
            var assembly = AssemblyPartManager.I.AllPhysicalAssemblies.GetValueOrDefault(assemblyId, null);

            assembly?.SetProperty(propertyName, value);
        }

        private string[] ListAssemblyProperties(int assemblyId)
        {
            var assembly = AssemblyPartManager.I.AllPhysicalAssemblies.GetValueOrDefault(assemblyId, null);
            if (assembly == null)
                return Array.Empty<string>();

            return assembly.Properties.Keys.ToArray();
        }

        #endregion

        #region Per-Part Methods

        private IMyCubeBlock[] GetConnectedBlocks(IMyCubeBlock block, string definitionName, bool useCached)
        {
            var definition = DefinitionHandler.TryGetDefinition(definitionName);
            if (definition == null)
                return Array.Empty<IMyCubeBlock>();

            GridAssemblyLogic gridLogic;
            if (!AssemblyPartManager.I.AllGridLogics.TryGetValue(block.CubeGrid, out gridLogic))
                return Array.Empty<IMyCubeBlock>();

            List<AssemblyPart> parts;
            if (!gridLogic.AllAssemblyParts[definition].TryGetValue(block.SlimBlock, out parts) || parts.Count == 0)
                return Array.Empty<IMyCubeBlock>();
            
            AssemblyPart part = parts[0];

            var conParts = new List<IMyCubeBlock>();
            if (useCached)
            {
                foreach (var conPart in part.ConnectedParts)
                    if (conPart.Block.FatBlock != null)
                        conParts.Add(conPart.Block.FatBlock);
            }
            else
            {
                return part.GetValidNeighbors(true).ToArray();
            }

            return conParts.ToArray();
        }

        private int GetContainingAssembly(IMyCubeBlock block, string definitionName)
        {
            var definition = DefinitionHandler.TryGetDefinition(definitionName);
            if (definition == null)
                return -1;

            GridAssemblyLogic gridLogic;
            if (!AssemblyPartManager.I.AllGridLogics.TryGetValue(block.CubeGrid, out gridLogic))
                return -1;

            List<AssemblyPart> parts;
            if (!gridLogic.AllAssemblyParts[definition].TryGetValue(block.SlimBlock, out parts) || parts.Count == 0)
                return -1;

            return parts[0].MemberAssembly?.AssemblyId ?? -1;
        }

        private void RecreateConnections(IMyCubeBlock block, string definitionName)
        {
            var definition = DefinitionHandler.TryGetDefinition(definitionName);
            if (definition == null)
                return;

            GridAssemblyLogic gridLogic;
            if (!AssemblyPartManager.I.AllGridLogics.TryGetValue(block.CubeGrid, out gridLogic))
                return;

            List<AssemblyPart> parts;
            if (!gridLogic.AllAssemblyParts[definition].TryGetValue(block.SlimBlock, out parts) || parts.Count == 0)
                return;
            
            AssemblyPart part = parts[0];

            part.PartRemoved();
            AssemblyPartManager.I.QueueConnectionCheck(part);
        }

        private IMyCubeBlock[] GetConnectedBlocksMulti(IMyCubeBlock block, int assemblyId, bool useCached)
        {
            PhysicalAssembly asm;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out asm))
                return Array.Empty<IMyCubeBlock>();

            AssemblyPart part;
            if (!asm.ComponentParts.TryGetValue(block.SlimBlock, out part))
                return Array.Empty<IMyCubeBlock>();

            var parts = new List<IMyCubeBlock>();
            if (useCached)
            {
                foreach (var conPart in part.ConnectedParts)
                    if (conPart.Block.FatBlock != null)
                        parts.Add(conPart.Block.FatBlock);
            }
            else
            {
                return part.GetValidNeighbors(true).ToArray();
            }

            return parts.ToArray();
        }

        private int[] GetContainingAssemblyMulti(IMyCubeBlock block, string definitionId)
        {
            var definition = DefinitionHandler.TryGetDefinition(definitionId);
            if (definition == null)
                return Array.Empty<int>();

            GridAssemblyLogic gridLogic;
            if (!AssemblyPartManager.I.AllGridLogics.TryGetValue(block.CubeGrid, out gridLogic))
                return Array.Empty<int>();

            List<AssemblyPart> parts;
            if (!gridLogic.AllAssemblyParts[definition].TryGetValue(block.SlimBlock, out parts))
                return Array.Empty<int>();

            int[] asmIds = new int[parts.Count];
            for (int i = 0; 0 < asmIds.Length; i++)
            {
                if (parts[i].MemberAssembly == null)
                    continue;
                asmIds[i] = parts[i].MemberAssembly.AssemblyId;
            }

            return asmIds;
        }

        private void RecreateConnectionsMulti(IMyCubeBlock block, int assemblyId)
        {
            PhysicalAssembly asm;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out asm))
                return;

            AssemblyPart part;
            if (!asm.ComponentParts.TryGetValue(block.SlimBlock, out part))
                return;

            part.PartRemoved();
            AssemblyPartManager.I.QueueConnectionCheck(part);
        }

        private Vector3I[] GetGridConnectingPositions(IMyCubeBlock block, string definitionName)
        {
            var definition = DefinitionHandler.TryGetDefinition(definitionName);

            if (definition == null)
                return Array.Empty<Vector3I>();

            // allowed connections are set
            Dictionary<Vector3I, string[]> allowedConns;
            if (definition.AllowedConnections.TryGetValue(block.BlockDefinition.SubtypeName, out allowedConns))
            {
                int i = 0;
                Vector3I[] gPoses = new Vector3I[allowedConns.Count];
                Matrix localOrientation = block.LocalMatrix.GetOrientation();
                foreach (var lPos in allowedConns.Keys)
                {
                    gPoses[i++] = (Vector3I) Vector3D.Rotate(lPos, localOrientation) + block.Position;
                }

                return gPoses;
            }

            return Array.Empty<Vector3I>();
        }

        private Vector3I[] GetLocalConnectingPositions(IMyCubeBlock block, string definitionName)
        {
            var definition = DefinitionHandler.TryGetDefinition(definitionName);
            if (definition == null)
                return Array.Empty<Vector3I>();

            Dictionary<Vector3I, string[]> allowedConns;
            if (definition.AllowedConnections.TryGetValue(block.BlockDefinition.SubtypeName, out allowedConns))
            {
                return allowedConns.Keys.ToArray();
            }

            return Array.Empty<Vector3I>();
        }

        #endregion
    }
}