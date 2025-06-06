﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Std.CommandLine.Binding
{
    internal interface IValueSource
    {
        bool TryGetValue(
            IValueDescriptor valueDescriptor,
            BindingContext? bindingContext,
            out object? boundValue);
    }
}
