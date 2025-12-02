namespace SharpDb.Tests

open SharpDb
open Xunit

module DbConnectionInfoTests =

    [<Fact>]
    let ``FromConnectionString parses server and database names`` () =
        let connStr = "Data Source=MyServer;Initial Catalog=MyDb;"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("MyServer", info.ServerName)
        Assert.Equal("MyDb", info.DatabaseName)
        Assert.Equal("[MyServer].[MyDb].[dbo].", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString handles missing server name`` () =
        let connStr = "Initial Catalog=MyDb;"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("", info.ServerName)
        Assert.Equal("MyDb", info.DatabaseName)
        Assert.Equal("[MyDb].[dbo].", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString handles missing database name`` () =
        let connStr = "Data Source=MyServer;"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("MyServer", info.ServerName)
        Assert.Equal("", info.DatabaseName)
        Assert.Equal("[MyServer].", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString handles empty connection string`` () =
        let info = DbConnectionInfo.FromConnectionString("")
        Assert.Equal("", info.ServerName)
        Assert.Equal("", info.DatabaseName)
        Assert.Equal("", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString parses alternative server and database names`` () =
        let connStr = "Server=AltServer;Database=AltDb;"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("AltServer", info.ServerName)
        Assert.Equal("AltDb", info.DatabaseName)
        Assert.Equal("[AltServer].[AltDb].[dbo].", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString parses Address as server name`` () =
        let connStr = "Address=AddrServer;Initial Catalog=AddrDb;"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("AddrServer", info.ServerName)
        Assert.Equal("AddrDb", info.DatabaseName)
        Assert.Equal("[AddrServer].[AddrDb].[dbo].", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString trims whitespace and ignores case in keys`` () =
        let connStr = "  data source =  WeirdServer  ;  INITIAL CATALOG =  WeirdDb  ;"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("WeirdServer", info.ServerName)
        Assert.Equal("WeirdDb", info.DatabaseName)
        Assert.Equal("[WeirdServer].[WeirdDb].[dbo].", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString handles mixed key synonyms and extra spaces`` () =
        let connStr = "Server = Svr ; Database = Db ; Address = Ignored ;"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("Svr", info.ServerName)
        Assert.Equal("Db", info.DatabaseName)
        Assert.Equal("[Svr].[Db].[dbo].", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString returns empty for missing values`` () =
        let connStr = "Data Source= ; Initial Catalog= ;"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("", info.ServerName)
        Assert.Equal("", info.DatabaseName)
        Assert.Equal("", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString handles brackets in server and database names`` () =
        let connStr = "Data Source=[BracketServer];Initial Catalog=[BracketDb];"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("[BracketServer]", info.ServerName)
        Assert.Equal("[BracketDb]", info.DatabaseName)
        Assert.Equal("[BracketServer].[BracketDb].[dbo].", info.TablePrefix)

    [<Fact>]
    let ``FromConnectionString handles mixed casing in keys`` () =
        let connStr = "DaTa SoUrCe=CaseSrv;InItIaL CaTaLoG=CaseDb;"
        let info = DbConnectionInfo.FromConnectionString(connStr)
        Assert.Equal("CaseSrv", info.ServerName)
        Assert.Equal("CaseDb", info.DatabaseName)
        Assert.Equal("[CaseSrv].[CaseDb].[dbo].", info.TablePrefix)
