namespace SharpDb.EntityFrameworkCore.Tests.Converters

open System
open Xunit
open SharpDb.EntityFrameworkCore.Converters

module BinaryToBase64AesEncryptConverterTests =

    let generateSecret () =
        // Use a fixed key for deterministic tests
        Array.init 32 (fun i -> byte i)

    [<Fact>]
    let ``Encrypt returns different value than input bytes`` () =
        let secret = generateSecret ()
        let plainBytes = [| 1uy; 2uy; 3uy; 4uy |]
        let encrypted = BinaryToBase64AesEncryptConverter.Encrypt(plainBytes, secret)
        let asBase64 = Convert.ToBase64String plainBytes
        Assert.NotEqual<string>(asBase64, encrypted)

    [<Fact>]
    let ``Decrypt returns original bytes after encryption`` () =
        let secret = generateSecret ()
        let plainBytes = [| 10uy; 20uy; 30uy; 40uy |]
        let encrypted = BinaryToBase64AesEncryptConverter.Encrypt(plainBytes, secret)
        let decrypted = BinaryToBase64AesEncryptConverter.Decrypt(encrypted, secret)
        Assert.Equal<byte[]>(plainBytes, decrypted)

    [<Fact>]
    let ``Converter round-trip works via ValueConverter`` () =
        let secret = generateSecret ()
        let converter = BinaryToBase64AesEncryptConverter(secret)
        let plainBytes = [| 100uy; 200uy; 50uy; 25uy |]
        let encrypted = converter.ConvertToProvider.Invoke(plainBytes)
        let decrypted = converter.ConvertFromProvider.Invoke(encrypted) :?> byte[]
        Assert.Equal<byte[]>(plainBytes, decrypted)

    [<Fact>]
    let ``Decrypt throws FormatException for invalid base64 input`` () =
        let secret = generateSecret ()
        let invalidInput = "not_base64"
        Assert.Throws<FormatException>(fun () ->
            BinaryToBase64AesEncryptConverter.Decrypt(invalidInput, secret) |> ignore
        )
