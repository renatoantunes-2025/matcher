namespace MatchR.Api.Models;

public enum ImportStatus
{
    Processing = 0,
    Completed = 1,
    Failed = 2
}

public class ImportBatch
{
    public int Id { get; set; }
    public int BrokerId { get; set; }
    public Broker? Broker { get; set; }

    public string FileName { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.Processing;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
