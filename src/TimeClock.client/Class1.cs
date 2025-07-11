using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace TimeClock.Client;

public record PunchInfo
{
    public PunchType PunchType { get; init; }
    public HourType HourType { get; init; }
}

public record PunchRecord
{
    public Guid PuchId { get; init; }
    public DateTime PunchIn { get; init; }
    public DateTime? PunchOut { get; init; }
    public HourType HourType { get; init; }
}

public enum PunchType {
    PunchIn,
    PunchOut
}

public enum HourType
{
    TechLead,
    Regular
}

public static class Constants
{
    public const string TimeClientString = "timeClient";
    public const string TimePunchApi = "/api/TimePunch";
    public const string TimeClientBaseUrl = "TimeClientBaseUrl";
}

public class TimePunchClient(IHttpClientFactory clientFactory)
{

    public async Task<IEnumerable<PunchRecord>> GetTodaysPunchs(CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateClient(Constants.TimeClientString);
        var timePunchResults = await client.GetAsync($"{Constants.TimePunchApi}?start={DateTime.Today}&end={DateTime.Today}", cancellationToken);
        var timePunchesJson = await timePunchResults.Content.ReadAsStringAsync();

        var timePunches = JsonConvert.DeserializeObject<PunchRecord[]>(timePunchesJson) ?? [];
        return timePunches;

    }

    public async Task Punch(HourType hourType, PunchType punchType)
    {
        var client = clientFactory.CreateClient(Constants.TimeClientString);
            var punchInfo = new PunchInfo
            {
                PunchType = punchType,
                HourType = hourType,
            };

            var json = JsonConvert.SerializeObject(punchInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await client.PostAsync("your-endpoint-url", content);
        
    }
    public async Task<IEnumerable<PunchRecord>> GetPunchesRange(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateClient(Constants.TimeClientString);
        var timePunchResults = await client.GetAsync($"{Constants.TimePunchApi}?start={start}&end={end}", cancellationToken);
        var timePunchesJson = await timePunchResults.Content.ReadAsStringAsync();

        var timePunches = JsonConvert.DeserializeObject<PunchRecord[]>(timePunchesJson) ?? [];
        return timePunches;

    }

}

public static class TimepunchServiceExtensions
{
    public static IServiceCollection RegsiterTimeClient(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient(Constants.TimeClientString, client =>
        {
            var baseUrl = config.GetValue<String>(Constants.TimeClientBaseUrl) ?? string.Empty;
            client.BaseAddress = new Uri(baseUrl);
        });
        return services;
    }
}