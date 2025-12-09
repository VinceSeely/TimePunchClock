namespace TimeClock.Client;

public record PunchUpdateDto
{
    public Guid PunchId { get; init; }
    public DateTime PunchIn { get; init; }
    public DateTime PunchOut { get; init; }
    public HourType HourType { get; init; }
}
