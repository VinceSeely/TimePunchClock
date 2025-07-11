using Microsoft.EntityFrameworkCore;
using TimeApi.Models;

namespace TimeApi.Services;

public class TimeClockDbContext (DbContextOptions<TimeClockDbContext> options): DbContext(options)
{
    public DbSet<PunchEntity> Punchs { get; set; }

}

