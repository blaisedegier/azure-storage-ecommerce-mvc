using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BasicEcom.Services;

/*
 * Code Attribution:
 * Add a custom action filter in ASP.NET Core
 * Maclain Wiltzer
 * 19 May 2024
 * makolyte
 * https://makolyte.com/aspdotnet-core-how-to-add-your-own-action-filter/
 */
namespace BasicEcom.Attributes
{
    // This class is used to authorize the user as an admin.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var tableService = (TableService)context.HttpContext.RequestServices.GetService(typeof(TableService))!;
            var partitionKey = context.HttpContext.Session.GetString("PartitionKey");
            var rowKey = context.HttpContext.Session.GetString("RowKey");

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var customer = tableService.GetCustomerAsync(partitionKey, rowKey).Result;
            if (customer == null || !customer.isAdmin)
            {
                context.Result = new ForbidResult();
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
