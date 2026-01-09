using Domain.ProcessEngine.Enums;

namespace Infrastructure.ProcessEngine.Execution;

public class ActionResult
{
    public ExecutionResult Result { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception Exception { get; set; } 
    public Dictionary<Guid, object> ReturnedFields { get; set; } = new();

    public static ActionResult Pass(string message = null)
    {
        return new ActionResult
        {
            Result = ExecutionResult.Pass,
            Message = message
        };

    }

    public static ActionResult Fail(string message = null, Exception ex = null)
    {
        return new ActionResult
        {
            Result = ExecutionResult.Fail,
            Message = message,
            Exception = ex
        };
    }
}
