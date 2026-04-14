using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.DTOs;
using System.Globalization;
using System.Text.Json;

namespace QuotationAPI.V2.Controllers
{
    [ApiController]
    [Route("api/order-analytics")]
    public class OrderAnalyticsController : ControllerBase
    {
        private readonly QuotationDbContext _context;
        private readonly ILogger<OrderAnalyticsController> _logger;

        public OrderAnalyticsController(
            QuotationDbContext context,
            ILogger<OrderAnalyticsController> logger
        )
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get total volume by month for a financial year from RollSales (order fulfillment)
        /// </summary>
        [HttpGet("volume-by-month")]
        public async Task<ActionResult<IEnumerable<VolumeByMonthDto>>> GetVolumeByMonth(
            int financialYear
        )
        {
            try
            {
                var data = await _context
                    .RollSales.AsNoTracking()
                    .Where(rs => rs.Status == "active")
                    .ToListAsync();

                var grouped = data
                    .Where(rs =>
                    {
                        var saleDate = ParseDateOrDefault(rs.SaleDate);
                        return saleDate.HasValue && saleDate.Value.Year == financialYear;
                    })
                    .GroupBy(rs => DateTime.Parse(rs.SaleDate).Month)
                    .Select(g => new VolumeByMonthDto
                    {
                        MonthNumber = g.Key,
                        Month = GetMonthName(g.Key),
                        TotalVolume = g.Sum(rs => rs.WeightKg),
                        OrderCount = g.Count(),
                    })
                    .OrderBy(x => x.MonthNumber)
                    .ToList();

                return Ok(grouped);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting volume by month for financial year {FinancialYear}",
                    financialYear
                );
                return StatusCode(
                    500,
                    new { message = "Error retrieving volume data", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get profit per customer with optional filters
        /// </summary>
        [HttpGet("profit-per-customer")]
        public async Task<ActionResult<IEnumerable<ProfitPerCustomerDto>>> GetProfitPerCustomer(
            string? customerId = null,
            string? customerName = null,
            int? month = null,
            int? year = null
        )
        {
            try
            {
                var selectedCustomer = NormalizeCustomerFilter(customerName ?? customerId);
                var query = _context.RollSales.AsNoTracking().Where(rs => rs.Status == "active");

                if (!string.IsNullOrWhiteSpace(selectedCustomer))
                {
                    query = query.Where(rs =>
                        (!string.IsNullOrWhiteSpace(rs.CustomerId)
                            && rs.CustomerId == selectedCustomer)
                        || rs.CustomerName == selectedCustomer);
                }

                var sales = (await query.ToListAsync())
                    .Where(rs => MatchesDateFilter(ParseDateOrDefault(rs.SaleDate), month, year))
                    .ToList();

                var data = sales
                    .GroupBy(rs => new { rs.CustomerId, rs.CustomerName })
                    .Select(g => new ProfitPerCustomerDto
                    {
                        CustomerId = g.Key.CustomerId ?? "Unknown",
                        CustomerName = g.Key.CustomerName,
                        TotalProfit = g.Sum(rs => rs.Profit),
                        TotalRevenue = g.Sum(rs => rs.TotalIncome),
                        OrderCount = g.Count(),
                        AverageProfitPerOrder = g.Count() > 0 ? g.Sum(rs => rs.Profit) / g.Count() : 0,
                    })
                    .OrderByDescending(x => x.TotalProfit)
                    .ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profit per customer");
                return StatusCode(
                    500,
                    new { message = "Error retrieving profit data", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get profit per customer from saved order details records
        /// </summary>
        [HttpGet("order-details-profit-per-customer")]
        public async Task<ActionResult<IEnumerable<ProfitPerCustomerDto>>> GetOrderDetailsProfitPerCustomer(
            string? customerId = null,
            string? customerName = null,
            int? month = null,
            int? year = null
        )
        {
            try
            {
                var selectedCustomer = NormalizeCustomerFilter(customerName ?? customerId);
                var records = await _context.InvoiceCalcRecords.AsNoTracking().ToListAsync();

                var data = records
                    .Where(record =>
                        MatchesCustomer(record.CompanyName, selectedCustomer)
                        && MatchesDateFilter(record.CreatedAt, month, year)
                    )
                    .Select(record => ParseInvoiceProfit(record.CompanyName, record.Amount, record.DataJson))
                    .Where(result => result.HasValue)
                    .Select(result => result!.Value)
                    .GroupBy(result => result.CustomerName)
                    .Select(g => new ProfitPerCustomerDto
                    {
                        CustomerId = g.Key,
                        CustomerName = g.Key,
                        TotalProfit = g.Sum(x => x.Profit),
                        TotalRevenue = g.Sum(x => x.Revenue),
                        OrderCount = g.Count(),
                        AverageProfitPerOrder = g.Any() ? g.Sum(x => x.Profit) / g.Count() : 0,
                    })
                    .OrderByDescending(x => x.TotalProfit)
                    .ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order details profit per customer");
                return StatusCode(
                    500,
                    new { message = "Error retrieving order details profit data", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get volume per customer with optional filters
        /// </summary>
        [HttpGet("volume-per-customer")]
        public async Task<ActionResult<IEnumerable<VolumePerCustomerDto>>> GetVolumePerCustomer(
            string? customerId = null,
            string? customerName = null,
            int? month = null,
            int? year = null
        )
        {
            try
            {
                var selectedCustomer = NormalizeCustomerFilter(customerName ?? customerId);
                var query = _context.RollSales.AsNoTracking().Where(rs => rs.Status == "active");

                if (!string.IsNullOrWhiteSpace(selectedCustomer))
                {
                    query = query.Where(rs =>
                        (!string.IsNullOrWhiteSpace(rs.CustomerId)
                            && rs.CustomerId == selectedCustomer)
                        || rs.CustomerName == selectedCustomer);
                }

                var sales = (await query.ToListAsync())
                    .Where(rs => MatchesDateFilter(ParseDateOrDefault(rs.SaleDate), month, year))
                    .ToList();

                var data = sales
                    .GroupBy(rs => new { rs.CustomerId, rs.CustomerName })
                    .Select(g => new VolumePerCustomerDto
                    {
                        CustomerId = g.Key.CustomerId ?? "Unknown",
                        CustomerName = g.Key.CustomerName,
                        TotalVolume = g.Sum(rs => rs.WeightKg),
                        OrderCount = g.Count(),
                        AverageVolumePerOrder = g.Count() > 0 ? g.Sum(rs => rs.WeightKg) / g.Count() : 0,
                    })
                    .OrderByDescending(x => x.TotalVolume)
                    .ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting volume per customer");
                return StatusCode(
                    500,
                    new { message = "Error retrieving volume data", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get combined profit by customer from roll sales and order details
        /// </summary>
        [HttpGet("combined-profit-by-customer")]
        public async Task<ActionResult<IEnumerable<CombinedCustomerProfitDto>>> GetCombinedProfitByCustomer(
            string? customerId = null,
            string? customerName = null,
            int? month = null,
            int? year = null
        )
        {
            try
            {
                var selectedCustomer = NormalizeCustomerFilter(customerName ?? customerId);
                var rollSales = await _context
                    .RollSales.AsNoTracking()
                    .Where(rs => rs.Status == "active")
                    .ToListAsync();

                var orderDetails = await _context.InvoiceCalcRecords.AsNoTracking().ToListAsync();
                var combined = new Dictionary<string, CombinedCustomerProfitDto>(StringComparer.OrdinalIgnoreCase);

                foreach (var sale in rollSales.Where(rs =>
                    MatchesCustomer(rs.CustomerName, selectedCustomer)
                    && MatchesDateFilter(ParseDateOrDefault(rs.SaleDate), month, year)))
                {
                    var customerKey = sale.CustomerName.Trim();
                    if (!combined.TryGetValue(customerKey, out var row))
                    {
                        row = new CombinedCustomerProfitDto
                        {
                            CustomerId = customerKey,
                            CustomerName = customerKey,
                        };
                        combined[customerKey] = row;
                    }

                    row.RollSalesProfit += sale.Profit;
                    row.RollSalesCount += 1;
                    row.CombinedProfit = row.RollSalesProfit + row.OrderDetailsProfit;
                }

                foreach (var record in orderDetails.Where(record =>
                    MatchesCustomer(record.CompanyName, selectedCustomer)
                    && MatchesDateFilter(record.CreatedAt, month, year)))
                {
                    var parsed = ParseInvoiceProfit(record.CompanyName, record.Amount, record.DataJson);
                    if (!parsed.HasValue)
                    {
                        continue;
                    }

                    var parsedValue = parsed.Value;

                    if (!combined.TryGetValue(parsedValue.CustomerName, out var row))
                    {
                        row = new CombinedCustomerProfitDto
                        {
                            CustomerId = parsedValue.CustomerName,
                            CustomerName = parsedValue.CustomerName,
                        };
                        combined[parsedValue.CustomerName] = row;
                    }

                    row.OrderDetailsProfit += parsedValue.Profit;
                    row.OrderDetailsCount += 1;
                    row.CombinedProfit = row.RollSalesProfit + row.OrderDetailsProfit;
                }

                return Ok(combined.Values.OrderByDescending(x => x.CombinedProfit).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting combined profit by customer");
                return StatusCode(
                    500,
                    new { message = "Error retrieving combined profit data", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get combination chart data (profit + volume)
        /// </summary>
        [HttpGet("combination-chart")]
        public async Task<ActionResult<IEnumerable<CombinationChartDto>>>
            GetCombinationChartData(string? customerId = null)
        {
            try
            {
                var query = _context.RollSales.AsNoTracking().Where(rs => rs.Status == "active");

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    query = query.Where(rs => rs.CustomerId == customerId);
                }

                var sales = await query.ToListAsync();

                var data = sales
                    .GroupBy(rs => DateTime.Parse(rs.SaleDate).Month)
                    .Select(g => new CombinationChartDto
                    {
                        MonthNumber = g.Key,
                        Month = GetMonthName(g.Key),
                        TotalProfit = g.Sum(rs => rs.Profit),
                        TotalVolume = g.Sum(rs => rs.WeightKg),
                        ProfitMarginPercentage =
                            g.Sum(rs => rs.TotalIncome) > 0
                                ? (g.Sum(rs => rs.Profit) / g.Sum(rs => rs.TotalIncome)) * 100
                                : 0,
                    })
                    .OrderBy(x => x.MonthNumber)
                    .ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting combination chart data");
                return StatusCode(
                    500,
                    new { message = "Error retrieving combination data", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get profit trend for a specific year
        /// </summary>
        [HttpGet("profit-trend")]
        public async Task<ActionResult<IEnumerable<ProfitTrendDto>>> GetProfitTrend(int year)
        {
            try
            {
                var sales = await _context
                    .RollSales.AsNoTracking()
                    .Where(rs => rs.Status == "active")
                    .ToListAsync();

                var data = sales
                    .Where(rs => DateTime.Parse(rs.SaleDate).Year == year)
                    .GroupBy(rs => DateTime.Parse(rs.SaleDate).Month)
                    .Select(g => new ProfitTrendDto
                    {
                        MonthNumber = g.Key,
                        Month = GetMonthName(g.Key),
                        TotalProfit = g.Sum(rs => rs.Profit),
                        TotalVolume = g.Sum(rs => rs.WeightKg),
                        OrderCount = g.Count(),
                    })
                    .OrderBy(x => x.MonthNumber)
                    .ToList();

                // Calculate cumulative profit
                decimal cumulativeProfit = 0;
                var result = data.ToList();
                foreach (var item in result)
                {
                    cumulativeProfit += item.TotalProfit;
                    item.CumulativeProfit = cumulativeProfit;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profit trend for year {Year}", year);
                return StatusCode(
                    500,
                    new { message = "Error retrieving profit trend data", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get all unique customers
        /// </summary>
        [HttpGet("customers")]
        public async Task<ActionResult<IEnumerable<CustomerReferenceDto>>> GetCustomers()
        {
            try
            {
                var salesCustomers = await _context
                    .RollSales.AsNoTracking()
                    .Where(rs => rs.Status == "active" && !string.IsNullOrWhiteSpace(rs.CustomerName))
                    .Select(rs => rs.CustomerName)
                    .ToListAsync();

                var orderCustomers = await _context
                    .InvoiceCalcRecords.AsNoTracking()
                    .Where(record => !string.IsNullOrWhiteSpace(record.CompanyName))
                    .Select(record => record.CompanyName)
                    .ToListAsync();

                var masterCustomers = await _context
                    .CustomerMasters.AsNoTracking()
                    .Where(customer => !string.IsNullOrWhiteSpace(customer.Name))
                    .Select(customer => customer.Name)
                    .ToListAsync();

                var customers = salesCustomers
                    .Concat(orderCustomers)
                    .Concat(masterCustomers)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)
                    .Select(name => new CustomerReferenceDto { Id = name, Name = name })
                    .ToList();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return StatusCode(
                    500,
                    new { message = "Error retrieving customers", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get order summary with optional filters
        /// </summary>
        [HttpGet("order-summary")]
        public async Task<ActionResult<OrderSummaryDto>> GetOrderSummary(
            int? month = null,
            int? year = null,
            string? customerId = null
        )
        {
            try
            {
                var query = _context.RollSales.AsNoTracking().Where(rs => rs.Status == "active");

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    query = query.Where(rs => rs.CustomerId == customerId);
                }

                var sales = await query.ToListAsync();

                // Apply date filters client-side
                if (month.HasValue)
                {
                    sales = sales
                        .Where(rs => DateTime.Parse(rs.SaleDate).Month == month.Value)
                        .ToList();
                }

                if (year.HasValue)
                {
                    sales = sales
                        .Where(rs => DateTime.Parse(rs.SaleDate).Year == year.Value)
                        .ToList();
                }

                var totalProfit = sales.Sum(rs => rs.Profit);
                var totalRevenue = sales.Sum(rs => rs.TotalIncome);
                var totalVolume = sales.Sum(rs => rs.WeightKg);

                var summary = new OrderSummaryDto
                {
                    TotalOrderValue = totalRevenue,
                    TotalProfit = totalProfit,
                    TotalVolume = totalVolume,
                    TotalOrders = sales.Count(),
                    ProfitMarginPercentage =
                        totalRevenue > 0 ? (totalProfit / totalRevenue) * 100 : 0,
                    AverageProfitPerOrder = sales.Count() > 0 ? totalProfit / sales.Count() : 0,
                    AverageOrderValue = sales.Count() > 0 ? totalRevenue / sales.Count() : 0,
                    UniqueCustomers = sales
                        .Select(rs => rs.CustomerId)
                        .Distinct()
                        .Count(),
                    Period = $"{GetMonthName(month ?? DateTime.Now.Month)} {year ?? DateTime.Now.Year}",
                    MonthFilter = month,
                    YearFilter = year,
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order summary");
                return StatusCode(
                    500,
                    new { message = "Error retrieving order summary", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Helper method to get month name from month number
        /// </summary>
        private static string GetMonthName(int month)
        {
            return month switch
            {
                1 => "January",
                2 => "February",
                3 => "March",
                4 => "April",
                5 => "May",
                6 => "June",
                7 => "July",
                8 => "August",
                9 => "September",
                10 => "October",
                11 => "November",
                12 => "December",
                _ => "Unknown",
            };
        }

        private static string? NormalizeCustomerFilter(string? customerFilter)
        {
            return string.IsNullOrWhiteSpace(customerFilter) ? null : customerFilter.Trim();
        }

        private static bool MatchesCustomer(string? sourceCustomer, string? selectedCustomer)
        {
            if (string.IsNullOrWhiteSpace(selectedCustomer))
            {
                return true;
            }

            return string.Equals(
                sourceCustomer?.Trim(),
                selectedCustomer,
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static DateTime? ParseDateOrDefault(string? rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate))
            {
                return null;
            }

            if (
                DateTime.TryParse(
                    rawDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var parsedDate
                )
            )
            {
                return parsedDate;
            }

            return null;
        }

        private static bool MatchesDateFilter(DateTime? value, int? month, int? year)
        {
            if (!value.HasValue)
            {
                return false;
            }

            if (month.HasValue && value.Value.Month != month.Value)
            {
                return false;
            }

            if (year.HasValue && value.Value.Year != year.Value)
            {
                return false;
            }

            return true;
        }

        private static (string CustomerName, decimal Profit, decimal Revenue)? ParseInvoiceProfit(
            string? companyName,
            decimal fallbackRevenue,
            string? dataJson
        )
        {
            var customerName = companyName?.Trim();
            if (string.IsNullOrWhiteSpace(customerName))
            {
                return null;
            }

            var profit = 0m;
            var revenue = fallbackRevenue;

            if (!string.IsNullOrWhiteSpace(dataJson))
            {
                try
                {
                    using var document = JsonDocument.Parse(dataJson);
                    if (document.RootElement.TryGetProperty("price", out var price))
                    {
                        if (price.TryGetProperty("profit", out var profitElement))
                        {
                            profit = profitElement.GetDecimal();
                        }

                        if (price.TryGetProperty("actualAmount", out var revenueElement))
                        {
                            revenue = revenueElement.GetDecimal();
                        }
                    }
                }
                catch
                {
                    // Ignore malformed payloads and use fallback values.
                }
            }

            return (customerName, profit, revenue);
        }
    }
}
