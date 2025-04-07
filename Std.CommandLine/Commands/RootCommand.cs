// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Std.CommandLine.Commands
{
    /// <summary>
    /// A command representing an application entry point.
    /// </summary>
    internal class RootCommand : Command
    {
        public RootCommand(string scriptName, string description = "")
            : base(scriptName, description)
        {
        }

        /// <summary>
        /// The name of the command. Defaults to the executable name.
        /// </summary>
        public override string Name
        {
            get => base.Name;
            set
            {
                base.Name = value;
                AddAlias(Name);
            }
        }
    }
}
