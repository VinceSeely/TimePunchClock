using TimeClock.Client;

namespace TimeApi.Services;

public interface ITimePunchRepository
{
    void InsertPunch(PunchInfo punch);
    IEnumerable<PunchRecord> GetPunchRecords(DateTime start, DateTime end);
    PunchRecord? GetLastPunch();
}
