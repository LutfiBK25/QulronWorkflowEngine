# Qulron Workflow Execution Engine - Architecture & Codebase Guide

## Table of Contents
1. [Project Overview](#project-overview)
2. [System Architecture](#system-architecture)
3. [Domain Layer](#domain-layer)
4. [Infrastructure Layer](#infrastructure-layer)
5. [Execution Flow](#execution-flow)
6. [Key Concepts](#key-concepts)
7. [Module Types Reference](#module-types-reference)
8. [Design Decisions](#design-decisions)

---

## Project Overview

**Qulron** is a **Workflow Execution Engine** for Warehouse Management Systems (WMS).

### Core Purpose
- Execute process workflows defined in a database
- Support multiple client types: Web (React), Android Scanners, Console Scanners
- Enable dynamic workflow composition without code changes
- Manage session state and user interactions

### Tech Stack
- **.NET 10** runtime
- **C# 14** language features
- **PostgreSQL** for data persistence
- **Redis** for session caching (optional) (not implmented yet)
- **EF Core** for ORM

### Key Features
✅ Database-driven workflow execution  
✅ Multi-step process orchestration  
✅ Real-time dialog/screen rendering  
✅ Dynamic field calculations  
✅ Conditional branching  
✅ Database operation integration  
✅ Pause/resume execution for user input  
✅ Deep call stacking (recursive process calls)  

---

## System Architecture

### High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│  QULRON WORKFLOW EXECUTION ENGINE                       │
│  (ProcessExecutor + Specialized Executors)              │
└─────────────────────────────────────────────────────────┘
         │              │              │
         ▼              ▼              ▼
    ┌─────────┐   ┌─────────┐   ┌──────────┐
    │ DialogEx│   │ CalcEx  │   │ CompareEx│
    │ecutor   │   │ecutor   │   │ecutor    │
    └─────────┘   └─────────┘   └──────────┘
         │              │              │
         └──────────────┬──────────────┘
                        │
         ┌──────────────┼──────────────┐
         ▼              ▼              ▼
    ┌─────────┐   ┌─────────┐   ┌─────────┐
    │   DB    │   │ Module  │   │Execution│
    │Executor │   │ Cache   │   │Session  │
    └─────────┘   └─────────┘   └─────────┘
         │              │              │
         └──────────────┼──────────────┘
                        │
         ┌──────────────┴──────────────┐
         ▼                             ▼
    ┌────────────┐           ┌────────────┐
    │ Repository │           │Device/User │
    │  Database  │           │ Management │
    └────────────┘           └────────────┘
```

### Execution Flow Overview

```
Device Request
    │
    ▼
ProcessExecutor.ExecuteAsync()
    │
    ├─→ Load Process Module from Cache
    ├─→ Push Execution Frame (call stack)
    │
    ▼
ExecuteStepsFromSequenceAsync()
    │
    ├─→ Get Step (sequence N)
    ├─→ Skip if commented
    │
    ▼
ExecuteStepAsync()
    │
    ├─→ Switch on ActionType
    │   ├─→ Dialog → DialogExecutor (PAUSES)
    │   ├─→ Calculate → CalculateExecutor
    │   ├─→ Compare → CompareExecutor
    │   ├─→ Database → DatabaseExecutor
    │   ├─→ Call → ExecuteAsync (recursion)
    │   └─→ Return Pass/Fail → End
    │
    ▼
ResolveNextSequence()
    │
    ├─→ NEXT (default increment)
    ├─→ PREV (go back)
    └─→ Label (jump to label)
    │
    └─→ Continue or Return
```

---

## Domain Layer

The domain layer defines the data models and enums that represent your workflow concepts.

### Core Entities

#### **Application**
```csharp
public class Application
{
    public Guid Id { get; set; }                    // Unique identifier
    public string Name { get; set; }                // App name
    public string Version { get; set; }             // Semantic version
    public string VersionBuild { get; set; }        // Build number
    public DateTime? LastCompiled { get; set; }     // Compilation timestamp
    public DateTime? LastActivated { get; set; }    // Last deployment
    public DateTime? CreatedDate { get; set; }      // Creation timestamp
    public DateTime? ModifiedDate { get; set; }     // Last modification
    public List<Module> Modules { get; set; }       // All modules in app
}
```

#### **Module** (Base Class)
```csharp
public class Module
{
    public Guid Id { get; set; }                    // Unique identifier
    public Guid ApplicationId { get; set; }         // Parent application
    public ModuleType ModuleType { get; set; }      // Module type
    public int Version { get; set; }                // Module version
    public string Name { get; set; }                // Human-readable name
    public string? Description { get; set; }        // Optional description
    public string? LockedBy { get; set; }           // User editing lock
    public DateTime CreatedDate { get; set; }       // Created when
    public DateTime ModifiedDate { get; set; }      // Last modified when
    public Application Application { get; set; }    // Navigation
}
```

---

### Module Types Reference

| Type | Value | Purpose | Entity Class |
|------|-------|---------|--------------|
| **Application** | 0 | Container | N/A |
| **ProcessModule** | 1 | Workflow/process | `ProcessModule` |
| **CalculateAction** | 2 | Multi-step math | `CalculateActionModule` |
| **CompareAction** | 3 | Field comparison | `CompareActionModule` |
| **DatabaseAction** | 4 | SQL/stored proc | `DatabaseActionModule` |
| **FieldModule** | 5 | Data storage | `FieldModule` |
| **ScreenFormatModule** | 6 | UI layout | `ScreenFormatModule` |
| **DialogAction** | 7 | User prompt | `DialogActionModule` |
| **SoftKeyModule** | 8 | Function keys | `SoftKeyModule` |

---

### Process Modules

#### **ProcessModule**
```csharp
public class ProcessModule
{
    public Guid ModuleId { get; set; }              // Reference to Module
    public string Subtype { get; set; }             // "Standard", "Remote", etc.
    public bool Remote { get; set; }                // Executes on remote system?
    public bool DynamicCall { get; set; }           // Called dynamically?
    public string? Comment { get; set; }            // Documentation
    public Module Module { get; set; }              // Navigation
    public List<ProcessModuleDetail> Details { get; set; } // Steps
}
```

#### **ProcessModuleDetail** (A Step)
```csharp
public class ProcessModuleDetail
{
    public Guid Id { get; set; }                    // Step ID
    public Guid ProcessModuleId { get; set; }       // Parent process
    public int Sequence { get; set; }               // Step number
    public string? LabelName { get; set; }          // Step name
    public ActionType? ActionType { get; set; }     // What to do
    public Guid? ActionId { get; set; }             // Which action
    public ModuleType? ActionModuleType { get; set; } // Action type
    public string PassLabel { get; set; }           // Jump on success
    public string FailLabel { get; set; }           // Jump on failure
    public bool CommentedFlag { get; set; }         // Skip step?
    public string? Comment { get; set; }            // Documentation
    public DateTime CreatedDate { get; set; }       // Created when
    public ProcessModule ProcessModule { get; set; } // Navigation
}
```

---

### Action Types

| Type | Value | Purpose |
|------|-------|---------|
| **Call** | 1 | Call another process |
| **ReturnPass** | 2 | End with success |
| **ReturnFail** | 3 | End with failure |
| **DatabaseExecute** | 4 | Run SQL/stored proc |
| **Dialog** | 5 | Display screen (PAUSES) |
| **Calculate** | 6 | Multi-step math |
| **Compare** | 7 | Compare values |

---

### Data Storage Modules

#### **FieldModule**
```csharp
public class FieldModule
{
    public Guid ModuleId { get; set; }              // Reference to Module
    public FieldType FieldType { get; set; }        // String, Integer, Boolean, DateTime
    public string? DefaultValue { get; set; }       // Default value
    public Module Module { get; set; }              // Navigation
}
```

#### **DatabaseActionModule**
```csharp
public class DatabaseActionModule
{
    public Guid ModuleId { get; set; }              // Reference to Module
    public string Statement { get; set; }           // SQL/stored proc with substitutions
    public Module Module { get; set; }              // Navigation
}
```

**Substitution Pattern:** `::#ModuleType#FieldModuleId#::`

---

### Calculation Modules

#### **CalculateActionModule**
```csharp
public class CalculateActionModule
{
    public Guid ModuleId { get; set; }              // Reference to Module
    public List<CalculateModuleDetail> Details { get; set; } // Calculation steps
    public Module Module { get; set; }              // Navigation
}

public class CalculateModuleDetail
{
    public Guid Id { get; set; }
    public Guid CalculateActionModuleId { get; set; } // Parent
    public int Sequence { get; set; }                 // Step order
    public CalculateOperator OperatorId { get; set; } // Operation
    public bool Input1IsConstant { get; set; }        // Constant or field?
    public Guid? Input1FieldId { get; set; }          // Field reference
    public string Input1Value { get; set; }           // Constant value
    public bool Input2IsConstant { get; set; }
    public Guid? Input2FieldId { get; set; }
    public string Input2Value { get; set; }
    public Guid ResultFieldId { get; set; }           // Store result here
}
```

**CalculateOperator Values:**

| Operator | Value | Inputs |
|----------|-------|--------|
| **Assign** | 1 | 1 |
| **Concatenate** | 2 | 1 |
| **Add** | 3 | 2 |
| **Subtract** | 4 | 2 |
| **Multiply** | 5 | 2 |
| **Divide** | 6 | 2 |
| **Modulus** | 7 | 2 |
| **Clear** | 8 | 0 |

---

### Comparison Modules

#### **CompareActionModule**
```csharp
public class CompareActionModule
{
    public Guid ModuleId { get; set; }              // Reference to Module
    public CompareOperator OperatorId { get; set; } // Comparison type
    public bool Input1IsConstant { get; set; }      // Constant or field?
    public Guid? Input1FieldId { get; set; }        // Field reference
    public string Input1Value { get; set; }         // Constant value
    public bool Input2IsConstant { get; set; }
    public Guid? Input2FieldId { get; set; }
    public string Input2Value { get; set; }
    public Module Module { get; set; }              // Navigation
}
```

**CompareOperator Values:**

| Operator | Value |
|----------|-------|
| **Equals** | 1 |
| **NotEquals** | 2 |
| **GreaterThan** | 3 |
| **LessThan** | 4 |
| **GreaterThanOrEqual** | 5 |
| **LessThanOrEqual** | 6 |
| **Contains** | 7 |
| **StartsWith** | 8 |
| **EndsWith** | 9 |

---

### UI Modules

#### **ScreenFormatModule** (Layout)
```csharp
public class ScreenFormatModule
{
    public Guid ModuleId { get; set; }              // Reference to Module
    public int ScreenGroup { get; set; }             // 4=8x16, 6=6x40, 8=8x20
    public Guid? SoftKeyId { get; set; }             // Function key mappings
    public Module Module { get; set; }              // Navigation
    public SoftKeyModule SoftKey { get; set; }      // Navigation
    public List<ScreenFormatDetail> Details { get; set; } // Layout elements
}

public class ScreenFormatDetail
{
    public Guid Id { get; set; }
    public Guid ScreenFormatId { get; set; }         // Parent screen
    public int Sequence { get; set; }                // Draw order
    public int DataUsage { get; set; }               // Input(1), Output(2), Read(3), Label(4)
    public int DataType { get; set; }                // Field type
    public Guid? DataId { get; set; }                // Which field?
    public int DataReference { get; set; }           // Reference number
    public string FormatId { get; set; }             // Format string
    public int PosRow { get; set; }                  // Screen row
    public int PosColumn { get; set; }               // Column
    public int PosWidth { get; set; }                // Width
    public int PosHeight { get; set; }               // Height
    public int Echo { get; set; }                    // Show input? (1=yes, 0=masked)
    public int OverflowMode { get; set; }            // Wrap/truncate
}
```

#### **DialogActionModule**
```csharp
public class DialogActionModule
{
    public Guid ModuleId { get; set; }              // Reference to Module
    public Module Module { get; set; }              // Navigation
    public List<DialogActionDetail> Details { get; set; } // Screen references
}

public class DialogActionDetail
{
    public Guid Id { get; set; }
    public Guid DialogActionId { get; set; }         // Parent dialog
    public int ScreenGroup { get; set; }             // 4, 6, or 8
    public Guid ScreenFormatId { get; set; }         // Screen format
    public int Reference { get; set; }               // Reference #
    public bool KeyEntry { get; set; }               // Keyboard entry allowed?
}
```

#### **SoftKeyModule** (Function Keys)
```csharp
public class SoftKeyModule
{
    public Guid ModuleId { get; set; }              // Reference to Module
    public int Layer { get; set; }                  // Key mapping layer
    public int State { get; set; }                  // Application state
    public Module Module { get; set; }              // Navigation
    public List<SoftKeyDetail> Details { get; set; } // F1-F12 mappings
}

public class SoftKeyDetail
{
    public Guid Id { get; set; }
    public Guid SoftKeyId { get; set; }              // Parent mapping
    public int Sequence { get; set; }                // Order
    public int OperatorId { get; set; }              // Which key?
    public int InputType { get; set; }               // What triggers?
    public Guid? InputId { get; set; }               // Trigger reference
    public int OutputType { get; set; }              // What happens?
    public Guid? OutputId { get; set; }              // Action reference
}
```

---

## Infrastructure Layer

### Execution Session Management

#### **ExecutionSession**
```csharp
public class ExecutionSession
{
    public Guid SessionId { get; set; }              // Unique session ID
    public DateTime StartTime { get; set; }          // When started
    public string UserId { get; set; }               // User running process
    public string DeviceId { get; set; }             // Device context
    public string CurrentDatabase { get; set; }      // Current DB connection

    // Pause/Resume Support
    public bool IsPaused { get; set; }               // Waiting for input?
    public Guid? PausedAtProcessModuleId { get; set; } // Which process?
    public int? PausedAtStep { get; set; }           // Which step?
    public string PausedScreenJson { get; set; }     // Screen JSON shown

    // Internal State
    private Dictionary<Guid, object> _fieldValues;   // Field values
    private Stack<ExecutionFrame> _callStack;        // Call stack
    private IDbConnection _currentConnection;        // DB connection
}
```

**Key Methods:**
```csharp
public void SetFieldValue(Guid fieldId, object value);
public object? GetFieldValue(Guid fieldId);
public T GetFieldValue<T>(Guid fieldId);
public void PushFrame(ExecutionFrame frame);
public ExecutionFrame? PopFrame();
public int CallDepth { get; }
public void Pause(Guid processModuleId, int step, string screenJson);
public void Resume();
public bool CanResume();
```

---

#### **ExecutionFrame**
```csharp
public class ExecutionFrame
{
    public Guid ProcessModuleId { get; set; }       // Which process?
    public string ProcessModuleName { get; set; }   // Process name
    public int CurrentSequence { get; set; }        // Current step #
    public DateTime EnteredAt { get; set; }         // When called
    public Dictionary<string, object> Parameters { get; set; } // Input params
}
```

**Stack Depth Protection:** Max 20 levels (prevents infinite recursion)

---

### Module Cache

#### **ModuleCache**
```csharp
public class ModuleCache
{
    // By type
    public Dictionary<Guid, Module> Modules;
    public Dictionary<Guid, ProcessModule> ProcessModules;
    public Dictionary<Guid, DatabaseActionModule> DatabaseActions;
    public Dictionary<Guid, FieldModule> FieldModules;
    public Dictionary<Guid, CompareActionModule> CompareActions;
    public Dictionary<Guid, CalculateActionModule> CalculateActions;
    public Dictionary<Guid, DialogActionModule> DialogActions;
    public Dictionary<Guid, ScreenFormatModule> ScreenFormats;

    // Quick lookup
    public Dictionary<string, FieldModule> FieldsByName;
}
```

**Why Cached?**
- ✅ O(1) lookup speed
- ✅ Avoid repeated DB queries
- ✅ Thread-safe concurrent access
- ✅ Single source of truth

---

### Execution Engine

#### **ProcessExecutor**
Main orchestrator that executes workflows.

**Key Methods:**
```csharp
// 1. Execute a process from the beginning
public async Task<ActionResult> ExecuteAsync(
    Guid processModuleId,
    ExecutionSession session,
    Dictionary<string, object> parameters = null)

// 2. Execute starting from a specific step
private async Task<ActionResult> ExecuteStepsFromSequenceAsync(
    ProcessModule processModule,
    ExecutionSession session,
    int startingSequence)

// 3. Execute a single step
private async Task<ActionResult> ExecuteStepAsync(
    ProcessModuleDetail step,
    ExecutionSession session,
    int currentSequence)

// 4. Determine next step
private int ResolveNextSequence(
    List<ProcessModuleDetail> steps,
    int currentSequence,
    string label)

// 5. Resume after user input
public async Task<ActionResult> ResumeAfterDialogAsync(
    ExecutionSession session,
    string inputValue)
```

---

### Specialized Executors

#### **DialogExecutor**
Handles dialog (screen) display and input capture.

#### **DatabaseExecutor**
Executes SQL statements and stored procedures with field substitution.

#### **CalculateExecutor**
Performs multi-step mathematical operations.

#### **CompareExecutor**
Compares two values to determine Pass/Fail.

---

### Screen Building

#### **ScreenBuilder**
Generates JSON output from screen formats and field values.

**Output Format:**
```json
{
  "heading": "Warehouse",
  "content": {
    "paragraph": "Item Putaway",
    "lines": ["Location: A1-R1-S1"]
  },
  "options": [
    {"value": "F5", "text": "Version"}
  ],
  "prompt": {
    "label": "USER ID",
    "defaultValue": "",
    "masked": {"on": "FALSE"}
  }
}
```

---

## Execution Flow

### Complete Example: "Item Putaway Process"

**Database Setup:**
```
Application: "Warehouse Management System"
├─ ProcessModule: "Item Putaway Process"
│  ├─ Step 1: Connect to WMS (DatabaseExecute)
│  ├─ Step 2: Validate User (Dialog)
│  ├─ Step 3: Calculate Total (Calculate)
│  ├─ Step 4: Execute Putaway (DatabaseExecute)
│  └─ Step 5: Return Success (ReturnPass)
├─ ScreenFormatModule: "Login Screen"
├─ DialogActionModule: "Login Dialog"
├─ CalculateActionModule: "Total Calculation"
├─ DatabaseActionModule: "Execute Putaway"
└─ Fields: Warehouse ID, User ID, Quantity, ExtraQty, TotalQty
```

---

## Key Concepts

### 1. No Code Changes = Dynamic Workflows
- Change workflow logic → Update database
- New workflow goes live immediately

### 2. Pause/Resume Pattern
- Dialog actions pause execution
- Server handles other devices meanwhile
- Resume when user responds

### 3. Field Substitution
- SQL statements need dynamic values at runtime
- Pattern: `::#ModuleType#FieldId#::`

### 4. Call Stacking
- ProcessA calls ProcessB (push frame)
- ProcessB completes (pop frame)
- Return to ProcessA

### 5. Branching Logic
- PassLabel/FailLabel pattern
- "NEXT" → increment sequence
- "PREV" → decrement sequence
- "LabelName" → jump to label

---

## Design Decisions

### 1. Why Module Cache?
- O(1) lookup speed (critical for real-time execution)
- Single source of truth
- Reduces DB load

### 2. Why JSON for Screens?
- Works across Web, Android, Console
- Engine-agnostic
- Separates concerns

### 3. Why Pause/Resume?
- Single thread handles multiple devices
- Scales horizontally
- Non-blocking

### 4. Why Field Substitution?
- Decouples SQL from field IDs
- Type-safe
- Prevents SQL injection

---

## File Directory Guide

```
Qulron/
├── Domain/
│   └── ProcessEngine/
│       ├── Entities/
│       │   ├── Application.cs
│       │   ├── Module.cs
│       │   ├── ProcessModule.cs
│       │   ├── FieldModule.cs
│       │   ├── DatabaseActionModule.cs
│       │   ├── CalculateActionModule.cs
│       │   ├── CompareActionModule.cs
│       │   ├── ScreenFormatModule.cs
│       │   ├── DialogActionModule.cs
│       │   ├── SoftKeyModule.cs
│       │   ├── Device.cs
│       │   └── DeviceSession.cs
│       └── Enums/
│           ├── ActionType.cs
│           ├── ModuleType.cs
│           ├── FieldType.cs
│           ├── CalculateOperator.cs
│           ├── CompareOperator.cs
│           └── ExecutionResult.cs
│
├── Infrastructure/
│   └── ProcessEngine/
│       ├── Execution/
│       │   ├── ExecutionSession.cs
│       │   ├── ExecutionFrame.cs
│       │   ├── ModuleCache.cs
│       │   └── ActionResult.cs
│       ├── Executors/
│       │   ├── ProcessExecutor.cs
│       │   ├── DialogExecutor.cs
│       │   ├── DatabaseExecutor.cs
│       │   ├── CalculateExecutor.cs
│       │   └── CompareExecutor.cs
│       ├── Rendering/
│       │   └── ScreenBuilder.cs
│       ├── Session/
│       │   └── EngineSessionManager.cs
│       └── Services/
│           └── ApplicationLoaderService.cs
│
└── README.md
    ARCHITECTURE.md (this file)
```

---

## Simple Process Module Example

Scenario:
Process1 (Main Workflow)
  Step 1: Display "Welcome" Dialog
  Step 2: Call Process2 (Sub-workflow)
  Step 3: Display "Summary" using values from Process2

Process2 (Called Workflow)
  Step 1: Calculate Total (uses value from Process1)
  Step 2: Display "Confirm" Dialog
  Step 3: Return success

Execution Flow:
────────────────────────────────────────────────────────

T0: ExecuteAsync(Process1, session)
    session.PushFrame(Process1, Seq 1)
    
T1: Process1, Step 1: Dialog "Welcome"
    user input: "WH001" → stored in Field[WarehouseId]
    session.Pause(Process1, step 1)
    ↓
    CLIENT DISPLAYS SCREEN

T2: User responds: ResumeAfterDialogAsync("WH001")
    ProcessInput stores "WH001" in Field[WarehouseId]
    Resumes Process1 from step 2
    
T3: Process1, Step 2: Call Process2
    ExecuteAsync(Process2, session)  ← SAME SESSION!
    session.PushFrame(Process2, Seq 1)
    Field[WarehouseId] = "WH001"  ← STILL THERE!
    
T4: Process2, Step 1: Calculate
    Input: Field[WarehouseId] = "WH001"
    Output: Field[TotalCost] = "1000"
    
T5: Process2, Step 2: Dialog "Confirm"
    user input: "YES"
    session.Pause(Process2, step 2)
    ↓
    CLIENT DISPLAYS SCREEN

T6: User responds: ResumeAfterDialogAsync("YES")
    ProcessInput stores "YES" in Field[ConfirmFlag]
    Resumes Process2 from step 3
    
T7: Process2, Step 3: ReturnPass
    session.PopFrame()  ← Back to Process1
    Returns result: Pass
    
T8: Process1, Step 2 complete
    ResolveNextSequence() → Step 3
    
T9: Process1, Step 3: Display Summary
    Read Field[WarehouseId] = "WH001" ✅
    Read Field[TotalCost] = "1000" ✅
    Read Field[ConfirmFlag] = "YES" ✅
    All values from both processes available!
    
T10: Process1, Step 4: ReturnPass
    session.PopFrame()  ← Back to top level
    Process complete!