﻿using System;
using System.Collections.Generic;
using Modular_Assemblies.AssemblyScripts.DebugUtils;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using static Modular_Assemblies.AssemblyScripts.Definitions.DefinitionDefs;

namespace Modular_Assemblies.AssemblyScripts
{
    public class ModularDefinition
    {
        public string[] AllowedBlocks;

        public Dictionary<string, Dictionary<Vector3I, string[]>> AllowedConnections;

        public string BaseBlockSubtype;
        public string Name;

        public Action<int, IMyCubeBlock, bool> OnPartAdd;
        public Action<int, IMyCubeBlock, bool> OnPartDestroy;
        public Action<int, IMyCubeBlock, bool> OnPartRemove;
        public Action<int> OnAssemblyClose;


        public static ModularDefinition Load(ModularPhysicalDefinition definition)
        {
            var def = new ModularDefinition
            {
                AllowedBlocks = definition.AllowedBlockSubtypes,
                AllowedConnections = definition.AllowedConnections ??
                                     new Dictionary<string, Dictionary<Vector3I, string[]>>(),
                BaseBlockSubtype = definition.BaseBlockSubtype,
                Name = definition.Name
            };

            if (def.AllowedBlocks == null || string.IsNullOrEmpty(def.Name))
            {
                var msg = $"Failed to create new ModularDefinition for {definition.Name}!";
                if (def.AllowedBlocks == null)
                    msg += "\nAllowedBlocks is null or empty!";
                if (string.IsNullOrEmpty(def.Name))
                    msg += "\nName is null or empty!";

                ModularLog.Log(msg);
                MyAPIGateway.Utilities.ShowMessage("Modular Assemblies",
                    msg);
                return null;
            }

            ModularLog.Log("Created new ModularDefinition for " + definition.Name);
            return def;
        }

        public bool DoesBlockConnect(IMySlimBlock block, IMySlimBlock adajent, bool lineCheck = true)
        {
            // Check if adjacent block connects first, but don't make an infinite loop
            if (lineCheck)
                if (!DoesBlockConnect(adajent, block, false))
                    return false;

            // Get local offset for below
            Matrix localOrientation;
            block.Orientation.GetMatrix(out localOrientation);

            Dictionary<Vector3I, string[]> connection;
            if (AllowedConnections.TryGetValue(block.BlockDefinition.Id.SubtypeName, out connection))
            {
                foreach (var allowedPosKvp in connection)
                {
                    var offsetAllowedPos =
                        (Vector3I)Vector3D.Rotate(allowedPosKvp.Key, localOrientation) + block.Position;

                    // If list is empty OR block is not in whitelist, continue.
                    if (allowedPosKvp.Value?.Length == 0 ||
                        !(allowedPosKvp.Value?.Contains(adajent.BlockDefinition.Id.SubtypeName) ?? true))
                    {
                        if (AssembliesSessionInit.DebugMode)
                            DebugDrawManager.AddGridPoint(offsetAllowedPos, block.CubeGrid, Color.Red, 3);
                        continue;
                    }

                    if (offsetAllowedPos.IsInsideInclusiveEnd(adajent.Min, adajent.Max))
                    {
                        if (AssembliesSessionInit.DebugMode)
                            DebugDrawManager.AddGridPoint(offsetAllowedPos, block.CubeGrid, Color.Green, 3);
                        return true;
                    }

                    if (AssembliesSessionInit.DebugMode)
                        DebugDrawManager.AddGridPoint(offsetAllowedPos, block.CubeGrid, Color.Red, 3);
                }

                return false;
            }

            // Return true by default.
            return true;
        }

        public bool IsTypeAllowed(string type)
        {
            return AllowedBlocks.Contains(type);
        }

        public bool IsBlockAllowed(IMySlimBlock block)
        {
            return IsTypeAllowed(block.BlockDefinition.Id.SubtypeName);
        }
    }
}