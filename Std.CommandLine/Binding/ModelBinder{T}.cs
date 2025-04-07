// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Linq.Expressions;


namespace Std.CommandLine.Binding
{
    internal class ModelBinder<TModel> : ModelBinder
    {
        public ModelBinder() : base(typeof(TModel))
        {
        }

        public ModelBinder(IValueDescriptor descriptor)
            : base(descriptor)
        { }

        public void BindMemberFromValue<TValue>(
            Expression<Func<TModel, TValue>> property,
            IValueDescriptor valueDescriptor)
        {
            var (propertyType, propertyName) = property.MemberTypeAndName();
            var propertyDescriptor = FindModelPropertyDescriptor(
                propertyType, propertyName);
            MemberBindingSources[propertyDescriptor] =
                new SpecificSymbolValueSource(valueDescriptor);
        }

        public void BindMemberFromValue<TValue>(
            Expression<Func<TModel, TValue>> property,
            Func<BindingContext?, TValue> getValue)
        {
            var (propertyType, propertyName) = property.MemberTypeAndName();
            var propertyDescriptor = FindModelPropertyDescriptor(
                propertyType, propertyName);
            MemberBindingSources[propertyDescriptor] =
                new DelegateValueSource(c => getValue(c));
        }
    }
}
