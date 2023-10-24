using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DbOut.Utilities;

public static class HashUtilities
{
    public static async Task<string> HashFileAsync(FileInfo fileInfo)
    {
        await using var stream = File.OpenRead(fileInfo.FullName);
        var hashBytes = await SHA1.HashDataAsync(stream);
        return Convert.ToHexString(hashBytes);
    }

    public static string Sha(this string str)
    {
        var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(str));
        return Convert.ToHexString(hashBytes);
    }

    public static string Sha<T>(this T obj)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(obj);
        var hashBytes = SHA1.HashData(jsonBytes);
        return Convert.ToHexString(hashBytes);
    }
}