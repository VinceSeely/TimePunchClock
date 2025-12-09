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

    public async Task<int> BulkInsertPunchesAsync(IEnumerable<PunchEntity> punches, string authId)
    {
        // Ensure all punches belong to the authenticated user
        var validPunches = punches.Where(p => p.AuthId == authId).ToList();

        if (validPunches.Count == 0)
            return 0;

        await context.Punchs.AddRangeAsync(validPunches);
        await context.SaveChangesAsync();

        return validPunches.Count;
    }

    public PunchRecord UpdatePunch(PunchUpdateDto updateDto, string authId)
    {
        // Validate punch out is after punch in
        if (updateDto.PunchOut <= updateDto.PunchIn)
        {
            throw new ArgumentException("Punch out time must be after punch in time");
        }

        // Find the punch record
        var punch = context.Punchs.FirstOrDefault(p => p.PunchId == updateDto.PunchId);

        if (punch == null)
        {
            throw new InvalidOperationException("Punch record not found");
        }

        // Verify the punch belongs to the authenticated user
        if (punch.AuthId != authId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this punch record");
        }

        // Update the punch times and hour type
        punch.PunchIn = updateDto.PunchIn;
        punch.PunchOut = updateDto.PunchOut;
        punch.HourType = updateDto.HourType;
        punch.UpdatedAt = DateTime.Now;

        context.SaveChanges();

        // Return the updated record
        return new PunchRecord
        {
            PunchId = punch.PunchId,
            PunchIn = punch.PunchIn,
            PunchOut = punch.PunchOut,
            HourType = punch.HourType,
            AuthId = punch.AuthId,
            WorkDescription = punch.WorkDescription
        };
    }

    public void DeletePunch(Guid punchId, string authId)
    {
        // Find the punch record
        var punch = context.Punchs.FirstOrDefault(p => p.PunchId == punchId);

        if (punch == null)
        {
            throw new InvalidOperationException("Punch record not found");
        }

        // Verify the punch belongs to the authenticated user
        if (punch.AuthId != authId)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this punch record");
        }

        // Remove the punch record
        context.Punchs.Remove(punch);
        context.SaveChanges();
    }
}
