using System.Text;
using Newtonsoft.Json;

namespace TimeClock.Client;

public class TimePunchClient(IHttpClientFactory clientFactory)
{

    public async Task<IEnumerable<PunchRecord>> GetTodaysPunchs(CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateClient(Constants.TimeClientString);
        try
        {
            var timePunchResults =
                await client.GetAsync($"{Constants.TimePunchApi}?start={DateTime.Today}&end={DateTime.Today}",
                    cancellationToken);
            var timePunchesJson = await timePunchResults.Content.ReadAsStringAsync();

            var timePunches = JsonConvert.DeserializeObject<PunchRecord[]>(timePunchesJson) ?? [];
            return timePunches;
        }
        catch
        {
            // ignored
        }
        return [];
    }

    public async Task<PunchRecord?> GetLastPunch()
    {
        var client = clientFactory.CreateClient(Constants.TimeClientString);
        try
        {
            var timePunchResults = await client.GetAsync($"{Constants.TimePunchApi}/lastpunch");
            var timePunchJson = await timePunchResults.Content.ReadAsStringAsync();
            var timePunch = JsonConvert.DeserializeObject<PunchRecord>(timePunchJson) ?? new PunchRecord();
            return timePunch;
        }
        catch
        {
            // ignore
        }
        return null;
    }

    public async Task<PunchRecord> Punch(HourType hourType, PunchType punchType)
    {
        var client = clientFactory.CreateClient(Constants.TimeClientString);
        var punchInfo = new PunchInfo
        {
            PunchType = punchType,
            HourType = hourType,
        };

        var json = JsonConvert.SerializeObject(punchInfo);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var timePunchResults = await client.PostAsync(Constants.TimePunchApi, content);
        var timePunchJson = await timePunchResults.Content.ReadAsStringAsync();
        var timePunch = JsonConvert.DeserializeObject<PunchRecord>(timePunchJson) ?? new PunchRecord();
        return timePunch;

    }
    public async Task<IEnumerable<PunchRecord>> GetPunchesRange(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateClient(Constants.TimeClientString);
        try
        {
            var timePunchResults =
                await client.GetAsync($"{Constants.TimePunchApi}?start={start}&end={end}", cancellationToken);
            var timePunchesJson = await timePunchResults.Content.ReadAsStringAsync();


            var timePunches = JsonConvert.DeserializeObject<PunchRecord[]>(timePunchesJson) ?? [];
            return timePunches;
        }
        catch
        {
            // ignored
        }

        return [];

    }

    public async Task<PunchRecord?> UpdatePunch(PunchUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateClient(Constants.TimeClientString);
        try
        {
            var json = JsonConvert.SerializeObject(updateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(Constants.TimePunchApi, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var updatedPunch = JsonConvert.DeserializeObject<PunchRecord>(resultJson);
            return updatedPunch;
        }
        catch
        {
            // ignored
        }

        return null;
    }

    public async Task<bool> DeletePunch(Guid punchId, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateClient(Constants.TimeClientString);
        try
        {
            var response = await client.DeleteAsync($"{Constants.TimePunchApi}/{punchId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // ignored
        }

        return false;
    }

}