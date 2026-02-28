using BotFatura.Application.Common.Interfaces;

namespace BotFatura.Application.Common.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.UtcNow.Date;
    public DateOnly TodayDateOnly => DateOnly.FromDateTime(DateTime.UtcNow.Date);
}
