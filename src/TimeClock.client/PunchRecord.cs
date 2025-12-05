namespace TimeClock.Client;

public record PunchRecord
{
    public Guid PunchId { get; init; }
    public DateTime PunchIn { get; init; }
    public DateTime? PunchOut { get; init; }
    public HourType HourType { get; init; }
    public string? AuthId { get; init; }
    public string? WorkDescription { get; init; }
}
