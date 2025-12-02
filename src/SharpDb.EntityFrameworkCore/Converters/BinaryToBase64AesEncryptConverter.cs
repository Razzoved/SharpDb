using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Cryptography;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class BinaryToBase64AesEncryptConverter : ValueConverter<byte[], string>
{
    public BinaryToBase64AesEncryptConverter(byte[] secret)
        : base(
            v => Encrypt(v, secret),
            v => Decrypt(v, secret))
    {
    }

    public static string Encrypt(byte[] bytes, byte[] secret)
    {
        byte[] encryptedBytes = AesEncryption.DeterministicEncrypt(bytes, secret);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static byte[] Decrypt(string cipherText, byte[] secret)
    {
        byte[] encryptedBytes = Convert.FromBase64String(cipherText);
        return AesEncryption.Decrypt(encryptedBytes, secret);
    }
}
