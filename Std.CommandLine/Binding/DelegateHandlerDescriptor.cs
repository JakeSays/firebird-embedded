// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Invocation;


namespace Std.CommandLine.Binding
{
    internal class DelegateHandlerDescriptor : HandlerDescriptor
    {
        private readonly Delegate _handlerDelegate;

        public DelegateHandlerDescriptor(Delegate handlerDelegate)
        {
            _handlerDelegate = handlerDelegate;
        }

        public override Invokable GetCommandHandler()
        {
            return new ModelBindingCommandHandler(
                _handlerDelegate,
                ParameterDescriptors);
        }

        public override ModelDescriptor? Parent => null;

        protected override IEnumerable<ParameterDescriptor> InitializeParameterDescriptors()
        {
            return _handlerDelegate.Method
                                   .GetParameters()
                                   .Select(p => new ParameterDescriptor(p, this));
        }
    }
}
