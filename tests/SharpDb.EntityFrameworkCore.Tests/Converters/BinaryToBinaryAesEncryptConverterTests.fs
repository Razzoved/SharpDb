namespace SharpDb.EntityFrameworkCore.Tests.Converters

open System
open Xunit
open SharpDb.EntityFrameworkCore.Converters
open System.Collections.Generic

module BinaryToBinaryAesEncryptConverterTests =

    let generateSecret () =
        // Use a fixed key for deterministic tests
        Array.init 32 (fun i -> byte i)

    let toResizeArray (bytes: byte[]) =
        ResizeArray<byte>(bytes) :> ICollection<byte>

    [<Fact>]
    let ``Encrypt returns different value than input bytes`` () =
        let secret = generateSecret ()
        let plainBytes = toResizeArray [| 1uy; 2uy; 3uy; 4uy |] :?> ResizeArray<byte>
        let encrypted = BinaryToBinaryAesEncryptConverter<ResizeArray<byte>, ResizeArray<byte>>.Encrypt(plainBytes, secret)
        let asArray = ResizeArray<byte>(plainBytes) :> seq<byte> |> Seq.toArray
        let encryptedArray = ResizeArray<byte>(encrypted) :> seq<byte> |> Seq.toArray
        Assert.NotEqual<byte[]>(asArray, encryptedArray)

    [<Fact>]
    let ``Decrypt returns original bytes after encryption`` () =
        let secret = generateSecret ()
        let plainBytes = toResizeArray [| 10uy; 20uy; 30uy; 40uy |] :?> ResizeArray<byte>
        let encrypted = BinaryToBinaryAesEncryptConverter<ResizeArray<byte>, ResizeArray<byte>>.Encrypt(plainBytes, secret)
        let decrypted = BinaryToBinaryAesEncryptConverter<ResizeArray<byte>, ResizeArray<byte>>.Decrypt(encrypted, secret)
        let originalArray = ResizeArray<byte>(plainBytes) :> seq<byte> |> Seq.toArray
        let decryptedArray = ResizeArray<byte>(decrypted) :> seq<byte> |> Seq.toArray
        Assert.Equal<byte[]>(originalArray, decryptedArray)

    [<Fact>]
    let ``Converter round-trip works via ValueConverter`` () =
        let secret = generateSecret ()
        let converter = BinaryToBinaryAesEncryptConverter<ResizeArray<byte>, ResizeArray<byte>>(secret)
        let plainBytes = toResizeArray [| 100uy; 200uy; 50uy; 25uy |]
        let encrypted = converter.ConvertToProvider.Invoke(plainBytes)
        let decrypted = converter.ConvertFromProvider.Invoke(encrypted) :?> ICollection<byte>
        let originalArray = ResizeArray<byte>(plainBytes) :> seq<byte> |> Seq.toArray
        let decryptedArray = ResizeArray<byte>(decrypted) :> seq<byte> |> Seq.toArray
        Assert.Equal<byte[]>(originalArray, decryptedArray)

    [<Fact>]
    let ``Encrypt and Decrypt work with byte[] types`` () =
        let secret = generateSecret ()
        let plainBytes = [| 5uy; 6uy; 7uy; 8uy |]
        let model = toResizeArray plainBytes :?> ResizeArray<byte>
        let encrypted = BinaryToBinaryAesEncryptConverter<ResizeArray<byte>, ResizeArray<byte>>.Encrypt(model, secret)
        let decrypted = BinaryToBinaryAesEncryptConverter<ResizeArray<byte>, ResizeArray<byte>>.Decrypt(encrypted, secret)
        let decryptedArray = ResizeArray<byte>(decrypted) :> seq<byte> |> Seq.toArray
        Assert.Equal<byte[]>(plainBytes, decryptedArray)
