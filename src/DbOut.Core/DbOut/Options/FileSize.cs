using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DbOut.Options;

public sealed class FileSize
{
    private sealed class FileSizeJsonConverter : JsonConverter<FileSize>
    {
        /// <inheritdoc />
        public override FileSize? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Parse(reader.GetString()!);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, FileSize value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }

    /// <summary>
    /// Gets a json converter.
    /// </summary>
    public static JsonConverter<FileSize> JsonConverter { get; } = new FileSizeJsonConverter();
    
    /// <summary>
    /// Creates a new instance
    /// </summary>
    /// <param name="unit">Unit</param>
    /// <param name="unitLength">Unit length</param>
    public FileSize(FileSizeUnit unit, int unitLength)
    {
        Unit = unit;
        UnitLength = unitLength;
    }

    /// <summary>
    /// Parses or throws an exception.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static FileSize Parse(string str) => TryParse(str, out var result)
        ? result
        : throw new FormatException($"Invalid file size value '{str}'");

    /// <summary>
    /// Parses the string to FileSize
    /// </summary>
    /// <param name="str">String to parse</param>
    /// <param name="fileSize">Parsed file size instance</param>
    /// <returns><c>true</c> if the operation succeeded</returns>
    public static bool TryParse(string? str, [NotNullWhen(true)] out FileSize? fileSize)
    {
        fileSize = null;

        if (string.IsNullOrWhiteSpace(str))
            return false;

        var match = Regex.Match(str, @"(\d+)([bkmg])b?");
        if (!match.Success)
            return false;

        var unit = match.Groups[2].Value switch
        {
            "b" => FileSizeUnit.Byte,
            "k" => FileSizeUnit.Kilobyte,
            "m" => FileSizeUnit.Megabyte,
            _ => FileSizeUnit.Gigabyte
        };

        fileSize = new FileSize(unit, int.Parse(match.Groups[1].Value));
        return true;
    }

    /// <summary>
    /// Gets the unit.
    /// </summary>
    public FileSizeUnit Unit { get; }
    
    /// <summary>
    /// Gets the unit length.
    /// </summary>
    public int UnitLength { get; }

    /// <summary>
    /// Gets the resolved file size.
    /// </summary>
    public int ComputedLength => Unit switch
    {
        FileSizeUnit.Byte => UnitLength,
        FileSizeUnit.Kilobyte => UnitLength * 1_000,
        FileSizeUnit.Megabyte => UnitLength * 1_000_000,
        _ => UnitLength * 1_000_000_000
    };

    /// <inheritdoc />
    public override string ToString()
    {
        var unitAbbreviation = Unit switch
        {
            FileSizeUnit.Byte => "b",
            FileSizeUnit.Kilobyte => "kb",
            FileSizeUnit.Megabyte => "mb",
            _ => "gb"
        };

        return $"{UnitLength}{unitAbbreviation}";
    }
}