namespace Std.CommandLine.Utility;

#if REFLECTION_INTERNAL
internal
#else
public
#endif
    enum TypeKind
{
    Unknown,
    Bool,
    SByte,
    Byte,
    Short,
    UShort,
    Int,
    UInt,
    Long,
    ULong,
    LongLong,
    ULongLong,
    Float,
    Double,
    Decimal,
    Char,
    String,
    DateTime,
    DateTimeOffset,
    TimeSpan,
    DateOnly,
    TimeOnly,
    Guid,
    Object
}