using System;
using System.Globalization;

namespace KeplerCMS.Areas.Housekeeping.Helpers;

/// <summary>
/// Dates for the housekeeping date pickers (jquery.datetimepicker, format 'Y-m-d H:i:s'), e.g. "2026-11-20 14:00:00".
/// Always written with colons: in a .NET format string ':' is the culture's time separator, and da-DK's is '.'
/// ("14.00.00"), which the picker can't read, so it opened on today instead of the saved date.
/// </summary>
public static class PickerDate
{
    public static string ForPicker(this DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    public static string ForPicker(this DateTime? value) => value?.ForPicker();
}
