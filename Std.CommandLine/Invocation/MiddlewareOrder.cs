// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Std.CommandLine.Invocation
{
    public enum MiddlewareOrder
    {
        ExceptionHandler = -2000,
        Configuration = -1000,
        Default = default,
        ErrorReporting = 1000,
    }
}
