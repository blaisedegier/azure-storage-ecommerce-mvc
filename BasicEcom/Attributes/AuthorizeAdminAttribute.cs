using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BasicEcom.Services;

namespace BasicEcom.Attributes
{
    // Allows the action only for a signed-in customer whose stored record has isAdmin set. The flag is
    // re-read from Table Storage on every request, so revoking it takes effect immediately.
    // Adapted from https://makolyte.com/aspdotnet-core-how-to-add-your-own-action-filter/
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var session = context.HttpContext.Session;
            var partitionKey = session.GetString("PartitionKey");
            var rowKey = session.GetString("RowKey");

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                context.Result = new RedirectToActionResult("Login", "Customers", null);
                return;
            }

            var tableService = context.HttpContext.RequestServices.GetRequiredService<TableService>();
            var customer = await tableService.FindCustomerAsync(partitionKey, rowKey);
            if (customer == null || !customer.isAdmin)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            await next();
        }
    }
}
