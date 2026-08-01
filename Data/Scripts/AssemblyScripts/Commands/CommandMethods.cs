using Modular_Assemblies.AssemblyScripts.AssemblyComponents;
using Sandbox.ModAPI;

namespace Modular_Assemblies.AssemblyScripts.Commands
{
    /// <summary>
    ///     Stores methods for commands in CommandHandler.
    /// </summary>
    internal static class CommandMethods
    {
        public static void PollAssemblies(string[] args)
        {
            MyAPIGateway.Utilities.ShowMessage("Modular Assemblies", $"{AssemblyPartManager.I.AllPhysicalAssemblies.Count} assemblies found:");
            foreach (var assembly in AssemblyPartManager.I.AllPhysicalAssemblies)
            {
                MyAPIGateway.Utilities.ShowMessage($"[ID {assembly.Key}]", $"[DEFID {assembly.Value.AssemblyDefinition.Name}] {assembly.Value.ComponentParts.Count} parts ({(assembly.Value.IsClosing ? "Closing" : "Open")})");
            }
        }
    }
}