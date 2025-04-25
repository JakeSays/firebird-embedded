### Firebird Embedded Package Builder

This tool is used to build the native asset `.nupkg` files for using FirebirdSQL as an embedded database.

Packages are created for all three product versions of firebird: 3, 4, and 5. Version 6 will be added when it is officially released.

Currently, there are 15 native asset packages that are built. They target Windows X32/X64, Linux X32/X64, and for firebird 5, Linux Arm32/Arm64.
There are also 6 "consolidated" packages built, and these packages contain the binary assets for all architectures
for each operating system.

The steps used to build these packages are:
1. Query the current release history from firebird's github repo.
2. Determine which firebird versions have new releases.
3. If there are new releases, download the `.tar.gz` or `.zip` install files from github.
4. Unpack the files.
5. Move the requisite binaries and timezone files to directories that match the nuget package layout.
6. Generate a readme and msbuild `.targets` files for each package.
7. Create the actual `.nupkg` files.

After the packages are built the tool can be re-run to push them to nuget.org.

#### Package Versioning

Things are a little confusing here because there are two entities whose versions need
to be tracked: the firebird binaries, and the packages themselves (`.target` files, the readme's, and other packaging related metadata).

To facilitate both entities, the package version is partitioned using the major and patch numbers.
The major number tracks the firebird binary version, and the patch tracks the package itself.
For instance, the current release of firebird 5 is 5.0.2. The initial release of the package is at version 1.0.1.
If a change were made to the package itself (say, a `.targets` file fix) then the package version would become 1.0.2.
When firebird releases 5.0.3 then the package version would become 2.0.2, reflecting
the updated firebird binaries but no changes to the package metadata.

#### The Native Asset Manager

There is one additional package that is created and it contains the native asset manager. Its purpose is to locate
the path of the native firebird binaries at runtime. It supports locating the assets in the build output directories,
traditional deployments, self-contained deployments, and self extracting single file deployments.
Note that non-self extracting single file deployments are not yet supported.

Each of the native asset `.nupkg` files has as a dependency the native asset manager package.
The only time an application needs to explicitly add the asset manager as a dependency is if there is a new version
and the native packages still reference an older one.

An example of using the asset manager:

```csharp
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Embedded;

var nativePath = FbNativeAssetManager.NativeAssetPath(FirebirdVersion.V5);
if (nativePath == null)
{
    Console.WriteLine("Couldn't get native asset path for V5");
    return;
}

var cs = new FbConnectionStringBuilder
{
    ClientLibrary = nativePath,
    Database = "test-v5.fdb",
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
```

#### Using Multiple Firebird Versions At Runtime

The .net firebird client supports using multiple versions of firebird in the same process space.
This can be handy in migration scenarios where one connection can load the version 3 native assets
and another the version 5 (or some combination of V3, V4, V5). While not extensively tested this
does work fine on Linux and all three versions can be loaded simultaneously.

However, on Windows things aren't so smooth. There is an issue loading version 3 with 4 or 5. Versions 4 and 5
seem to work well together, but throw version 3 into the mix and your day is ruined.

