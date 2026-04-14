namespace QuotationAPI.V2.DTOs
{
    /// <summary>
    /// DTO for monthly volume analytics
    /// </summary>
    public class VolumeByMonthDto
    {
        public string Month { get; set; } = string.Empty;
        public int MonthNumber { get; set; }
        public decimal TotalVolume { get; set; }
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// DTO for profit per customer analytics
    /// </summary>
    public class ProfitPerCustomerDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalProfit { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageProfitPerOrder { get; set; }
    }

    /// <summary>
    /// DTO for combined customer profit analytics from order details and roll sales
    /// </summary>
    public class CombinedCustomerProfitDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal RollSalesProfit { get; set; }
        public decimal OrderDetailsProfit { get; set; }
        public decimal CombinedProfit { get; set; }
        public int RollSalesCount { get; set; }
        public int OrderDetailsCount { get; set; }
    }

    /// <summary>
    /// DTO for volume per customer analytics
    /// </summary>
    public class VolumePerCustomerDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalVolume { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageVolumePerOrder { get; set; }
    }

    /// <summary>
    /// DTO for combination chart (profit + volume)
    /// </summary>
    public class CombinationChartDto
    {
        public string Month { get; set; } = string.Empty;
        public int MonthNumber { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
    }

    /// <summary>
    /// DTO for profit trend analytics
    /// </summary>
    public class ProfitTrendDto
    {
        public string Month { get; set; } = string.Empty;
        public int MonthNumber { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalVolume { get; set; }
        public int OrderCount { get; set; }
        public decimal CumulativeProfit { get; set; }
    }

    /// <summary>
    /// DTO for customer reference
    /// </summary>
    public class CustomerReferenceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for order summary dashboard
    /// </summary>
    public class OrderSummaryDto
    {
        public decimal TotalOrderValue { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalVolume { get; set; }
        public int TotalOrders { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
        public decimal AverageProfitPerOrder { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int UniqueCustomers { get; set; }

        // Time period info
        public string Period { get; set; } = string.Empty;
        public int? MonthFilter { get; set; }
        public int? YearFilter { get; set; }
    }

    /// <summary>
    /// Request filter DTO for analytics queries
    /// </summary>
    public class AnalyticsFilterDto
    {
        public int FinancialYear { get; set; }
        public string? CustomerId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
