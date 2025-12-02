using System.Security.Cryptography;

namespace SharpDb.Cryptography;

/// <summary>
/// Utility class for AES encryption and decryption.
/// </summary>
/// <remarks>!!! DO NOT USE OUTSIDE OF YOUR DATABASE PROJECTS !!!</remarks>
public static class AesEncryption
{
    public static byte[] DeterministicEncrypt(byte[] bytes, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = MD5.HashData(bytes)[..16];
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);

        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
        csEncrypt.Write(bytes);
        csEncrypt.FlushFinalBlock();

        return msEncrypt.ToArray();
    }

    public static byte[] Decrypt(byte[] cipherBytes, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = cipherBytes[..16];
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var outStream = new MemoryStream();

        try
        {
            csDecrypt.CopyTo(outStream);
            return outStream.ToArray();
        }
        catch (CryptographicException e)
        {
            throw new CryptographicException("Decryption failed", e);
        }
    }
}
