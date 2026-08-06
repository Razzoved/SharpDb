module HashTests

open Xunit
open SharpDb.Cryptography
open System

[<Fact>]
let ``ConvertToSha1_EmptyString_ShouldReturnExpectedHash`` () =
    let data = ""
    let expectedHash = "da39a3ee5e6b4b0d3255bfef95601890afd80709"
    let actualHash = Hash.ConvertToSha1(data)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToSha1_HelloWorld_ShouldReturnExpectedHash`` () =
    let data = "Hello, World!"
    let expectedHash = "0a0a9f2a6772942557ab5355d76af442f8f65e01"
    let actualHash = Hash.ConvertToSha1(data)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToSha1_EncodedString_ShouldReturnExpectedHash`` () =
    let encoding = System.Text.Encoding.BigEndianUnicode
    let data = "SharpDb" |> System.Text.Encoding.UTF8.GetBytes |> encoding.GetString
    let expectedHash = "818aea9218d8f38269add07d83383c10f144b262"
    let actualHash = Hash.ConvertToSha1(data, encoding)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToSha256_EmptyString_ShouldReturnExpectedHash`` () =
    let data = ""
    let expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
    let actualHash = Hash.ConvertToSha256(data)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToSha256_HelloWorld_ShouldReturnExpectedHash`` () =
    let data = "Hello, World!"
    let expectedHash = "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f"
    let actualHash = Hash.ConvertToSha256(data)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToSha256_EncodedString_ShouldReturnExpectedHash`` () =
    let encoding = System.Text.Encoding.BigEndianUnicode
    let data = "SharpDb" |> System.Text.Encoding.UTF8.GetBytes |> encoding.GetString
    let expectedHash = "95c828ad9cc49c96a626f4fa4cea22f259baa1cdbe2d74f98fb97d478a86211d"
    let actualHash = Hash.ConvertToSha256(data, encoding)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToSha512_EmptyString_ShouldReturnExpectedHash`` () =
    let data = ""
    let expectedHash = "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e"
    let actualHash = Hash.ConvertToSha512(data)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToSha512_HelloWorld_ShouldReturnExpectedHash`` () =
    let data = "Hello, World!"
    let expectedHash = "374d794a95cdcfd8b35993185fef9ba368f160d8daf432d08ba9f1ed1e5abe6cc69291e0fa2fe0006a52570ef18c19def4e617c33ce52ef0a6e5fbe318cb0387"
    let actualHash = Hash.ConvertToSha512(data)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToSha512_EncodedString_ShouldReturnExpectedHash`` () =
    let encoding = System.Text.Encoding.BigEndianUnicode
    let data = "SharpDb" |> System.Text.Encoding.UTF8.GetBytes |> encoding.GetString
    let expectedHash = "83f75430fcc754ed487bec4a9c97f9188420bf788ba529262efcd77efa3727f00cb1c40e1580db0c1bbc4e615fc7d8643c25c24c497a287451b67161346df7f6"
    let actualHash = Hash.ConvertToSha512(data, encoding)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToMd5_EmptyString_ShouldReturnExpectedHash`` () =
    let data = ""
    let expectedHash = "d41d8cd98f00b204e9800998ecf8427e"
    let actualHash = Hash.ConvertToMd5(data)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToMd5_HelloWorld_ShouldReturnExpectedHash`` () =
    let data = "Hello, World!"
    let expectedHash = "65a8e27d8879283831b664bd8b7f0ad4"
    let actualHash = Hash.ConvertToMd5(data)
    Assert.Equal<string>(expectedHash, actualHash)

[<Fact>]
let ``ConvertToMd5_EncodedString_ShouldReturnExpectedHash`` () =
    let encoding = System.Text.Encoding.BigEndianUnicode
    let data = "SharpDb" |> System.Text.Encoding.UTF8.GetBytes |> encoding.GetString
    let expectedHash = "27de20c5fd47354380ca26eea1de43d7"
    let actualHash = Hash.ConvertToMd5(data, encoding)
    Assert.Equal<string>(expectedHash, actualHash)
