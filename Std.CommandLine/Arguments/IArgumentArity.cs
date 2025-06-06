﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Std.CommandLine.Arguments
{
    public interface IArgumentArity
    {
        int MinimumNumberOfValues { get;  }

        int MaximumNumberOfValues { get;  }
    }
}
