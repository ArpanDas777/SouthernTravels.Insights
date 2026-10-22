using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Base of the report controllers: the URL state (Q) and the side menu's "?pick=1" handling.</summary>
public abstract class BaseController : Controller
{
    /// <summary>The URL state every report reads (tab, page, Show all).</summary>
    protected ReportQuery Q => ReportQuery.From(Request.Query);

    /// <summary>
    /// Picking a report in the side menu links here with ?pick=1: remember that the rail should start collapsed
    /// (so the report gets the full width) and continue to the same page without the marker.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (Request.Query.ContainsKey("pick"))
        {
            Response.Cookies.Append("rail", "c", new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
            context.Result = Redirect(Request.PathBase + Request.Path);
            return;
        }
        base.OnActionExecuting(context);
    }
}
