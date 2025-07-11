using System;
using Microsoft.EntityFrameworkCore;
using TimeClock.Client;

namespace TimeApi.Services;

public class TimeClockDbContext : DbContext
{
    public DbSet<PunchEntity> Punchs { get; set; }

}

