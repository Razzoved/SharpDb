module AesEncryptTests

open Xunit
open SharpDb.Cryptography

[<Fact>]
let ``DeterministicEncrypt_EmptyData_Key16_ShouldReturnOk`` () =
    let result = AesEncryption.DeterministicEncrypt(seq { (byte)1 .. (byte)100 } |> Seq.toArray, seq { (byte)1 .. (byte)16 } |> Seq.toArray )
    Assert.NotNull(result)
    Assert.True(result.Length > 16)

[<Fact>]
let ``DeterministicEncrypt_EmptyData_Key24_ShouldReturnOk`` () =
    let result = AesEncryption.DeterministicEncrypt(Array.empty, seq { (byte)1 .. (byte)24 } |> Seq.toArray )
    Assert.NotNull(result)
    Assert.True(result.Length > 16)

[<Fact>]
let ``DeterministicEncrypt_EmptyData_Key32_ShouldReturnOk`` () =
    let result = AesEncryption.DeterministicEncrypt(Array.empty, seq { (byte)1 .. (byte)32 } |> Seq.toArray )
    Assert.NotNull(result)
    Assert.True(result.Length > 16)

[<Fact>]
let ``DeterministicEncrypt_EmptyData_KeyEmpty_ShouldFail`` () =
    Assert.ThrowsAny<System.Exception>(fun () ->
        AesEncryption.DeterministicEncrypt(Array.empty, Array.empty )
        |> ignore)

[<Fact>]
let ``DeterministicEncrypt_EmptyData_Key8_ShouldFail`` () =
    Assert.ThrowsAny<System.Exception>(fun () ->
        AesEncryption.DeterministicEncrypt(Array.empty, seq { (byte)1 .. (byte)8 } |> Seq.toArray )
        |> ignore)

[<Fact>]
let ``DeterministicEncrypt_EmptyData_Key400_ShouldFail`` () =
    Assert.ThrowsAny<System.Exception>(fun () ->
        AesEncryption.DeterministicEncrypt(Array.empty, seq { (byte)1 .. (byte)400 } |> Seq.toArray )
        |> ignore)

[<Fact>]
let ``DeterministicEncrypt_SmallData_ShouldReturnOk`` () =
    let data = System.Text.Encoding.UTF8.GetBytes("Hello, World!")
    let key = seq { (byte)1 .. (byte)16 } |> Seq.toArray
    let result = AesEncryption.DeterministicEncrypt(data, key)
    Assert.NotNull(result)
    Assert.True(result.Length > 16)

[<Fact>]
let ``DeterministicEncrypt_LargeData_ShouldReturnOk`` () =
    let data = Array.init 10000 (fun i -> byte (i % 256))
    let key = seq { (byte)1 .. (byte)16 } |> Seq.toArray
    let result = AesEncryption.DeterministicEncrypt(data, key)
    Assert.NotNull(result)
    Assert.True(result.Length > 16)

[<Fact>]
let ``DeterministicEncrypt_SameDataSameKey_ShouldReturnSameCiphertext`` () =
    let data = System.Text.Encoding.UTF8.GetBytes("Consistent Data")
    let key = seq { (byte)1 .. (byte)16 } |> Seq.toArray
    let result1 = AesEncryption.DeterministicEncrypt(data, key)
    let result2 = AesEncryption.DeterministicEncrypt(data, key)
    Assert.Equal<byte>(result1, result2)

[<Fact>]
let ``DeterministicEncrypt_SameDataDifferentKeys_ShouldReturnDifferentCiphertexts`` () =
    let data = System.Text.Encoding.UTF8.GetBytes("Consistent Data")
    let key1 = seq { (byte)1 .. (byte)16 } |> Seq.toArray
    let key2 = seq { (byte)16 .. (byte)31 } |> Seq.toArray
    let result1 = AesEncryption.DeterministicEncrypt(data, key1)
    let result2 = AesEncryption.DeterministicEncrypt(data, key2)
    Assert.NotEqual<byte>(result1, result2)

[<Fact>]
let ``Decrypt_ShouldReturnOriginalData`` () =
    let data = System.Text.Encoding.UTF8.GetBytes("Secret Message")
    let key = seq { (byte)1 .. (byte)16 } |> Seq.toArray
    let encrypted = AesEncryption.DeterministicEncrypt(data, key)
    let decrypted = AesEncryption.Decrypt(encrypted, key)
    Assert.Equal<byte>(data, decrypted)
