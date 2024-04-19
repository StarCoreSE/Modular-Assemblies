using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Debug;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    public class CommandHandler
    {
        public static CommandHandler I;

        private Dictionary<string, Command> commands = new Dictionary<string, Command>()
        {
            ["help"] = new Command("ModularAssemblies", "Displays command help.", message => I.ShowHelp()),
            ["debug"] = new Command("ModularAssemblies", "Toggles debug draw.", message => AssembliesSessionInit.DebugMode = !AssembliesSessionInit.DebugMode;),
        };

        private void ShowHelp()
        {
            StringBuilder helpBuilder = new StringBuilder();
            List<string> modNames = new List<string>();
            foreach (var command in commands.Values)
                if (!modNames.Contains(command.modName))
                    modNames.Add(command.modName);

            MyAPIGateway.Utilities.ShowMessage("Modular Assemblies Help", "");

            foreach (var modName in modNames)
            {
                foreach (var command in commands)
                    if (command.Value.modName == modName)
                        helpBuilder.Append($"\n{{!md {command.Key}}}: " + command.Value.helpText);

                MyAPIGateway.Utilities.ShowMessage($"[{modName}]", helpBuilder + "\n");
                helpBuilder.Clear();
            }
        }

        private CommandHandler()
        {
        }

        public static void Init()
        {
            Close(); // Close existing command handlers.
            I = new CommandHandler();
            MyAPIGateway.Utilities.MessageEnteredSender += I.Command_MessageEnteredSender;
            MyAPIGateway.Utilities.ShowMessage($"Modular Assemblies v{ApiHandler.ModVersion.X}", "Chat commands registered - run \"!md help\" for help.");
        }

        public static void Close()
        {
            if (I != null)
                MyAPIGateway.Utilities.MessageEnteredSender -= I.Command_MessageEnteredSender;
            I = null;
        }

        private void Command_MessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
        {
            try
            {
                // Only register for commands
                if (messageText.Length == 0 || !messageText.ToLower().StartsWith("!md"))
                    return;

                sendToOthers = false;

                string[] parts = messageText.Substring(4).Trim(' ').Split(' '); // Convert commands to be more parseable

                if (parts[0] == "")
                {
                    ShowHelp();
                    return;
                }

                // Really basic command handler
                if (commands.ContainsKey(parts[0].ToLower()))
                    commands[parts[0].ToLower()].action.Invoke(parts);
                else
                    MyAPIGateway.Utilities.ShowMessage("Modular Assemblies", $"Unrecognized command \"{messageText}\" ({sender})");
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(CommandHandler));
            }
        }

        /// <summary>
        /// Registers a command for Modular Assemblies' command handler.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="action"></param>
        /// <param name="modName"></param>
        public static void AddCommand(string command, string helpText, Action<string[]> action, string modName = "Modular Assemblies")
        {
            if (I == null)
                return;

            if (I.commands.ContainsKey(command))
            {
                SoftHandle.RaiseException("Attempted to add duplicate command " + command + " from [" + modName + "]", callingType: typeof(CommandHandler));
                return;
            }

            I.commands.Add(command, new Command(modName, helpText, action));
            ModularLog.Log($"Registered new chat command \"!{command}\" from [{modName}]");
        }

        private class Command
        {
            public string modName;
            public string helpText;
            public Action<string[]> action;

            public Command(string modName, string helpText, Action<string[]> action)
            {
                this.modName = modName;
                this.helpText = helpText;
                this.action = action;
            }
        }
    }
}
