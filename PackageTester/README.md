#### These are the steps required to use the Firebird embedded packages.

##### Create a new solution in visual studio with a test console project.

Currently the packages are stored in nuget's staging environment, so a custom package source must be added to the solution. In the solution directory, create a file named `NuGet.Config` and add the following XML snippet to it:

```XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <add key="int.nugettest.org" value="https://apiint.nugettest.org/v3/index.json" protocolVersion="3" />
    </packageSources>
</configuration>
```

Note that this is only a temporary requirement. Once I am comfortable with the packages I will push them to nuget.org.

##### Add the native asset nuget packages to the project

Open the package manager and search for `FirebirdDb.Embedded`. Visual studio should find 21 packages. For this test add the following:

- `FirebirdDb.Embedded.V3.NativeAssets.(Linux|Windows).X64`
- `FirebirdDb.Embedded.V4.NativeAssets.(Linux|Windows).X64`
- `FirebirdDb.Embedded.V5.NativeAssets.(Linux|Windows).X64`

Also add `FirebirdSql.Data.FirebirdClient`.

##### Add test source code

The sample code below tests using multiple versions of firebird in the same process. If it works there should be three test .fdb files created in the build output directory.

```c#
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Embedded;

class Program
{
    static void Main(string[] args)
    {
        RunTest(FirebirdVersion.V5);
        RunTest(FirebirdVersion.V4);
        RunTest(FirebirdVersion.V3);
    }

    private static void RunTest(FirebirdVersion version)
    {
        Console.WriteLine($"Test of {version} running.");

        var nativePath = FbNativeAssetManager.NativeAssetPath(version);
        if (nativePath == null)
        {
            Console.WriteLine("Couldn't get native asset path for V5");
            return;
        }

        var dbFile = $"test{version}.fdb";

        File.Delete(dbFile);

        var cs = new FbConnectionStringBuilder
        {
            ClientLibrary = nativePath,
            Database = dbFile,
            ServerType = FbServerType.Embedded,
            UserID = "SYSDBA",
        }.ToString();

        FbConnection.CreateDatabase(cs, overwrite: true);
        using (var connection = new FbConnection(cs))
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                CREATE TABLE "TestTable" (
                "Id" INTEGER NOT NULL,
                "Name" VARCHAR(255),
                "CreateDate" TIMESTAMP,
                CONSTRAINT "id_pk" PRIMARY KEY ("Id"))
                """;
            cmd.ExecuteNonQuery();
        }

        Console.WriteLine($"Test of {version} succeeded.");
    }
}
```

The `FbNativeAssetManager` class is contained in its own package (`FirebirdDb.Embedded.NativeAssetManager`) but it does not need to be added directly. Each native asset package has the asset manager as a dependency, so it will be added to the project automatically.

##### A note on version numbers

Originally the nuget package version numbers matched the firebird release numbers, but this has been changed. In the new scheme the major version number will be incremented whenever the firebird organization releases a new point version of firebird (5.0.2 -> 5.0.3 for example), and the patch version number will restart at 1. The patch version number is used to represent a new version of the package itself, which allows the .nuget files to be updated if, for example, an issue arises in the `.targets` file.
