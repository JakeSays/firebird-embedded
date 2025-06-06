namespace Std.FirebirdEmbedded.Tools.MetaData;

internal sealed class NullableChangeTrackingProperty<TValue>
    where TValue : struct, IEquatable<TValue>
{
    private readonly Action? _notifier;

    public NullableChangeTrackingProperty(Action notifier, TValue? initialValue = null)
    {
        Value = initialValue;
        _notifier = notifier;
    }

    public TValue? Value
    {
        get;
        set
        {
            if (_notifier == null)
            {
                field = value;
                return;
            }

            //either both are null or both are not null
            if (!(field == null ^
                value == null))
            {
                //either both are null or their values are equal
                if (field == null ||
                    field.Value.Equals(value!.Value))
                {
                    return;
                }
            }

            //one or the other is null or different
            field = value;
            _notifier();
        }
    }
}
