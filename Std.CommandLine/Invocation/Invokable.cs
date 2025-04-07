// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Threading.Tasks;


namespace Std.CommandLine.Invocation
{
    internal interface Invokable
    {
        int Invoke(InvocationContext context);
    }
}
