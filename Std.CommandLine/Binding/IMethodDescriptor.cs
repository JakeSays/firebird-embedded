// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;


namespace Std.CommandLine.Binding
{
    internal interface IMethodDescriptor
    {
        ModelDescriptor? Parent { get; }

        IReadOnlyList<ParameterDescriptor> ParameterDescriptors { get; }
    }
}
