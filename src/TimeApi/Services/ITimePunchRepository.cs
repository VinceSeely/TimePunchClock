using TimeClock.Client;

namespace TimeApi.Services;

public interface ITimePunchRepository
{
    void InsertPunch(PunchInfo punch, string authId);
    IEnumerable<PunchRecord> GetPunchRecords(DateTime start, DateTime end, string authId);
    PunchRecord? GetLastPunch(string authId);
}
