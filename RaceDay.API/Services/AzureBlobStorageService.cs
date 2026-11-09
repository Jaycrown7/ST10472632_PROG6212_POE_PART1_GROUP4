using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.API.Services
{
    public class AzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string ContainerName = "raceday-files";

        public AzureBlobStorageService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var container = _blobServiceClient
                .GetBlobContainerClient(ContainerName);

            await container.CreateIfNotExistsAsync(
                PublicAccessType.None);

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";

            var blob = container.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();

            await blob.UploadAsync(
                stream,
                new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                BlobName = fileName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blob.GenerateSasUri(sasBuilder).ToString();
        }
    }
}