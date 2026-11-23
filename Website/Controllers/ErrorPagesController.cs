using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

/// The pages a visitor sees when a page is missing or something goes wrong: in the site's own look, with the menu, and with the
/// right status code so search engines do not index them.
[Route("error/{code:int}")]
public sealed class ErrorPagesController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    [HttpGet(""), HttpPost("")]
    public IActionResult Show(int code)
    {
        var status = code is 400 or 403 or 404 or 405 or 500 ? code : 500;
        Response.StatusCode = status;
        ViewData["NoIndex"] = true;
        var lang = HttpContext.Lang();
        ViewData["Title"] = (status == 404 ? lang.T("er.404.title") : lang.T("er.500.title")) + " | " + SiteContent!.BusinessName;
        return View("~/Views/Shared/ErrorPage.cshtml", status);
    }
}
