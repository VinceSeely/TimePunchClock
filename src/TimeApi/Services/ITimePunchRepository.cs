using TimeApi.Models;
using TimeClock.Client;

namespace TimeApi.Services;

public interface ITimePunchRepository
{
    void InsertPunch(PunchInfo punch, string authId);
    IEnumerable<PunchRecord> GetPunchRecords(DateTime start, DateTime end, string authId);
    PunchRecord? GetLastPunch(string authId);
    Task<int> BulkInsertPunchesAsync(IEnumerable<PunchEntity> punches, string authId);
    PunchRecord UpdatePunch(PunchUpdateDto updateDto, string authId);
    void DeletePunch(Guid punchId, string authId);
}
