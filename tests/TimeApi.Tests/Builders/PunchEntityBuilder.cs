using TimeApi.Models;
using TimeClock.Client;

namespace TimeApi.Tests.Builders;

/// <summary>
/// Builder for creating PunchEntity test data.
/// Optional - you can also just use "new PunchEntity { ... }" directly.
/// </summary>
public class PunchEntityBuilder
{
    private PunchEntity _entity = new()
    {
        PunchId = Guid.NewGuid(),
        PunchIn = DateTime.Now,
        PunchOut = null,
        HourType = HourType.Regular,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now,
        AuthId = null,
        WorkDescription = null
    };

    public PunchEntityBuilder WithPunchId(Guid punchId)
    {
        _entity.PunchId = punchId;
        return this;
    }

    public PunchEntityBuilder WithPunchIn(DateTime time)
    {
        _entity.PunchIn = time;
        return this;
    }

    public PunchEntityBuilder WithPunchOut(DateTime? time)
    {
        _entity.PunchOut = time;
        return this;
    }

    public PunchEntityBuilder WithHourType(HourType hourType)
    {
        _entity.HourType = hourType;
        return this;
    }

    public PunchEntityBuilder WithAuthId(string? authId)
    {
        _entity.AuthId = authId;
        return this;
    }

    public PunchEntityBuilder WithWorkDescription(string? workDescription)
    {
        _entity.WorkDescription = workDescription;
        return this;
    }

    public PunchEntityBuilder WithCreatedAt(DateTime time)
    {
        _entity.CreatedAt = time;
        return this;
    }

    public PunchEntityBuilder WithUpdatedAt(DateTime time)
    {
        _entity.UpdatedAt = time;
        return this;
    }

    /// <summary>
    /// Creates an open punch (PunchOut is null)
    /// </summary>
    public PunchEntityBuilder AsOpenPunch()
    {
        _entity.PunchOut = null;
        return this;
    }

    /// <summary>
    /// Creates a closed punch with PunchOut set
    /// </summary>
    public PunchEntityBuilder AsClosedPunch(DateTime? punchOutTime = null)
    {
        _entity.PunchOut = punchOutTime ?? _entity.PunchIn.AddHours(8);
        return this;
    }

    public PunchEntity Build() => _entity;

    /// <summary>
    /// Implicit conversion so you can use the builder directly without calling Build()
    /// Example: PunchEntity entity = new PunchEntityBuilder().WithAuthId("123");
    /// </summary>
    public static implicit operator PunchEntity(PunchEntityBuilder builder) => builder.Build();
}
