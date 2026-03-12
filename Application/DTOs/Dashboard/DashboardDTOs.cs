namespace swd.Application.DTOs.Dashboard
{
    public class RevenueDashboardResponse
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public List<MonthlyRevenueDto> RevenueByMonth { get; set; } = new();
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = string.Empty; // e.g., "2023-01"
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}
