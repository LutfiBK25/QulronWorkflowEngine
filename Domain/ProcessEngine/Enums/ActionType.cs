namespace Domain.ProcessEngine.Enums;

public enum ActionType
{
    Call = 1,
    ReturnPass = 2,
    ReturnFail = 3,
    DatabaseExecute = 4,
    Dialog = 5,
    Calculate = 6,
    Compare = 7,
}
