using PerfumeStore.Domain.Entities;

namespace PerfumeStore.Domain.Repositories;

public class RecordFilters {
    public List<int> CategoryIds { get; set; } = [];
    public int? BrandId { get; set; }
    public int? ProductId { get; set; }
    public Guid? PartyId { get; set; }
    public string? PartyType { get; set; }
    public int? AccountId { get; set; }
    public int? FromAccountId { get; set; }
    public int? ToAccountId { get; set; }
    public long? InvoiceId { get; set; }
    public int? Direction { get; set; }
    public string? Status { get; set; }
    public string? StockStatus { get; set; }
    public string? SourceType { get; set; }
    public bool CorrectionsOnly { get; set; }
    public bool HasLineFilters => CategoryIds.Count > 0 || BrandId.HasValue || ProductId.HasValue;
    public bool Matches(Product product) => (CategoryIds.Count == 0 || CategoryIds.Contains(product.ProductCategoryID))
        && (!BrandId.HasValue || product.BrandID == BrandId) && (!ProductId.HasValue || product.ID == ProductId);
}
