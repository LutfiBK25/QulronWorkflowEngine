using Domain.ProcessEngine.Entities;
using Domain.ProcessEngine.Enums;
using Infrastructure.ProcessEngine.Execution;
using System.ComponentModel;

namespace Infrastructure.ProcessEngine.Executors;

/// <summary>
/// Field or constant comparisons
/// Supports all operators(Equals, NotEquals, GreaterThan, LessThan, Contains, StartsWith, EndsWith)
/// Returns Pass/Fail for branching
/// </summary>
public class CompareExecutor
{
    private readonly ModuleCache _moduleCache;

    public CompareExecutor(ModuleCache moduleCache)
    {
        _moduleCache = moduleCache; 
    }

    public async Task<ActionResult> ExecuteAsync(
        CompareActionModule compareAction, 
        ExecutionSession session)
    {
        try
        {
            // Get values to compare
            var value1 = GetValue(compareAction.Input1IsConstant, compareAction.Input1FieldId, compareAction.Input1Value, session);
            var value2 = GetValue(compareAction.Input2IsConstant, compareAction.Input2FieldId, compareAction.Input2Value, session);

            // Perform comparison
            bool comparisonResult = PerformComparison(value1, value2, compareAction.OperatorId);

            // Return pass if true, fail if false
            return comparisonResult ? ActionResult.Pass("Comparison passed") : ActionResult.Fail("Comparison failed");

        }
        catch (Exception ex)
        {
            return ActionResult.Fail($"Comparison error: {ex.Message}", ex);
        }
    }

    private object GetValue(bool isConstant, Guid? fieldId, string value, ExecutionSession session)
    {
        if(isConstant)
        {
            return value;
        }

        if(fieldId.HasValue)
        {
            return session.GetFieldValue(fieldId.Value);
        }

        return null;
    }

    private bool PerformComparison(object value1, object value2, CompareOperator op)
    {
        var str1 = value1?.ToString() ?? "";
        var str2 = value2?.ToString() ?? "";

        switch (op)
        {
            case CompareOperator.Equals:
                return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);

            case CompareOperator.NotEquals:
                return !str1.Equals(str2, StringComparison.OrdinalIgnoreCase);

            case CompareOperator.GreaterThan:
                return CompareNumeric(value1, value2) > 0;

            case CompareOperator.LessThan:
                return CompareNumeric(value1, value2) < 0;

            case CompareOperator.GreaterThanOrEqual:
                return CompareNumeric(value1, value2) >= 0;

            case CompareOperator.LessThanOrEqual:
                return CompareNumeric(value1, value2) <= 0;

            case CompareOperator.Contains:
                return str1.Contains(str2, StringComparison.OrdinalIgnoreCase);

            case CompareOperator.StartsWith:
                return str1.StartsWith(str2, StringComparison.OrdinalIgnoreCase);

            case CompareOperator.EndsWith:
                return str1.EndsWith(str2, StringComparison.OrdinalIgnoreCase);

            default:
                throw new InvalidOperationException($"Unknow comparison operator: {op}");
        }
    
    }

    private int CompareNumeric(object value1, object value2)
    {
        if (decimal.TryParse(value1?.ToString(), out var num1) &&
            decimal.TryParse(value2?.ToString(), out var num2))
        {
            return num1.CompareTo(num2);
        }

        // Fall back to string comparison
        return string.Compare(value1?.ToString(), value2?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
