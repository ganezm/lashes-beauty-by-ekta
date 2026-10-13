using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
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

    /// Fixed site copy written as trusted markup (it may carry entities such as &rsquo;), so it is not encoded again.
    public HtmlString H(string key) => new(T(key));

    /// As H, with {name} placeholders filled in; the values are HTML encoded the way Razor would encode them.
    public HtmlString H(string key, params (string Name, object? Value)[] args)
    {
        var text = T(key);
        foreach (var (name, value) in args)
            text = text.Replace("{" + name + "}", HtmlEncoder.Default.Encode(Convert.ToString(value, CultureInfo.CurrentCulture) ?? ""));
        return new HtmlString(text);
    }

    /// A calendar date, with the month spelled in Hindi when the site is in Hindi.
    public string Date(DateOnly date, bool withYear)
    {
        if (!IsHi) return date.ToString(withYear ? "d MMM yyyy" : "d MMM");
        var text = $"{date.Day} {SiteText.HiMonths[date.Month - 1]}";
        return withYear ? $"{text} {date.Year}" : text;
    }

    /// The strings the booking script needs, as JSON for a script tag.
    public string BookingJson() => JsonFor("bk.");

    /// The strings a script needs (every key with this prefix), as JSON for a script tag.
    public string JsonFor(string prefix)
    {
        var table = SiteText.Table(Code).Where(p => p.Key.StartsWith(prefix)).ToDictionary(p => p.Key, p => p.Value);
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

    public static readonly string[] HiMonths =
    {
        "जनवरी", "फ़रवरी", "मार्च", "अप्रैल", "मई", "जून", "जुलाई", "अगस्त", "सितंबर", "अक्टूबर", "नवंबर", "दिसंबर"
    };

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

        // Home page
        ["hm.explore"] = "Explore services", ["hm.bookNow"] = "Book now", ["hm.pauseHero"] = "Pause hero video",
        ["hm.playing"] = "Playing", ["hm.paused"] = "Paused",
        ["hm.philosophy"] = "our philosophy", ["hm.story"] = "Discover our story &rarr;",
        ["hm.edit"] = "the lash edit", ["hm.editTitle"] = "Find the look that feels unmistakably you", ["hm.bookLook"] = "Book this look &rarr;",
        ["hm.quizEyebrow"] = "find your look", ["hm.quizA"] = "What&rsquo;s your ", ["hm.quizB"] = "lash personality?",
        ["hm.match"] = "Your match &middot; ", ["hm.bookName"] = "Book {name} &rarr;",
        ["hm.results"] = "real results", ["hm.beforeAfter"] = "Before &amp; after", ["hm.before"] = "Before", ["hm.after"] = "After",
        ["hm.memberships"] = "memberships", ["hm.membTitle"] = "Keep the look on retainer", ["hm.perMonth"] = "/ month", ["hm.choosePlan"] = "Choose plan",
        ["hm.work"] = "work from the chair", ["hm.viewLookbook"] = "View full lookbook",
        ["hm.theStudio"] = "the studio", ["hm.meet"] = "Meet the artists", ["hm.reviews"] = "reviews", ["hm.follow"] = "Follow the feed &rarr;",
        ["hm.journal"] = "aftercare journal", ["hm.journalTitle"] = "How to keep them perfect",
        ["hm.tagAftercare"] = "aftercare", ["hm.tagRetention"] = "retention", ["hm.tagChoosing"] = "choosing",
        ["hm.art1t"] = "The first 24 hours after your set", ["hm.art1b"] = "No steam, no oil cleanser, no sleeping face-down. What actually damages retention.", ["hm.read4"] = "4 min read",
        ["hm.art2t"] = "Why lashes shed faster in monsoon", ["hm.art2b"] = "Humidity changes cure time and shedding cycles. How to adjust your infill schedule.", ["hm.read5"] = "5 min read",
        ["hm.art3t"] = "Classic vs hybrid vs volume", ["hm.art3b"] = "A plain comparison of weight, look and upkeep, so you book the right set once.", ["hm.read6"] = "6 min read",
        ["hm.gcLine"] = "Digital gift cards delivered instantly, redeemable against any service or infill.", ["hm.gcBuy"] = "Buy a gift card &rarr;",

        // Gift cards
        ["gc.eyebrow"] = "gift cards", ["gc.cardLower"] = "gift card", ["gc.title"] = "Give the set, not the guesswork.", ["gc.card"] = "Gift card", ["gc.noExpiry"] = "No expiry",
        ["gc.intro"] = "A digital gift card for {name}, delivered by email or SMS as soon as payment completes &mdash; redeemable against any service.",
        ["gc.amount"] = "amount", ["gc.custom"] = "Or enter a custom amount", ["gc.recipient"] = "recipient",
        ["gc.theirName"] = "Their name *", ["gc.theirEmail"] = "Their email", ["gc.theirMobile"] = "Their mobile",
        ["gc.hintRecipient"] = "Provide at least one of email or mobile so we can deliver the card.",
        ["gc.fromYou"] = "from you", ["gc.yourName"] = "Your name *", ["gc.yourEmail"] = "Your email", ["gc.yourMobile"] = "Your mobile",
        ["gc.hintYou"] = "Provide at least one of email or mobile so we can send your receipt.",
        ["gc.message"] = "A personal message (optional)", ["gc.placeholder"] = "Happy birthday! Treat yourself...",
        ["gc.continue"] = "Continue to payment &rarr;",
        ["gc.secure"] = "You&rsquo;ll be redirected to a secure Razorpay payment page. The gift card is delivered automatically once payment is confirmed.",
        ["gc.side"] = "Redeemable in-studio against any service. Delivered straight to the recipient&rsquo;s inbox or phone &mdash; nothing to print.",
        ["gc.checking"] = "Confirming your payment&hellip;", ["gc.checkingBody"] = "This only takes a moment.",
        ["gc.paid"] = "Your gift card is on its way.", ["gc.home"] = "Back to home &rarr;",
        ["gc.pending"] = "Still confirming with Razorpay&hellip;",
        ["gc.pendingBody"] = "Your payment is being processed. This page will update automatically &mdash; you can also close it and check your email or SMS shortly.",
        ["gc.notFound"] = "We couldn&rsquo;t find that order.",
        ["gc.notFoundBody"] = "If you completed a payment, please check your email/SMS, or get in touch and we&rsquo;ll sort it out.",
        ["gc.contact"] = "Contact us &rarr;",

        // About, Academy, Lookbook
        ["ab.eyebrow"] = "about the studio", ["ab.team"] = "The team", ["ab.credentials"] = "Credentials", ["ab.timeline"] = "The studio timeline",
        ["ac.eyebrow"] = "lash academy", ["ac.title"] = "Train with {name}",
        ["ac.lead"] = "Small-group certification courses with kit, models and post-course mentoring.",
        ["ac.reserve"] = "Reserve a seat",
        ["ac.upcoming"] = "Upcoming dates", ["ac.course"] = "Course", ["ac.seatsLeft"] = "Seats left", ["ac.seatsOf"] = "{left} of {total}", ["ac.enquire"] = "Enquire",
        ["lb.eyebrow"] = "the work", ["lb.all"] = "All",

        // Services page
        ["sv.eyebrow"] = "the menu", ["sv.title"] = "Lashes and brows",
        ["sv.lead"] = "A short menu, kept deliberately short. Every set is mapped in person before a single lash is placed.",
        ["sv.core"] = "core services", ["sv.window"] = "2&ndash;3 wks", ["sv.windowLbl"] = "infill window", ["sv.consult"] = "30 min", ["sv.consultLbl"] = "consultation, free",
        ["sv.prices"] = "Prices in INR &middot; patch test 24h prior",
        ["sv.patchTag"] = "Patch test", ["sv.patchBody"] = "A patch test is required 24 hours before a first set. It takes five minutes and is free.",
        ["sv.infillTag"] = "Infill window", ["sv.infillBody"] = "Infills are priced by the gap since your last set. Past three weeks, a full set is usually better value.",
        ["sv.addonsTag"] = "Add-ons", ["sv.addonsBody"] = "Eyelash tint, brow tint and lash lifting can be added to any appointment, subject to time.",
        ["sv.policy"] = "A {percent}% deposit is required to confirm your appointment. Cancellations within {hours} hours forfeit the deposit.",

        // Page titles (English titles keep their existing wording in the controllers)
        ["title.about"] = "About", ["title.academy"] = "Academy", ["title.lookbook"] = "Lookbook", ["title.services"] = "Services",
        ["title.book"] = "Book", ["title.gift"] = "Gift Cards", ["title.thanks"] = "Thank you",

        // Unavailable and error pages
        ["un.title"] = "Temporarily unavailable", ["un.head"] = "We&rsquo;ll be right back.",
        ["un.body"] = "This page couldn&rsquo;t load real-time content just now. Please refresh in a moment, or reach us directly.",
        ["er.title"] = "Error", ["er.head"] = "Error.", ["er.body"] = "An error occurred while processing your request.", ["er.requestId"] = "Request ID:",
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

        // Home page
        ["hm.explore"] = "सेवाएँ देखें", ["hm.bookNow"] = "अभी बुक करें", ["hm.pauseHero"] = "वीडियो रोकें",
        ["hm.playing"] = "चल रहा है", ["hm.paused"] = "रुका हुआ है",
        ["hm.philosophy"] = "हमारा दर्शन", ["hm.story"] = "हमारी कहानी जानें &rarr;",
        ["hm.edit"] = "लैश संग्रह", ["hm.editTitle"] = "वह लुक चुनिए जो बिल्कुल आपका अपना लगे", ["hm.bookLook"] = "यह लुक बुक करें &rarr;",
        ["hm.quizEyebrow"] = "अपना लुक खोजें", ["hm.quizA"] = "आपकी ", ["hm.quizB"] = "लैश पर्सनैलिटी क्या है?",
        ["hm.match"] = "आपका मैच &middot; ", ["hm.bookName"] = "{name} बुक करें &rarr;",
        ["hm.results"] = "असली नतीजे", ["hm.beforeAfter"] = "पहले और बाद में", ["hm.before"] = "पहले", ["hm.after"] = "बाद में",
        ["hm.memberships"] = "मेंबरशिप", ["hm.membTitle"] = "अपना लुक हमेशा बनाए रखें", ["hm.perMonth"] = "/ माह", ["hm.choosePlan"] = "प्लान चुनें",
        ["hm.work"] = "हमारे काम की झलक", ["hm.viewLookbook"] = "पूरी लुकबुक देखें",
        ["hm.theStudio"] = "स्टूडियो", ["hm.meet"] = "हमारे आर्टिस्ट से मिलिए", ["hm.reviews"] = "समीक्षाएँ", ["hm.follow"] = "हमें फ़ॉलो करें &rarr;",
        ["hm.journal"] = "आफ्टरकेयर जर्नल", ["hm.journalTitle"] = "इन्हें हमेशा परफ़ेक्ट कैसे रखें",
        ["hm.tagAftercare"] = "आफ्टरकेयर", ["hm.tagRetention"] = "टिकाऊपन", ["hm.tagChoosing"] = "सही चुनाव",
        ["hm.art1t"] = "आपके सेट के बाद के पहले 24 घंटे", ["hm.art1b"] = "न भाप, न ऑयल क्लींज़र, न पेट के बल सोना। लैशेज़ के टिकाऊपन को असल में क्या नुकसान पहुँचाता है।", ["hm.read4"] = "पढ़ने में 4 मिनट",
        ["hm.art2t"] = "मानसून में लैशेज़ जल्दी क्यों झड़ती हैं", ["hm.art2b"] = "नमी से जमने का समय और झड़ने का चक्र बदल जाता है। अपना इनफ़िल शेड्यूल कैसे ठीक करें।", ["hm.read5"] = "पढ़ने में 5 मिनट",
        ["hm.art3t"] = "क्लासिक, हाइब्रिड और वॉल्यूम में फ़र्क", ["hm.art3b"] = "वज़न, लुक और देखभाल की सीधी तुलना, ताकि आप एक बार में सही सेट बुक करें।", ["hm.read6"] = "पढ़ने में 6 मिनट",
        ["hm.gcLine"] = "डिजिटल गिफ़्ट कार्ड तुरंत भेजे जाते हैं, किसी भी सेवा या इनफ़िल पर इस्तेमाल करें।", ["hm.gcBuy"] = "गिफ़्ट कार्ड खरीदें &rarr;",

        // Gift cards
        ["gc.eyebrow"] = "गिफ़्ट कार्ड", ["gc.cardLower"] = "गिफ़्ट कार्ड", ["gc.title"] = "सेट दीजिए, अंदाज़ा नहीं।", ["gc.card"] = "गिफ़्ट कार्ड", ["gc.noExpiry"] = "कोई समाप्ति तिथि नहीं",
        ["gc.intro"] = "{name} का डिजिटल गिफ़्ट कार्ड, भुगतान पूरा होते ही ईमेल या SMS से भेज दिया जाता है। किसी भी सेवा पर इस्तेमाल किया जा सकता है।",
        ["gc.amount"] = "राशि", ["gc.custom"] = "या अपनी पसंद की राशि लिखें", ["gc.recipient"] = "पाने वाले",
        ["gc.theirName"] = "उनका नाम *", ["gc.theirEmail"] = "उनका ईमेल", ["gc.theirMobile"] = "उनका मोबाइल",
        ["gc.hintRecipient"] = "कृपया ईमेल या मोबाइल में से कम से कम एक दें, ताकि हम कार्ड भेज सकें।",
        ["gc.fromYou"] = "आपकी ओर से", ["gc.yourName"] = "आपका नाम *", ["gc.yourEmail"] = "आपका ईमेल", ["gc.yourMobile"] = "आपका मोबाइल",
        ["gc.hintYou"] = "कृपया ईमेल या मोबाइल में से कम से कम एक दें, ताकि हम आपको रसीद भेज सकें।",
        ["gc.message"] = "व्यक्तिगत संदेश (वैकल्पिक)", ["gc.placeholder"] = "जन्मदिन मुबारक! खुद को कुछ खास तोहफ़ा दीजिए...",
        ["gc.continue"] = "भुगतान के लिए आगे बढ़ें &rarr;",
        ["gc.secure"] = "आपको Razorpay के सुरक्षित भुगतान पेज पर ले जाया जाएगा। भुगतान की पुष्टि होते ही गिफ़्ट कार्ड अपने आप भेज दिया जाता है।",
        ["gc.side"] = "स्टूडियो में किसी भी सेवा पर इस्तेमाल किया जा सकता है। सीधे पाने वाले के इनबॉक्स या फ़ोन पर भेजा जाता है, प्रिंट करने की ज़रूरत नहीं।",
        ["gc.checking"] = "आपके भुगतान की पुष्टि हो रही है&hellip;", ["gc.checkingBody"] = "बस एक पल लगेगा।",
        ["gc.paid"] = "आपका गिफ़्ट कार्ड भेजा जा रहा है।", ["gc.home"] = "होम पर वापस जाएँ &rarr;",
        ["gc.pending"] = "Razorpay से पुष्टि अभी जारी है&hellip;",
        ["gc.pendingBody"] = "आपका भुगतान प्रोसेस हो रहा है। यह पेज अपने आप अपडेट हो जाएगा, आप इसे बंद करके थोड़ी देर बाद अपना ईमेल या SMS भी देख सकते हैं।",
        ["gc.notFound"] = "हमें वह ऑर्डर नहीं मिला।",
        ["gc.notFoundBody"] = "अगर आपने भुगतान किया है, तो कृपया अपना ईमेल या SMS देखें, या हमसे संपर्क करें, हम इसे सुलझा देंगे।",
        ["gc.contact"] = "हमसे संपर्क करें &rarr;",

        // About, Academy, Lookbook
        ["ab.eyebrow"] = "स्टूडियो के बारे में", ["ab.team"] = "हमारी टीम", ["ab.credentials"] = "प्रमाणपत्र और योग्यताएँ", ["ab.timeline"] = "स्टूडियो का सफ़र",
        ["ac.eyebrow"] = "लैश अकादमी", ["ac.title"] = "{name} के साथ सीखिए",
        ["ac.lead"] = "छोटे समूहों में प्रमाणित कोर्स, किट, मॉडल और कोर्स के बाद मार्गदर्शन के साथ।",
        ["ac.reserve"] = "सीट बुक करें",
        ["ac.upcoming"] = "आने वाली तारीख़ें", ["ac.course"] = "कोर्स", ["ac.seatsLeft"] = "बची हुई सीटें", ["ac.seatsOf"] = "{total} में से {left}", ["ac.enquire"] = "पूछताछ करें",
        ["lb.eyebrow"] = "हमारा काम", ["lb.all"] = "सभी",

        // Services page
        ["sv.eyebrow"] = "मेन्यू", ["sv.title"] = "लैश और ब्रो",
        ["sv.lead"] = "एक छोटा सा मेन्यू, जानबूझकर छोटा रखा गया है। हर सेट एक भी लैश लगाने से पहले आपके सामने नापकर तय किया जाता है।",
        ["sv.core"] = "मुख्य सेवाएँ", ["sv.window"] = "2&ndash;3 हफ़्ते", ["sv.windowLbl"] = "इनफ़िल का अंतराल", ["sv.consult"] = "30 मिनट", ["sv.consultLbl"] = "परामर्श, मुफ़्त",
        ["sv.prices"] = "कीमतें INR में &middot; पैच टेस्ट 24 घंटे पहले",
        ["sv.patchTag"] = "पैच टेस्ट", ["sv.patchBody"] = "पहले सेट से 24 घंटे पहले पैच टेस्ट ज़रूरी है। इसमें पाँच मिनट लगते हैं और यह मुफ़्त है।",
        ["sv.infillTag"] = "इनफ़िल का अंतराल", ["sv.infillBody"] = "इनफ़िल की कीमत पिछले सेट के बाद बीते समय के हिसाब से तय होती है। तीन हफ़्ते के बाद आमतौर पर पूरा सेट लगवाना ज़्यादा फ़ायदेमंद रहता है।",
        ["sv.addonsTag"] = "ऐड-ऑन", ["sv.addonsBody"] = "आईलैश टिंट, ब्रो टिंट और लैश लिफ़्टिंग किसी भी अपॉइंटमेंट में जोड़ी जा सकती है, समय के अनुसार।",
        ["sv.policy"] = "आपका अपॉइंटमेंट पक्का करने के लिए {percent}% जमा राशि ज़रूरी है। {hours} घंटे के भीतर रद्द करने पर जमा राशि वापस नहीं होगी।",

        // Fixed text kept in the views next to the owner's own wording (em dash lines)
        ["ac.soon"] = "कोर्स की तारीख़ें तय की जा रही हैं, कृपया जल्द ही दोबारा देखें।",
        ["lb.soon"] = "नया काम जल्द आ रहा है, कृपया दोबारा देखें।",
        ["sv.updating"] = "सेवाएँ अपडेट की जा रही हैं, कृपया थोड़ी देर में देखें, या सीधे हमसे संपर्क करें।",

        // Page titles
        ["title.about"] = "हमारे बारे में", ["title.academy"] = "अकादमी", ["title.lookbook"] = "लुकबुक", ["title.services"] = "सेवाएँ",
        ["title.book"] = "बुकिंग", ["title.gift"] = "गिफ़्ट कार्ड", ["title.thanks"] = "धन्यवाद",

        // Unavailable and error pages
        ["un.title"] = "अस्थायी रूप से उपलब्ध नहीं", ["un.head"] = "हम जल्द ही लौटेंगे।",
        ["un.body"] = "इस पेज पर अभी असली समय की जानकारी लोड नहीं हो सकी। कृपया थोड़ी देर में रिफ़्रेश करें, या हमसे सीधे संपर्क करें।",
        ["er.title"] = "त्रुटि", ["er.head"] = "त्रुटि।", ["er.body"] = "आपका अनुरोध प्रोसेस करते समय एक त्रुटि हुई।", ["er.requestId"] = "रिक्वेस्ट आईडी:",

        // Strings used by site.js (English defaults live in the script)
        ["sj.match"] = "आपका मैच · ", ["sj.bookName"] = "{name} बुक करें →",

        // Strings used by giftcards.js (English defaults live in the script)
        ["gcjs.errAmount"] = "कृपया ₹500 से ₹50,000 के बीच की राशि लिखें।",
        ["gcjs.errRecipientName"] = "कृपया पाने वाले का नाम लिखें।",
        ["gcjs.errRecipientContact"] = "कृपया पाने वाले का ईमेल या मोबाइल नंबर दें।",
        ["gcjs.errPurchaserName"] = "कृपया अपना नाम लिखें।",
        ["gcjs.errPurchaserContact"] = "कृपया अपना ईमेल या मोबाइल नंबर दें।",
        ["gcjs.wait"] = "कृपया प्रतीक्षा करें…", ["gcjs.continue"] = "भुगतान के लिए आगे बढ़ें →",
        ["gcjs.fallback"] = "हम इसे प्रोसेस नहीं कर सके। कृपया अपनी जानकारी जाँचकर दोबारा कोशिश करें।",
        ["gcjs.network"] = "भुगतान सेवा तक पहुँचा नहीं जा सका। कृपया अपना कनेक्शन जाँचें और दोबारा कोशिश करें।",
        ["gcjs.codeSent"] = "कोड {code} पाने वाले को भेज दिया गया है।",
    };
}
