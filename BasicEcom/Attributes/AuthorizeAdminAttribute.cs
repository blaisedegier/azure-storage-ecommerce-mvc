using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BasicEcom.Services;

namespace BasicEcom.Attributes
{
    // Allows the action only for a signed-in customer whose session token still matches their stored record
    // and whose record has isAdmin set. Both are re-read from Table Storage on every request, so signing out,
    // signing in elsewhere or revoking the flag takes effect immediately.
    // Adapted from https://makolyte.com/aspdotnet-core-how-to-add-your-own-action-filter/
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var tableService = context.HttpContext.RequestServices.GetRequiredService<TableService>();
            var customer = await SessionAuth.GetSignedInCustomerAsync(context.HttpContext, tableService);
            if (customer == null)
            {
                context.Result = new RedirectToActionResult("Login", "Customers", null);
                return;
            }

            if (!customer.isAdmin)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            await next();
        }
    }
}
