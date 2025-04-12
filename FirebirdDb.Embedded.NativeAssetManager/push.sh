#!/bin/bash

this_path=$(readlink -f $0)
this_dir=$(dirname $this_path)

#dotnet nuget push $this_dir/bin/Release/FirebirdDb.Embedded.NativeAssetManager.1.0.3.nupkg --api-key super-test-key  --source http://localhost:5000/v3/index.json
dotnet nuget push $this_dir/bin/Release/FirebirdDb.Embedded.NativeAssetManager.1.0.4.nupkg --api-key $1 --source https://apiint.nugettest.org/v3/index.json
