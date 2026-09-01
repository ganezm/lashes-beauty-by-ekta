namespace LashesWebsite.Models;

// Plain POCOs mirroring GessFlow's PublicWebsiteContract/PublicBookingContract records exactly
// (this is a separate solution with no reference to GessFlow's C# assemblies, so the shapes are
// duplicated here rather than shared). System.Text.Json's case-insensitive property matching
// handles the camelCase JSON <-> PascalCase C# translation with no attributes needed.

public sealed class PublicHeroSlide { public Guid Id { get; set; } public string Kicker { get; set; } = ""; public string HeadlineLine1 { get; set; } = ""; public string HeadlineLine2 { get; set; } = ""; public string Body { get; set; } = ""; public string? PosterImageUrl { get; set; } public string? VideoUrl { get; set; } }
public sealed class PublicTrustStat { public Guid Id { get; set; } public string Number { get; set; } = ""; public string Label { get; set; } = ""; }
public sealed class PublicTestimonial { public Guid Id { get; set; } public string QuoteText { get; set; } = ""; public string AttributionFreeText { get; set; } = ""; }
public sealed class PublicGalleryItem { public Guid Id { get; set; } public string ImageUrl { get; set; } = ""; public string Category { get; set; } = ""; public string? LayoutHint { get; set; } public bool FeaturedOnHome { get; set; } }
public sealed class PublicBeforeAfterPair { public Guid Id { get; set; } public string BeforeImageUrl { get; set; } = ""; public string AfterImageUrl { get; set; } = ""; public string? Caption { get; set; } }
public sealed class PublicCredential { public Guid Id { get; set; } public string Tag { get; set; } = ""; public string Body { get; set; } = ""; }
public sealed class PublicTimelineEntry { public Guid Id { get; set; } public string Year { get; set; } = ""; public string Body { get; set; } = ""; }
public sealed class PublicFaq { public Guid Id { get; set; } public string Question { get; set; } = ""; public string Answer { get; set; } = ""; }
public sealed class PublicFooterLink { public Guid Id { get; set; } public string Column { get; set; } = ""; public string Text { get; set; } = ""; public string? Url { get; set; } }
public sealed class PublicMarqueeMessage { public Guid Id { get; set; } public string Text { get; set; } = ""; }
public sealed class PublicHowItWorksStep { public Guid Id { get; set; } public string Title { get; set; } = ""; public string Body { get; set; } = ""; }
public sealed class PublicConsultationChecklistItem { public Guid Id { get; set; } public string Text { get; set; } = ""; }
public sealed class PublicQuizResult { public Guid Id { get; set; } public string Name { get; set; } = ""; public string Descriptor { get; set; } = ""; public string ResultCopy { get; set; } = ""; public Guid RecommendedServiceId { get; set; } public string? RecommendedServiceName { get; set; } }
public sealed class PublicTeamMember { public Guid Id { get; set; } public string Name { get; set; } = ""; public string? Designation { get; set; } public string? Bio { get; set; } public string? PhotoUrl { get; set; } }
public sealed class PublicAcademyIntake { public Guid Id { get; set; } public DateOnly StartDate { get; set; } public DateOnly EndDate { get; set; } public int SeatsTotal { get; set; } public int SeatsBooked { get; set; } }
public sealed class PublicAcademyCourse { public Guid Id { get; set; } public string Level { get; set; } = ""; public string Name { get; set; } = ""; public string? Meta { get; set; } public decimal Price { get; set; } public List<string> Modules { get; set; } = new(); public List<PublicAcademyIntake> Intakes { get; set; } = new(); }
public sealed class PublicMembershipPlan { public Guid Id { get; set; } public string Name { get; set; } = ""; public decimal MonthlyPrice { get; set; } public int? SessionAllowance { get; set; } }

public sealed class PublicWebsiteContent
{
    public string Slug { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string? PrimaryColor { get; set; }
    public string? LogoUrl { get; set; }
    public string? MetaDescription { get; set; }
    public string? SocialInstagramUrl { get; set; }
    public string? SocialFacebookUrl { get; set; }
    public string? FooterTagline { get; set; }
    public string? CopyrightText { get; set; }
    public string? CtaHeadline { get; set; }
    public string? CtaBody { get; set; }
    public string? CtaButtonLabel { get; set; }
    public List<PublicHeroSlide> HeroSlides { get; set; } = new();
    public List<PublicTrustStat> TrustStats { get; set; } = new();
    public List<PublicTestimonial> Testimonials { get; set; } = new();
    public List<PublicGalleryItem> GalleryItems { get; set; } = new();
    public List<PublicBeforeAfterPair> BeforeAfterPairs { get; set; } = new();
    public List<PublicCredential> Credentials { get; set; } = new();
    public List<PublicTimelineEntry> TimelineEntries { get; set; } = new();
    public List<PublicFaq> Faqs { get; set; } = new();
    public List<PublicFooterLink> FooterLinks { get; set; } = new();
    public List<PublicMarqueeMessage> MarqueeMessages { get; set; } = new();
    public List<PublicHowItWorksStep> HowItWorksSteps { get; set; } = new();
    public List<PublicConsultationChecklistItem> ConsultationChecklistItems { get; set; } = new();
    public List<PublicQuizResult> QuizResults { get; set; } = new();
    public List<PublicTeamMember> Team { get; set; } = new();
    public List<PublicAcademyCourse> AcademyCourses { get; set; } = new();
    public List<PublicMembershipPlan> MembershipPlans { get; set; } = new();
}

public sealed class PublicBookingLocation { public Guid Id { get; set; } public string Name { get; set; } = ""; public string? Address { get; set; } public string? TimeZone { get; set; } public string? Phone { get; set; } public string? WhatsAppNumber { get; set; } public string? Email { get; set; } public string? MapEmbedUrl { get; set; } public string? OpeningHoursText { get; set; } }
public sealed class PublicBookingService { public Guid Id { get; set; } public string Name { get; set; } = ""; public decimal Price { get; set; } public int DurationMinutes { get; set; } public string? Description { get; set; } public string? ImageUrl { get; set; } public bool Featured { get; set; } public string? CategoryName { get; set; } }
public sealed class PublicBookingEmployee { public Guid Id { get; set; } public string Name { get; set; } = ""; public string? Designation { get; set; } public List<Guid> ServiceIds { get; set; } = new(); }
public sealed class BookingPolicySummary { public bool DepositRequired { get; set; } public decimal DepositPercent { get; set; } public decimal DepositMinimumAmount { get; set; } public int CancellationWindowHours { get; set; } public decimal LateCancellationFeeAmount { get; set; } public string? WaiverText { get; set; } }
public sealed class PublicBookingStorefront
{
    public string Slug { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string? PrimaryColor { get; set; }
    public string? Headline { get; set; }
    public string? Description { get; set; }
    public List<PublicBookingLocation> Locations { get; set; } = new();
    public List<PublicBookingService> Services { get; set; } = new();
    public List<PublicBookingEmployee> Employees { get; set; } = new();
    public BookingPolicySummary? Policy { get; set; }
}

public sealed class PublicBookingSlot { public Guid EmployeeId { get; set; } public DateTime StartAtUtc { get; set; } public DateTime EndAtUtc { get; set; } }

public sealed class CreatePublicBookingRequest
{
    public Guid LocationId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public string FirstName { get; set; } = "";
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}

public sealed class PublicBookingConfirmation { public Guid AppointmentId { get; set; } public string Status { get; set; } = ""; public DateTime StartAtUtc { get; set; } public DateTime EndAtUtc { get; set; } }
