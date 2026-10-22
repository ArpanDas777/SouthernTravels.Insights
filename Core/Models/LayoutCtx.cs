namespace SouthernTravels.Core.Models;

/// <summary>One entry of an area's side menu. <paramref name="Id"/> matches <see cref="ReportPage.Id"/> on that report's page (it highlights the entry).</summary>
public sealed record NavItem(string Id, string Label, string Icon, string Url);

/// <summary>Everything the shared layout needs besides the page model: who is signed in and the area's name and menu.</summary>
public sealed class LayoutCtx
{
    /// <summary>The signed-in user's id, display name, initials and role (set from the login session).</summary>
    public string User { get; init; } = "";
    public string UserName { get; init; } = "";
    public string Initials { get; init; } = "";
    public string UserRole { get; init; } = "";

    /// <summary>The area being shown (e.g. "Operations"): its menu heading, short label for the collapsed rail and menu.</summary>
    public string AreaName { get; init; } = "";
    public string AreaShort { get; init; } = "";
    public List<NavItem> Nav { get; init; } = [];
}
