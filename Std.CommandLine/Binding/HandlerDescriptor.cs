// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Reflection;
using Std.CommandLine.Invocation;


namespace Std.CommandLine.Binding
{
    internal abstract class HandlerDescriptor : IMethodDescriptor
    {
        private List<ParameterDescriptor>? _parameterDescriptors;

        public abstract Invokable GetCommandHandler();

        public abstract ModelDescriptor? Parent { get; }

        public IReadOnlyList<ParameterDescriptor> ParameterDescriptors =>
            _parameterDescriptors ??= [..InitializeParameterDescriptors()];

        protected abstract IEnumerable<ParameterDescriptor> InitializeParameterDescriptors();

        public override string ToString() =>
            $"{Parent} ({string.Join(", ", ParameterDescriptors)})";

        public static HandlerDescriptor FromMethodInfo(MethodInfo methodInfo, object? target = null) =>
            new MethodInfoHandlerDescriptor(methodInfo, target);

        public static HandlerDescriptor FromDelegate(Delegate @delegate) =>
            new DelegateHandlerDescriptor(@delegate);
    }
}
