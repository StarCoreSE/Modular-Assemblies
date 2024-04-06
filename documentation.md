# Modular Assemblies Mod Documentation

## Assemblies_SessionInit Class

The `Assemblies_SessionInit` class is the main entry point for the Modular Assemblies mod. It initializes and manages the mod's components.

### Fields
- `I` (static): Instance of the `Assemblies_SessionInit` class.
- `AssemblyPartManager`: Instance of the `AssemblyPartManager` class.
- `DefinitionHandler`: Instance of the `DefinitionHandler` class.
- `DebugMode` (static): Flag indicating whether debug mode is enabled.
- `random`: Instance of the `Random` class for generating random values.

### Methods
- `LoadData()`: Loads the mod's data and initializes the `AssemblyPartManager` and `DefinitionHandler`. Registers the chat command handler.
- `UpdateAfterSimulation()`: Updates the `AssemblyPartManager` after each simulation step.
- `UnloadData()`: Unloads the mod's data and closes the `AssemblyPartManager` and `DefinitionHandler`. Unregisters the chat command handler.
- `ChatCommandHandler(ulong sender, string messageText, ref bool sendToOthers)`: Handles chat commands for the mod.

## AssemblyPart Class

The `AssemblyPart` class represents a single part of an assembly.

### Properties
- `Block`: The `IMySlimBlock` representing the block associated with the assembly part.
- `IsBaseBlock`: Flag indicating whether the part is a base block.
- `MemberAssembly`: The `PhysicalAssembly` to which the part belongs.
- `ConnectedParts`: HashSet of connected `AssemblyPart`s.
- `AssemblyDefinition`: The `ModularDefinition` associated with the assembly.
- `PrevAssemblyId`: The ID of the previous assembly the part belonged to.

### Constructor
- `AssemblyPart(IMySlimBlock block, ModularDefinition AssemblyDefinition)`: Initializes a new instance of the `AssemblyPart` class.

### Methods
- `DoConnectionCheck(bool cascadingUpdate = false, HashSet<AssemblyPart> visited = null)`: Performs a connection check for the assembly part.
- `PartRemoved()`: Handles the removal of the assembly part.
- `GetValidNeighbors(bool MustShareAssembly = false)`: Retrieves the valid neighboring blocks of the assembly part.
- `GetValidNeighborParts(bool MustShareAssembly = false)`: Retrieves the valid neighboring assembly parts.
- `GetAllConnectedParts(ref HashSet<AssemblyPart> connectedParts)`: Retrieves all connected assembly parts.

## AssemblyPartManager Class

The `AssemblyPartManager` class manages the assembly parts and physical assemblies.

### Fields
- `I` (static): Instance of the `AssemblyPartManager` class.
- `AllAssemblyParts`: Dictionary mapping `IMySlimBlock`s to `AssemblyPart`s.
- `AllPhysicalAssemblies`: Dictionary mapping assembly IDs to `PhysicalAssembly` instances.
- `CreatedPhysicalAssemblies`: Counter for created physical assemblies.
- `OnAssemblyClose`: Action invoked when an assembly is closed.

### Methods
- `QueueBlockAdd(IMySlimBlock block)`: Queues a block to be added.
- `QueueConnectionCheck(AssemblyPart part)`: Queues a connection check for an assembly part.
- `QueueAssemblyCheck(AssemblyPart part, PhysicalAssembly assembly)`: Queues an assembly check for an assembly part.
- `Init()`: Initializes the `AssemblyPartManager` and registers event handlers.
- `Unload()`: Unloads the `AssemblyPartManager` and unregisters event handlers.
- `UpdateAfterSimulation()`: Updates the `AssemblyPartManager` after each simulation step.

## ModularDefinition Class

The `ModularDefinition` class represents a modular assembly definition.

### Fields
- `AllowedBlocks`: Array of allowed block subtypes for the assembly.
- `AllowedConnections`: Dictionary defining the allowed connections between blocks.
- `BaseBlockSubtype`: The subtype of the base block for the assembly.
- `Name`: The name of the assembly definition.

### Methods
- `Load(PhysicalDefinition definition)` (static): Loads a `ModularDefinition` from a `PhysicalDefinition`.
- `DoesBlockConnect(IMySlimBlock block, IMySlimBlock adajent, bool lineCheck = true)`: Checks if two blocks can connect according to the assembly definition.
- `IsTypeAllowed(string type)`: Checks if a block type is allowed in the assembly.
- `IsBlockAllowed(IMySlimBlock block)`: Checks if a block is allowed in the assembly.

## PhysicalAssembly Class

The `PhysicalAssembly` class represents a physical instance of an assembly.

### Properties
- `BasePart`: The base part of the assembly.
- `ComponentParts`: List of component parts in the assembly.
- `AssemblyDefinition`: The `ModularDefinition` associated with the assembly.
- `AssemblyId`: The unique ID of the assembly.
- `IsClosing`: Flag indicating whether the assembly is being closed.

### Methods
- `Update()`: Updates the assembly.
- `PhysicalAssembly(int id, AssemblyPart basePart, ModularDefinition AssemblyDefinition)`: Initializes a new instance of the `PhysicalAssembly` class.
- `AddPart(AssemblyPart part)`: Adds a part to the assembly.
- `RemovePart(AssemblyPart part)`: Removes a part from the assembly.
- `Close()`: Closes the assembly.
- `MergeWith(PhysicalAssembly assembly)`: Merges the assembly with another assembly.

## DebugDrawManager Class

The `DebugDrawManager` class manages the debug drawing functionality for the mod.

### Methods
- `AddPoint(Vector3D globalPos, Color color, float duration)` (static): Adds a debug point at the specified position.
- `AddGPS(string name, Vector3D position, float duration)` (static): Adds a debug GPS marker at the specified position.
- `AddGridGPS(string name, Vector3I gridPosition, IMyCubeGrid grid, float duration)` (static): Adds a debug GPS marker at the specified grid position.
- `AddGridPoint(Vector3I blockPos, IMyCubeGrid grid, Color color, float duration)` (static): Adds a debug point at the specified grid position.
- `AddLine(Vector3D origin, Vector3D destination, Color color, float duration)` (static): Adds a debug line between the specified positions.
- `Draw()`: Draws the debug elements.

## ApiDefinitions Class

The `ApiDefinitions` class defines the API methods exposed by the mod.

### Constructor
- `ApiDefinitions()`: Initializes a new instance of the `ApiDefinitions` class and populates the `ModApiMethods` dictionary.

### Methods
- `IsDebug()`: Checks if debug mode is enabled.
- `GetAllParts()`: Retrieves all assembly parts.
- `GetAllAssemblies()`: Retrieves all physical assemblies.
- `GetMemberParts(int assemblyId)`: Retrieves the member parts of a specific assembly.
- `GetConnectedBlocks(MyEntity blockEntity, bool useCached)`: Retrieves the connected blocks of a block entity.
- `GetBasePart(int assemblyId)`: Retrieves the base part of a specific assembly.
- `GetContainingAssembly(MyEntity blockEntity)`: Retrieves the containing assembly of a block entity.
- `GetAssemblyGrid(int assemblyId)`: Retrieves the grid of a specific assembly.
- `AddOnAssemblyClose(Action<int> action)`: Adds an action to be invoked when an assembly is closed.
- `RemoveOnAssemblyClose(Action<int> action)`: Removes an action from being invoked when an assembly is closed.

## ApiHandler Class

The `ApiHandler` class handles the API communication between the mod and other mods.

### Properties
- `IsReady`: Flag indicating whether the API handler is ready.

### Methods
- `HandleMessage(object o)`: Handles incoming messages from other mods.
- `LoadData()`: Loads the API handler and registers message handlers.
- `UnloadData()`: Unloads the API handler and unregisters message handlers.

## DefinitionDefs Class

The `DefinitionDefs` class contains definitions for serializable data structures used by the mod.

### DefinitionContainer Class
- Represents a container for `PhysicalDefinition` objects.

### PhysicalDefinition Class
- Represents a physical definition for an assembly.

### FunctionCall Class
- Represents a function call from another mod.

### SerializedObjectArray Class
- Represents a serializable array of objects.

## DefinitionHandler Class

The `DefinitionHandler` class handles the loading and processing of assembly definitions.

### Fields
- `I` (static): Instance of the `DefinitionHandler` class.
- `ModularDefinitions`: List of loaded `ModularDefinition` objects.

### Methods
- `Init()`: Initializes the `DefinitionHandler` and registers message handlers.
- `Unload()`: Unloads the `DefinitionHandler` and unregisters message handlers.
- `DefMessageHandler(object o)`: Handles incoming definition messages.
- `ActionMessageHandler(object o)`: Handles incoming action messages.
- `SendOnPartAdd(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)`: Sends an "OnPartAdd" event to other mods.
- `SendOnPartRemove(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)`: Sends an "OnPartRemove" event to other mods.
- `SendOnPartDestroy(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)`: Sends an "OnPartDestroy" event to other mods.
- `CheckValidDefinitions()`: Checks the validity of loaded definitions.
- `CheckDefinitionValid(ModularDefinition modDef, List<string> validSubtypes)`: Checks the validity of a specific `ModularDefinition`.