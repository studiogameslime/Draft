using System;
using System.Diagnostics;

public static class DailyResetUtil
{
    // ===== CONFIG =====
    public static readonly int DailyResetHourUtc = 6; //UTC (+2)
    public static readonly DayOfWeek WeeklyResetDay = DayOfWeek.Sunday;
    // ==================

    public static DateTime GetCurrentDailyResetUtc(DateTime nowUtc)
    {
        DateTime reset = new DateTime(
            nowUtc.Year, nowUtc.Month, nowUtc.Day,
            DailyResetHourUtc, 0, 0, DateTimeKind.Utc);

        if (nowUtc < reset)
            reset = reset.AddDays(-1);

        return reset;
    }

    public static DateTime GetNextDailyResetUtc(DateTime nowUtc)
    {
        return GetCurrentDailyResetUtc(nowUtc).AddDays(1);
    }

    public static DateTime GetCurrentWeeklyResetUtc(DateTime nowUtc)
    {
        int diff = (7 + (nowUtc.DayOfWeek - WeeklyResetDay)) % 7;
        DateTime resetDay = nowUtc.Date.AddDays(-diff);

        DateTime reset = new DateTime(
            resetDay.Year, resetDay.Month, resetDay.Day,
            DailyResetHourUtc, 0, 0, DateTimeKind.Utc);

        if (nowUtc < reset)
            reset = reset.AddDays(-7);

        return reset;
    }

    public static DateTime GetNextWeeklyResetUtc(DateTime nowUtc)
    {
        return GetCurrentWeeklyResetUtc(nowUtc).AddDays(7);
    }

    public static bool IsReady(long nextTicks)
    {
        return nextTicks <= 0 || DateTime.UtcNow.Ticks >= nextTicks;
    }
    
}
