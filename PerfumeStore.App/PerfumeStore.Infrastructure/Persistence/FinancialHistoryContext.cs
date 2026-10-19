namespace PerfumeStore.Infrastructure.Persistence;

// One operation can save several times (e.g. an invoice and its FIFO costs).
public class FinancialHistoryContext {
    public Guid OperationId { get; } = Guid.NewGuid();
    public string Resource { get; set; } = "";
    public string SourceId { get; set; } = "";
    public string Action { get; set; } = "Change";
}
