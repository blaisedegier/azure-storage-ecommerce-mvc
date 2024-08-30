using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.Diagnostics;

namespace BasicEcom.Services
{
    // This class is responsible for uploading, downloading, and deleting files from Azure File Share.
    public class FileService
    {
        /*
         * Code Attribution:
         * Using Azure File Storage In C#
         * Munib Butt
         * 28 May 2021
         * c-sharpcorner
         * https://www.c-sharpcorner.com/article/using-azure-file-storage-in-c-sharp/
         */
        private readonly ShareClient _shareClient;

        public FileService(string connectionString, string shareName)
        {
            _shareClient = new ShareClient(connectionString, shareName);
            _shareClient.CreateIfNotExists();
        }

        public async Task UploadFileAsync(string directoryName, string fileName, Stream fileStream)
        {
            try
            {
                ShareDirectoryClient directoryClient = _shareClient.GetDirectoryClient(directoryName);
                await directoryClient.CreateIfNotExistsAsync().ConfigureAwait(false);

                ShareFileClient fileClient = directoryClient.GetFileClient(fileName);
                await fileClient.CreateAsync(fileStream.Length).ConfigureAwait(false);
                await fileClient.UploadAsync(fileStream).ConfigureAwait(false);

                Debug.WriteLine($"File {fileName} uploaded successfully to {directoryName}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error uploading file: {ex.Message}");
                throw new Exception("Error uploading file", ex);
            }
        }

        /*
         * Code Attribution:
         * Download File from Azure File Share (C#)
         * .
         * 26 June 2021
         * iVersiS
         * https://www.iversis.com.au/post/download-file-from-azure-file-share
         */
        public async Task<Stream> DownloadFileAsync(string directoryName, string fileName)
        {
            try
            {
                ShareFileClient fileClient = _shareClient.GetDirectoryClient(directoryName).GetFileClient(fileName);
                ShareFileDownloadInfo download = await fileClient.DownloadAsync().ConfigureAwait(false);
                return download.Content;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading file: {ex.Message}");
                throw new Exception("Error downloading file", ex);
            }
        }

        public async Task DeleteFileAsync(string directoryName)
        {
            try
            {
                ShareDirectoryClient directoryClient = _shareClient.GetDirectoryClient(directoryName);

                await directoryClient.DeleteIfExistsAsync().ConfigureAwait(false);
                Debug.WriteLine($"Directory {directoryClient} deleted successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting file: {ex.Message}");
                throw new Exception("Error deleting file", ex);
            }
        }

        public string GetFileUrl(string directoryName, string fileName)
        {
            ShareFileClient fileClient = _shareClient.GetDirectoryClient(directoryName).GetFileClient(fileName);
            return fileClient.Uri.ToString();
        }
    }
}
