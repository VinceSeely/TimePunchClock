namespace TimeClock.Client;

public record PunchInfo
{
    public PunchType PunchType { get; init; }
    public HourType HourType { get; init; }
}
