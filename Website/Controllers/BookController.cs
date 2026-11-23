using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LashesWebsite.Controllers;

[Route("book")]
public sealed class BookController(IGessFlowApiClient api, IOptions<GessFlowApiOptions> options) : SiteControllerBase(api)
{
    [HttpGet("")]
    public IActionResult Index(Guid? service)
    {
        ViewData["Title"] = PageTitle("title.book", $"Book — {SiteContent!.BusinessName}");
        ViewData["Description"] = $"Book your lash or brow appointment online at {SiteContent!.BusinessName}. Choose a service, an artist and a time that suits you.";
        ViewData["GessFlowApiBase"] = options.Value.BaseUrl;
        ViewData["GessFlowSlug"] = options.Value.Slug;
        ViewData["PreselectedServiceId"] = service;
        return View(Storefront);
    }
}
