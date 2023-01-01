using Azure.Storage.Blobs;
using ImageIngest.Functions.Model;

namespace ImageIngest.Functions;

public class ZipDeleteActivity
{
    private static string AzureWebJobsZipStorage => Environment.GetEnvironmentVariable("AzureWebJobsZipStorage");

    [FunctionName(nameof(ZipDeleteActivity))]
    public static async Task Run(
        [ActivityTrigger] string blobName,
        [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient ftpBlobContainerClient,
        ILogger log)
    {
        log.LogInformation($"[ZipDeleteActivity] start delete zip {blobName}");
        var blobClient = new BlobClient(AzureWebJobsZipStorage, "zip", blobName);
        await blobClient.DeleteIfExistsAsync();
        log.LogInformation($"[ZipDeleteActivity] zip {blobName} deleted successfully");
        string batchId = Path.GetFileNameWithoutExtension(blobName);
        string deleteQuery = $@"""Status""='{BlobStatus.Zipped}' AND ""BatchId""= '{batchId}'";
        log.LogInformation($"[ZipDeleteActivity] Delete by query '{deleteQuery}', BatchId = {batchId}");
        await ftpBlobContainerClient.DeleteByQueryAsync(deleteQuery);
        log.LogInformation($"[ZipDeleteActivity] zip {blobName} images deleted successfully");
    }
}