using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Std.CommandLine.Utility;

internal static class TypeSystemExtensions
{
    public static MethodInfo? GetStaticMethod(this Type type, string methodName)
    {
        Guard.NotNull(type, nameof(type));
        Guard.NotNullOrEmpty(methodName, nameof(methodName));

        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        return method;
    }

    public static MethodInfo? GetStaticMethod(this Type type, string methodName, Type[] argTypes)
    {
        Guard.NotNull(type, nameof(type));
        Guard.NotNullOrEmpty(methodName, nameof(methodName));
        Guard.NotNull(argTypes, nameof(argTypes));

        // if (argTypes.Length == 0)
        // {
        //     argTypes = null;
        // }

        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, argTypes, null);

        return method;
    }

    /// <summary>
    ///     Determines if <paramref name="type" /> implements interface <paramref name="interfaceType" />.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="interfaceType">The interface type in question.</param>
    /// <returns><code>true</code> if <paramref name="type" /> implements <paramref name="interfaceType" /></returns>
    public static bool ImplementsInterface(this Type type, Type interfaceType)
    {
        Guard.NotNull(type, nameof(type));
        Guard.NotNull(interfaceType, nameof(interfaceType));

        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException(@"interface type required", nameof(interfaceType));
        }

        var foundTypes = type.FindInterfaces((t, _) => t == interfaceType, null);

        return foundTypes.Length > 0;
    }

    public static bool ImplementsInterface<TInterface>(this Type type)
    {
        Guard.NotNull(type, nameof(type));

        return ImplementsInterface(typeof(TInterface), type);
    }

    /// <summary>
    ///     Determines if <paramref name="type" /> implements generic interface <paramref name="interfaceType" />.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="interfaceType">The interface type in question.</param>
    /// <returns><code>true</code> if <paramref name="type" /> implements <paramref name="interfaceType" /></returns>
    public static bool ImplementsGenericInterface(this Type type, Type interfaceType)
    {
        Guard.NotNull(type, nameof(type));
        Guard.NotNull(interfaceType, nameof(interfaceType));

        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException(@"Interface type required", nameof(interfaceType));
        }

        if (!interfaceType.IsGenericType)
        {
            throw new ArgumentException(@"Interface must be generic", nameof(interfaceType));
        }

        var foundTypes = type.FindInterfaces(IsMatch, null);

        return foundTypes.Length > 0;

        bool IsMatch(Type targetType, object? _)
        {
            if (!targetType.IsGenericType)
            {
                return false;
            }

            var genericType = targetType.GetGenericTypeDefinition();
            return genericType == interfaceType;
        }
    }

    public static (Type? Key, Type? Value) GetContainedType(this Type type)
    {
        if (type.IsArray)
        {
            return (null, type.GetElementType()!);
        }

        if (type.IsGenericType)
        {
            var defn = type.GetGenericTypeDefinition();
            if (defn == typeof(List<>))
            {
                return (null, type.GetGenericArguments()[0]);
            }

            if (defn == typeof(Dictionary<,>))
            {
                return (type.GetGenericArguments()[0], type.GetGenericArguments()[1]);
            }
        }

        return (null, null);
    }

    /// <summary>
    ///     Determines if <paramref name="type" />represents a simple type.
    ///     Simple types are primitive types (bool, sbyte, byte, short, ushort, int, uint, long, ulong, float, double, char,
    ///     decimal)
    ///     and DateTime, DateTimeOffset, TimeSpan, char, string and enum.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><code>true</code> if <paramref name="type" />is a simple type.</returns>
    public static bool IsSimpleType(this Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.IsByRef)
        {
            type = type.GetElementType()!;
        }

        return type.IsSimpleValueType() || type == typeof(string);
    }

    /// <summary>
    ///     Determines if <paramref name="type" />represents a simple type.
    ///     Simple types are primitive types (bool, sbyte, byte, short, ushort, int, uint, long, ulong, float, double, char,
    ///     decimal)
    ///     and DateTime, DateTimeOffset, TimeSpan, char and enum.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><code>true</code> if <paramref name="type" />is a simple type.</returns>
    public static bool IsSimpleValueType(this Type type)
    {
        Guard.NotNull(type, nameof(type));

        if (type.IsArray)
        {
            return false;
        }

        return IsNumericValueType(type) ||
#if NET7_0_OR_GREATER
               type == typeof(TimeOnly) ||
               type == typeof(DateOnly) ||
#endif
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(char) ||
               type.IsEnum;
    }

    public static bool IsNumericValueType(this Type type)
    {
        Guard.NotNull(type, nameof(type));

        if (type.IsArray)
        {
            return false;
        }

        return type == typeof(bool) ||
               type == typeof(sbyte) ||
               type == typeof(byte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
#if NET7_0_OR_GREATER
               type == typeof(Int128) ||
               type == typeof(UInt128) ||
#endif
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    /// <summary>
    ///     Determines if <paramref name="type" /> represents a <c>System.Nullable&lt;&gt;</c> type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><code>true</code>if <paramref name="type" /> is a nullable type.</returns>
    public static bool IsNullable(this Type type)
    {
        Guard.NotNull(type, nameof(type));

        if (!type.IsValueType &&
            !type.IsEnum)
        {
            return false;
        }

        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    /// <summary>
    ///     Return the concrete type of Nullable{T}.
    /// </summary>
    /// <param name="type">The nullable type in question.</param>
    /// <returns>The concrete type if <paramref name="type" /> is nullable, or null otherwise.</returns>
    public static Type? GetTypeOfNullable(this Type type)
    {
        Guard.NotNull(type, nameof(type));

        if (type is { IsValueType: false, IsEnum: false })
        {
            return null;
        }
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return type.GetGenericArguments()[0];
        }

        return null;
    }

    /// <summary>
    ///     Returns the default value for a type.
    /// </summary>
    /// <param name="type">The target type.</param>
    /// <param name="emptyCollections">
    ///     For collections and arrays,
    ///     return empty collections if true, otherwise return null.
    /// </param>
    /// <returns>The default value for the target type.</returns>
    public static object? GetDefaultValue(this Type type, bool emptyCollections = true)
    {
        Guard.NotNull(type, nameof(type));

        if (type.IsEnum)
        {
            return Enum.ToObject(type, GetPrimitiveDefaultValue(Enum.GetUnderlyingType(type)));
        }

        if (type.IsNullable())
        {
            return GetDefaultValue(type.GetGenericArguments()[0], false);
        }

        if (type.IsSimpleValueType())
        {
            return GetPrimitiveDefaultValue(type);
        }

        if (!emptyCollections)
        {
            return null;
        }
        if (type.IsArray)
        {
            return Activator.CreateInstance(type, 0);
        }

        if (!type.IsGenericType)
        {
            return null;
        }
        return type.GetGenericTypeDefinition()
            .ImplementsInterface(typeof(IEnumerable<>)) ? Activator.CreateInstance(type) :
            //default for reference types is null
            null;

        static object GetPrimitiveDefaultValue(Type type)
        {
            if (type == typeof(bool))
            {
                return false;
            }
            if (type == typeof(sbyte))
            {
                return 0;
            }
            if (type == typeof(byte))
            {
                return 0;
            }
            if (type == typeof(short))
            {
                return 0;
            }
            if (type == typeof(ushort))
            {
                return 0;
            }
            if (type == typeof(int))
            {
                return 0;
            }
            if (type == typeof(uint))
            {
                return 0;
            }
            if (type == typeof(long))
            {
                return 0;
            }
            if (type == typeof(ulong))
            {
                return 0;
            }
#if NET7_0_OR_GREATER
            if (type == typeof(Int128))
            {
                return default(Int128);
            }
            if (type == typeof(UInt128))
            {
                return default(UInt128);
            }
#endif
            if (type == typeof(float))
            {
                return 0;
            }
            if (type == typeof(double))
            {
                return 0;
            }
            if (type == typeof(decimal))
            {
                return 0;
            }
            if (type == typeof(DateTime))
            {
                return default(DateTime);
            }
            if (type == typeof(DateTimeOffset))
            {
                return default(DateTimeOffset);
            }
            if (type == typeof(TimeSpan))
            {
                return TimeSpan.Zero;
            }
#if NET7_0_OR_GREATER
            if (type == typeof(TimeOnly))
            {
                return default(TimeOnly);
            }
            if (type == typeof(DateOnly))
            {
                return default(DateOnly);
            }
#endif
            if (type == typeof(char))
            {
                return '\0';
            }

            throw new NotSupportedException($"{type.FullName} is not a primitive type.");
        }
    }

    public static TypeKind Kind(this Type type)
    {
        if (type == typeof(bool))
        {
            return TypeKind.Bool;
        }

        if (type == typeof(sbyte))
        {
            return TypeKind.SByte;
        }

        if (type == typeof(byte))
        {
            return TypeKind.Byte;
        }

        if (type == typeof(short))
        {
            return TypeKind.Short;
        }

        if (type == typeof(ushort))
        {
            return TypeKind.UShort;
        }

        if (type == typeof(int))
        {
            return TypeKind.Int;
        }

        if (type == typeof(uint))
        {
            return TypeKind.UInt;
        }

        if (type == typeof(long))
        {
            return TypeKind.Long;
        }

        if (type == typeof(ulong))
        {
            return TypeKind.ULong;
        }
#if NET7_0_OR_GREATER
        if (type == typeof(Int128))
        {
            return TypeKind.LongLong;
        }

        if (type == typeof(UInt128))
        {
            return TypeKind.ULongLong;
        }
#endif
        if (type == typeof(float))
        {
            return TypeKind.Float;
        }

        if (type == typeof(double))
        {
            return TypeKind.Double;
        }

        if (type == typeof(decimal))
        {
            return TypeKind.Decimal;
        }

        if (type == typeof(DateTime))
        {
            return TypeKind.DateTime;
        }

        if (type == typeof(DateTimeOffset))
        {
            return TypeKind.DateTimeOffset;
        }

        if (type == typeof(TimeSpan))
        {
            return TypeKind.TimeSpan;
        }
#if NET7_0_OR_GREATER
        if (type == typeof(TimeOnly))
        {
            return TypeKind.TimeOnly;
        }

        if (type == typeof(DateOnly))
        {
            return TypeKind.DateOnly;
        }
#endif
        if (type == typeof(char))
        {
            return TypeKind.Char;
        }

        return TypeKind.Object;
    }

    public static Type? Dereference(this Type type)
    {
        Guard.NotNull(type, nameof(type));

        return type.IsByRef
            ? type.GetElementType()
            : type;
    }

    public static string GetSimpleTypeName(this Type type)
    {
        Guard.NotNull(type, nameof(type));

        type = type.Dereference() ?? type;

        var isNullable = type.IsNullable();

        if (!isNullable &&
            !type.IsSimpleType())
        {
            return type.Name;
        }

        var typeName = type.Name;
        if (isNullable)
        {
            typeName = type.GetTypeOfNullable()
                ?.Name;
        }

        if (typeName == null)
        {
            throw new InvalidOperationException($"Type {type.FullName} is not a simple type.");
        }

        switch (typeName)
        {
            case "Int8":
                typeName = "sbyte";
                break;
            case "UInt8":
                typeName = "byte";
                break;
            case "Int16":
                typeName = "short";
                break;
            case "UInt16":
                typeName = "ushort";
                break;
            case "Int32":
                typeName = "int";
                break;
            case "UInt32":
                typeName = "uint";
                break;
            case "Int64":
                typeName = "long";
                break;
            case "UInt64":
                typeName = "ulong";
                break;
#if NET7_0_OR_GREATER
            case "Int128":
                typeName = "Int128";
                break;
            case "UInt128":
                typeName = "Int128";
                break;
#endif
            case "Decimal":
                typeName = "decimal";
                break;
            case "Single":
                typeName = "float";
                break;
            case "Double":
                typeName = "double";
                break;
            case "DateTime":
                typeName = "DateTime";
                break;
            case "DateTimeOffset":
                typeName = "DateTimeOffset";
                break;
            case "TimeSpan":
                typeName = "TimeSpan";
                break;
#if NET7_0_OR_GREATER
            case "DateOnly":
                typeName = "DateOnly";
                break;
            case "TimeOnly":
                typeName = "TimeOnly";
                break;
#endif
            case "String":
                typeName = "string";
                break;
            case "Boolean":
                typeName = "bool";
                break;
        }

        if (isNullable)
        {
            typeName += "?";
        }

        return typeName;
    }

    public static string GetDisplayName(this Type type, bool resolveGenericArgs = true)
    {
        Guard.NotNull(type, nameof(type));

        type = type.Dereference() ?? type;

        var originalName = type.Name;
        var displayName = type.GetSimpleTypeName();
        if (displayName != originalName)
        {
            return displayName;
        }

        if (!type.IsGenericType &&
            !type.IsGenericTypeDefinition)
        {
            return type.Name;
        }

        if (type.IsGenericTypeDefinition ||
            !resolveGenericArgs)
        {
            displayName = $"{type.Name.SplitOnFirst('`')[0]}<{new string(',', type.GetGenericArguments() .Length - 1)}> ";
        }
        else
        {
            displayName = $"{type.Name.SplitOnFirst('`')[0]}<{string.Join(",", type.GetGenericArguments().Select(t => GetDisplayName(t)))}>";
        }

        return displayName;
    }

    /// <summary>
    ///     Checks the specified type to see if it is a subclass of the <paramref name="baseType" />. This method will
    ///     crawl up the inheritance heirarchy to check for equality using generic type definitions (if exists)
    /// </summary>
    /// <param name="type">The type to be checked as a subclass of <paramref name="baseType" /></param>
    /// <param name="baseType">The possible superclass of <paramref name="type" /></param>
    /// <returns>
    ///     True if <paramref name="type" /> is a subclass of the generic type definition
    ///     <paramref name="baseType" />
    /// </returns>
    public static bool HasGenericBase(this Type type, Type baseType)
    {
        var t = type;
        while (t != null &&
               t != typeof(object))
        {
            Type cur;
            if (t.IsGenericType &&
                baseType.IsGenericTypeDefinition)
            {
                cur = t.GetGenericTypeDefinition();
            }
            else
            {
                cur = t;
            }
            if (baseType == cur)
            {
                return true;
            }

            t = t.BaseType;
        }

        return false;
    }

    public static bool HasGenericBase<TBaseType>(this Type type)
    {
        var baseType = typeof(TBaseType);

        var t = type;
        while (t != null &&
               t != typeof(object))
        {
            Type cur;
            if (t.IsGenericType &&
                baseType.IsGenericTypeDefinition)
            {
                cur = t.GetGenericTypeDefinition();
            }
            else
            {
                cur = t;
            }
            if (baseType == cur)
            {
                return true;
            }

            t = t.BaseType;
        }

        return false;
    }
}
