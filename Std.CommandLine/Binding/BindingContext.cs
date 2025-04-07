// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Arguments;
using Std.CommandLine.Help;
using Std.CommandLine.Invocation;
using Std.CommandLine.Parsing;
using Std.CommandLine.Utility;


#nullable enable

namespace Std.CommandLine.Binding
{
    internal sealed class BindingContext
    {
        private readonly Dictionary<Type, ModelBinder> _modelBindersByValueDescriptor = new Dictionary<Type, ModelBinder>();

        private readonly Dictionary<string, ModelBinder> _bindersByName = new Dictionary<string, ModelBinder>();

        public BindingContext(
            ParseResult parseResult,
            SystemConsole? console = default)
        {
            Console = console ?? DefaultConsoles.StdOut;

            ParseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
            ServiceProvider = new ServiceProvider(this);
        }

        public ParseResult ParseResult { get; set; }

        internal IHelpBuilder HelpBuilder => (IHelpBuilder)ServiceProvider.GetService(typeof(IHelpBuilder))!;

        public SystemConsole Console { get; }

        internal ServiceProvider ServiceProvider { get; }

        public void AddModelBinder(ModelBinder binder) =>
            _modelBindersByValueDescriptor.Add(binder.ValueDescriptor.ValueType, binder);

        public ModelBinder? GetModelBinder(IValueDescriptor valueDescriptor)
        {
            if (valueDescriptor == null)
            {
                throw new ArgumentNullException(nameof(valueDescriptor));
            }

            if (valueDescriptor.ValueName.IsNullOrEmpty())
            {
                return GetModelBinderByType(valueDescriptor);
            }

            if (_bindersByName.TryGetValue(valueDescriptor.ValueName!, out var binder))
            {
                return binder;
            }

            var valueType = valueDescriptor.ValueType;

            if (!valueType.IsSimpleType())
            {
                binder = new ModelBinder(valueDescriptor);
            }
            else
            {
                var binderGenericType = typeof(ModelBinder<>);
                var binderType = binderGenericType.MakeGenericType(valueType);

                binder = Activator.CreateInstance(binderType, valueDescriptor) as ModelBinder;
            }

            _bindersByName.Add(valueDescriptor.ValueName!, binder!);

            return binder;
        }

        public ModelBinder GetModelBinderByType(IValueDescriptor valueDescriptor)
        {
            var valueType = valueDescriptor.ValueType;

            if (_modelBindersByValueDescriptor.TryGetValue(valueType, out var binder))
            {
                return binder;
            }

            if (!valueType.IsSimpleType())
            {
                return new ModelBinder(valueDescriptor);
            }

            var binderGenericType = typeof(ModelBinder<>);
            var binderType = binderGenericType.MakeGenericType(valueType);

            var binderInstance = Activator.CreateInstance(binderType, valueDescriptor) as ModelBinder;

            _modelBindersByValueDescriptor.Add(valueType, binderInstance!);
            return binderInstance!;
        }

        public void AddService(Type serviceType, Func<IServiceProvider, object> factory)
        {
            ServiceProvider.AddService(serviceType, factory);
        }

        public void AddService<T>(Func<IServiceProvider, T> factory)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            ServiceProvider.AddService(typeof(T), s => factory(s));
        }

        internal bool TryGetValueSource(
            IValueDescriptor valueDescriptor,
            [MaybeNullWhen(false)] out IValueSource valueSource)
        {
            if (ServiceProvider.AvailableServiceTypes.Contains(valueDescriptor.ValueType))
            {
                valueSource = new ServiceProviderValueSource();
                return true;
            }

            valueSource = default!;
            return false;
        }

        internal bool TryBindToScalarValue(
            IValueDescriptor valueDescriptor,
            IValueSource valueSource,
            out BoundValue? boundValue)
        {
            if (valueSource.TryGetValue(valueDescriptor, this, out var value))
            {
                if (value is null || valueDescriptor.ValueType.IsInstanceOfType(value))
                {
                    boundValue = new BoundValue(value, valueDescriptor, valueSource);
                    return true;
                }

                var parsed = ArgumentConverter.ConvertObject(
                    valueDescriptor as IArgument ?? new Argument(valueDescriptor.ValueName),
                    valueDescriptor.ValueType,
                    value);

                if (parsed is SuccessfulArgumentConversionResult successful)
                {
                    boundValue = new BoundValue(successful.Value, valueDescriptor, valueSource);
                    return true;
                }
            }

            boundValue = default;
            return false;
        }
    }
}
