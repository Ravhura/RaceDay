using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>What the rest of the API needs from cloud storage. A pretend version is used in the unit tests.</summary>
public interface IBlobStorageService
{
    /// <summary>Saves a file and returns its public address.</summary>
    Task<string> UploadAsync(Stream content, string contentType, string blobName);

    /// <summary>Deletes a file from its public address (used when a banner is replaced).</summary>
    Task DeleteAsync(string blobUrl);
}

/// <summary>Stores files in Azure Blob Storage. The connection string comes from configuration (user secrets), never from the code.</summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _container;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = configuration["AzureStorage:ContainerName"];
        if (string.IsNullOrWhiteSpace(containerName))
        {
            containerName = "event-banners";
        }

        // If no connection string is set, the rest of the API still works. Only uploads fail, with a friendly message.
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _container = new BlobContainerClient(connectionString, containerName);
        }
    }

    public async Task<string> UploadAsync(Stream content, string contentType, string blobName)
    {
        if (_container == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        await _container.CreateIfNotExistsAsync();

        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl)
    {
        if (_container == null)
        {
            return;
        }

        var blobName = Path.GetFileName(new Uri(blobUrl).AbsolutePath);
        await _container.DeleteBlobIfExistsAsync(blobName);
    }
}