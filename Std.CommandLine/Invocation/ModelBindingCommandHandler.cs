// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Std.CommandLine.Binding;


namespace Std.CommandLine.Invocation
{
    internal class ModelBindingCommandHandler : Invokable
    {
        private readonly Delegate? _handlerDelegate;
        private readonly object? _invocationTarget;
        private readonly ModelBinder? _invocationTargetBinder;
        private readonly MethodInfo? _handlerMethodInfo;
        private readonly IReadOnlyList<ParameterDescriptor> _parameterDescriptors;

        public ModelBindingCommandHandler(
            MethodInfo handlerMethodInfo,
            IReadOnlyList<ParameterDescriptor> parameterDescriptors)
        {
            _handlerMethodInfo = handlerMethodInfo ?? throw new ArgumentNullException(nameof(handlerMethodInfo));
            _invocationTargetBinder = _handlerMethodInfo.IsStatic
                                          ? null
                                          : new ModelBinder(_handlerMethodInfo.DeclaringType!);
            _parameterDescriptors = parameterDescriptors ?? throw new ArgumentNullException(nameof(parameterDescriptors));
        }

        public ModelBindingCommandHandler(
            MethodInfo handlerMethodInfo,
            IReadOnlyList<ParameterDescriptor> parameterDescriptors,
            object? invocationTarget)
        {
            _invocationTarget = invocationTarget;
            _handlerMethodInfo = handlerMethodInfo ?? throw new ArgumentNullException(nameof(handlerMethodInfo));
            _parameterDescriptors = parameterDescriptors ?? throw new ArgumentNullException(nameof(parameterDescriptors));
        }

        public ModelBindingCommandHandler(
            Delegate handlerDelegate,
            IReadOnlyList<ParameterDescriptor> parameterDescriptors)
        {
            _handlerDelegate = handlerDelegate ?? throw new ArgumentNullException(nameof(handlerDelegate));
            _parameterDescriptors = parameterDescriptors ?? throw new ArgumentNullException(nameof(parameterDescriptors));
        }

        public int Invoke(InvocationContext context)
        {
            var bindingContext = context.BindingContext;

            var parameterBinders = _parameterDescriptors
                                   .Select(p => bindingContext.GetModelBinder(p))
                                   .ToList();

            var invocationArguments =
                parameterBinders
                    .Select(binder => binder?.CreateInstance(bindingContext))
                    .ToArray();

            var invocationTarget = _invocationTarget ??
                                   _invocationTargetBinder?.CreateInstance(bindingContext);

            var result = _handlerDelegate is null
                ? _handlerMethodInfo!.Invoke(invocationTarget,
                    invocationArguments)
                : _handlerDelegate.DynamicInvoke(invocationArguments);

            return CommandHandler.GetResultCode(result, context);
        }
    }
}
