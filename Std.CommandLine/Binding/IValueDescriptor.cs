// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;


namespace Std.CommandLine.Binding
{
    public interface IValueDescriptor
    {
        string? ValueName { get; }

        Type ValueType { get; }

        bool HasDefaultValue { get; }

        object? GetDefaultValue();
    }
}
