using System.Security.Cryptography;
using System.Text;

namespace SharpDb.Cryptography;

/// <summary>
/// Utility class for hashing data.
/// </summary>
/// <remarks>!!! DO NOT USE OUTSIDE OF YOUR DATABASE PROJECTS !!!</remarks>
public static class Hash
{
    public static string ConvertToSha256(string input)
    {
        return ConvertToSha256(input, Encoding.UTF8);
    }

    public static string ConvertToSha256(string input, Encoding encoding)
    {
        byte[] inputBytes = encoding.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        StringBuilder output = new();
        foreach (byte b in hashBytes)
        {
            output.Append(b.ToString("x2"));
        }
        return output.ToString();
    }

    public static string ConvertToMd5(string input)
    {
        return ConvertToMd5(input, Encoding.UTF8);
    }

    public static string ConvertToMd5(string input, Encoding encoding)
    {
        byte[] inputBytes = encoding.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        StringBuilder output = new();
        foreach (byte b in hashBytes)
        {
            output.Append(b.ToString("x2"));
        }
        return output.ToString();
    }
}
