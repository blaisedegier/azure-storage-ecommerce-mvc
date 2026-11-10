using System.Security.Cryptography;
using System.Text;
using BasicEcom.Models;

namespace BasicEcom.Services
{
    // Session-based sign-in. A sign-in stores the customer's keys and the session token that was just
    // written to their customer record. Every protected request reloads the record and accepts the session
    // only while the two tokens still match, so signing out (which clears the stored token) or signing in
    // again elsewhere (which replaces it) ends every older session for that customer.
    public static class SessionAuth
    {
        public const string SessionTokenKey = "SessionToken";
        public const string CustomerNameKey = "CustomerName";
        public const string PartitionKeyKey = "PartitionKey";
        public const string RowKeyKey = "RowKey";
        public const string IsAdminKey = "isAdmin";

        // Replaces whatever the session held with the signed-in customer's details.
        public static void SignIn(ISession session, Customers customer)
        {
            session.Clear();
            session.SetString(SessionTokenKey, customer.SessionToken!);
            session.SetString(CustomerNameKey, customer.Name!);
            session.SetString(PartitionKeyKey, customer.PartitionKey!);
            session.SetString(RowKeyKey, customer.RowKey!);
            session.SetString(IsAdminKey, customer.isAdmin.ToString());
        }

        // Returns the signed-in customer, or null when the session is empty, the customer no longer exists,
        // or the session token no longer matches the one on the customer record. A session that fails the
        // check is cleared.
        public static async Task<Customers?> GetSignedInCustomerAsync(HttpContext httpContext, TableService tableService)
        {
            var session = httpContext.Session;
            var partitionKey = session.GetString(PartitionKeyKey);
            var rowKey = session.GetString(RowKeyKey);
            var token = session.GetString(SessionTokenKey);
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey) || string.IsNullOrEmpty(token))
            {
                return null;
            }

            var customer = await tableService.FindCustomerAsync(partitionKey, rowKey);
            if (customer == null || !TokensMatch(customer.SessionToken, token))
            {
                session.Clear();
                return null;
            }

            return customer;
        }

        private static bool TokensMatch(string? stored, string presented) =>
            !string.IsNullOrEmpty(stored)
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(stored), Encoding.UTF8.GetBytes(presented));
    }
}
