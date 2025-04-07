using System.Runtime.InteropServices;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Embedded;

namespace PackageTester;

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

        var dbFile = $"/p/firebird/test{version}.fdb";

        File.Delete(dbFile);

        var cs = new FbConnectionStringBuilder
        {
            ClientLibrary = nativePath,
            Database = dbFile,
            ServerType = FbServerType.Embedded,
            UserID = "SYSDBA",
            Password = "something-special"
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
