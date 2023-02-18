namespace ImageIngest.Functions;

public class BlobsCleaner
{
    public static TimeSpan BlobOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(Environment.GetEnvironmentVariable("BlobOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

    public static TimeSpan DuplicateBlobsDeleteThreshold { get; set; } =
            TimeSpan.TryParse(Environment.GetEnvironmentVariable("DuplicateBlobsDeleteThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

    public static string DuplicateBlobsEntityName = Environment.GetEnvironmentVariable("DuplicateBlobsEntityName");


    [FunctionName(nameof(BlobsCleaner))]
    public async Task Run(
        [TimerTrigger("0 */%BlobsDeleteTimer% * * * *")] TimerInfo myTimer,
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        [DurableClient] IDurableClient durableClient,
        ILogger log)
    {
        log.LogInformation($"[BlobsCleaner] Start delete zipped blobs at: {DateTime.Now}");
        string deleteQuery = BlobClientExtensions.BuildTagsQuery(status: BlobStatus.Zipped, modifiedTime: DateTime.UtcNow.Subtract(BlobOutdatedThreshold).ToFileTimeUtc());
        long deleteCount = await blobContainerClient.DeleteByTagsAsync(deleteQuery);
        log.LogInformation($"[BlobsCleaner] {deleteCount} zipped blobs deleted successfully");

        log.LogInformation($"[BlobsCleaner] Start clean duplicate blobs");
        var entityId = new EntityId(nameof(DuplicateBlobs), DuplicateBlobsEntityName);
        await durableClient.SignalEntityAsync<IDuplicateBlobs>(entityId, x => x.Remove(DateTime.UtcNow.Subtract(DuplicateBlobsDeleteThreshold)));
        deleteQuery = BlobClientExtensions.BuildTagsQuery(isDuplicate: true, modifiedTime: DateTime.UtcNow.Subtract(DuplicateBlobsDeleteThreshold).ToFileTimeUtc());
        deleteCount = await blobContainerClient.DeleteByTagsAsync(deleteQuery);
        log.LogInformation($"[BlobsCleaner] {deleteCount} duplicate blobs cleaned successfully");
    }
}

