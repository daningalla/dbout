namespace DbOut.Threading;

/// <summary>
/// Wraps a value that can be safely read and modified by multiple threads.
/// </summary>
/// <typeparam name="T">Value type</typeparam>
public sealed class VolatileValue<T>
{
    private T _value;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new instance of this type
    /// </summary>
    public VolatileValue(T value)
    {
        _value = value;
    }

    /// <summary>
    /// Reads the value.
    /// </summary>
    /// <param name="reader">Delegate that has access to the value.</param>
    public void Read(Action<T> reader)
    {
        lock (_lock)
        {
            reader(_value);
        }
    }

    /// <summary>
    /// Exchanges the value.
    /// </summary>
    /// <param name="exchanger">Delegate that returns a new value.</param>
    public void Exchange(Func<T, T> exchanger)
    {
        lock (_lock)
        {
            _value = exchanger(_value);
        }
    }
}