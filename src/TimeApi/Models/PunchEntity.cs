using System;
using System.ComponentModel.DataAnnotations;
using TimeClock.Client;

namespace TimeApi.Models;

public class PunchEntity
{
    [Key]
    public Guid PunchId { get; set; }
    public DateTime PunchIn { get; set; }
    public DateTime? PunchOut { get; set; }
    public HourType HourType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
