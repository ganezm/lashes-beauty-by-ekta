using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

/// The page shown for any address the site does not have: in the site's own look, with a real 404 status, and kept out of search results.
public sealed class NotFoundPageController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    public IActionResult Index()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["NoIndex"] = true;
        ViewData["Title"] = HttpContext.Lang().T("er.404.title") + " | " + SiteContent!.BusinessName;
        return View("~/Views/Shared/ErrorPage.cshtml", 404);
    }
}
