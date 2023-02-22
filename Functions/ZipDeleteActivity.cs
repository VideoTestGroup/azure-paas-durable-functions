using Azure.Storage.Blobs;
using ImageIngest.Functions.Model;

namespace ImageIngest.Functions;

public class ZipDeleteActivity
{
    [FunctionName(nameof(ZipDeleteActivity))]
    public static async Task Run(
        [ActivityTrigger] string blobName,
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient ftpBlobContainerClient,
        [Blob(Consts.ZipContainerName, Connection = "AzureWebJobsZipStorage")] BlobContainerClient zipBlobContainerClient,
        ILogger log)
    {
        log.LogInformation($"[ZipDeleteActivity] start delete zip {blobName}");

        var blobClient = zipBlobContainerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();

        log.LogInformation($"[ZipDeleteActivity] zip {blobName} deleted successfully");

        string batchId = Path.GetFileNameWithoutExtension(blobName);
        string deleteQuery = BlobClientExtensions.BuildTagsQuery(status: BlobStatus.Zipped, batchId: batchId);

        log.LogInformation($"[ZipDeleteActivity] Delete by query '{deleteQuery}', BatchId = {batchId}");

        var deletedCount = await ftpBlobContainerClient.DeleteByTagsAsync(deleteQuery);

        log.LogInformation($"[ZipDeleteActivity] zip {blobName} blobs deleted successfully, deleted: {deletedCount} blobs");
    }
}