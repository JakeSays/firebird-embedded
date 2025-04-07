// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Std.CommandLine.Commands;


namespace Std.CommandLine.Help
{
    public interface IHelpBuilder
    {
        void Write(ICommand command);
    }
}
