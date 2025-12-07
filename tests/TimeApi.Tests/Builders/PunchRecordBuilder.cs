using TimeClock.Client;

namespace TimeApi.Tests.Builders;

/// <summary>
/// Builder for creating PunchRecord DTO test data.
/// Optional - you can also just use "new PunchRecord { ... }" directly.
/// </summary>
public class PunchRecordBuilder
{
    private Guid _punchId = Guid.NewGuid();
    private DateTime _punchIn = DateTime.Now;
    private DateTime? _punchOut = null;
    private HourType _hourType = HourType.Regular;
    private string? _authId = null;
    private string? _workDescription = null;

    public PunchRecordBuilder WithPunchId(Guid punchId)
    {
        _punchId = punchId;
        return this;
    }

    public PunchRecordBuilder WithPunchIn(DateTime time)
    {
        _punchIn = time;
        return this;
    }

    public PunchRecordBuilder WithPunchOut(DateTime? time)
    {
        _punchOut = time;
        return this;
    }

    public PunchRecordBuilder WithHourType(HourType hourType)
    {
        _hourType = hourType;
        return this;
    }

    public PunchRecordBuilder WithAuthId(string? authId)
    {
        _authId = authId;
        return this;
    }

    public PunchRecordBuilder WithWorkDescription(string? workDescription)
    {
        _workDescription = workDescription;
        return this;
    }

    /// <summary>
    /// Creates an open punch (PunchOut is null)
    /// </summary>
    public PunchRecordBuilder AsOpenPunch()
    {
        _punchOut = null;
        return this;
    }

    /// <summary>
    /// Creates a closed punch with PunchOut set
    /// </summary>
    public PunchRecordBuilder AsClosedPunch(DateTime? punchOutTime = null)
    {
        _punchOut = punchOutTime ?? _punchIn.AddHours(8);
        return this;
    }

    public PunchRecord Build() => new()
    {
        PunchId = _punchId,
        PunchIn = _punchIn,
        PunchOut = _punchOut,
        HourType = _hourType,
        AuthId = _authId,
        WorkDescription = _workDescription
    };

    /// <summary>
    /// Implicit conversion so you can use the builder directly without calling Build()
    /// </summary>
    public static implicit operator PunchRecord(PunchRecordBuilder builder) => builder.Build();
}
