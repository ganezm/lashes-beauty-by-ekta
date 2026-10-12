using System.Net.Http.Json;
using LashesWebsite.Models;
using Microsoft.Extensions.Options;

namespace LashesWebsite.Services;

public sealed class GessFlowApiOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string Slug { get; set; } = "";
}

public interface IGessFlowApiClient
{
    Task<PublicWebsiteContent?> GetWebsiteContentAsync(CancellationToken ct);
    Task<PublicBookingStorefront?> GetStorefrontAsync(CancellationToken ct);
    Task<List<PublicBookingSlot>> GetAvailabilityAsync(Guid locationId, Guid serviceId, DateOnly date, Guid? employeeId, CancellationToken ct);
    Task<(bool Ok, PublicBookingConfirmation? Confirmation, string? Error)> CreateBookingAsync(CreatePublicBookingRequest request, CancellationToken ct);
    Task<(bool Ok, bool Rejected)> SendEnquiryAsync(PublicEnquiryRequest request, CancellationToken ct);
}

/// Thin HTTP wrapper over GessFlow's two anonymous public endpoints (booking storefront/availability/
/// create, and website content) -- this whole project is a pure rendering layer with no database of
/// its own. A failed content/storefront fetch returns null rather than throwing, so a page can render
/// an honest "temporarily unavailable" state instead of a raw 500 (see SiteControllerBase).
public sealed class GessFlowApiClient(HttpClient http, IOptions<GessFlowApiOptions> options, ILogger<GessFlowApiClient> logger) : IGessFlowApiClient
{
    private readonly GessFlowApiOptions _options = options.Value;

    public async Task<PublicWebsiteContent?> GetWebsiteContentAsync(CancellationToken ct)
    {
        try
        {
            var res = await http.GetAsync($"{_options.BaseUrl}/api/public/website/{_options.Slug}", ct);
            if (!res.IsSuccessStatusCode) { logger.LogWarning("GetWebsiteContent failed: {Status}", res.StatusCode); return null; }
            return await res.Content.ReadFromJsonAsync<PublicWebsiteContent>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetWebsiteContent errored");
            return null;
        }
    }

    public async Task<PublicBookingStorefront?> GetStorefrontAsync(CancellationToken ct)
    {
        try
        {
            var res = await http.GetAsync($"{_options.BaseUrl}/api/public/bookings/{_options.Slug}", ct);
            if (!res.IsSuccessStatusCode) { logger.LogWarning("GetStorefront failed: {Status}", res.StatusCode); return null; }
            return await res.Content.ReadFromJsonAsync<PublicBookingStorefront>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetStorefront errored");
            return null;
        }
    }

    public async Task<List<PublicBookingSlot>> GetAvailabilityAsync(Guid locationId, Guid serviceId, DateOnly date, Guid? employeeId, CancellationToken ct)
    {
        try
        {
            var url = $"{_options.BaseUrl}/api/public/bookings/{_options.Slug}/availability?locationId={locationId}&serviceId={serviceId}&date={date:yyyy-MM-dd}" +
                      (employeeId.HasValue ? $"&employeeId={employeeId}" : "");
            var res = await http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return new List<PublicBookingSlot>();
            return await res.Content.ReadFromJsonAsync<List<PublicBookingSlot>>(cancellationToken: ct) ?? new List<PublicBookingSlot>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetAvailability errored");
            return new List<PublicBookingSlot>();
        }
    }

    public async Task<(bool Ok, PublicBookingConfirmation? Confirmation, string? Error)> CreateBookingAsync(CreatePublicBookingRequest request, CancellationToken ct)
    {
        try
        {
            var res = await http.PostAsJsonAsync($"{_options.BaseUrl}/api/public/bookings/{_options.Slug}/appointments", request, ct);
            if (!res.IsSuccessStatusCode)
            {
                var raw = await res.Content.ReadAsStringAsync(ct);
                string message = raw;
                try { var parsed = System.Text.Json.JsonSerializer.Deserialize<string>(raw); if (parsed is not null) message = parsed; } catch { /* not a bare JSON string, use raw */ }
                return (false, null, string.IsNullOrWhiteSpace(message) ? "That time is no longer available. Please choose another slot." : message);
            }
            var confirmation = await res.Content.ReadFromJsonAsync<PublicBookingConfirmation>(cancellationToken: ct);
            return (true, confirmation, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CreateBooking errored");
            return (false, null, "We could not reach the booking service. Please check your connection and try again.");
        }
    }

    /// Posts the Contact page enquiry so it lands in the salon's Leads. Rejected is true when the API
    /// refused the content itself (400); any other failure is a plain not-ok so the page can ask to retry.
    public async Task<(bool Ok, bool Rejected)> SendEnquiryAsync(PublicEnquiryRequest request, CancellationToken ct)
    {
        try
        {
            var res = await http.PostAsJsonAsync($"{_options.BaseUrl}/api/public/bookings/{_options.Slug}/enquiry", request, ct);
            if (res.IsSuccessStatusCode) return (true, false);
            logger.LogWarning("SendEnquiry failed: {Status}", res.StatusCode);
            return (false, res.StatusCode == System.Net.HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SendEnquiry errored");
            return (false, false);
        }
    }
}
