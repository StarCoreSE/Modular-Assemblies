using Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Debug;
using VRage.Game.Components;
using VRage.Utils;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class AssembliesSessionInit : MySessionComponentBase
    {
        public static AssembliesSessionInit I;
        AssemblyPartManager AssemblyPartManager = new AssemblyPartManager();
        DefinitionHandler DefinitionHandler = new DefinitionHandler();
        public static bool DebugMode = false;
        public Random Random = new Random();

        #region Base Methods

        public override void LoadData()
        {
            Stopwatch watch = Stopwatch.StartNew();

            I = this;
            ModularLog.Init();

            AssemblyPartManager.Init();
            DefinitionHandler.Init();

            MyAPIGateway.Utilities.ShowMessage($"Modular Assemblies v{ApiHandler.ModVersion.X}", "Run !mwHelp for commands.");
            MyAPIGateway.Utilities.MessageEnteredSender += ChatCommandHandler;

            watch.Stop();
            ModularLog.Log($"Fully initialized in {watch.ElapsedMilliseconds}ms.");
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                AssemblyPartManager.UpdateAfterSimulation();
            }
            catch (Exception e)
            {
                ModularLog.Log("Handled exception in Modular Assemblies!\n" + e);
            }
        }

        protected override void UnloadData()
        {
            Stopwatch watch = Stopwatch.StartNew();

            ModularLog.Log("\n===========================================\nUnload started...");

            MyAPIGateway.Utilities.MessageEnteredSender -= ChatCommandHandler;

            AssemblyPartManager.Unload();
            DefinitionHandler.Unload();

            I = null;
            watch.Stop();
            ModularLog.Log($"Finished unloading in {watch.ElapsedMilliseconds}ms.");
        }

        #endregion

        private void ChatCommandHandler(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("!"))
                return;

            string[] split = messageText.Split(' ');
            switch (split[0].ToLower())
            {
                case "!mwhelp":
                    MyAPIGateway.Utilities.ShowMessage("Modular Assemblies", "Commands:\n!mwHelp - Prints all commands\n!mwDebug - Toggles debug draw");
                    sendToOthers = false;
                    break;
                case "!mwdebug":
                    DebugMode = !DebugMode;
                    sendToOthers = false;
                    break;
            }
        }
    }
}
