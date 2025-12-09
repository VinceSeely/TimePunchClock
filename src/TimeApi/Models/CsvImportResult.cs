namespace TimeApi.Models;

public class CsvImportResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsSuccess => FailureCount == 0 && SuccessCount > 0;
}
