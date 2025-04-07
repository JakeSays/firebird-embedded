// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Reflection;
using Std.CommandLine.Utility;

namespace Std.CommandLine.Binding
{
    internal class ParameterDescriptor : IValueDescriptor
    {
        private readonly ParameterInfo _parameterInfo;
        private bool? _allowsNull;

        internal ParameterDescriptor(
            ParameterInfo parameterInfo,
            IMethodDescriptor parent)
        {
            Parent = parent;
            _parameterInfo = parameterInfo;
        }

        public string ValueName => _parameterInfo.Name ?? "";

        public IMethodDescriptor Parent { get; }

        public Type ValueType => _parameterInfo.ParameterType;

        public bool HasDefaultValue => _parameterInfo.HasDefaultValue;

        public bool AllowsNull
        {
            get
            {
                if (_allowsNull is not null)
                {
                    return _allowsNull ?? false;
                }

                if (_parameterInfo.ParameterType.IsNullable())
                {
                    _allowsNull = true;
                }

                if (_parameterInfo.HasDefaultValue &&
                    _parameterInfo.DefaultValue is null)
                {
                    _allowsNull = true;
                }

                return _allowsNull ?? false;
            }
        }

        public object? GetDefaultValue()
        {
            var defaultValue =  _parameterInfo.DefaultValue is DBNull
                ? ValueType.GetDefaultValueForType()
                : _parameterInfo.DefaultValue;

            if (defaultValue != null)
            {
                return defaultValue;
            }

            defaultValue = _parameterInfo.ParameterType.GetDefaultValue(true);
            return defaultValue;
        }

        public override string ToString() => $"{ValueType.Name} {ValueName}";
    }
}
