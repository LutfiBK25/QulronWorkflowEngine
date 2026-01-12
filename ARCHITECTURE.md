# Qulron Workflow Execution Engine - Architecture & Codebase Guide

## Table of Contents
1. [Project Overview](#project-overview)
2. [System Architecture](#system-architecture)
3. [Projects Structure](#projects-structure)
4. [Domain Layer](#domain-layer)
5. [Infrastructure Layer](#infrastructure-layer)
6. [API Service Layer](#api-service-layer)
7. [Execution Flow](#execution-flow)
8. [Device Auto-Start Flow](#device-auto-start-flow)
9. [Key Concepts](#key-concepts)
10. [Module Types Reference](#module-types-reference)
11. [Design Decisions](#design-decisions)

---

## Project Overview

**Qulron** is a **Workflow Execution Engine** for Warehouse Management Systems (WMS).

### Core Purpose
- Execute process workflows defined in a database
- Support multiple client types: Web (React), Android Scanners, Console Scanners
- Enable dynamic workflow composition without code changes
- Manage session state and user interactions
- Auto-start device workflows on engine startup

### Tech Stack
- **.NET 10** runtime
- **C# 14** language features
- **ASP.NET Core** for API service
- **PostgreSQL** for data persistence
- **Redis** for session caching (optional, not implemented yet)
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
✅ **Device auto-start on engine startup** (NEW!)  
✅ **REST API for scanner communication** (NEW!)  
✅ **Session isolation per device** (NEW!)  

---

## System Architecture

### High-Level Architecture Diagram

```
┌────────────────────────────────────────────────────────────┐
│  CONSOLE CONTROL APP (Future)                              │
│  ├─ Start/Stop engine service                              │
│  ├─ View engine status                                     │
│  └─ Register devices                                       │
└────────────────────────────────────────────────────────────┘
         │
         │ HTTP to localhost:5000
         │
┌────────────────────────────────────────────────────────────┐
│  QULRON ENGINE SERVICE (ASP.NET Core Web API)              │
│  ├─ Loads applications at startup                          │
│  ├─ Loads all active devices from database                 │
│  ├─ Auto-starts RootProcessModuleId for each device        │
│  ├─ Manages device sessions (EngineSessionManager)         │
│  ├─ Provides REST API endpoints                            │
│  └─ Handles pause/resume with user input                   │
└────────────────────────────────────────────────────────────┘
         │
         │ HTTP to localhost:5000
         │
┌────────────────────────────────────────────────────────────┐
│  SCANNER APPS (Multiple Instances)                         │
│  ├─ Connect with device_id                                 │
│  ├─ Receive screen JSON from paused dialog                 │
│  ├─ Display in console/Android/web                         │
│  ├─ Accept user input                                      │
│  └─ Send input to resume execution                         │
└────────────────────────────────────────────────────────────┘
```

### Execution Flow Overview

```
Engine Startup
    │
    ▼
Load Applications → Load Devices from DB → Auto-Start Processes
    │
    ▼
Device Request (Scanner connects)
    │
    ▼
API: POST /api/devices/connect
    │
    ▼
Return Current Screen JSON (from paused dialog)
    │
    ▼
User Enters Input → API: POST /api/devices/{id}/input
    │
    ▼
ProcessExecutor.ResumeAfterDialogAsync()
    │
    ├─→ Continue execution from next step
    ├─→ If Dialog: Pause and return screen JSON
    └─→ If Complete: Return success/fail status
```

---

## Projects Structure

### Solution Layout

```
Qulron/
├── Domain/                          # Domain entities and enums
│   └── Domain.csproj
├── Infrastructure/                  # Execution engine, persistence
│   └── Infrastructure.csproj
├── QulronEngineService/            # ASP.NET Core Web API (NEW!)
│   └── QulronEngineService.csproj
├── QulronEngineConsoleApp/         # Console test application
│   └── QulronEngineConsoleApp.csproj
└── README.md
    ARCHITECTURE.md
```

### Project Dependencies

```
QulronEngineService
├─→ Infrastructure
│   ├─→ Domain
│   └─→ Microsoft.EntityFrameworkCore
└─→ Microsoft.AspNetCore.App

QulronEngineConsoleApp
├─→ Infrastructure
└─→ Domain
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

#### **Device** (NEW!)
```csharp
public class Device
{
    public string DeviceId { get; set; }            // Unique device ID (e.g., "SCANNER-001")
    public string DeviceName { get; set; }          // Human-readable name
    public string DeviceType { get; set; }          // SCANNER, WORKSTATION
    public Guid RootProcessModuleId { get; set; }   // Default process to auto-start
    public DateTime RegisteredAt { get; set; }      // Registration timestamp
    public DateTime? LastConnected { get; set; }    // Last connection time
    public bool IsActive { get; set; }              // Active devices auto-start at engine startup
    public string Metadata { get; set; }            // JSON metadata
}
```

#### **DeviceSession** (In-Memory)
```csharp
public class DeviceSession
{
    public string DeviceId { get; set; }            // Device identifier
    public Guid SessionId { get; set; }             // Unique session ID
    public string CurrentUserId { get; set; }       // Currently logged-in user
    public DateTime ConnectedAt { get; set; }       // Connection timestamp
    public DateTime LastActivity { get; set; }      // Last activity timestamp
    public string Status { get; set; }              // CONNECTED, IDLE, ACTIVE, DISCONNECTED
    public Guid RootProcessModuleId { get; set; }   // Default process
    public int CurrentStep { get; set; }            // Current step number
    public string CurrentScreenJson { get; set; }   // Current screen JSON
    public Dictionary<string, object> SessionData { get; set; }
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

## Infrastructure Layer

### Execution Session Management

#### **ExecutionSession**
```csharp
public class ExecutionSession
{
    public Guid SessionId { get; }                  // Unique session ID
    public DateTime StartTime { get; }              // When started
    public string UserId { get; set; }              // User running process
    public string DeviceId { get; set; }            // Device context
    public string CurrentDatabase { get; set; }     // Current DB connection

    // Pause/Resume Support
    public bool IsPaused { get; set; }              // Waiting for input?
    public Guid? PausedAtProcessModuleId { get; set; } // Which process?
    public int? PausedAtStep { get; set; }          // Which step?
    public string PausedScreenJson { get; set; }    // Screen JSON shown

    // Internal State
    private Dictionary<Guid, object> _fieldValues;  // Field values
    private Stack<ExecutionFrame> _callStack;       // Call stack
    private IDbConnection _currentConnection;       // DB connection
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

#### **EngineSessionManager** (UPDATED!)

Manages all active device sessions in memory.

**Key Methods:**
```csharp
// Device Registration
public DeviceSession RegisterDevice(string deviceId, Guid rootProcessModuleId);

// NEW: Auto-start device process
public async Task<ActionResult> StartDeviceProcessAsync(string deviceId, ExecutionEngine engine);

// NEW: Resume paused process with user input
public async Task<ActionResult> ResumeDeviceProcessAsync(string deviceId, string userInput, ExecutionEngine engine);

// NEW: Get last execution result
public ActionResult GetLastExecutionResult(string deviceId);

// Session Queries
public DeviceSession GetDeviceSession(string deviceId);
public ExecutionSession GetExecutionSessionByDevice(string deviceId);
public IEnumerable<DeviceSession> GetActiveDevices();
public SessionStatistics GetStatistics();
```

**Internal State:**
```csharp
private ConcurrentDictionary<string, DeviceSession> _deviceSessions;           // Device sessions
private ConcurrentDictionary<Guid, ExecutionSession> _executionSessions;       // Execution sessions
private ConcurrentDictionary<string, ActionResult> _deviceExecutionResults;    // Last result per device
```

---

#### **ExecutionEngine** (UPDATED!)

**Key Methods:**
```csharp
// Load application modules into cache
public async Task LoadApplicationAsync(Guid applicationId);

// Execute with parameters (creates new session) - for console testing
public async Task<ActionResult> ExecuteProcessModuleAsync(
    Guid processModuleId,
    string userId = null,
    Dictionary<string, object> parameters = null);

// NEW: Execute with existing session - for device auto-start
public async Task<ActionResult> ExecuteProcessModuleAsync(
    Guid processModuleId,
    ExecutionSession session,
    Dictionary<string, object> parameters = null);

// NEW: Resume paused execution
public async Task<ActionResult> ResumeProcessModuleAsync(
    ExecutionSession session,
    string inputValue);
```

---

#### **DeviceInitializationService** (NEW!)

Loads devices from database and initializes their sessions at engine startup.

```csharp
public class DeviceInitializationService
{
    private readonly RepositoryDBContext _dbContext;
    private readonly ExecutionEngine _engine;
    private readonly EngineSessionManager _sessionManager;

    // Load all active devices and auto-start their default process
    public async Task InitializeAllDevicesAsync();

    // Get device info with session details
    public DeviceWithSessionInfo GetDeviceInfo(string deviceId);
    
    // Get all devices info
    public IEnumerable<DeviceWithSessionInfo> GetAllDevicesInfo();
}
```

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

---

## API Service Layer

### QulronEngineService (ASP.NET Core)

**Port:** 5000 (localhost)  
**Startup:** Auto-loads applications and devices

#### **API Endpoints**

##### **1. Device Connection**
```http
POST /api/devices/connect?deviceId=SCANNER-001
```

**Response:**
```json
{
  "sessionId": "guid",
  "screenJson": { "heading": "Login", "prompt": {...} },
  "status": "IDLE",
  "message": "Connected successfully"
}
```

##### **2. Send User Input**
```http
POST /api/devices/{deviceId}/input
Content-Type: application/json

{
  "inputValue": "user-input-here"
}
```

**Response:**
```json
{
  "sessionId": "guid",
  "screenJson": { ... } or null,
  "status": "paused" or "completed",
  "message": "Process completed successfully"
}
```

##### **3. Get Device Status**
```http
GET /api/devices/{deviceId}/status
```

**Response:**
```json
{
  "deviceId": "SCANNER-001",
  "status": "IDLE",
  "currentUserId": "admin",
  "connectedAt": "2026-01-27T10:00:00Z",
  "lastActivity": "2026-01-27T10:05:00Z",
  "currentStep": 2,
  "currentScreenJson": { ... },
  "isPaused": true
}
```

##### **4. List All Devices**
```http
GET /api/devices/list
```

**Response:**
```json
{
  "count": 3,
  "devices": [
    {
      "deviceId": "SCANNER-001",
      "status": "IDLE",
      "currentUserId": null,
      "connectedAt": "2026-01-27T09:00:00Z",
      "lastActivity": "2026-01-27T10:05:00Z",
      "sessionId": "guid"
    }
  ]
}
```

##### **5. Engine Health**
```http
GET /api/health
```

**Response:**
```json
{
  "status": "healthy",
  "startTime": "2026-01-27T09:00:00Z",
  "uptime": "01:05:30",
  "activeDevices": 3,
  "totalSessions": 3,
  "loadedModules": 45,
  "processModules": 8,
  "databaseActions": 12,
  "fieldModules": 25,
  "devicesByStatus": {
    "IDLE": 2,
    "CONNECTED": 1
  }
}
```

---

## Device Auto-Start Flow

### Engine Startup Sequence

```
Time T0: Engine Service Starts
├─ Load configuration (appsettings.json)
├─ Initialize database connection
├─ Create ExecutionEngine instance
├─ Create EngineSessionManager instance
└─ Create DeviceInitializationService instance

Time T1: Load Applications
├─ Read "Applications:LoadOnStartup" from config
├─ For each application GUID:
│  ├─ Call ExecutionEngine.LoadApplicationAsync(appId)
│  └─ Populate ModuleCache with all modules
└─ Log: "Applications loaded: N success, M failed"

Time T2: Initialize Devices (NEW!)
├─ Call DeviceInitializationService.InitializeAllDevicesAsync()
├─ Query database: SELECT * FROM devices WHERE is_active = true
│
└─ For each active device:
   ├─ Create DeviceSession in memory
   │  ├─ Assign unique SessionId
   │  ├─ Set Status = "CONNECTED"
   │  └─ Set RootProcessModuleId from device record
   │
   ├─ Create ExecutionSession
   │  ├─ Link to DeviceSession via SessionId
   │  └─ Initialize empty field values dictionary
   │
   ├─ Call StartDeviceProcessAsync(deviceId, engine)
   │  ├─ Execute RootProcessModuleId (e.g., Login process)
   │  ├─ Process reaches first Dialog action → PAUSES
   │  ├─ Store screen JSON in DeviceSession.CurrentScreenJson
   │  └─ Set DeviceSession.Status = "IDLE" (waiting for input)
   │
   └─ Log: "✓ Device initialized and paused on login screen"

Time T3: Engine Ready
├─ All devices initialized
├─ All paused on their first dialog (login screen)
├─ API service listening on http://localhost:5000
└─ Ready to accept scanner connections
```

### Scanner Connection Flow

```
Time T0: Scanner App Starts
├─ Input: server=localhost, port=5000, deviceId=SCANNER-001
└─ POST /api/devices/connect?deviceId=SCANNER-001

Time T1: Engine Receives Connection
├─ Look up SCANNER-001 in EngineSessionManager
├─ Device found (created at engine startup)
├─ Return DeviceSession.CurrentScreenJson (login screen)
└─ Response: { sessionId, screenJson, status: "IDLE" }

Time T2: Scanner Displays Screen
├─ Parse screenJson
├─ Render login form in console/UI
└─ Wait for user input

Time T3: User Enters Credentials
├─ User types "admin" and presses Enter
└─ POST /api/devices/SCANNER-001/input
    Body: { "inputValue": "admin" }

Time T4: Engine Resumes Execution
├─ Get ExecutionSession for SCANNER-001
├─ Call ResumeDeviceProcessAsync("SCANNER-001", "admin", engine)
│  ├─ Call ProcessExecutor.ResumeAfterDialogAsync(session, "admin")
│  ├─ Store "admin" in appropriate field
│  ├─ Continue execution from next step
│  │
│  └─ If next step is Dialog:
│     ├─ Pause again
│     ├─ Store new screen JSON
│     └─ Return { screenJson, status: "paused" }
│
└─ If process completes:
   └─ Return { screenJson: null, status: "completed", message: "Success" }

Time T5: Loop Until Complete
└─ Repeat T2-T4 until process reaches ReturnPass/ReturnFail
```

---

## Execution Flow

### Complete Example: "Login Process"

**Database Setup:**
```
Devices Table:
├─ device_id: "SCANNER-001"
├─ device_name: "Front Desk Scanner"
├─ device_type: "SCANNER"
├─ root_process_module_id: <Login Process GUID>
└─ is_active: true

Application: "Warehouse Management System"
├─ ProcessModule: "Login Process" (RootProcessModuleId)
│  ├─ Step 1: Dialog "Enter Username" (PAUSES)
│  ├─ Step 2: Database "Validate User"
│  ├─ Step 3: Dialog "Enter Password" (PAUSES)
│  ├─ Step 4: Database "Authenticate"
│  ├─ Step 5: Compare "Check Auth Result"
│  ├─ Step 6 (Pass): Return Success
│  └─ Step 6 (Fail): Return Fail
├─ DialogActionModule: "Username Dialog"
├─ DialogActionModule: "Password Dialog"
├─ DatabaseActionModule: "Validate User Query"
├─ DatabaseActionModule: "Authenticate Query"
├─ CompareActionModule: "Auth Check"
└─ Fields: Username, Password, AuthResult
```

**Execution Flow:**
```
T0: Engine Starts
    ├─ Load "Login Process" into cache
    ├─ Register SCANNER-001 with RootProcessModuleId = Login Process
    ├─ Execute Login Process
    └─ Step 1: Dialog "Enter Username" → PAUSES
        ├─ ScreenJson stored: { "prompt": { "label": "Username" } }
        └─ Status = IDLE

T1: Scanner Connects
    ├─ POST /api/devices/connect?deviceId=SCANNER-001
    └─ Returns: { screenJson: { "prompt": { "label": "Username" } } }

T2: User Enters "admin"
    ├─ POST /api/devices/SCANNER-001/input
    │   Body: { "inputValue": "admin" }
    ├─ Engine resumes from Step 2
    ├─ Step 2: Database query validates "admin" → Success
    ├─ Step 3: Dialog "Enter Password" → PAUSES
    └─ Returns: { screenJson: { "prompt": { "label": "Password", "masked": true } } }

T3: User Enters "password123"
    ├─ POST /api/devices/SCANNER-001/input
    │   Body: { "inputValue": "password123" }
    ├─ Engine resumes from Step 4
    ├─ Step 4: Database authenticate → Sets AuthResult = "SUCCESS"
    ├─ Step 5: Compare AuthResult == "SUCCESS" → Pass
    ├─ Step 6: Return Success
    └─ Returns: { status: "completed", message: "Login successful" }
```

---

## Key Concepts

### 1. No Code Changes = Dynamic Workflows
- Change workflow logic → Update database
- New workflow goes live immediately
- No deployment required

### 2. Pause/Resume Pattern
- Dialog actions pause execution
- Engine returns screen JSON to client
- Engine handles other devices meanwhile
- Resume when user responds

### 3. Field Substitution
- SQL statements need dynamic values at runtime
- Pattern: `::#ModuleType#FieldId#::`
- Example: `SELECT * FROM users WHERE username = ::#5#<field-guid>#::`

### 4. Call Stacking
- ProcessA calls ProcessB (push frame)
- ProcessB completes (pop frame)
- Return to ProcessA
- Max 20 levels deep (prevents infinite recursion)

### 5. Branching Logic
- PassLabel/FailLabel pattern
- "NEXT" → increment sequence
- "PREV" → decrement sequence
- "LabelName" → jump to label

### 6. Session Isolation
- Each device has unique ExecutionSession
- Field values completely isolated
- Users' inputs never interfere

### 7. Device Auto-Start
- Engine loads all active devices at startup
- Each device auto-starts its RootProcessModuleId
- Devices pause on first dialog (login screen)
- Ready for scanner connections immediately

---

## Design Decisions

### 1. Why Module Cache?
- O(1) lookup speed (critical for real-time execution)
- Single source of truth
- Reduces DB load
- Thread-safe concurrent access

### 2. Why JSON for Screens?
- Works across Web, Android, Console
- Engine-agnostic
- Separates concerns
- Easy to extend

### 3. Why Pause/Resume?
- Single thread handles multiple devices
- Scales horizontally
- Non-blocking
- Natural for dialog-driven workflows

### 4. Why Field Substitution?
- Decouples SQL from field IDs
- Type-safe
- Prevents SQL injection
- Dynamic at runtime

### 5. Why Auto-Start Devices?
- Devices ready immediately
- No manual initialization
- Consistent state (paused on login)
- Simplifies scanner app logic

### 6. Why REST API?
- Language-agnostic clients
- Standard HTTP protocol
- Easy to debug and test
- Works with web/mobile/console

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
│       │   ├── Device.cs (NEW!)
│       │   └── DeviceSession.cs (NEW!)
│       └── Enums/
│           ├── ActionType.cs
│           ├── ModuleType.cs
│           ├── FieldType.cs
│           ├── CalculateOperator.cs
│           ├── CompareOperator.cs
│           └── ExecutionResult.cs
│
├── Infrastructure/
│   ├── Presistence/
│   │   └── RepositoryDBContext.cs (UPDATED!)
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
│       │   └── EngineSessionManager.cs (UPDATED!)
│       ├── Services/
│       │   ├── ApplicationLoaderService.cs
│       │   └── DeviceInitializationService.cs (NEW!)
│       └── ExecutionEngine.cs (UPDATED!)
│
├── QulronEngineService/ (NEW!)
│   ├── Controllers/
│   │   ├── DeviceController.cs
│   │   └── HealthController.cs
│   ├── Models/
│   │   ├── DeviceConnectRequest.cs
│   │   ├── DeviceInputRequest.cs
│   │   ├── DeviceScreenResponse.cs
│   │   ├── DeviceStatusResponse.cs
│   │   └── EngineHealthResponse.cs
│   ├── appsettings.json
│   ├── Program.cs
│   └── QulronEngineService.csproj
│
├── QulronEngineConsoleApp/
│   ├── Program.cs
│   └── QulronEngineConsoleApp.csproj
│
└── README.md
    ARCHITECTURE.md
```

---

## Next Steps

1. ✅ **Engine Service** - Complete with device auto-start
2. ✅ **API Endpoints** - REST API for scanner communication
3. ❌ **Scanner Console App** - Client application to test workflow
4. ❌ **Control Console App** - Start/stop/monitor engine service
5. ❌ **Unit Tests** - Test each executor independently
6. ❌ **Integration Tests** - Test full workflows
7. ❌ **Session Persistence** - Redis integration (optional)
8. ❌ **Logging/Monitoring** - Execution traces and metrics

---

**Document Version:** 2.0  
**Last Updated:** January 27, 2026  
**Project:** Qulron Workflow Execution Engine