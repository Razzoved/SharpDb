module HashTests

open Xunit
open SharpDb.Cryptography
open System

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
