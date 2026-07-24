namespace MainBackend.Services.BackgroundWorker;

public sealed class BackgroundJobTimestampSequence
{
    private const long MicrosecondTicks = 10;
    private DateTime _lastTimestamp;

    public BackgroundJobTimestampSequence()
    {
        _lastTimestamp = TruncateToMicrosecond(DateTime.UtcNow);
    }

    public DateTime Next()
    {
        var currentTimestamp = TruncateToMicrosecond(DateTime.UtcNow);

        _lastTimestamp = currentTimestamp > _lastTimestamp
            ? currentTimestamp
            : _lastTimestamp.AddTicks(MicrosecondTicks);

        return _lastTimestamp;
    }

    private static DateTime TruncateToMicrosecond(DateTime timestamp)
    {
        return timestamp.AddTicks(-(timestamp.Ticks % MicrosecondTicks));
    }
}
