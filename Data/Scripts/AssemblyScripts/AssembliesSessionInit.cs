using Modular_Assemblies.AssemblyScripts.AssemblyComponents;
using Modular_Assemblies.AssemblyScripts.Commands;
using Modular_Assemblies.AssemblyScripts.DebugUtils;
using Modular_Assemblies.AssemblyScripts.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageMath;
using static VRage.Game.MyObjectBuilder_BehaviorTreeDecoratorNode;

namespace Modular_Assemblies.AssemblyScripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class AssembliesSessionInit : MySessionComponentBase
    {
        public static readonly Vector2I ModVersion = new Vector2I(2, 3); // Mod version, API version
        public static readonly Guid ModStorageGuid = new Guid("ba5bfb16-27fe-4f83-8e1b-9fe04ed919d8");

        public static AssembliesSessionInit I;
        public static bool DebugMode = false;
        public static bool IsSessionInited;
        private readonly AssemblyPartManager AssemblyPartManager = new AssemblyPartManager();
        private readonly DefinitionHandler DefinitionHandler = new DefinitionHandler();
        public Random Random = new Random();

        private Dictionary<MyCubeGrid, GridAssemblyLogic> GridAssemblyLogics = new Dictionary<MyCubeGrid, GridAssemblyLogic>();
        private List<GridAssemblyLogic> GridAssemblyLogicsList = new List<GridAssemblyLogic>();

        #region Base Methods

        public override void LoadData()
        {
            var watch = Stopwatch.StartNew();
            IsSessionInited = false;

            I = this;
            ModularLog.Init();

            AssemblyPartManager.Init();
            DefinitionHandler.Init();

            CommandHandler.Init();

            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd; // water mod breaks grid gamelogiccomponents
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
            MyAPIGateway.Entities.GetEntities(null, e =>
            {
                OnEntityAdd(e);
                return false;
            });

            watch.Stop();
            ModularLog.Log($"Fully initialized in {watch.ElapsedMilliseconds}ms.");
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                IsSessionInited = true;
                AssemblyPartManager.UpdateAfterSimulation();

                if (MyAPIGateway.Session.GameplayFrameCounter % 127 == 0) // every ~2 seconds, prime number to spread load
                {
                    foreach (var logic in GridAssemblyLogicsList)
                    {
                        logic.UpdateSlow();
                    }
                }
            }
            catch (Exception e)
            {
                ModularLog.Log("Handled exception in Modular Assemblies!\n" + e);
            }
        }

        protected override void UnloadData()
        {
            var watch = Stopwatch.StartNew();

            ModularLog.Log("=================================\n          Unload started...\n");

            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;

            CommandHandler.Close();

            AssemblyPartManager.Unload();
            DefinitionHandler.Unload();

            I = null;
            watch.Stop();
            ModularLog.Log($"Finished unloading in {watch.ElapsedMilliseconds}ms.");
            ModularLog.Close();
        }

        #endregion

        private void OnEntityAdd(IMyEntity e)
        {
            MyCubeGrid g = e as MyCubeGrid;
            if (g == null || GridAssemblyLogics.ContainsKey(g))
                return;

            GridAssemblyLogic l = new GridAssemblyLogic();
            GridAssemblyLogics.Add(g, l);
            GridAssemblyLogicsList.Add(l);
            l.UpdateOnceBeforeFrame(g);
        }

        private void OnEntityRemove(IMyEntity e)
        {
            MyCubeGrid g = e as MyCubeGrid;
            GridAssemblyLogic l;
            if (g == null || !GridAssemblyLogics.TryGetValue(g, out l))
                return;

            l.Close();
            GridAssemblyLogics.Remove(g);
            GridAssemblyLogicsList.Remove(l);
        }
    }
}