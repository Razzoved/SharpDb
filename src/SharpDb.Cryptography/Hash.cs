using System.Security.Cryptography;
using System.Text;

namespace SharpDb.Cryptography;

/// <summary>
/// Utility class for hashing data.
/// </summary>
/// <remarks>!!! DO NOT USE OUTSIDE YOUR DATABASE PROJECTS !!!</remarks>
public static class Hash
{
    public static string ConvertToSha1(string input)
        => ConvertToSha1(input, Encoding.UTF8);

    public static string ConvertToSha1(string input, Encoding encoding)
    {
        byte[] inputBytes = encoding.GetBytes(input);
        byte[] hashBytes = SHA1.HashData(inputBytes);
        StringBuilder output = new(hashBytes.Length * 2 + 1);
        foreach (byte b in hashBytes)
        {
            output.Append(b.ToString("x2"));
        }
        return output.ToString();
    }

    public static string ConvertToSha256(string input)
        => ConvertToSha256(input, Encoding.UTF8);

    public static string ConvertToSha256(string input, Encoding encoding)
    {
        byte[] inputBytes = encoding.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        StringBuilder output = new(hashBytes.Length * 2 + 1);
        foreach (byte b in hashBytes)
        {
            output.Append(b.ToString("x2"));
        }
        return output.ToString();
    }

    public static string ConvertToSha512(string input)
        => ConvertToSha512(input, Encoding.UTF8);

    public static string ConvertToSha512(string input, Encoding encoding)
    {
        byte[] inputBytes = encoding.GetBytes(input);
        byte[] hashBytes = SHA512.HashData(inputBytes);
        StringBuilder output = new(hashBytes.Length * 2 + 1);
        foreach (byte b in hashBytes)
        {
            output.Append(b.ToString("x2"));
        }
        return output.ToString();
    }

    public static string ConvertToSha3_256(string input)
        => ConvertToSha3_256(input, Encoding.UTF8);

    public static string ConvertToSha3_256(string input, Encoding encoding)
    {
        byte[] inputBytes = encoding.GetBytes(input);
        byte[] hashBytes = SHA3_256.HashData(inputBytes);
        StringBuilder output = new(hashBytes.Length * 2 + 1);
        foreach (byte b in hashBytes)
        {
            output.Append(b.ToString("x2"));
        }
        return output.ToString();
    }

    public static string ConvertToSha3_512(string input)
        => ConvertToSha3_512(input, Encoding.UTF8);

    public static string ConvertToSha3_512(string input, Encoding encoding)
    {
        byte[] inputBytes = encoding.GetBytes(input);
        byte[] hashBytes = SHA3_512.HashData(inputBytes);
        StringBuilder output = new(hashBytes.Length * 2 + 1);
        foreach (byte b in hashBytes)
        {
            output.Append(b.ToString("x2"));
        }
        return output.ToString();
    }

    public static string ConvertToMd5(string input)
        => ConvertToMd5(input, Encoding.UTF8);

    public static string ConvertToMd5(string input, Encoding encoding)
    {
        byte[] inputBytes = encoding.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        StringBuilder output = new(hashBytes.Length * 2 + 1);
        foreach (byte b in hashBytes)
        {
            output.Append(b.ToString("x2"));
        }
        return output.ToString();
    }
}
