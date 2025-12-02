using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Cryptography;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class Base64ToBase64AesEncryptConverter : ValueConverter<string, string>
{
    public Base64ToBase64AesEncryptConverter(byte[] secret)
        : base(
            v => Encrypt(v, secret),
            v => Decrypt(v, secret))
    {
    }

    public static string Encrypt(string plainText, byte[] secret)
    {
        byte[] bytes = Convert.FromBase64String(plainText);
        byte[] encryptedBytes = AesEncryption.DeterministicEncrypt(bytes, secret);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string Decrypt(string cipherText, byte[] secret)
    {
        byte[] encryptedBytes = Convert.FromBase64String(cipherText);
        byte[] bytes = AesEncryption.Decrypt(encryptedBytes, secret);
        return Convert.ToBase64String(bytes);
    }
}
