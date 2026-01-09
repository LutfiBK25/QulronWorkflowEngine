using System.Linq.Expressions;

namespace Domain.ProcessEngine.Enums;
/// <summary>
/// Calculate operators
/// </summary>
public enum CalculateOperator
{
    Assign = 1, // Takes 1 Input
    Concatenate = 2, // Takes 1 Input
    Add = 3, // Takes 2 inputs
    Subtract = 4, // Takes 2 inputs
    Multiply = 5, // Takes 2 inputs
    Divide = 6, // Takes 2 inputs
    Modulus = 7, // Takes 2 inputs 
    Clear = 8, // Takes 0 Input just clears Result
}
