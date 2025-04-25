// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace Std.CommandLine.Binding
{
    internal class ModelDescriptor
    {
        private const BindingFlags CommonBindingFlags =
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        private static readonly ConcurrentDictionary<Type, ModelDescriptor> ModelDescriptors = [];

        private List<PropertyDescriptor>? _propertyDescriptors;
        private List<ConstructorDescriptor>? _constructorDescriptors;

        protected ModelDescriptor(Type modelType) =>
            ModelType = modelType ??
                throw new ArgumentNullException(nameof(modelType));

        public IReadOnlyList<ConstructorDescriptor> ConstructorDescriptors =>
            _constructorDescriptors ??=
                ModelType.GetConstructors(CommonBindingFlags)
                         .Select(i => new ConstructorDescriptor(i, this))
                         .ToList();

        public IReadOnlyList<IValueDescriptor> PropertyDescriptors =>
            _propertyDescriptors ??=
                ModelType.GetProperties(CommonBindingFlags)
                         .Where(p => p.CanWrite)
                         .Select(i => new PropertyDescriptor(i, this))
                         .ToList();

        public Type ModelType { get; }

        public override string ToString() => $"{ModelType.Name}";

        public static ModelDescriptor FromType<T>() =>
            ModelDescriptors.GetOrAdd(
                typeof(T),
                _ => new ModelDescriptor(typeof(T)));

        public static ModelDescriptor FromType(Type type) =>
            ModelDescriptors.GetOrAdd(
                type,
                _ => new ModelDescriptor(type));
    }
}
