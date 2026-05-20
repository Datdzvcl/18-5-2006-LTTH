using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BaseCore.Entities;

namespace BaseCore.APIService.Services
{
    public class ExternalIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExternalIntegrationService> _logger;

        public ExternalIntegrationService(HttpClient httpClient, IConfiguration configuration, ILogger<ExternalIntegrationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RouteEstimateResponse?> EstimateRouteAsync(string from, string to)
        {
            var endpoint = GetEndpoint("GoRouteEstimateUrl", "http://localhost:7001/api/go/routes/estimate");
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, new { from, to });
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RouteEstimateResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Go route estimate service is unavailable. Falling back to local behavior.");
                return null;
            }
        }

        public async Task<List<StopPoint>?> GenerateStopsAsync(Trip trip)
        {
            var endpoint = GetEndpoint("GoStopsGenerateUrl", "http://localhost:7001/api/go/stops/generate");
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, new
                {
                    trip.DepartureLocation,
                    trip.ArrivalLocation,
                    DepartureTime = trip.DepartureTime,
                    ArrivalTime = trip.ArrivalTime
                });
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadFromJsonAsync<StopGenerateResponse>();
                return body?.Items?
                    .OrderBy(x => x.StopOrder)
                    .Select(x => new StopPoint
                    {
                        TripID = trip.TripID,
                        StopName = x.StopName,
                        StopAddress = x.StopAddress,
                        StopOrder = x.StopOrder,
                        StopType = x.StopType,
                        ArrivalOffset = x.ArrivalOffset,
                        IsActive = true
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Go stop generation service is unavailable. Falling back to local behavior.");
                return null;
            }
        }

        public async Task<List<string>?> GenerateSeatsAsync(int capacity)
        {
            var endpoint = GetEndpoint("RustSeatsGenerateUrl", "http://localhost:7002/api/rust/seats/generate");
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, new { capacity });
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadFromJsonAsync<SeatGenerateResponse>();
                return body?.Seats;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rust seat generation service is unavailable. Falling back to local behavior.");
                return null;
            }
        }

        public async Task<List<string>?> ValidateSeatsAsync(int capacity, List<string> seatLabels)
        {
            var endpoint = GetEndpoint("RustSeatsValidateUrl", "http://localhost:7002/api/rust/seats/validate");
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, new { capacity, seatLabels });
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadFromJsonAsync<SeatValidateResponse>();
                return body?.InvalidSeats ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rust seat validation service is unavailable. Falling back to local behavior.");
                return null;
            }
        }

        public async Task<string?> GenerateInvoiceCodeAsync(int bookingId)
        {
            var endpoint = GetEndpoint("JavaInvoiceCodeUrl", "http://localhost:7003/api/java/tickets/invoice-code");
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, new { bookingId, bookingDate = DateTime.Now });
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadFromJsonAsync<InvoiceCodeResponse>();
                return body?.InvoiceCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Java invoice service is unavailable. Falling back to local behavior.");
                return null;
            }
        }

        public async Task<string?> GenerateQrTextAsync(int bookingId, int tripId, List<string> seatLabels, string? customerPhone)
        {
            var endpoint = GetEndpoint("JavaQrUrl", "http://localhost:7003/api/java/tickets/qr");
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, new
                {
                    bookingId,
                    tripId,
                    seatLabels,
                    customerPhone
                });
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadFromJsonAsync<QrTextResponse>();
                return body?.QrText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Java QR service is unavailable. Falling back to local behavior.");
                return null;
            }
        }

        private string GetEndpoint(string key, string fallback)
        {
            return _configuration[$"Integrations:{key}"] ?? fallback;
        }
    }

    public class RouteEstimateResponse
    {
        [JsonPropertyName("routeName")]
        public string? RouteName { get; set; }

        [JsonPropertyName("distanceKm")]
        public int DistanceKm { get; set; }

        [JsonPropertyName("estimatedMinutes")]
        public int EstimatedMinutes { get; set; }
    }

    public class StopGenerateResponse
    {
        [JsonPropertyName("items")]
        public List<StopGenerateItem>? Items { get; set; }
    }

    public class StopGenerateItem
    {
        [JsonPropertyName("stopName")]
        public string StopName { get; set; } = string.Empty;

        [JsonPropertyName("stopAddress")]
        public string? StopAddress { get; set; }

        [JsonPropertyName("stopOrder")]
        public int StopOrder { get; set; }

        [JsonPropertyName("stopType")]
        public int StopType { get; set; }

        [JsonPropertyName("arrivalOffset")]
        public int? ArrivalOffset { get; set; }
    }

    public class SeatGenerateResponse
    {
        [JsonPropertyName("seats")]
        public List<string>? Seats { get; set; }
    }

    public class SeatValidateResponse
    {
        [JsonPropertyName("invalidSeats")]
        public List<string>? InvalidSeats { get; set; }
    }

    public class InvoiceCodeResponse
    {
        [JsonPropertyName("invoiceCode")]
        public string? InvoiceCode { get; set; }
    }

    public class QrTextResponse
    {
        [JsonPropertyName("qrText")]
        public string? QrText { get; set; }
    }
}
