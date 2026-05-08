using System;

namespace KeplerCMS.Areas.Housekeeping.Helpers;

public class PrettyFormatTime
{
    public static string PrettyFormatTimeSpan(TimeSpan span)
    {
        if (span.Days > 0)
            return $"{span.Days} days, {span.Hours} hours, {span.Minutes} minutes";
        if (span.Hours > 0)
            return $"{span.Hours} hours, {span.Minutes} minutes";

        return $"{span.Minutes} minutes";
    }
}