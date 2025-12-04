using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeApi.Models;
using TimeClock.Client;

namespace TimeApi.Services;

public class TimePunchRepository(TimeClockDbContext context) : ITimePunchRepository
{

    public void InsertPunch(PunchInfo punch)
    {
        PunchEntity newPunch;
        var latestPunch = context.Punchs
            .OrderByDescending(p => p.PunchIn)
            .FirstOrDefault();
        if (punch.PunchType == PunchType.PunchOut && latestPunch != null && latestPunch.PunchOut == null)
        {
            latestPunch.PunchOut = DateTime.Now;
        }
        else if (punch.PunchType == PunchType.PunchIn)
        {
            if (latestPunch != null && latestPunch.PunchOut == null)
            {
                latestPunch.PunchOut = DateTime.Now;
            }
            newPunch = new PunchEntity
            {
                PunchIn = DateTime.Now,
                HourType = punch.HourType
            };
            context.Punchs.Add(newPunch);
        }
        context.SaveChanges();
    }

    public IEnumerable<PunchRecord> GetPunchRecords(DateTime start, DateTime end)
    {
        var query = context.Punchs.Where(punch => punch.PunchIn.Date >= start.Date && punch.PunchOut != null && punch.PunchOut.Value.Date <= end.Date).Select(x => new PunchRecord
        {
            PunchIn = x.PunchIn,
            PunchOut = x.PunchOut.Value,
            HourType = x.HourType,
            PunchId = x.PunchId
        });
        return query;
    }

    public PunchRecord? GetLastPunch()
    {
        var mostRecentPunch = context.Punchs
            .OrderByDescending(punch => punch.PunchIn)
            .ThenByDescending(punch => punch.PunchOut)
            .FirstOrDefault();

        if (mostRecentPunch == null)
        {
            return null;
        }

        return new PunchRecord
        {
            PunchIn = mostRecentPunch.PunchIn,
            PunchOut = mostRecentPunch.PunchOut,
            PunchId = mostRecentPunch.PunchId,
            HourType = mostRecentPunch.HourType
        };
    }
}
