﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Threading;
using Std.CommandLine.Binding;
using Std.CommandLine.Help;
using Std.CommandLine.Parsing;


#nullable enable

namespace Std.CommandLine.Invocation
{
    internal class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, Func<IServiceProvider, object?>> _services;

        public ServiceProvider(BindingContext bindingContext)
        {
            _services = new Dictionary<Type, Func<IServiceProvider, object?>>
                        {
                            [typeof(ParseResult)] = _ => bindingContext.ParseResult,
//                            [typeof(IConsole)] = _ => bindingContext.Console,
                            [typeof(CancellationToken)] = _ => CancellationToken.None,
                            [typeof(IHelpBuilder)] = _ => bindingContext.ParseResult.Parser.Configuration.HelpBuilderFactory(bindingContext),
                            [typeof(BindingContext)] = _ => bindingContext
                        };
        }

        public void AddService<T>(Func<IServiceProvider, T> factory) => _services[typeof(T)] = p => factory(p)!;

        public void AddService(Type serviceType, Func<IServiceProvider, object?> factory) => _services[serviceType] = factory;

        public IReadOnlyCollection<Type> AvailableServiceTypes => _services.Keys;

        public object? GetService(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out var factory))
            {
                return factory(this);
            }

            return null;
        }
    }
}
