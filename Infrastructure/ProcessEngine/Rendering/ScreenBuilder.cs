


using Domain.ProcessEngine.Entities;
using Infrastructure.ProcessEngine.Execution;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infrastructure.ProcessEngine.Rendering;

/// <summary>
/// Builds screen JSON from screen format modules and session field values
/// Reads screen formate modules from cache
/// Substitue field values from session
/// Generates JSON matching your format
/// Handles heading, content, options, prompts, masking
/// </summary>
public class ScreenBuilder
{
    private readonly ModuleCache _moduleCache;

    public ScreenBuilder(ModuleCache moduleCache)
    {
        _moduleCache = moduleCache; 
    }


    public string BuildScreenJson(ScreenFormatModule screenFormat, ExecutionSession session)
    {
        var screenData = new ScreenData();

        // Process each detail element in sequence
        foreach (var detail in screenFormat.Details.OrderBy(d => d.Sequence))
        {
            ProcessDetailElement(detail, session, screenData);
        }

        // Build final JSON
        var json = JsonSerializer.Serialize(screenData, new JsonSerializerOptions { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase});

        return json;
    }

    private void ProcessDetailElement(ScreenFormatDetail detail, ExecutionSession session, ScreenData screenData)
    {
        string value = GetElementValue(detail, session);

        // Route to appropriate screen section based on data usage and position
        switch(detail.DataUsage)
        {
            case 1: // Input field
                AddInputField(detail, value, screenData);
                break;

            case 3: // Read/Display field
                AddDisplayField(detail, value, screenData);
                break;

            case 4: // Label
                AddLabel(detail, value, screenData);
                break;

            default: //Unknown usage, add as display
                AddDisplayField(detail, value, screenData);
                break;
        }
    }

    private string GetElementValue(ScreenFormatDetail detail, ExecutionSession session)
    {
        if(detail.DataType == 0) // Literal
        {
            return detail.FormatId == "DEFAULT" ? "" : detail.FormatId;
        }

        if (detail.DataType == 17 && detail.DataId.HasValue) // Field reference
        {
            var fieldValue = session.GetFieldValue(detail.DataId.Value);
            return fieldValue?.ToString() ?? "";
        }

        if (detail.DataType == -1)
        {
            return "";
        }

        return "";
    }

    private void AddInputField(ScreenFormatDetail detail, string value, ScreenData screenData)
    {
        if(screenData.Prompt == null)
        {
            screenData.Prompt = new PromptData();
        }

        screenData.Prompt.DefaultValue = value;
        screenData.Prompt.DisplayValue = value;
        screenData.Prompt.Masked = new MaskData
        {
            On = detail.Echo == 1 ? "TRUE" : "FALSE",
            Char = "*"
        };
        screenData.Prompt.InputFieldId = detail.DataId;
    }

    private void AddDisplayField(ScreenFormatDetail detail, string value, ScreenData screenData)
    {
        // Determine where this field foes based on row position
        if (detail.PosRow == 1) // Header row
        {
            screenData.Heading = value;
        }

        else if (detail.PosRow >= 2 && detail.PosRow <= 5)
        {
            if (screenData.Content == null)
            {
                screenData.Content = new ContentData { Lines = new List<string>() };
            }

            if (detail.PosRow == 2)
                screenData.Content.Paragraph = value;
            else
                screenData.Content.Lines.Add(value);
        }
        else if (detail.PosRow == 8) // Options Row
        {
            ParseOptions(value, screenData);
        }

    }

    private void AddLabel(ScreenFormatDetail detail, string value, ScreenData screenData)
    {
        // Labels are typically prompts or field labels
        if(detail.PosRow == 6 || detail.PosRow ==7)
        {
            if(screenData.Prompt == null)
            {
                screenData.Prompt = new PromptData();
            }
            screenData.Prompt.Label = value;
        }
    }

    private void ParseOptions(string optionsString, ScreenData screenData)
    {
        if (string.IsNullOrEmpty(optionsString)) return;

        screenData.Options = new List<OptionData>();

        // Parse format: "F1:Help F2:Menu F5:Version"
        var parts = optionsString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split(':');
            if (keyValue.Length == 2)
            {
                screenData.Options.Add(new OptionData
                {
                    Value = keyValue[0].Trim(),
                    Text = keyValue[1].Trim(),
                });
            }
        }
    }
}

// =============================================
// Screen JSON Data Structures
// =============================================

public class ScreenData
{
    public string Heading { get; set;  }
    public ContentData Content { get; set; }
    public List<OptionData> Options { get; set; }
    public PromptData Prompt { get; set; }
}

public class ContentData
{
    public string Paragraph { get; set; }
    public List<string> Lines { get; set; }
}

public class OptionData
{
    public string Value { get; set; }
    public string Text { get; set; }
}

public class PromptData
{
    public string Label { get; set; }
    public string DefaultValue { get; set; }
    public string DisplayValue { get; set; }
    public MaskData Masked { get; set; }
    public Guid? InputFieldId { get; set; }
}

public class MaskData
{
    public string On { get; set; }
    public string Char { get; set; }
}

// =============================================
// Example Output JSON:
// =============================================
/*
{
  "heading": "Qulron Software",
  "content": {
    "paragraph": "Warehouse",
    "lines": ["Advantage", "Version 1.0"]
  },
  "options": [
    { "value": "F5", "text": "Version" }
  ],
  "prompt": {
    "label": "USER ID",
    "defaultValue": "",
    "displayValue": "USER ID",
    "masked": {
      "on": "FALSE",
      "char": "*"
    },
    "inputFieldId": "field-guid-here"
  }
}
*/