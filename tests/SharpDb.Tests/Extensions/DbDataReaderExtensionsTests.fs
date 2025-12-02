namespace SharpDb.Tests.Extensions

open System
open System.Data
open System.Data.Common
open SharpDb.Extensions
open Xunit

module DbDataReaderExtensionsTests =

    type FakeDbDataReader(columns: (string * Type * obj) list) =
        inherit DbDataReader()
        let columnsArr = columns |> List.toArray
        let mutable currentRow = 0
        override _.FieldCount = columnsArr.Length
        override _.GetName(i) = columnsArr[i] |> fun (n, _, _) -> n
        override _.GetOrdinal(name) =
            columnsArr
            |> Array.tryFindIndex (fun (n, _, _) -> n.Equals(name, StringComparison.OrdinalIgnoreCase))
            |> function Some i -> i | None -> -1
        override _.GetFieldType(i) = columnsArr[i] |> fun (_, t, _) -> t
        override _.IsDBNull(i) = columnsArr[i] |> fun (_, _, v) -> v = null || obj.ReferenceEquals(v, DBNull.Value)
        override _.GetValue(i) = columnsArr[i] |> fun (_, _, v) -> v
        override _.GetString(i) = columnsArr[i] |> fun (_, _, v) -> v :?> string
        override _.GetInt16 (i) = columnsArr[i] |> fun (_, _, v) -> v :?> int16
        override _.GetInt32(i) = columnsArr[i] |> fun (_, _, v) -> v :?> int
        override _.GetInt64(i) = columnsArr[i] |> fun (_, _, v) -> v :?> int64
        override _.GetFloat(i) = columnsArr[i] |> fun (_, _, v) -> v :?> float32
        override _.GetDouble(i) = columnsArr[i] |> fun (_, _, v) -> v :?> double
        override _.GetDecimal(i) = columnsArr[i] |> fun (_, _, v) -> v :?> decimal
        override _.GetBoolean(i) = columnsArr[i] |> fun (_, _, v) -> v :?> bool
        override _.GetChar(i) = columnsArr[i] |> fun (_, _, v) -> v :?> char
        override _.GetGuid(i) = columnsArr[i] |> fun (_, _, v) -> v :?> Guid
        override _.GetDateTime(i) = columnsArr[i] |> fun (_, _, v) -> v :?> DateTime
        override _.GetByte(i) = columnsArr[i] |> fun (_, _, v) -> v :?> byte
        override _.GetFieldValue<'T>(i) = columnsArr[i] |> fun (_, _, v) -> v :?> 'T
        // Unused abstract members
        override _.Depth = 0
        override _.HasRows = columnsArr.Length > 0
        override _.IsClosed = false
        override _.RecordsAffected = 0
        override _.NextResult() = false
        override _.Read() = false
        override _.GetEnumerator() = Seq.empty.GetEnumerator() :> _
        override _.Close() = ()
        override _.Item with get (name: string): obj = raise (NotImplementedException())
        override _.Item with get (ordinal: int): obj = raise (NotImplementedException())
        override _.GetSchemaTable() = raise (NotImplementedException())
        override _.GetChars(i, dataIndex, buffer, bufferIndex, length) = raise (NotImplementedException())
        override _.GetBytes(i, dataIndex, buffer, bufferIndex, length) = raise (NotImplementedException())
        override _.GetValues (values: obj array) = raise (NotImplementedException())
        override _.GetDataTypeName(i) = raise (NotImplementedException())

    let private setupReader columns = new FakeDbDataReader(columns)

    [<Fact>]
    let ``GetValue returns correct value for existing column and type`` () =
        let reader = setupReader [ ("Age", typeof<int>, 42) ]
        let value = reader.GetValue<int>("Age")
        Assert.Equal(42, value)

    [<Fact>]
    let ``GetValue throws ArgumentOutOfRangeException for missing column`` () =
        let reader = setupReader [ ("Name", typeof<string>, "John") ]
        Assert.Throws<ArgumentOutOfRangeException>(fun () -> reader.GetValue<int>("Age") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws InvalidCastException for DBNull and non-nullable type`` () =
        let reader = setupReader [ ("Age", typeof<int>, DBNull.Value) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<int>("Age") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue returns default for nullable type and DBNull`` () =
        let reader = setupReader [ ("Age", typeof<int>, DBNull.Value) ]
        let value = reader.GetValue<Nullable<int>>("Age")
        Assert.Equal(Nullable(), value)

    [<Fact>]
    let ``GetNullableValue returns value for non-null field`` () =
        let reader = setupReader [ ("Score", typeof<int>, 100) ]
        let value = reader.GetNullableValue<int>("Score")
        Assert.Equal(Nullable(100), value)

    [<Fact>]
    let ``GetNullableValue returns None for DBNull`` () =
        let reader = setupReader [ ("Score", typeof<int>, DBNull.Value) ]
        let value = reader.GetNullableValue<int>("Score")
        Assert.Equal(Nullable(), value)

    [<Fact>]
    let ``GetNullableString returns string value`` () =
        let reader = setupReader [ ("Name", typeof<string>, "Alice") ]
        let value = reader.GetNullableString("Name")
        Assert.Equal("Alice", value)

    [<Fact>]
    let ``GetNullableString returns null for DBNull`` () =
        let reader = setupReader [ ("Name", typeof<string>, DBNull.Value) ]
        let value = reader.GetNullableString("Name")
        Assert.Null(value)

    [<Fact>]
    let ``GetValueOrDefault returns value for present column`` () =
        let reader = setupReader [ ("Flag", typeof<bool>, true) ]
        let value = reader.GetValueOrDefault<bool>("Flag", false)
        Assert.True(value)

    [<Fact>]
    let ``GetValueOrDefault returns default for missing column`` () =
        let reader = setupReader [ ("Flag", typeof<bool>, true) ]
        let value = reader.GetValueOrDefault<bool>("Missing", false)
        Assert.False(value)

    [<Fact>]
    let ``GetValueOrDefault returns default for DBNull`` () =
        let reader = setupReader [ ("Flag", typeof<bool>, DBNull.Value) ]
        let value = reader.GetValueOrDefault<bool>("Flag", false)
        Assert.False(value)

    [<Fact>]
    let ``GetValue supports type conversion from string to int`` () =
        let reader = setupReader [ ("Age", typeof<string>, "123") ]
        let value = reader.GetValue<int>("Age")
        Assert.Equal(123, value)

    [<Fact>]
    let ``GetValue supports type conversion from int to string`` () =
        let reader = setupReader [ ("Age", typeof<int>, 123) ]
        let value = reader.GetValue<string>("Age")
        Assert.Equal("123", value)

    [<Fact>]
    let ``GetValue supports type conversion from int to float`` () =
        let reader = setupReader [ ("Value", typeof<int>, 42) ]
        let value = reader.GetValue<float>("Value")
        Assert.Equal(42.0, value)

    [<Fact>]
    let ``GetValue supports type conversion from string to DateTime`` () =
        let dt = DateTime(2023, 1, 2, 3, 4, 5)
        let reader = setupReader [ ("Created", typeof<string>, dt.ToString("o")) ]
        let value = reader.GetValue<DateTime>("Created")
        Assert.Equal(dt, value)

    [<Fact>]
    let ``GetValue supports type conversion from string to Guid`` () =
        let guid = Guid.NewGuid()
        let reader = setupReader [ ("Id", typeof<string>, guid.ToString()) ]
        let value = reader.GetValue<Guid>("Id")
        Assert.Equal(guid, value)

    [<Fact>]
    let ``GetValue supports type conversion from int to bool (nonzero is true)`` () =
        let reader = setupReader [ ("Flag", typeof<int>, 1) ]
        let value = reader.GetValue<bool>("Flag")
        Assert.True(value)

    [<Fact>]
    let ``GetValue supports type conversion from bool to int`` () =
        let reader = setupReader [ ("Flag", typeof<bool>, true) ]
        let value = reader.GetValue<int>("Flag")
        Assert.Equal(1, value)

    [<Fact>]
    let ``GetValue supports type conversion from string to bool (true/false)`` () =
        let reader = setupReader [ ("Flag", typeof<string>, "true") ]
        let value = reader.GetValue<bool>("Flag")
        Assert.True(value)

    [<Fact>]
    let ``GetValue throws InvalidCastException for invalid conversion`` () =
        let reader = setupReader [ ("Value", typeof<string>, "notanint") ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<int>("Value") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue supports type conversion from decimal to double`` () =
        let reader = setupReader [ ("Amount", typeof<decimal>, 123.45M) ]
        let value = reader.GetValue<double>("Amount")
        Assert.Equal(123.45, value, 2)

    [<Fact>]
    let ``GetValue supports type conversion from double to decimal`` () =
        let reader = setupReader [ ("Amount", typeof<double>, 123.45) ]
        let value = reader.GetValue<decimal>("Amount")
        Assert.Equal(123.45M, value)

    [<Fact>]
    let ``GetValue supports type conversion from int to Nullable<int>`` () =
        let reader = setupReader [ ("Age", typeof<int>, 42) ]
        let value = reader.GetValue<Nullable<int>>("Age")
        Assert.Equal(Nullable(42), value)

    [<Fact>]
    let ``GetValue supports type conversion from DBNull to Nullable<DateTime>`` () =
        let reader = setupReader [ ("Created", typeof<DateTime>, DBNull.Value) ]
        let value = reader.GetValue<Nullable<DateTime>>("Created")
        Assert.Equal(Nullable(), value)

    [<Fact>]
    let ``GetValue supports type conversion from string to char`` () =
        let reader = setupReader [ ("Letter", typeof<string>, "A") ]
        let value = reader.GetValue<char>("Letter")
        Assert.Equal('A', value)

    [<Fact>]
    let ``GetValue throws InvalidCastException for string to char with length > 1`` () =
        let reader = setupReader [ ("Letter", typeof<string>, "AB") ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<char>("Letter") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on type conversion from int to byte`` () =
        let reader = setupReader [ ("ByteValue", typeof<int>, 255) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<byte>("ByteValue") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws InvalidCastException for out-of-range int to byte`` () =
        let reader = setupReader [ ("ByteValue", typeof<int>, 300) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<byte>("ByteValue") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue allows upcasting decimal to float`` () =
        let reader = setupReader [ ("Num", typeof<decimal>, 123.0M) ]
        let value = reader.GetValue<float>("Num")
        Assert.Equal(123.0, value)

    [<Fact>]
    let ``GetValue allows upcasting decimal to double`` () =
        let reader = setupReader [ ("Num", typeof<decimal>, 123.0M) ]
        let value = reader.GetValue<double>("Num")
        Assert.Equal(123.0, value)

    // Downcasting between numeric types should throw InvalidCastException

    [<Fact>]
    let ``GetValue throws on downcasting int64 to int32`` () =
        let reader = setupReader [ ("Num", typeof<int64>, 123L) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<int>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting int32 to int16`` () =
        let reader = setupReader [ ("Num", typeof<int>, 123) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<int16>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting int16 to byte`` () =
        let reader = setupReader [ ("Num", typeof<int16>, 123s) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<byte>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting uint64 to uint32`` () =
        let reader = setupReader [ ("Num", typeof<uint64>, 123UL) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<uint32>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting uint32 to uint16`` () =
        let reader = setupReader [ ("Num", typeof<uint32>, 123u) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<uint16>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting uint16 to byte`` () =
        let reader = setupReader [ ("Num", typeof<uint16>, 123us) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<byte>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting float to int`` () =
        let reader = setupReader [ ("Num", typeof<float>, 123.0) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<int>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting float32 to int16`` () =
        let reader = setupReader [ ("Num", typeof<float32>, 123.0f) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<int16>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting decimal to int`` () =
        let reader = setupReader [ ("Num", typeof<decimal>, 123.0M) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<int>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting double to float32`` () =
        let reader = setupReader [ ("Num", typeof<double>, 123.0) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<float32>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting int to sbyte`` () =
        let reader = setupReader [ ("Num", typeof<int>, 123) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<sbyte>("Num") |> ignore) |> ignore

    [<Fact>]
    let ``GetValue throws on downcasting int64 to int8`` () =
        let reader = setupReader [ ("Num", typeof<int64>, 123L) ]
        Assert.Throws<InvalidCastException>(fun () -> reader.GetValue<sbyte>("Num") |> ignore) |> ignore
