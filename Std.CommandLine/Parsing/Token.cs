// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;

namespace Std.CommandLine.Parsing;

public class Token
{
    public Token(string? value, TokenType type)
    {
        Value = value ?? "";
        UnprefixedValue = Value.RemovePrefix();
        Type = type;
    }

    public string Value { get; }

    internal string UnprefixedValue { get; }

    public TokenType Type { get; }

    public override bool Equals(object? obj)
    {
        return obj is Token token &&
               Value == token.Value &&
               Type == token.Type;
    }

    public override int GetHashCode()
    {
        return (Value, Type).GetHashCode();
    }

    public override string ToString()
    {
        return $"{Type}: {Value}";
    }

    public static bool operator ==(Token left, Token right)
    {
        return EqualityComparer<Token>.Default.Equals(left, right);
    }

    public static bool operator !=(Token left, Token right)
    {
        return !(left == right);
    }
}