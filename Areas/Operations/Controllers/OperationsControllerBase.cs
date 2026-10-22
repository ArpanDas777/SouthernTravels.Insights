using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Core.Controllers;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>
/// Shared by the Operations report controllers: the area's shell settings (menu, home page). Each report controller
/// owns its page at /Operations/&lt;Report&gt; and renders Areas/Operations/Views/&lt;Report&gt;/Index.cshtml.
/// </summary>
[Area(OperationsArea.Name)]
public abstract class OperationsControllerBase : AppController
{
    protected override AreaInfo Area => OperationsArea.Info;
}
