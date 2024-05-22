using System;
using System.Collections.Generic;
using System.Linq;
using Modular_Assemblies.AssemblyScripts.DebugUtils;
using Modular_Assemblies.AssemblyScripts.Definitions;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace Modular_Assemblies.AssemblyScripts.AssemblyComponents
{
    internal static class AssemblySerializer
    {
        public static byte[] SerializeGrid(IMyCubeGrid grid)
        {
            return MyAPIGateway.Utilities.SerializeToBinary(new GridAssemblyStorage(grid, false));
        }

        public static AssemblyStorage[] DeserializeGrid(byte[] serialized)
        {
            try
            {
                var gridStorage =
                    MyAPIGateway.Utilities.SerializeFromBinary<GridAssemblyStorage>(serialized);

                return gridStorage?.AllAssemblies;
            }
            catch (Exception ex)
            {
                ModularLog.LogException(ex, typeof(AssemblySerializer));
            }

            return null;
        }

        public static Dictionary<string, object> DeserializeAssembly(byte[] serialized)
        {
            try
            {
                var storage = MyAPIGateway.Utilities.SerializeFromBinary<AssemblyStorage>(serialized);

                if (storage == null)
                    throw new Exception("Null assembly storage!");

                return storage.AssemblyProperties();
            }
            catch (Exception ex)
            {
                ModularLog.LogException(ex, typeof(AssemblySerializer));
            }

            return null;
        }

        /// <summary>
        ///     Stores properties of all assemblies on a grid.
        /// </summary>
        [ProtoContract]
        public class GridAssemblyStorage
        {
            /// <summary>
            ///     List of all assembly properties.
            /// </summary>
            [ProtoMember(1)] public AssemblyStorage[] AllAssemblies;


            private GridAssemblyStorage()
            {
            }

            public GridAssemblyStorage(IMyCubeGrid grid, bool useEntityIds)
            {
                // Populate _allDefinitionNames
                var allDefinitionNames = new List<string>(DefinitionHandler.I.ModularDefinitions.Count);
                foreach (var definition in DefinitionHandler.I.ModularDefinitions)
                    allDefinitionNames.Add(definition.Name);

                var allAssemblies = new List<AssemblyStorage>();

                foreach (var assembly in AssemblyPartManager.I.AllPhysicalAssemblies.Values.Where(assembly =>
                             assembly.ComponentParts[0].Block.CubeGrid == grid))
                    allAssemblies.Add(new AssemblyStorage(assembly, useEntityIds));

                AllAssemblies = allAssemblies.ToArray();
            }

            /// <summary>
            ///     Returns a Dictionary(definitionName, AssemblyStorage) of all grid storages.
            /// </summary>
            /// <returns></returns>
            public Dictionary<string, List<AssemblyStorage>> AssemblyStorageMap()
            {
                var toReturn = new Dictionary<string, List<AssemblyStorage>>();
                foreach (var storage in AllAssemblies)
                {
                    if (!toReturn.ContainsKey(storage.DefinitionName))
                        toReturn.Add(storage.DefinitionName, new List<AssemblyStorage>());
                    toReturn[storage.DefinitionName].Add(storage);
                }

                return toReturn;
            }
        }

        /// <summary>
        ///     Stores properties of a single Assembly.
        /// </summary>
        [ProtoContract]
        public class AssemblyStorage
        {
            [ProtoMember(1)] public long[] BlockEntityIds;
            [ProtoMember(3)] public Vector3I[] BlockPositions;
            [ProtoMember(2)] public string DefinitionName;

            [ProtoMember(11)] private Dictionary<string, byte[]> _byteProperties;
            [ProtoMember(12)] private Dictionary<string, string> _stringProperties;
            [ProtoMember(13)] private Dictionary<string, bool> _boolProperties;
            [ProtoMember(14)] private Dictionary<string, int> _intProperties;
            [ProtoMember(15)] private Dictionary<string, short> _shortProperties;
            [ProtoMember(16)] private Dictionary<string, float> _floatProperties;
            [ProtoMember(17)] private Dictionary<string, double> _doubleProperties;
            [ProtoMember(18)] private Dictionary<string, long> _longProperties;

            private AssemblyStorage()
            {
            }

            public AssemblyStorage(PhysicalAssembly assembly, bool useEntityIds)
            {
                if (assembly == null)
                    throw new Exception("Null assembly reference!");

                var assemblyPartEntityIds = new List<long>();

                var componentParts = assembly.ComponentParts;
                if (componentParts == null)
                    return;
                foreach (var part in componentParts) assemblyPartEntityIds.Add(part.Block.FatBlock.EntityId);

                DefinitionName = assembly.AssemblyDefinition.Name + "";

                if (useEntityIds)
                {
                    BlockEntityIds = assemblyPartEntityIds.ToArray();
                }
                else
                {
                    BlockPositions = new Vector3I[componentParts.Length];
                    for (var i = 0; i < BlockPositions.Length; i++)
                        BlockPositions[i] = componentParts[i].Block.Position;
                }

                Populate(assembly.Properties);
            }

            public AssemblyStorage(string definitionName, long[] allBlockIds,
                Dictionary<string, object> assemblyProperties)
            {
                DefinitionName = definitionName;
                BlockEntityIds = allBlockIds;
                Populate(assemblyProperties);
            }

            public Dictionary<string, object> AssemblyProperties()
            {
                var properties = new Dictionary<string, object>();

                properties.Clear();
                if (_byteProperties != null)
                    foreach (var kvp in _byteProperties)
                        properties.Add(kvp.Key, kvp.Value);
                if (_stringProperties != null)
                    foreach (var kvp in _stringProperties)
                        properties.Add(kvp.Key, kvp.Value);
                if (_boolProperties != null)
                    foreach (var kvp in _boolProperties)
                        properties.Add(kvp.Key, kvp.Value);
                if (_intProperties != null)
                    foreach (var kvp in _intProperties)
                        properties.Add(kvp.Key, kvp.Value);
                if (_shortProperties != null)
                    foreach (var kvp in _shortProperties)
                        properties.Add(kvp.Key, kvp.Value);
                if (_floatProperties != null)
                    foreach (var kvp in _floatProperties)
                        properties.Add(kvp.Key, kvp.Value);
                if (_doubleProperties != null)
                    foreach (var kvp in _doubleProperties)
                        properties.Add(kvp.Key, kvp.Value);
                if (_longProperties != null)
                    foreach (var kvp in _longProperties)
                        properties.Add(kvp.Key, kvp.Value);

                return properties;
            }

            private void Populate(Dictionary<string, object> assemblyProperties)
            {
                _byteProperties = new Dictionary<string, byte[]>();
                _stringProperties = new Dictionary<string, string>();
                _boolProperties = new Dictionary<string, bool>();
                _intProperties = new Dictionary<string, int>();
                _shortProperties = new Dictionary<string, short>();
                _floatProperties = new Dictionary<string, float>();
                _doubleProperties = new Dictionary<string, double>();
                _longProperties = new Dictionary<string, long>();

                foreach (var kvp in assemblyProperties)
                    // This is awful, please let Aristeas know if there's a better way.
                    if (kvp.Value is byte[])
                        _byteProperties[kvp.Key] = (byte[])kvp.Value;
                    else if (kvp.Value is string)
                        _stringProperties[kvp.Key] = (string)kvp.Value;
                    else if (kvp.Value is bool)
                        _boolProperties[kvp.Key] = (bool)kvp.Value;
                    else if (kvp.Value is int)
                        _intProperties[kvp.Key] = (int)kvp.Value;
                    else if (kvp.Value is short)
                        _shortProperties[kvp.Key] = (short)kvp.Value;
                    else if (kvp.Value is float)
                        _floatProperties[kvp.Key] = (float)kvp.Value;
                    else if (kvp.Value is double)
                        _doubleProperties[kvp.Key] = (double)kvp.Value;
                    else if (kvp.Value is long)
                        _longProperties[kvp.Key] = (long)kvp.Value;
            }

            public bool IsBlockValid(IMyCubeBlock block)
            {
                return BlockEntityIds?.Contains(block.EntityId) ?? BlockPositions?.Contains(block.Position) ?? false;
            }
        }
    }
}