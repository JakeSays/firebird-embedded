﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;


namespace Std.CommandLine.Suggestions
{
    public interface ISuggestionSource
    {
        IEnumerable<string?> GetSuggestions(string? textToMatch = null);
    }
}
