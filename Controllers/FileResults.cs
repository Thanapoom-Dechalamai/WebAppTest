using Microsoft.AspNetCore.Mvc;

namespace Nontiwat_7362_L002.Controllers;

public static class FileResults
{
    // A file is on record (slip, proof, attachment) but not on disk: explain it instead of a bare 404 (D, 2026-11-25).
    // Missing records and records the user may not see still get a plain 404, so nothing is revealed.
    public static IActionResult FileMissing(this Controller controller)
    {
        controller.Response.StatusCode = StatusCodes.Status404NotFound;
        return controller.View("~/Views/Shared/FileMissing.cshtml");
    }
}
