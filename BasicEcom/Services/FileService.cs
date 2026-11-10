using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace BasicEcom.Services
{
    // Stores generated invoices in an Azure File Share, one directory per customer.
    // Adapted from https://www.c-sharpcorner.com/article/using-azure-file-storage-in-c-sharp/
    public class FileService
    {
        private readonly ShareClient? _shareClient;
        private readonly ILogger<FileService> _logger;

        public FileService(string connectionString, string shareName, ILogger<FileService> logger)
        {
            _logger = logger;
            try
            {
                var shareClient = new ShareClient(connectionString, shareName);
                shareClient.CreateIfNotExists();
                _shareClient = shareClient;
            }
            catch (Exception ex)
            {
                // Azurite emulates Blob, Queue and Table storage but not File Shares, so a local run carries on
                // without archiving invoices rather than failing every request that needs this service.
                _logger.LogWarning(ex, "File share '{ShareName}' is not available; invoices will not be archived.", shareName);
            }
        }

        public bool IsAvailable => _shareClient != null;

        public async Task UploadFileAsync(string directoryName, string fileName, Stream fileStream)
        {
            if (_shareClient == null)
            {
                return;
            }

            ShareDirectoryClient directoryClient = _shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            ShareFileClient fileClient = directoryClient.GetFileClient(fileName);
            await fileClient.CreateAsync(fileStream.Length).ConfigureAwait(false);
            await fileClient.UploadAsync(fileStream).ConfigureAwait(false);
        }

        // Adapted from https://www.iversis.com.au/post/download-file-from-azure-file-share
        public async Task<Stream?> DownloadFileAsync(string directoryName, string fileName)
        {
            if (_shareClient == null)
            {
                return null;
            }

            ShareFileClient fileClient = _shareClient.GetDirectoryClient(directoryName).GetFileClient(fileName);
            ShareFileDownloadInfo download = await fileClient.DownloadAsync().ConfigureAwait(false);
            return download.Content;
        }

        // Azure Files only deletes empty directories, so the files inside are removed first.
        public async Task DeleteDirectoryAsync(string directoryName)
        {
            if (_shareClient == null)
            {
                return;
            }

            ShareDirectoryClient directoryClient = _shareClient.GetDirectoryClient(directoryName);
            if (!await directoryClient.ExistsAsync().ConfigureAwait(false))
            {
                return;
            }

            await foreach (ShareFileItem item in directoryClient.GetFilesAndDirectoriesAsync().ConfigureAwait(false))
            {
                if (!item.IsDirectory)
                {
                    await directoryClient.DeleteFileAsync(item.Name).ConfigureAwait(false);
                }
            }
            await directoryClient.DeleteIfExistsAsync().ConfigureAwait(false);
        }
    }
}
