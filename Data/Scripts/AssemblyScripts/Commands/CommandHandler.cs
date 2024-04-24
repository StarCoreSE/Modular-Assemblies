using System;
using System.Collections.Generic;
using System.Text;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugUtils;
using Sandbox.ModAPI;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    /// <summary>
    ///     Parses commands from chat and triggers relevant methods.
    /// </summary>
    public class CommandHandler
    {
        public static CommandHandler I;

        private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>
        {
            ["help"] = new Command(
                "Modular Assemblies",
                "Displays command help.",
                message => I.ShowHelp()),
            ["debug"] = new Command(
                "Modular Assemblies",
                "Toggles debug draw.",
                message => AssembliesSessionInit.DebugMode = !AssembliesSessionInit.DebugMode)
        };

        private CommandHandler()
        {
        }

        private void ShowHelp()
        {
            var helpBuilder = new StringBuilder();
            var modNames = new List<string>();
            foreach (var command in _commands.Values)
                if (!modNames.Contains(command.modName))
                    modNames.Add(command.modName);

            MyAPIGateway.Utilities.ShowMessage("Modular Assemblies Help", "");

            foreach (var modName in modNames)
            {
                foreach (var command in _commands)
                    if (command.Value.modName == modName)
                        helpBuilder.Append($"\n{{!md {command.Key}}}: " + command.Value.helpText);

                MyAPIGateway.Utilities.ShowMessage($"[{modName}]", helpBuilder + "\n");
                helpBuilder.Clear();
            }
        }

        public static void Init()
        {
            Close(); // Close existing command handlers.
            I = new CommandHandler();
            MyAPIGateway.Utilities.MessageEnteredSender += I.Command_MessageEnteredSender;
            MyAPIGateway.Utilities.ShowMessage($"Modular Assemblies v{AssembliesSessionInit.ModVersion.X}",
                "Chat commands registered - run \"!md help\" for help.");
        }

        public static void Close()
        {
            if (I != null)
            {
                MyAPIGateway.Utilities.MessageEnteredSender -= I.Command_MessageEnteredSender;
                I._commands.Clear();
            }

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

                var parts = messageText.Substring(4).Trim(' ').Split(' '); // Convert commands to be more parseable

                if (parts[0] == "")
                {
                    ShowHelp();
                    return;
                }

                // Really basic command handler
                if (_commands.ContainsKey(parts[0].ToLower()))
                    _commands[parts[0].ToLower()].action.Invoke(parts);
                else
                    MyAPIGateway.Utilities.ShowMessage("Modular Assemblies",
                        $"Unrecognized command \"{parts[0].ToLower()}\"");
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(CommandHandler));
            }
        }

        /// <summary>
        ///     Registers a command for Modular Assemblies' command handler.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="action"></param>
        /// <param name="modName"></param>
        public static void AddCommand(string command, string helpText, Action<string[]> action,
            string modName = "Modular Assemblies")
        {
            if (I == null)
                return;

            command = command.ToLower();
            if (I._commands.ContainsKey(command))
            {
                SoftHandle.RaiseException("Attempted to add duplicate command " + command + " from [" + modName + "]",
                    callingType: typeof(CommandHandler));
                return;
            }

            I._commands.Add(command, new Command(modName, helpText, action));
            ModularLog.Log($"Registered new chat command \"!{command}\" from [{modName}]");
        }

        /// <summary>
        ///     Removes a command from Modular Assemblies' command handler.
        /// </summary>
        /// <param name="command"></param>
        public static void RemoveCommand(string command)
        {
            command = command.ToLower();
            if (I == null || command == "help" || command == "debug") // Debug and Help should never be removed.
                return;
            if (I._commands.Remove(command))
                ModularLog.Log($"De-registered chat command \"!{command}\".");
        }

        private class Command
        {
            public readonly Action<string[]> action;
            public readonly string helpText;
            public readonly string modName;

            public Command(string modName, string helpText, Action<string[]> action)
            {
                this.modName = modName;
                this.helpText = helpText;
                this.action = action;
            }
        }
    }
}