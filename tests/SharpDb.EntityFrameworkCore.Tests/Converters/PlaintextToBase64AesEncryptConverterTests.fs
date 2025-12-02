namespace SharpDb.EntityFrameworkCore.Tests.Converters

open System
open System.Text
open Xunit
open SharpDb.EntityFrameworkCore.Converters

module PlaintextToBase64AesEncryptConverterTests =

    let generateSecret () =
        // Use a fixed key for deterministic tests
        Array.init 32 (fun i -> byte i)

    [<Fact>]
    let ``Encrypt returns different value than input for valid plaintext`` () =
        let secret = generateSecret ()
        let plainText = "Hello, world!"
        let encrypted = PlaintextToBase64AesEncryptConverter.Encrypt(plainText, secret, null)
        Assert.NotEqual<string>(plainText, encrypted)

    [<Fact>]
    let ``Decrypt returns original plaintext after encryption`` () =
        let secret = generateSecret ()
        let plainText = "Sensitive data 123"
        let encrypted = PlaintextToBase64AesEncryptConverter.Encrypt(plainText, secret, null)
        let decrypted = PlaintextToBase64AesEncryptConverter.Decrypt(encrypted, secret, null)
        Assert.Equal(plainText, decrypted)

    [<Fact>]
    let ``Converter round-trip works via ValueConverter`` () =
        let secret = generateSecret ()
        let converter = PlaintextToBase64AesEncryptConverter(secret)
        let plainText = "Round-trip test!"
        let encrypted = converter.ConvertToProvider.Invoke(plainText)
        let decrypted = converter.ConvertFromProvider.Invoke(encrypted) |> string
        Assert.Equal(plainText, decrypted)

    [<Fact>]
    let ``Encrypt and Decrypt work with custom encoding`` () =
        let secret = generateSecret ()
        let plainText = "Český text s diakritikou"
        let encoding = Encoding.UTF32
        let encrypted = PlaintextToBase64AesEncryptConverter.Encrypt(plainText, secret, encoding)
        let decrypted = PlaintextToBase64AesEncryptConverter.Decrypt(encrypted, secret, encoding)
        Assert.Equal(plainText, decrypted)
