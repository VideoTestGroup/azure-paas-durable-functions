namespace ImageIngest.Functions;

public class BlobsCleaner
{
    public static TimeSpan BlobOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(Environment.GetEnvironmentVariable("BlobOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

    [FunctionName(nameof(BlobsCleaner))]
    public async Task Run(
        [TimerTrigger("0 */%BlobsDeleteTimer% * * * *")] TimerInfo myTimer,
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation($"[DeleteZippedBlobsTimer] Start delete zipped blobs at: {DateTime.Now}");
        string deleteQuery = BlobClientExtensions.BuildTagsQuery(status: BlobStatus.Zipped, modifiedTime: DateTime.UtcNow.Subtract(BlobOutdatedThreshold).ToFileTimeUtc());
        log.LogInformation($"[DeleteZippedBlobsTimer] Delete by query {deleteQuery}");
        long deleteCount = await blobContainerClient.DeleteByTagsAsync(deleteQuery);
        log.LogInformation($"[DeleteZippedBlobsTimer] {deleteCount} blobs deleted successfully");
    }
}

