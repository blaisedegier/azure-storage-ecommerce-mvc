using Azure.Storage.Queues;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BasicEcom.Services
{
    // This class is responsible for enqueuing messages to the Azure Queue Storage
    public class QueueService
    {
        /*
         * Code Attribution:
         * Using Azure Storage Queues In .NET
         * Vincent Nyanga
         * n.d.
         * Hones Dev
         * https://honesdev.com/using-azure-storage-queues-in-dotnet/
         */
        private readonly QueueClient _queueClient;

        public QueueService(string connectionString, string queueName)
        {
            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        public async Task EnqueueMessageAsync<T>(T message)
        {
            try
            {
                string messageContent = JsonSerializer.Serialize(message);
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageContent);
                string base64Message = Convert.ToBase64String(messageBytes);

                await _queueClient.SendMessageAsync(base64Message).ConfigureAwait(false);
                Debug.WriteLine("Message enqueued successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enqueuing message: {ex.Message}");
                throw new Exception("Error enqueuing message", ex);
            }
        }
    }
}
