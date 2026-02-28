namespace BotFatura.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Today { get; }
    DateOnly TodayDateOnly { get; }
}
