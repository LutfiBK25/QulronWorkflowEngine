

using Domain.ProcessEngine.Enums;
using System.Data;
using System.Globalization;

namespace Infrastructure.ProcessEngine.Execution;

public class ExecutionSession
{
    public Guid SessionId { get; }
    public DateTime StartTime { get; }
    public string UserId { get; set; }
    public string DeviceId { get; set; }
    public string CurrentDatabase { get; set; } // Track current database connection

    // Pause/Resume Support
    public bool IsPaused { get; set; } // Session is waiting for an Input
    public Guid? PausedAtProcessModuleId { get; set; } // Which Process
    public int? PausedAtStep { get; set; } // Which Step
    public string PausedScreenJson { get; set; } // Current Screen


    private readonly Dictionary<Guid, object> _fieldValues = new();
    private readonly Stack<ExecutionFrame> _callStack = new();
    private IDbConnection _currentConnection;

    public ExecutionSession(string userId = null, string deviceId = null)
    {
        SessionId = Guid.NewGuid();
        StartTime = DateTime.UtcNow;
        UserId = userId;
        DeviceId = deviceId;
    }

    // Database connection management
    public IDbConnection CurrentConnection
    {
        get => _currentConnection;
        set => _currentConnection = value;
    }

    public async Task CloseConnectionAsync()
    {
        if (_currentConnection != null)
        {
            if (_currentConnection is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                _currentConnection.Dispose();
            }
            _currentConnection = null;
            CurrentDatabase = null;
        }
    }

    public void SetFieldValue(Guid fieldId, object value)
    {
        _fieldValues[fieldId] = value;
    }

    public object? GetFieldValue(Guid fieldId)
    {
        return _fieldValues.TryGetValue(fieldId, out var value) ? value : null;
    }

    public T GetFieldValue<T>(Guid fieldId)
    {
        var value = GetFieldValue(fieldId);
        if (value == null) return default;
        return (T)Convert.ChangeType(value, typeof(T));
    }


    public bool HasField(Guid fieldId)
    {
        return _fieldValues.ContainsKey(fieldId);
    }

    public void ClearFields()
    {
        _fieldValues.Clear();
    }

    public Dictionary<Guid, object> GetAllFields()
    {
        return new Dictionary<Guid, object>(_fieldValues);
    }

    // Call stack management
    public void PushFrame(ExecutionFrame frame)
    {
        _callStack.Push(frame);
    }

    public ExecutionFrame? PopFrame()
    {
        return _callStack.Count > 0 ? _callStack.Pop() : null;
    }

    public ExecutionFrame? CurrentFrame => _callStack.Count > 0 ? _callStack.Peek() : null;

    public int CallDepth => _callStack.Count;


    // Pause/Resume functionality 
    public void Pause(Guid processModuleId, int step, string screenJson = null)
    {
        IsPaused = true;
        PausedAtProcessModuleId = processModuleId;
        PausedAtStep = step;
        PausedScreenJson = screenJson;
    }

    public void Resume()
    {
        IsPaused = false; 
    }

    public bool CanResume()
    {
        return IsPaused && PausedAtProcessModuleId.HasValue && PausedAtStep.HasValue;
    }
}
