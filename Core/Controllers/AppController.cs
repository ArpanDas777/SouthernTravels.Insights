using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SouthernTravels.Core.Infrastructure;
using SouthernTravels.Core.Models;
using SouthernTravels.Core.Security;

namespace SouthernTravels.Core.Controllers;

/// <summary>What an area tells the shell about itself: its menu heading, short label, home page and menu.</summary>
public sealed record AreaInfo(string Name, string Short, string HomeUrl, List<NavItem> Nav);

/// <summary>
/// Base of every controller in every area. Builds the <see cref="LayoutCtx"/> (signed-in user, area menu) that the shared
/// layout reads, and handles the side menu's "?pick=1" marker.
/// </summary>
[AutoValidateAntiforgeryToken]
public abstract class AppController : Controller
{
    /// <summary>Implemented once per area.</summary>
    protected abstract AreaInfo Area { get; }

    protected LayoutCtx Ctx { get; private set; } = new();

    /// <summary>The URL state every report reads (tab, page, Show all).</summary>
    protected ReportQuery Q => ReportQuery.From(Request.Query);

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Picking a report in the side menu links here with ?pick=1: remember that the rail should start collapsed
        // (so the report gets the full width) and continue to the same page without the marker.
        if (Request.Query.ContainsKey("pick"))
        {
            Response.Cookies.Append("rail", "c", new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
            context.Result = Redirect(Request.PathBase + Request.Path);
            return;
        }

        // The signed-in user comes from the session set at login (SessionMiddleware sends anyone without one to the login page).
        var s = HttpContext.Session;
        var user = s.GetString(SessionKeys.UserId) ?? "";
        var name = s.GetString(SessionKeys.UserName) is { Length: > 0 } n ? n : user;
        var branch = s.GetString(SessionKeys.HandlingBranch);
        var isAdmin = string.Equals(s.GetString(SessionKeys.IsAdmin), "true", StringComparison.OrdinalIgnoreCase);
        Ctx = new LayoutCtx
        {
            User = user, UserName = name, Initials = Initials(name),
            UserRole = (isAdmin ? "Administrator" : "User") + (string.IsNullOrEmpty(branch) ? "" : " · " + branch),
            AreaName = Area.Name, AreaShort = Area.Short, Nav = Area.Nav,
        };
        ViewData["Ctx"] = Ctx;
        base.OnActionExecuting(context);
    }

    /// <summary>"Ravi Kumar" -> "RK", "ravi.k@x.com" -> "RK".</summary>
    static string Initials(string name)
    {
        var s = string.Concat(name.Split(new[] { ' ', '.', '@' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));
        return s.Length > 0 ? s : "?";
    }
}
