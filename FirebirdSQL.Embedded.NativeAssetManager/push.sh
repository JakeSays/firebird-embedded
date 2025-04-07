#!/bin/bash

this_path=$(readlink -f $0)
this_dir=$(dirname $this_path)

dotnet nuget push $this_dir/bin/Release/Std.FirebirdSql.Embedded.NativeAssetManager.1.0.1.nupkg --api-key $1 --source https://apiint.nugettest.org/v3/index.json
