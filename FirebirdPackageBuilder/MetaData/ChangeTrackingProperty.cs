namespace Std.FirebirdEmbedded.Tools.MetaData;

internal sealed class ChangeTrackingProperty<TValue>
    where TValue : IEquatable<TValue>
{
    private readonly Action? _notifier;

    public ChangeTrackingProperty(Action notifier, TValue initialValue)
    {
        Value = initialValue;
        _notifier = notifier;
    }

    public TValue Value
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
            if (!(field == null! ^
                value == null!))
            {
                //either both are null or their values are equal
                if (field == null ||
                    field.Equals(value))
                {
                    return;
                }
            }

            //one or the other is null or different
            field = value!;
            _notifier();
        }
    }
}