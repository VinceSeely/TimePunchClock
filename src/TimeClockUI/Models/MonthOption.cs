namespace TimeClockUI.Models;

public class MonthOption
{
    public MonthOption(int month, string name)
    {
        Month = month;
        Name = name;
    }

    public int Month { get; set; }
    public string Name { get; set; } = "";
}