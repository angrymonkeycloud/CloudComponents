namespace AngryMonkey.CloudComponents.DataGrid.Models;

/// <summary>Writes grid values as CSV fields that a spreadsheet shows as text and never runs as a formula.</summary>
public static class CloudDataGridCsv
{
    /// <summary>The value as one quoted CSV field.</summary>
    public static string Field(object? value)
    {
        string text = Neutralize(value?.ToString() ?? string.Empty);

        return '"' + text.Replace("\"", "\"\"") + '"';
    }

    /// <summary>
    /// Text a spreadsheet would read as a formula is given a leading apostrophe. A leading + or - is left alone when the rest can only
    /// be a number or a phone number, so exported amounts and phone numbers stay usable.
    /// </summary>
    public static string Neutralize(string text)
    {
        if (text.Length == 0)
            return text;

        char first = text[0];

        bool formula = first is '=' or '@' or '\t' or '\r'
            || (first is '+' or '-' && !text.All(character => char.IsAsciiDigit(character) || character is ' ' or '(' or ')' or '.' or ',' or '+' or '-'));

        return formula ? "'" + text : text;
    }
}
