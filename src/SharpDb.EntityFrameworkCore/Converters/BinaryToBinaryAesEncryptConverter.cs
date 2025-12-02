using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Cryptography;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class BinaryToBinaryAesEncryptConverter<TModel, TProvider> : ValueConverter<TModel, TProvider>

    where TModel : ICollection<byte>, new()
    where TProvider : ICollection<byte>, new()
{
    public BinaryToBinaryAesEncryptConverter(byte[] secret)
        : base(
            v => Encrypt(v, secret),
            v => Decrypt(v, secret))
    {
    }

    public static TProvider Encrypt(TModel bytes, byte[] secret)
    {
        if (bytes is not byte[] bytesArray)
        {
            bytesArray = [.. bytes];
        }
        byte[] encryptedBytes = AesEncryption.DeterministicEncrypt(bytesArray, secret);
        if (encryptedBytes is not TProvider provider)
        {
            provider = [.. encryptedBytes];
        }
        return provider;
    }

    public static TModel Decrypt(TProvider encryptedBytes, byte[] secret)
    {
        if (encryptedBytes is not byte[] encryptedBytesArray)
        {
            encryptedBytesArray = [.. encryptedBytes];
        }
        byte[] bytes = AesEncryption.Decrypt(encryptedBytesArray, secret);
        if (bytes is not TModel model)
        {
            model = [.. bytes];
        }
        return model;
    }
}
