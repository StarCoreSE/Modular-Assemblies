using Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugDraw;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Debug;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions.DefinitionDefs;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    public class ModularDefinition
    {

        public string[] AllowedBlocks = null;

        public Dictionary<string, Dictionary<Vector3I, string[]>> AllowedConnections = null;

        public string BaseBlockSubtype = null;
        public string Name = null;


        public static ModularDefinition Load(PhysicalDefinition definition)
        {
            ModularDefinition def = new ModularDefinition()
            {
                AllowedBlocks = definition.AllowedBlocks,
                AllowedConnections = definition.AllowedConnections,
                BaseBlockSubtype = definition.BaseBlock,
                Name = definition.Name,
            };
            
            if (def.AllowedBlocks == null || def.AllowedConnections == null || def.Name == null)
            {
                ModularLog.Log("Failed to create new ModularDefinition for " + definition.Name);
                MyAPIGateway.Utilities.ShowMessage("Modular Assemblies", "Failed to create new ModularDefinition for " + definition.Name);
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
                    Vector3I offsetAllowedPos = (Vector3I)Vector3D.Rotate(allowedPosKvp.Key, localOrientation) + block.Position;

                    // If list is empty OR block is not in whitelist, continue.
                    if (allowedPosKvp.Value?.Length == 0 || !(allowedPosKvp.Value?.Contains(adajent.BlockDefinition.Id.SubtypeName) ?? true))
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
            foreach (string id in AllowedBlocks)
                if (type == id)
                    return true;
            return false;
        }

        public bool IsBlockAllowed(IMySlimBlock block)
        {
            return IsTypeAllowed(block.BlockDefinition.Id.SubtypeName);
        }
    }
}
