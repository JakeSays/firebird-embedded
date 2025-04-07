// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Std.CommandLine.Arguments;
using Std.CommandLine.Parsing;
using Std.CommandLine.Utility;
using static Std.CommandLine.Binding.ArgumentConversionResult;

namespace Std.CommandLine.Binding
{
    internal static class ArgumentConverter
    {
        private static readonly Dictionary<Type, Func<string, object>> Converters = new()
        {
            [typeof(FileSystemInfo)] = value =>
            {
                if (Directory.Exists(value))
                {
                    return new DirectoryInfo(value);
                }

                if (value.EndsWith(Path.DirectorySeparatorChar.ToString()) ||
                    value.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    return new DirectoryInfo(value);
                }

                return new FileInfo(value);
            }
        };

        internal static ArgumentConversionResult ConvertObject(
            IArgument argument,
            Type type,
            object? value)
        {
            switch (value)
            {
                case string singleValue:
                    if (type.IsEnumerable() && !type.HasStringTypeConverter())
                    {
                        return ConvertStrings(argument, type, [singleValue]);
                    }

                    return ConvertString(argument, type, singleValue);

                case IReadOnlyCollection<string> manyValues:
                    return ConvertStrings(argument, type, manyValues);
            }

            return TryConvertArray(argument, type, value);
        }

        internal static ArgumentConversionResult TryConvertList(
            IArgument argument,
            Type type,
            object? value)
        {
            var genericType = type.IsGenericType
                ? type.GetGenericTypeDefinition()
                : null;
            if (genericType == null ||
                genericType != typeof(List<>))
            {
                return None(argument);
            }

            var elementType = type.GetGenericArguments()[0];

            var results = Activator.CreateInstance(type)!;

            foreach (var element in (value as IEnumerable)!)
            {
                if (element == null ||
                    element.GetType() != elementType)
                {
                    return None(argument);
                }

                ((IList)results).Add(element);
            }

            return Success(argument, results);
        }

        internal static ArgumentConversionResult TryConvertArray(
            IArgument argument,
            Type type,
            object? value)
        {
            if (!type.IsArray || value == null)
            {
                return TryConvertList(argument, type, value);
            }

            var count = ((Array) value).Length;

            var elementType = type.GetElementType()!;

            var results = Array.CreateInstance(elementType, count);

            var index = 0;
            foreach (var element in (value as IEnumerable)!)
            {
                if (element == null ||
                    element.GetType() != elementType)
                {
                    return None(argument);
                }

                results.SetValue(element, index++);
            }


            return Success(argument, results);
        }

        private static ArgumentConversionResult ConvertString(
            IArgument argument,
            Type? type,
            string value)
        {
            type ??= typeof(string);

            if (TypeDescriptor.GetConverter(type) is { } typeConverter)
            {
                if (typeConverter.CanConvertFrom(typeof(string)))
                {
                    try
                    {
                        return Success(
                            argument,
                            typeConverter.ConvertFromInvariantString(value));
                    }
                    catch (Exception)
                    {
                        return Failure(argument, type, value);
                    }
                }
            }

            if (Converters.TryGetValue(type, out var convert))
            {
                return Success(
                    argument,
                    convert(value));
            }

            if (!type.TryFindConstructorWithSingleParameterOfType(typeof(string), out var ctor))
            {
                return Failure(argument, type, value);
            }

            var instance = ctor?.Invoke([
                value
            ]);

            return Success(argument, instance);

        }

        public static ArgumentConversionResult ConvertStrings(
            IArgument argument,
            Type type,
            IReadOnlyCollection<string> arguments)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            var itemType = type == typeof(string)
                               ? typeof(string)
                               : GetItemTypeIfEnumerable(type);

            var successfulParseResults = arguments
                                         .Select(arg => ConvertString(argument, itemType, arg))
                                         .OfType<SuccessfulArgumentConversionResult>()
                                         .ToArray();

            // var list = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType!))!;
            //
            // foreach (var parseResult in successfulParseResults)
            // {
            //     list.Add(parseResult.Value);
            // }
            //
            // var value = type.IsArray
            //                 ? (object) Enumerable.ToArray((dynamic) list)
            //                 : list;

            var count = successfulParseResults.Length;
            var list = new object[count];

            var index = 0;
            foreach (var parseResult in successfulParseResults)
            {
                list[index++] = parseResult.Value!;
            }

            return Success(argument, list);
        }

        private static Type? GetItemTypeIfEnumerable(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            var enumerableInterface =
                IsEnumerable(type)
                    ? type
                    : type
                      .GetInterfaces()
                      .FirstOrDefault(IsEnumerable);

            if (enumerableInterface is null)
            {
                return null;
            }

            return enumerableInterface.GenericTypeArguments[0];
        }

        internal static bool IsEnumerable(this Type type)
        {
            if (type == typeof(string))
            {
                return false;
            }

            return
                type.IsArray
                ||
                (type.IsGenericType &&
                 type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        private static bool HasStringTypeConverter(this Type type)
        {
            return TypeDescriptor.GetConverter(type) is TypeConverter typeConverter
                && typeConverter.CanConvertFrom(typeof(string));
        }

        private static FailedArgumentConversionResult Failure(
            IArgument argument,
            Type type,
            string value)
        {
            return new FailedArgumentTypeConversionResult(argument, type, value);
        }

        public static bool CanBeBoundFromScalarValue(this Type type)
        {
            if (type.IsPrimitive ||
                type.IsEnum)
            {
                return true;
            }

            if (type == typeof(string))
            {
                return true;
            }

            if (TypeDescriptor.GetConverter(type) is TypeConverter typeConverter &&
                typeConverter.CanConvertFrom(typeof(string)))
            {
                return true;
            }

            if (TryFindConstructorWithSingleParameterOfType(type, typeof(string), out _))
            {
                return true;
            }

            if (GetItemTypeIfEnumerable(type) is Type itemType)
            {
                return itemType.CanBeBoundFromScalarValue();
            }

            return false;
        }

        private static bool TryFindConstructorWithSingleParameterOfType(
            this Type type,
            Type parameterType,
            [NotNullWhen(true)] out ConstructorInfo? ctor)
        {
            var (x, y) = type.GetConstructors()
                             .Select(c => (ctor: c, parameters: c.GetParameters()))
                             .SingleOrDefault(tuple => tuple.ctor.IsPublic &&
                                                       tuple.parameters.Length == 1 &&
                                                       tuple.parameters[0].ParameterType == parameterType);

            if (x != null)
            {
                ctor = x;
                return true;
            }
            else
            {
                ctor = null;
                return false;
            }
        }

        internal static ArgumentConversionResult ConvertIfNeeded(
            this ArgumentConversionResult conversionResult,
            SymbolResult symbolResult,
            Type type)
        {
            if (conversionResult is null)
            {
                throw new ArgumentNullException(nameof(conversionResult));
            }

            switch (conversionResult)
            {
                case SuccessfulArgumentConversionResult successful when !type.IsInstanceOfType(successful.Value):
                    return ConvertObject(
                        conversionResult.Argument,
                        type,
                        successful.Value);

                case NoArgumentConversionResult _ when type == typeof(bool):
                    return Success(conversionResult.Argument, true);

                case NoArgumentConversionResult _ when conversionResult.Argument.Arity.MinimumNumberOfValues > 0:
                    return new MissingArgumentConversionResult(
                        conversionResult.Argument,
                        ValidationMessages.Instance.RequiredArgumentMissing(symbolResult));

                case NoArgumentConversionResult _ when type.IsEnumerable():
                    return ConvertObject(
                        conversionResult.Argument,
                        type,
                        Array.Empty<string>());

                default:
                    return conversionResult;
            }
        }

        internal static object? GetValueOrDefault(this ArgumentConversionResult result) =>
            result.GetValueOrDefault<object?>();

        [return: MaybeNull]
        internal static T GetValueOrDefault<T>(this ArgumentConversionResult result)
        {
            switch (result)
            {
                case SuccessfulArgumentConversionResult successful:
                    return (T)successful.Value!;
                case FailedArgumentConversionResult failed:
                    throw new InvalidOperationException(failed.ErrorMessage);
                case NoArgumentConversionResult _:
                    return default!;
                default:
                    return default!;
            }
        }
    }
}
