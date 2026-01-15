using System;

public static class DailyResetUtil
{
    public static readonly TimeSpan ResetTimeUtc = TimeSpan.Zero;

    public static DateTime GetNextResetUtc()
    {
        DateTime now = DateTime.UtcNow;
        DateTime todayReset = now.Date + ResetTimeUtc;
        return now < todayReset
            ? todayReset
            : todayReset.AddDays(1);
    }

    public static bool IsReady(long nextTicks)
    {
        if (nextTicks <= 0) return true;
        return DateTime.UtcNow.Ticks >= nextTicks;
    }

    public static long GetNextResetTicks()
    {
        return GetNextResetUtc().Ticks;
    }
}
