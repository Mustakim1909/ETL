using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;

public class MultiFormatDateTimeConverter : DefaultTypeConverter
{
    private readonly string[] formats = new[]
    {
        "dd-MM-yyyy hh:mm:ss tt",
        "dd-MM-yyyy HH:mm:ss",
        "dd-MM-yyyy",
        "MM/dd/yyyy",
        "yyyy-MM-dd",
        "yyyy-MM-dd HH:mm:ss",
        "HH:mm:ss",
        "hh:mm:ss tt"
    };

    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();

        // Handle time-only strings like "12:00:00"
        if (TimeSpan.TryParse(text, out var timeOnly))
        {
            return DateTime.Today.Add(timeOnly);
        }

        // Try exact formats
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt;
        }

        // Fallback to general parse
        if (DateTime.TryParse(text, out var parsed))
        {
            return parsed;
        }

        throw new FormatException($"Cannot parse '{text}' as DateTime.");
    }

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is DateTime dt)
        {
            return dt.ToString("dd-MM-yyyy hh:mm:ss tt");
        }

        return base.ConvertToString(value, row, memberMapData);
    }
}
