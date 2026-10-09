namespace PerfumeStore.Application.Dashboard.Dtos {
    public class DashboardDto {
        public DashboardKpisDto Kpis { get; set; } = new();
        public DashboardDailySeriesDto Daily { get; set; } = new();
        public DashboardProfitTrendDto Profit { get; set; } = new();
        public List<DashboardBrandSalesDto> Brands { get; set; } = new();
        public List<DashboardExpenseBreakdownDto> Expenses { get; set; } = new();
        public List<DashboardAlertDto> Alerts { get; set; } = new();
        public List<DashboardRecentActivityDto> Activity { get; set; } = new();
        public decimal SalesRevenue { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal ExpenseTotal { get; set; }
        public decimal NetProfit { get; set; }
        public bool HasEstimatedProfit { get; set; }
        public string MonthLabel { get; set; } = string.Empty;
    }

    public class DashboardKpisDto {
        public DashboardKpiItemDto Sales { get; set; } = new();
        public DashboardKpiItemDto Purchases { get; set; } = new();
        public DashboardKpiItemDto Earnings { get; set; } = new();
        public DashboardCashKpiDto Cash { get; set; } = new();
    }

    public class DashboardKpiItemDto {
        public decimal Value { get; set; }
        public decimal DeltaPct { get; set; }
    }

    public class DashboardCashKpiDto {
        public decimal Value { get; set; }
    }

    public class DashboardDailySeriesDto {
        public List<string> Labels { get; set; } = new();
        public List<decimal> Sales { get; set; } = new();
        public List<decimal> Purchases { get; set; } = new();
    }

    public class DashboardProfitTrendDto {
        public List<string> Labels { get; set; } = new();
        public List<decimal> Values { get; set; } = new();
    }

    public class DashboardBrandSalesDto {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class DashboardExpenseBreakdownDto {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class DashboardAlertDto {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Tone { get; set; } = string.Empty;
        public string NavigateUrl { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DashboardRecentActivityDto {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Party { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ViewUrl { get; set; } = string.Empty;
    }
}
