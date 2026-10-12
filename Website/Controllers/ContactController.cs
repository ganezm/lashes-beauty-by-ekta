using System.Globalization;
using System.Text.RegularExpressions;
using LashesWebsite.Models;
using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

[Route("contact")]
public sealed partial class ContactController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    private const int MaxMessage = 900;

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailPattern();

    [HttpGet("")]
    public IActionResult Index(bool sent = false)
    {
        return Page(new EnquiryForm(), new Dictionary<string, string>(), sent, null);
    }

    /// Sends the enquiry server-side to the platform so it lands in the salon's Leads (source
    /// WebsiteForm; the platform has no separate bridal/event source, so that is carried by the
    /// event date, party size and a short prefix on the message).
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(EnquiryForm form, CancellationToken ct)
    {
        var lang = HttpContext.Lang();
        var errors = new Dictionary<string, string>();

        var name = (form.Name ?? "").Trim();
        var phone = (form.Phone ?? "").Trim();
        var email = (form.Email ?? "").Trim();
        var message = (form.Message ?? "").Trim();

        if (name.Length == 0 || name.Length > 120) errors[nameof(form.Name)] = lang.T("ct.errName");
        if (phone.Length == 0 && email.Length == 0) errors[nameof(form.Phone)] = lang.T("ct.errContact");
        if (email.Length > 0 && (email.Length > 200 || !EmailPattern().IsMatch(email))) errors[nameof(form.Email)] = lang.T("ct.errEmail");
        if (message.Length > MaxMessage) errors[nameof(form.Message)] = lang.T("ct.errMessage");

        DateOnly? eventDate = null;
        int? partySize = null;
        if (form.IsEvent)
        {
            if (!string.IsNullOrWhiteSpace(form.EventDate))
            {
                if (DateOnly.TryParseExact(form.EventDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) eventDate = d;
                else errors[nameof(form.EventDate)] = lang.T("ct.errDate");
            }
            if (!string.IsNullOrWhiteSpace(form.PartySize))
            {
                if (int.TryParse(form.PartySize.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n is >= 1 and <= 999) partySize = n;
                else errors[nameof(form.PartySize)] = lang.T("ct.errParty");
            }
        }

        if (errors.Count > 0) return Page(form, errors, false, lang.T("ct.errFix"));

        var interest = form.IsEvent
            ? (message.Length > 0 ? "Bridal or event enquiry: " + message : "Bridal or event enquiry")
            : (message.Length > 0 ? message : null);

        var request = new PublicEnquiryRequest
        {
            Name = name,
            Phone = phone.Length > 0 ? phone : null,
            Email = email.Length > 0 ? email : null,
            Interest = interest,
            EventDate = eventDate,
            PartySize = partySize,
            Source = "WebsiteForm",
            Website = form.Website
        };

        var (ok, _) = await Api.SendEnquiryAsync(request, ct);
        if (!ok) return Page(form, errors, false, lang.T("ct.errServer"));

        // Post, redirect, get: a refresh must not send the enquiry twice.
        return RedirectToAction(nameof(Index), new { sent = true });
    }

    private IActionResult Page(EnquiryForm form, Dictionary<string, string> errors, bool sent, string? error)
    {
        ViewData["Title"] = $"{HttpContext.Lang().T("nav.studio")} | {SiteContent!.BusinessName}";
        return View("Index", new ContactViewModel(SiteContent!, Storefront!, form, errors, sent, error));
    }
}

public sealed class EnquiryForm
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public bool IsEvent { get; set; }
    public string? EventDate { get; set; }
    public string? PartySize { get; set; }
    /// Honeypot: hidden from people, so only a bot fills it.
    public string? Website { get; set; }
}

public sealed record ContactViewModel(
    PublicWebsiteContent Content, PublicBookingStorefront Storefront,
    EnquiryForm Form, IReadOnlyDictionary<string, string> Errors, bool Sent, string? Error);
