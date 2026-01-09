

using Domain.ProcessEngine.Enums;
using Infrastructure.ProcessEngine.Execution;
using System.Text.RegularExpressions;

namespace Infrastructure.ProcessEngine.Parsing;

public class FieldParser
{
    private static readonly Regex FieldPattern = new Regex(
            @"::#5#([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})#::",
            RegexOptions.Compiled
    );

    private readonly ModuleCache _moduleCache;

    public FieldParser(ModuleCache moduleCache)
    {
        _moduleCache = moduleCache;
    }

    public string SubstituteFields(string statement, ExecutionSession session)
    {
        if(string.IsNullOrEmpty(statement))
        {  return statement; }

        return FieldPattern.Replace(statement, match =>
        {
            var fieldIdStr = match.Groups[1].Value;
            if (Guid.TryParse(fieldIdStr, out var fieldId))
            {
                var value = session.GetFieldValue(fieldId);

                if (value == null)
                {
                    return "NULL";
                }

                // Get the field module to determine type
                var fieldModule = _moduleCache.GetFieldModule(fieldId);

                if (fieldModule == null)
                {
                    // If we can't find the field module, treat as string for safety
                    return FormatValueAsString(value);
                }

                // Format value based on field type
                return FormatValueByType(value, fieldModule.FieldType);
            }
            return match.Value; // Keep original if can't parse
        });
    }

    private string FormatValueByType(object value, FieldType fieldType)
    {
        if (value == null)
            return "NULL";

        switch (fieldType)
        {
            case FieldType.String:
                return FormatValueAsString(value);

            case FieldType.Integer:
                return value.ToString();

            case FieldType.Boolean:
                // PostgreSQL boolean format
                bool boolValue = Convert.ToBoolean(value);
                return boolValue ? "TRUE" : "FALSE";

            case FieldType.DateTime:
                // PostgreSQL timestamp format
                if (value is DateTime dt)
                {
                    return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
                }
                return FormatValueAsString(value);

            default:
                // Default to string formatting for unknown types
                return FormatValueAsString(value);
        }
    }

    private string FormatValueAsString(object value)
    {
        if (value == null)
            return "NULL";

        var stringValue = value.ToString();

        // Escape single quotes by doubling them (SQL standard)
        stringValue = stringValue.Replace("'", "''");

        return $"'{stringValue}'";
    }


    public List<Guid> ExtractFieldIds(string statement)
    {
        var fieldIds = new List<Guid>();
        if (string.IsNullOrEmpty(statement)) { return fieldIds; }

        var matches = FieldPattern.Matches(statement);
        foreach (Match match in matches)
        {
            var fieldIdStr = match.Groups[1].Value;
            if (Guid.TryParse(fieldIdStr, out var fieldId))
            {
                fieldIds.Add(fieldId);
            }
        }
        return fieldIds;
    }
}
