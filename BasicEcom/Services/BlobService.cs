using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BasicEcom.Services
{
    // Stores product images in a private Blob container. Images are served through the app
    // (ProductsController.Image), so the container never needs anonymous read access.
    // Adapted from https://www.c-sharpcorner.com/article/integrating-azure-blob-storage-with-net/
    public class BlobService
    {
        private readonly BlobContainerClient _blobContainerClient;

        public BlobService(string connectionString, string containerName)
        {
            _blobContainerClient = new BlobContainerClient(connectionString, containerName);
            _blobContainerClient.CreateIfNotExists();
        }

        // Adapted from https://www.c-sharpcorner.com/article/uploading-files-to-azure-blob-storage-with-net-core/
        public async Task UploadBlobAsync(string blobName, Stream blobStream, string contentType)
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(blobStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            }).ConfigureAwait(false);
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync().ConfigureAwait(false);
        }

        // Returns the blob's content and content type, or null when it does not exist.
        public async Task<(Stream Content, string ContentType)?> DownloadBlobAsync(string blobName)
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
            if (!await blobClient.ExistsAsync().ConfigureAwait(false))
            {
                return null;
            }

            var download = await blobClient.DownloadStreamingAsync().ConfigureAwait(false);
            return (download.Value.Content, download.Value.Details.ContentType ?? "application/octet-stream");
        }
    }
}
