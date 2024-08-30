using Azure.Storage.Blobs;
using System.Diagnostics;

namespace BasicEcom.Services
{
    // This class is responsible for uploading, deleting and getting the URL of a blob
    public class BlobService
    {
        /*
         * Code Attribution:
         * Integrating Azure Blob Storage with .NET
         * Yogeshkumar Hadiya
         * 29 February 2024
         * C-sharpcorner
         * https://www.c-sharpcorner.com/article/integrating-azure-blob-storage-with-net/
         */
        private readonly BlobContainerClient _blobContainerClient;

        public BlobService(string connectionString, string containerName)
        {
            _blobContainerClient = new BlobContainerClient(connectionString, containerName);
            _blobContainerClient.CreateIfNotExists();
        }

        /*
         * Code Attribution:
         * Uploading Files to Azure Blob Storage with .NET Core
         * Nikunj Satasiya
         * 25 July 2024
         * C-sharpcorner
         * https://www.c-sharpcorner.com/article/uploading-files-to-azure-blob-storage-with-net-core/
         */
        public async Task UploadBlobAsync(string blobName, Stream blobStream)
        {
            try
            {
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(blobStream, overwrite: true).ConfigureAwait(false);
                Debug.WriteLine($"Successfully uploaded {blobName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error uploading blob: {ex.Message}");
                throw new Exception("Error uploading blob", ex);
            }
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            try
            {
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync().ConfigureAwait(false);
                Debug.WriteLine($"Successfully deleted {blobName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting blob: {ex.Message}");
                throw new Exception("Error deleting blob", ex);
            }
        }

        public string GetBlobUrl(string blobName)
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
            return blobClient.Uri.ToString();
        }
    }
}
