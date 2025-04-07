// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Std.CommandLine.Utility;
// ReSharper disable ParameterTypeCanBeEnumerable.Local


namespace Std.CommandLine.Binding
{
    internal class ModelBinder
    {
        public ModelBinder(Type modelType, ModelDescriptor? descriptor = null)
            : this(new AnonymousValueDescriptor(modelType))
        {
            if (modelType is null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            ModelDescriptor = descriptor;
        }

        internal ModelBinder(IValueDescriptor valueDescriptor)
        {
            ValueDescriptor = valueDescriptor ?? throw new ArgumentNullException(nameof(valueDescriptor));

            ModelDescriptor = ModelDescriptor.FromType(valueDescriptor.ValueType);
        }

        public ModelDescriptor? ModelDescriptor { get; }

        public IValueDescriptor ValueDescriptor { get; }

        public bool EnforceExplicitBinding { get; set; }

        internal Dictionary<IValueDescriptor, IValueSource> ConstructorArgumentBindingSources { get; } =
            new Dictionary<IValueDescriptor, IValueSource>();

        internal Dictionary<IValueDescriptor, IValueSource> MemberBindingSources { get; } =
            new Dictionary<IValueDescriptor, IValueSource>();

        protected ConstructorDescriptor FindModelConstructorDescriptor(
            ConstructorInfo constructorInfo)
        {
            var cmpCtorDesc = new ConstructorDescriptor(constructorInfo,
                // Parent does not matter for comparison and can be invalid.
                parent: ModelDescriptor!);
            var cmpParamDescs = cmpCtorDesc.ParameterDescriptors
                .Select(GetParameterDescriptorComparands)
                .ToList();

            return ModelDescriptor!.ConstructorDescriptors
                .FirstOrDefault(matchCtorDesc =>
                {
                    if (matchCtorDesc.Parent.ModelType != constructorInfo.DeclaringType)
                    {
                        return false;
                    }

                    return matchCtorDesc.ParameterDescriptors
                        .Select(GetParameterDescriptorComparands)
                        .SequenceEqual(cmpParamDescs);
                })!;

            // Name matching is not necessary for overload descisions.
            static (Type paramType, bool allowNull, bool hasDefaultValue)
                GetParameterDescriptorComparands(ParameterDescriptor desc) =>
                (desc.ValueType, desc.AllowsNull, desc.HasDefaultValue);
        }

        protected IValueDescriptor FindModelPropertyDescriptor(
            Type propertyType, string propertyName) =>
            ModelDescriptor?.PropertyDescriptors
                .FirstOrDefault(desc =>
                    desc.ValueType == propertyType &&
                    string.Equals(desc.ValueName, propertyName, StringComparison.Ordinal)
                )!;

        public void BindConstructorArgumentFromValue(ParameterInfo parameter,
            IValueDescriptor valueDescriptor)
        {
            if (parameter.Member is not ConstructorInfo constructor)
                throw new ArgumentException(paramName: nameof(parameter),
                    message: "Parameter must be declared on a constructor.");

            var ctorDesc = FindModelConstructorDescriptor(constructor);
            if (ctorDesc is null)
                throw new ArgumentException(paramName: nameof(parameter),
                    message: "Parameter is not described by any of the model constructor descriptors.");

            var paramDesc = ctorDesc.ParameterDescriptors[parameter.Position];
            ConstructorArgumentBindingSources[paramDesc] =
                new SpecificSymbolValueSource(valueDescriptor);
        }

        public void BindMemberFromValue(PropertyInfo property,
            IValueDescriptor valueDescriptor)
        {
            var propertyDescriptor = FindModelPropertyDescriptor(
                property.PropertyType, property.Name);
            if (propertyDescriptor is null)
                throw new ArgumentException(paramName: nameof(property),
                    message: "Property is not described by any of the model property descriptors.");

            MemberBindingSources[propertyDescriptor] =
                new SpecificSymbolValueSource(valueDescriptor);
        }

        public object? CreateInstance(BindingContext context)
        {
            var values = GetValues(
                // No binding sources, as were are attempting to bind a value
                // for the model itself, not for its ctor args or its members.
                bindingSources: null,
                bindingContext: context,
                [ValueDescriptor],
                includeMissingValues: false);

            if (values.Count == 1 &&
                (ModelDescriptor?.ModelType.IsAssignableFrom(values[0].ValueDescriptor.ValueType) ?? false))
            {
                return values[0].Value;
            }

            return TryDefaultConstructorAndPropertiesStrategy(context, out var fromCtor)
                ? fromCtor
                : values.SingleOrDefault()?.Value;
        }

        private bool TryDefaultConstructorAndPropertiesStrategy(
            BindingContext context,
            [NotNullWhen(true)] out object? instance)
        {
            var constructorDescriptors =
                ModelDescriptor
                    ?.ConstructorDescriptors
                    .OrderByDescending(d => d.ParameterDescriptors.Count)
                    .ToArray() ?? [];

            foreach (var constructor in constructorDescriptors)
            {
                var boundConstructorArguments = GetValues(
                    ConstructorArgumentBindingSources,
                    context,
                    constructor.ParameterDescriptors,
                    true);

                if (boundConstructorArguments.Count != constructor.ParameterDescriptors.Count)
                {
                    continue;
                }

                // Found invokable constructor, invoke and return
                var values = boundConstructorArguments.Select(v => v.Value).ToArray();

                try
                {
                    var fromModelBinder = constructor.Invoke(values);

                    UpdateInstance(fromModelBinder, context);

                    instance = fromModelBinder;

                    return true;
                }
                catch
                {
                    instance = null;
                    return false;
                }
            }

            instance = null;
            return false;
        }

        public void UpdateInstance<T>(T instance, BindingContext bindingContext)
        {
            var boundValues = GetValues(
                MemberBindingSources,
                bindingContext,
                ModelDescriptor?.PropertyDescriptors ?? Array.Empty<IValueDescriptor>(),
                includeMissingValues: false);

            foreach (var boundValue in boundValues)
            {
                ((PropertyDescriptor)boundValue.ValueDescriptor).SetValue(instance, boundValue.Value);
            }
        }

        private IReadOnlyList<BoundValue> GetValues(
            IDictionary<IValueDescriptor, IValueSource>? bindingSources,
            BindingContext bindingContext,
            IReadOnlyList<IValueDescriptor> valueDescriptors,
            bool includeMissingValues)
        {
            var values = new List<BoundValue>();

            foreach (var valueDescriptor in valueDescriptors)
            {
                var valueSource = GetValueSource(bindingSources, bindingContext, valueDescriptor);

                BoundValue? boundValue;
                if (valueSource is null)
                {
                    // If there is no source to bind from, no value can be bound.
                    boundValue = null;
                }
                else if (!bindingContext.TryBindToScalarValue(
                    valueDescriptor,
                    valueSource,
                    out boundValue) && valueDescriptor.HasDefaultValue)
                {
                    boundValue = BoundValue.DefaultForValueDescriptor(valueDescriptor);
                }

                if (boundValue is null)
                {
                    if (includeMissingValues)
                    {
                        if (valueDescriptor is ParameterDescriptor parameterDescriptor &&
                            parameterDescriptor.Parent is ConstructorDescriptor constructorDescriptor)
                        {
                            if (parameterDescriptor.HasDefaultValue)
                            {
                                boundValue = BoundValue.DefaultForValueDescriptor(parameterDescriptor);
                            }
                            else if (parameterDescriptor.AllowsNull &&
                                ShouldPassNullToConstructor(constructorDescriptor.Parent, constructorDescriptor))
                            {
                                boundValue = BoundValue.DefaultForType(valueDescriptor);
                            }
                        }
                    }
                }

                if (boundValue != null)
                {
                    values.Add(boundValue);
                }
            }

            return values;
        }

        private IValueSource? GetValueSource(
            IDictionary<IValueDescriptor, IValueSource>? bindingSources,
            BindingContext bindingContext,
            IValueDescriptor valueDescriptor)
        {
            if (!(bindingSources is null) &&
                bindingSources.TryGetValue(valueDescriptor, out var valueSource))
            {
                return valueSource;
            }

            if (bindingContext.TryGetValueSource(valueDescriptor, out valueSource))
            {
                return valueSource;
            }

            if (!EnforceExplicitBinding)
            {
                // Return a value source that will match from the parseResult
                // by name and type (or a possible conversion)
                return new ParseResultMatchingValueSource();
            }

            return null;
        }

        public override string ToString() =>
            $"{ModelDescriptor?.ModelType.Name ?? "none"}";

        private bool ShouldPassNullToConstructor(ModelDescriptor modelDescriptor,
            ConstructorDescriptor? ctor = null)
        {
            if (ctor is not null)
            {
                return ctor.ParameterDescriptors.All(d => d.AllowsNull);
            }

            return !modelDescriptor.ModelType.IsNullable();
        }

        private class AnonymousValueDescriptor : IValueDescriptor
        {
            public Type ValueType { get; }

            public AnonymousValueDescriptor(Type modelType)
            {
                ValueType = modelType;
            }

            public string? ValueName => null;

            public bool HasDefaultValue => false;

            public object? GetDefaultValue() => null;

            public override string ToString() => $"{ValueType}";
        }
    }
}
