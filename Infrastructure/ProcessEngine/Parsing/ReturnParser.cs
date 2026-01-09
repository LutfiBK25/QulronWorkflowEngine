using Infrastructure.ProcessEngine.Execution;
using System.Text.RegularExpressions;

namespace Infrastructure.ProcessEngine.Parsing;

public class ReturnParser
{
    private readonly ModuleCache _moduleCache;

    // Pattern to match RETURNS( ::#5#UUID#::, ::#5#UUID#:: )
    private static readonly Regex ReturnsPattern = new Regex(
        @"RETURNS\s*\((.*?)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public ReturnParser(ModuleCache moduleCache)
    {
        _moduleCache = moduleCache;
    }

    public List<Guid> ParseReturnFields(string statement)
    {
        var fieldIds = new List<Guid>();
        if (string.IsNullOrEmpty(statement))
            return fieldIds;

        var match = ReturnsPattern.Match(statement);
        if (match.Success)
        {
            var returnsClause = match.Groups[1].Value;
            var parser = new FieldParser(_moduleCache);
            fieldIds = parser.ExtractFieldIds(returnsClause);
        }
        return fieldIds;
    }

    public string RemoveReturnsClause(string statement)
    {
        if (string.IsNullOrEmpty(statement))
            return statement;

        return ReturnsPattern.Replace(statement, "").Trim();
    }
}
