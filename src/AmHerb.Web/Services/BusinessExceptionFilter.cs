using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AmHerb.Web.Services;
public class BusinessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not (BusinessException or DbUpdateException) && !DatabaseConflict.IsDeadlock(context.Exception)) return;
        var message = context.Exception is BusinessException ? context.Exception.Message : "ข้อมูลเปลี่ยนแปลงหรือซ้ำกับรายการเดิม กรุณาตรวจข้อมูลแล้วลองใหม่";
        context.Result = ErrorResult(context.HttpContext, message, 409);
        context.ExceptionHandled = true;
    }
    public static IActionResult ErrorResult(HttpContext http, string message, int status)
    {
        if (http.Request.Path.StartsWithSegments("/api")) return new ObjectResult(new ProblemDetails { Status = status, Title = message }) { StatusCode = status };
        return new ViewResult { ViewName = "BusinessError", StatusCode = status, ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary()) { Model = message } };
    }
}
public static class DatabaseConflict
{
    public static bool IsDeadlock(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
            if (current is Microsoft.Data.SqlClient.SqlException { Number: 1205 }) return true;
        return false;
    }
}
public class InputValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid && context.HttpContext.Request.Method != "GET")
            context.Result = BusinessExceptionFilter.ErrorResult(context.HttpContext, "ข้อมูลไม่ถูกต้อง: " + string.Join("; ", context.ModelState.Where(x => x.Value?.Errors.Count > 0).Select(x => x.Key)), 400);
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}
