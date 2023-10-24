using System.Text;

namespace DbOut.Utilities;

public static class StringExtensions
{
    public static string Indent(this string str, int count)
    {
        var indent = new string('\t', count);
        var split = str.Split(Environment.NewLine);
        var sb = new StringBuilder(str.Length + split.Length * 5);
        var newLine = string.Empty;

        foreach (var line in split)
        {
            sb.Append(newLine);
            sb.Append(indent);
            sb.Append(line);
            newLine = Environment.NewLine;
        }

        return sb.ToString();
    }
}