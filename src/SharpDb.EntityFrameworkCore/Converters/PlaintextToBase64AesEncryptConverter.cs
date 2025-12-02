using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Cryptography;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class PlaintextToBase64AesEncryptConverter : ValueConverter<string, string>
{
    public PlaintextToBase64AesEncryptConverter(byte[] secret, Encoding? encoding = null)
        : base(
            v => Encrypt(v, secret, encoding),
            v => Decrypt(v, secret, encoding))
    {
    }

    public static string Encrypt(string plainText, byte[] secret, Encoding? encoding)
    {
        byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(plainText);
        byte[] encryptedBytes = AesEncryption.DeterministicEncrypt(bytes, secret);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string Decrypt(string cipherText, byte[] secret, Encoding? encoding)
    {
        byte[] encryptedBytes = Convert.FromBase64String(cipherText);
        byte[] bytes = AesEncryption.Decrypt(encryptedBytes, secret);
        return (encoding ?? Encoding.UTF8).GetString(bytes);
    }
}
