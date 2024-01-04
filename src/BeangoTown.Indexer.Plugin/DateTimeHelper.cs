using System.Globalization;

namespace BeangoTown.Indexer.Plugin;

public class DateTimeHelper
{
    private static string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    

    public static DateTime ParseDateTimeByStr(string time)
    {
        return DateTime.ParseExact(time, _dateTimeFormat, CultureInfo.InvariantCulture);
    }

    public static string DatetimeToString(DateTime time)
    {
        return time.ToString(_dateTimeFormat);
    }
}