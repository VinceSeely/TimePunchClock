using System;
using TimeClock.Client;

namespace TimeApi.Models;

public class PunchEntity
{
    public Guid PunchId { get; set; }
    public DateTime PunchIn { get; init; }
    public DateTime PunchOut { get; init; }
    public HourType HourType { get; init; }
}
