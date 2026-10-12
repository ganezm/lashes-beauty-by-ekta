using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace LashesWebsite.Services;

/// The guest's language for the whole site: English by default, Hindi on request. A ?lang=hi (or en)
/// link wins and is remembered in a cookie; otherwise the cookie decides. Content that comes from the
/// salon's CMS (hero slides, testimonials and so on) is not translated here.
public sealed class SiteLang(string code)
{
    public const string CookieName = "site-lang";
    public const string ItemKey = "site-lang";

    public string Code { get; } = code == "hi" ? "hi" : "en";
    public bool IsHi => Code == "hi";

    public string T(string key) => SiteText.Get(Code, key);

    /// The owner's Hindi wording when the language is Hindi and it is not blank; otherwise the English text.
    public string Pick(string? english, string? hindi) =>
        IsHi && !string.IsNullOrWhiteSpace(hindi) ? hindi! : english ?? "";

    /// Current page with ?lang= replaced, keeping every other query value.
    public static string SwitchUrl(HttpRequest request, string code)
    {
        var query = request.Query
            .Where(q => !q.Key.Equals("lang", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(q => q.Key, q => (string?)q.Value.ToString());
        query["lang"] = code;
        return QueryHelpers.AddQueryString(request.Path.HasValue ? request.Path.Value! : "/", query);
    }

    /// The strings the booking script needs, as JSON for a script tag.
    public string BookingJson()
    {
        var table = SiteText.Table(Code).Where(p => p.Key.StartsWith("bk.")).ToDictionary(p => p.Key, p => p.Value);
        return JsonSerializer.Serialize(table, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default
        });
    }
}

public static class SiteLangExtensions
{
    public static SiteLang Lang(this HttpContext context) =>
        context.Items[SiteLang.ItemKey] as SiteLang ?? new SiteLang("en");
}

public static class SiteText
{
    public static string Get(string lang, string key)
    {
        if (lang == "hi" && Hi.TryGetValue(key, out var hi)) return hi;
        return En.TryGetValue(key, out var en) ? en : key;
    }

    public static IReadOnlyDictionary<string, string> Table(string lang)
    {
        if (lang != "hi") return En;
        var merged = new Dictionary<string, string>(En);
        foreach (var p in Hi) merged[p.Key] = p.Value;
        return merged;
    }

    private static readonly Dictionary<string, string> En = new()
    {
        // Site chrome
        ["nav.home"] = "Home", ["nav.services"] = "Services", ["nav.lookbook"] = "Lookbook", ["nav.academy"] = "Academy",
        ["nav.about"] = "About Ekta", ["nav.studio"] = "Studio", ["nav.book"] = "Book appointment",
        ["nav.menu"] = "Open menu", ["nav.close"] = "Close menu", ["nav.language"] = "Language",
        ["footer.tagline"] = "Beauty, designed around you.", ["footer.instagram"] = "Instagram", ["footer.facebook"] = "Facebook",
        ["common.call"] = "Call", ["common.callStudio"] = "Call studio", ["common.whatsapp"] = "WhatsApp",
        ["svc.book"] = "Book", ["svc.min"] = "min", ["svc.patch"] = "Patch test needed",

        // Contact page
        ["ct.eyebrow"] = "get in touch", ["ct.title"] = "The studio", ["ct.mapTitle"] = "Studio location", ["ct.mapSoon"] = "Map coming soon",
        ["ct.address"] = "Address", ["ct.hours"] = "Hours", ["ct.contact"] = "Contact",
        ["ct.send"] = "Send an enquiry", ["ct.faq"] = "FAQ", ["ct.noFaq"] = "No FAQs published yet.",
        ["ct.name"] = "Name", ["ct.phone"] = "Mobile", ["ct.email"] = "Email", ["ct.message"] = "Message",
        ["ct.bridal"] = "Bridal or event enquiry", ["ct.eventDate"] = "Event date", ["ct.partySize"] = "Party size (people)",
        ["ct.submit"] = "Send enquiry",
        ["ct.sent"] = "Thank you. Your enquiry has reached the studio and we will reply soon.",
        ["ct.errName"] = "Please enter your name.", ["ct.errContact"] = "Please enter a mobile number or an email address so we can reply.",
        ["ct.errEmail"] = "Please enter a valid email address.", ["ct.errMessage"] = "Please keep your message under 900 characters.",
        ["ct.errDate"] = "Please enter a valid event date.", ["ct.errParty"] = "Party size must be a number between 1 and 999.",
        ["ct.errServer"] = "We could not send your enquiry just now. Please try again, or call or message the studio.",
        ["ct.errFix"] = "Please fix the highlighted fields and try again.",

        // Booking flow (bk.* keys are also sent to the booking script)
        ["bk.eyebrow"] = "book your visit", ["bk.title"] = "Book an appointment",
        ["bk.step1"] = "Service", ["bk.step2"] = "Date & artist", ["bk.step3"] = "Details", ["bk.step4"] = "Confirm",
        ["bk.selectService"] = "select a service", ["bk.patch"] = "Patch test needed before this service",
        ["bk.loadingAvail"] = "Loading real-time availability…", ["bk.chooseArtist"] = "choose artist",
        ["bk.pickDate"] = "pick a date", ["bk.pickDateShort"] = "Pick a date",
        ["bk.mon"] = "Mon", ["bk.tue"] = "Tue", ["bk.wed"] = "Wed", ["bk.thu"] = "Thu", ["bk.fri"] = "Fri", ["bk.sat"] = "Sat", ["bk.sun"] = "Sun",
        ["bk.slots"] = "available slots", ["bk.slotsPrompt"] = "Choose an artist and date to see real available times.",
        ["bk.slotsLoading"] = "Loading available times…", ["bk.slotsEmpty"] = "No open times for this artist on this date. Please try another date.",
        ["bk.details"] = "your details", ["bk.firstName"] = "First name *", ["bk.lastName"] = "Last name", ["bk.mobile"] = "Mobile", ["bk.email"] = "Email",
        ["bk.notes"] = "Anything we should know",
        ["bk.consentLegend"] = "Offers and updates (optional)", ["bk.consentWhatsApp"] = "Send me offers on WhatsApp", ["bk.consentEmailSms"] = "Send me offers by email and SMS",
        ["bk.consentNote"] = "Booking confirmations and reminders are sent either way. You can stop offers at any time.",
        ["bk.policyTitle"] = "Deposit and cancellation",
        ["bk.depositMay"] = "A deposit of {amount} ({percent}%) is required to confirm your booking.",
        ["bk.cancelWindow"] = "Cancelling within {hours} hours of your appointment is a late cancellation.",
        ["bk.lateFee"] = "A late cancellation fee of {fee} applies.", ["bk.lateForfeit"] = "A late cancellation forfeits the deposit.",
        ["bk.confirmedTitle"] = "Your booking request is in.", ["bk.confirmedBody"] = "We’ve captured your appointment request and will confirm shortly.",
        ["bk.reference"] = "Booking reference: {ref}",
        ["bk.consultNote"] = "A short consultation form may be emailed to you before your visit. Please fill it in so we can prepare.",
        ["bk.depositDue"] = "A deposit of {amount} is required to secure this booking.",
        ["bk.depositHold"] = "Your slot is held for a limited time. If the deposit is not paid in that time, the slot is released.",
        ["bk.depositSent"] = "We have also sent you the payment link by message and email.",
        ["bk.depositNoLink"] = "The studio will send you the payment details shortly.",
        ["bk.payDeposit"] = "Pay deposit",
        ["bk.errorFallback"] = "That time is no longer available. Please choose another slot.",
        ["bk.back"] = "← Back", ["bk.continue"] = "Continue →", ["bk.confirm"] = "Confirm booking", ["bk.confirming"] = "Confirming…",
        ["bk.yourBooking"] = "your booking", ["bk.sService"] = "Service", ["bk.sArtist"] = "Artist", ["bk.sDate"] = "Date", ["bk.sTime"] = "Time",
        ["bk.sDuration"] = "Duration", ["bk.sTotal"] = "Total", ["bk.notSelected"] = "Not selected", ["bk.min"] = "min",
        ["bk.eSelectService"] = "Please select a service before continuing.", ["bk.eSelectSlot"] = "Please select an artist, date and available time.",
        ["bk.eFirstName"] = "Please enter your first name.", ["bk.eContact"] = "Please provide a mobile number or email so we can reach you.",
        ["bk.eEmail"] = "Please enter a valid email address.", ["bk.eMissing"] = "Something is missing from your booking. Please review your selections.",
        ["bk.eAvail"] = "Could not load available times. Please try again.",
        ["bk.eNetwork"] = "We could not reach the booking service. Please check your connection and try again.",
        ["bk.sumDeposit"] = "A {percent}% deposit ({amount}) is required to confirm.",
        ["bk.sumCancel"] = "Cancellations within {hours} hours may incur a fee.",
    };

    private static readonly Dictionary<string, string> Hi = new()
    {
        ["nav.home"] = "होम", ["nav.services"] = "सेवाएँ", ["nav.lookbook"] = "लुकबुक", ["nav.academy"] = "अकादमी",
        ["nav.about"] = "एकता के बारे में", ["nav.studio"] = "स्टूडियो", ["nav.book"] = "अपॉइंटमेंट बुक करें",
        ["nav.menu"] = "मेन्यू खोलें", ["nav.close"] = "मेन्यू बंद करें", ["nav.language"] = "भाषा",
        ["footer.tagline"] = "आपके लिए गढ़ी गई सुंदरता।", ["footer.instagram"] = "इंस्टाग्राम", ["footer.facebook"] = "फ़ेसबुक",
        ["common.call"] = "कॉल", ["common.callStudio"] = "स्टूडियो को कॉल करें", ["common.whatsapp"] = "व्हाट्सऐप",
        ["svc.book"] = "बुक करें", ["svc.min"] = "मिनट", ["svc.patch"] = "पैच टेस्ट ज़रूरी",

        ["ct.eyebrow"] = "संपर्क करें", ["ct.title"] = "स्टूडियो", ["ct.mapTitle"] = "स्टूडियो का स्थान", ["ct.mapSoon"] = "नक्शा जल्द ही उपलब्ध होगा",
        ["ct.address"] = "पता", ["ct.hours"] = "समय", ["ct.contact"] = "संपर्क",
        ["ct.send"] = "पूछताछ भेजें", ["ct.faq"] = "अक्सर पूछे जाने वाले प्रश्न", ["ct.noFaq"] = "अभी कोई प्रश्न प्रकाशित नहीं हुआ है।",
        ["ct.name"] = "नाम", ["ct.phone"] = "मोबाइल", ["ct.email"] = "ईमेल", ["ct.message"] = "संदेश",
        ["ct.bridal"] = "दुल्हन या इवेंट की पूछताछ", ["ct.eventDate"] = "इवेंट की तारीख़", ["ct.partySize"] = "लोगों की संख्या",
        ["ct.submit"] = "पूछताछ भेजें",
        ["ct.sent"] = "धन्यवाद। आपकी पूछताछ स्टूडियो तक पहुँच गई है, हम जल्द ही जवाब देंगे।",
        ["ct.errName"] = "कृपया अपना नाम लिखें।", ["ct.errContact"] = "कृपया मोबाइल नंबर या ईमेल दें ताकि हम आपको जवाब दे सकें।",
        ["ct.errEmail"] = "कृपया सही ईमेल पता लिखें।", ["ct.errMessage"] = "कृपया संदेश 900 अक्षरों से कम रखें।",
        ["ct.errDate"] = "कृपया इवेंट की सही तारीख़ लिखें।", ["ct.errParty"] = "लोगों की संख्या 1 से 999 के बीच होनी चाहिए।",
        ["ct.errServer"] = "अभी आपकी पूछताछ भेजी नहीं जा सकी। कृपया दोबारा कोशिश करें, या स्टूडियो को कॉल या संदेश करें।",
        ["ct.errFix"] = "कृपया चिह्नित जानकारी ठीक करके दोबारा कोशिश करें।",

        ["bk.eyebrow"] = "अपनी विज़िट बुक करें", ["bk.title"] = "अपॉइंटमेंट बुक करें",
        ["bk.step1"] = "सेवा", ["bk.step2"] = "तारीख़ और आर्टिस्ट", ["bk.step3"] = "विवरण", ["bk.step4"] = "पुष्टि",
        ["bk.selectService"] = "सेवा चुनें", ["bk.patch"] = "इस सेवा से पहले पैच टेस्ट ज़रूरी है",
        ["bk.loadingAvail"] = "उपलब्ध समय लोड हो रहा है…", ["bk.chooseArtist"] = "आर्टिस्ट चुनें",
        ["bk.pickDate"] = "तारीख़ चुनें", ["bk.pickDateShort"] = "तारीख़ चुनें",
        ["bk.mon"] = "सोम", ["bk.tue"] = "मंगल", ["bk.wed"] = "बुध", ["bk.thu"] = "गुरु", ["bk.fri"] = "शुक्र", ["bk.sat"] = "शनि", ["bk.sun"] = "रवि",
        ["bk.slots"] = "उपलब्ध समय", ["bk.slotsPrompt"] = "असली उपलब्ध समय देखने के लिए आर्टिस्ट और तारीख़ चुनें।",
        ["bk.slotsLoading"] = "उपलब्ध समय खोजे जा रहे हैं…", ["bk.slotsEmpty"] = "इस दिन इस आर्टिस्ट के पास कोई समय उपलब्ध नहीं है। कृपया कोई दूसरी तारीख़ चुनें।",
        ["bk.details"] = "आपका विवरण", ["bk.firstName"] = "पहला नाम *", ["bk.lastName"] = "उपनाम", ["bk.mobile"] = "मोबाइल", ["bk.email"] = "ईमेल",
        ["bk.notes"] = "कुछ और जो हमें पता होना चाहिए",
        ["bk.consentLegend"] = "ऑफ़र और अपडेट (वैकल्पिक)", ["bk.consentWhatsApp"] = "मुझे ऑफ़र व्हाट्सऐप पर भेजें", ["bk.consentEmailSms"] = "मुझे ऑफ़र ईमेल और SMS से भेजें",
        ["bk.consentNote"] = "बुकिंग की पुष्टि और रिमाइंडर हर हाल में भेजे जाते हैं। ऑफ़र आप कभी भी बंद कर सकते हैं।",
        ["bk.policyTitle"] = "जमा राशि और रद्द करने की नीति",
        ["bk.depositMay"] = "आपकी बुकिंग पक्की करने के लिए {amount} ({percent}%) की जमा राशि ज़रूरी है।",
        ["bk.cancelWindow"] = "अपॉइंटमेंट से {hours} घंटे के भीतर रद्द करना देर से रद्द करना माना जाएगा।",
        ["bk.lateFee"] = "देर से रद्द करने पर {fee} का शुल्क लगेगा।", ["bk.lateForfeit"] = "देर से रद्द करने पर जमा राशि वापस नहीं होगी।",
        ["bk.confirmedTitle"] = "आपकी बुकिंग का अनुरोध मिल गया है।", ["bk.confirmedBody"] = "हमें आपका अपॉइंटमेंट अनुरोध मिल गया है, हम जल्द ही पुष्टि करेंगे।",
        ["bk.reference"] = "बुकिंग संदर्भ: {ref}",
        ["bk.consultNote"] = "आपकी विज़िट से पहले एक छोटा परामर्श फ़ॉर्म ईमेल से भेजा जा सकता है। कृपया उसे भर दें ताकि हम तैयारी कर सकें।",
        ["bk.depositDue"] = "इस बुकिंग को पक्का करने के लिए {amount} की जमा राशि ज़रूरी है।",
        ["bk.depositHold"] = "आपका समय सीमित अवधि के लिए रोका गया है। उस अवधि में जमा राशि न भरने पर समय छोड़ दिया जाएगा।",
        ["bk.depositSent"] = "हमने पेमेंट लिंक संदेश और ईमेल से भी भेज दिया है।",
        ["bk.depositNoLink"] = "स्टूडियो जल्द ही आपको भुगतान की जानकारी भेजेगा।",
        ["bk.payDeposit"] = "जमा राशि भरें",
        ["bk.errorFallback"] = "यह समय अब उपलब्ध नहीं है। कृपया कोई दूसरा समय चुनें।",
        ["bk.back"] = "← पीछे", ["bk.continue"] = "आगे बढ़ें →", ["bk.confirm"] = "बुकिंग पक्की करें", ["bk.confirming"] = "पुष्टि हो रही है…",
        ["bk.yourBooking"] = "आपकी बुकिंग", ["bk.sService"] = "सेवा", ["bk.sArtist"] = "आर्टिस्ट", ["bk.sDate"] = "तारीख़", ["bk.sTime"] = "समय",
        ["bk.sDuration"] = "अवधि", ["bk.sTotal"] = "कुल", ["bk.notSelected"] = "चुना नहीं गया", ["bk.min"] = "मिनट",
        ["bk.eSelectService"] = "आगे बढ़ने से पहले कृपया एक सेवा चुनें।", ["bk.eSelectSlot"] = "कृपया आर्टिस्ट, तारीख़ और उपलब्ध समय चुनें।",
        ["bk.eFirstName"] = "कृपया अपना पहला नाम लिखें।", ["bk.eContact"] = "कृपया मोबाइल नंबर या ईमेल दें ताकि हम आपसे संपर्क कर सकें।",
        ["bk.eEmail"] = "कृपया सही ईमेल पता लिखें।", ["bk.eMissing"] = "आपकी बुकिंग में कुछ जानकारी छूट गई है। कृपया अपनी पसंद जाँच लें।",
        ["bk.eAvail"] = "उपलब्ध समय लोड नहीं हो सका। कृपया दोबारा कोशिश करें।",
        ["bk.eNetwork"] = "बुकिंग सेवा तक पहुँचा नहीं जा सका। कृपया अपना कनेक्शन जाँचें और दोबारा कोशिश करें।",
        ["bk.sumDeposit"] = "पुष्टि के लिए {percent}% जमा राशि ({amount}) ज़रूरी है।",
        ["bk.sumCancel"] = "{hours} घंटे के भीतर रद्द करने पर शुल्क लग सकता है।",
    };
}
