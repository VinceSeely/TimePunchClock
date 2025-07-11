using System;
using TimeClock.Client;

namespace TimeApi.Models;

public class PunchEntity
{
    public Guid PunchId { get; set; }
    public DateTime PunchIn { get; set; }
    public DateTime PunchOut { get; set; }
    public HourType HourType { get; set; }
}
