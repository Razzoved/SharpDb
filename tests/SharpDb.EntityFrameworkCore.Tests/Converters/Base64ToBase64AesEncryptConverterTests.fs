namespace SharpDb.EntityFrameworkCore.Tests.Converters

open System
open Xunit
open SharpDb.EntityFrameworkCore.Converters

module Base64ToBase64AesEncryptConverterTests =

    let generateSecret () =
        // Use a fixed key for deterministic tests
        Array.init 32 (fun i -> byte i)

    [<Fact>]
    let ``Encrypt returns different value than input for valid base64`` () =
        let secret = generateSecret ()
        let plainBytes = [| 1uy; 2uy; 3uy; 4uy |]
        let plainText = Convert.ToBase64String plainBytes
        let encrypted = Base64ToBase64AesEncryptConverter.Encrypt(plainText, secret)
        Assert.NotEqual<string>(plainText, encrypted)

    [<Fact>]
    let ``Decrypt returns original base64 after encryption`` () =
        let secret = generateSecret ()
        let plainBytes = [| 10uy; 20uy; 30uy; 40uy |]
        let plainText = Convert.ToBase64String plainBytes
        let encrypted = Base64ToBase64AesEncryptConverter.Encrypt(plainText, secret)
        let decrypted = Base64ToBase64AesEncryptConverter.Decrypt(encrypted, secret)
        Assert.Equal(plainText, decrypted)

    [<Fact>]
    let ``Converter round-trip works via ValueConverter`` () =
        let secret = generateSecret ()
        let converter = Base64ToBase64AesEncryptConverter(secret)
        let plainBytes = [| 100uy; 200uy; 50uy; 25uy |]
        let plainText = Convert.ToBase64String plainBytes
        let encrypted = converter.ConvertToProvider.Invoke(plainText)
        let decrypted = converter.ConvertFromProvider.Invoke(encrypted) |> string
        Assert.Equal(plainText, decrypted)

    [<Fact>]
    let ``Encrypt throws FormatException for invalid base64 input`` () =
        let secret = generateSecret ()
        let invalidInput = "not_base64"
        Assert.Throws<FormatException>(fun () ->
            Base64ToBase64AesEncryptConverter.Encrypt(invalidInput, secret) |> ignore
    )
