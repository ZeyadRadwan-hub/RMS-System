using Microsoft.AspNetCore.Mvc;

namespace RMS_BACKEND.Controllers;

internal static class ApiError
{
    public static ActionResult Map(ControllerBase controller, Exception error)
    {
        return error switch
        {
            UnauthorizedAccessException => controller.Forbid(),
            KeyNotFoundException => controller.NotFound(new { message = "Resource not found" }),
            ArgumentException validation => controller.BadRequest(new { message = validation.Message }),
            _ => Unexpected(controller, error)
        };
    }

    private static ActionResult Unexpected(ControllerBase controller, Exception error)
    {
        var logger = controller.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("RMS.ApiError");
        logger.LogError("Unexpected API failure ({ExceptionType}), trace {TraceId}",
            error.GetType().Name, controller.HttpContext.TraceIdentifier);
        return controller.StatusCode(500, new { message = "Unexpected server error",
            traceId = controller.HttpContext.TraceIdentifier });
    }
}
