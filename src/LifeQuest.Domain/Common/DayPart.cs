namespace LifeQuest.Domain.Common;

[Flags]
public enum DayPart
{
    None = 0,
    Morning = 1,
    Afternoon = 2,
    Evening = 4,
    Night = 8,
    Any = Morning | Afternoon | Evening | Night
}

public static class DayParts
{
    public static DayPart FromHour(int hour) => hour switch
    {
        >= 6 and < 12 => DayPart.Morning,
        >= 12 and < 17 => DayPart.Afternoon,
        >= 17 and < 22 => DayPart.Evening,
        _ => DayPart.Night
    };
}
