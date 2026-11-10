using Microsoft.AspNetCore.Mvc;

namespace SchoolAdmission.Common;

public static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (!result.Success)
        {
            return controller.ToErrorResult(result);
        }

        return controller.Ok(result.Data);
    }

    // 201 Created for endpoints that make a new row.
    public static ActionResult<T> ToCreatedResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (!result.Success)
        {
            return controller.ToErrorResult(result);
        }

        return controller.StatusCode(StatusCodes.Status201Created, result.Data);
    }

    // 204 No Content for endpoints that return nothing on success.
    public static ActionResult ToNoContentResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (!result.Success)
        {
            return controller.ToErrorResult(result);
        }

        return controller.NoContent();
    }

    private static ActionResult ToErrorResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (result.IsNotFound)
        {
            return controller.NotFound(result.ErrorMessage);
        }

        if (result.IsConflict)
        {
            return controller.Conflict(result.ErrorMessage);
        }

        return controller.BadRequest(result.ErrorMessage);
    }
}
