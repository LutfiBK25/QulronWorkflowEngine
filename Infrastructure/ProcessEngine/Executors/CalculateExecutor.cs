
using Domain.ProcessEngine.Entities;
using Domain.ProcessEngine.Enums;
using Infrastructure.ProcessEngine.Execution;

namespace Infrastructure.ProcessEngine.Executors;
/// <summary>
/// Multi-step calculations
/// Supports all operators(Assign, Add, Subtract, Multiply, Divide, Modulus, Concatenate, Clear)
/// Field or constant inputs
/// Stores result in target fields
/// </summary>
public class CalculateExecutor
{

    public async Task<ActionResult> ExecuteAsync(
        CalculateActionModule calculateActionModule,
        ExecutionSession session
        )
    {
        try
        {
            // Execute each calculation detail in sequence
            foreach (var detail in calculateActionModule.Details.OrderBy(d => d.Sequence))
            {
                ExecuteCalculation(detail, session);
            }
            return ActionResult.Pass("Calculations completed");
        }
        catch (Exception ex)
        {
            return ActionResult.Fail($"Calculation failed: {ex.Message}", ex);
        }
    }

    private void ExecuteCalculation(CalculateModuleDetail detail, ExecutionSession session)
    {
        // Get input values
        var input1 = GetValue(detail.Input1IsConstant, detail.Input1FieldId, detail.Input1Value, session);
        var input2 = GetValue(detail.Input2IsConstant, detail.Input2FieldId, detail.Input2Value, session);

        object result;

        switch (detail.OperatorId)
        {
            case CalculateOperator.Assign:
                result = input1; 
                break;

            case CalculateOperator.Concatenate:
                result = $"{input1}{input2}";
                break;

            case CalculateOperator.Add:
                result = ConvertToDecimal(input1) + ConvertToDecimal(input2);
                break;

            case CalculateOperator.Subtract:
                result = ConvertToDecimal(input1) - ConvertToDecimal(input2);
                break;

            case CalculateOperator.Multiply:
                result = ConvertToDecimal(input1) * ConvertToDecimal(input2);
                break;

            case CalculateOperator.Divide:
                var divisor = ConvertToDecimal(input2);
                if (divisor == 0)
                    throw new DivideByZeroException("Cannot divide by zero");
                result = ConvertToDecimal(input1) / divisor;
                break;

            case CalculateOperator.Modulus:
                result = ConvertToDecimal(input1) % ConvertToDecimal(input2);
                break;

            case CalculateOperator.Clear:
                result = null;
                break;

            default:
                throw new InvalidOperationException($"Unknown operator: {detail.OperatorId}");
        }

        // Store result in target field
        session.SetFieldValue(detail.ResultFieldId, result);
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

    private decimal ConvertToDecimal(object value)
    {
        if (value == null) return 0;
        if (value is decimal d) return d;
        if (decimal.TryParse(value.ToString(), out var result))
            return result;
        return 0;
    }
}
