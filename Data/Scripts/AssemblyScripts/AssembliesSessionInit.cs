using System;
using System.Diagnostics;
using Modular_Assemblies.AssemblyScripts.AssemblyComponents;
using Modular_Assemblies.AssemblyScripts.Commands;
using Modular_Assemblies.AssemblyScripts.DebugUtils;
using Modular_Assemblies.AssemblyScripts.Definitions;
using VRage.Game.Components;
using VRageMath;

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

            watch.Stop();
            ModularLog.Log($"Fully initialized in {watch.ElapsedMilliseconds}ms.");
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                IsSessionInited = true;
                AssemblyPartManager.UpdateAfterSimulation();
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

            CommandHandler.Close();

            AssemblyPartManager.Unload();
            DefinitionHandler.Unload();

            I = null;
            watch.Stop();
            ModularLog.Log($"Finished unloading in {watch.ElapsedMilliseconds}ms.");
            ModularLog.Close();
        }

        #endregion
    }
}