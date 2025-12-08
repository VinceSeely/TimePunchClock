using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeApi.Models;
using TimeClock.Client;

namespace TimeApi.Services;

public class TimePunchRepository(TimeClockDbContext context) : ITimePunchRepository
{

    public void InsertPunch(PunchInfo punch, string authId)
    {
        PunchEntity newPunch;
        var latestPunch = context.Punchs
            .Where(p => p.AuthId == authId)
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
                HourType = punch.HourType,
                WorkDescription = punch.WorkDescription,
                AuthId = authId
            };
            context.Punchs.Add(newPunch);
        }
        context.SaveChanges();
    }

    public IEnumerable<PunchRecord> GetPunchRecords(DateTime start, DateTime end, string authId)
    {
        var query = context.Punchs
            .Where(punch => punch.AuthId == authId &&
                           punch.PunchIn.Date >= start.Date &&
                           punch.PunchOut != null &&
                           punch.PunchOut.Value.Date <= end.Date)
            .Select(x => new PunchRecord
            {
                PunchIn = x.PunchIn,
                PunchOut = x.PunchOut.Value,
                HourType = x.HourType,
                PunchId = x.PunchId,
                AuthId = x.AuthId,
                WorkDescription = x.WorkDescription
            });
        return query;
    }

    public PunchRecord? GetLastPunch(string authId)
    {
        var mostRecentPunch = context.Punchs
            .Where(punch => punch.AuthId == authId)
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
            HourType = mostRecentPunch.HourType,
            AuthId = mostRecentPunch.AuthId,
            WorkDescription = mostRecentPunch.WorkDescription
        };
    }
}
