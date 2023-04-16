namespace ImageIngest.Functions;

public class BlobsCleaner
{
    public static TimeSpan BlobOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(Environment.GetEnvironmentVariable("BlobOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

//     public static TimeSpan DuplicateBlobsDeleteThreshold { get; set; } =
//             TimeSpan.TryParse(Environment.GetEnvironmentVariable("DuplicateBlobsDeleteThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);
    
    public static TimeSpan BatchedBlobsRetryThreshold { get; set; } =
            TimeSpan.TryParse(Environment.GetEnvironmentVariable("BatchedBlobsRetryThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

    public static string DuplicateBlobsEntityName = Environment.GetEnvironmentVariable("DuplicateBlobsEntityName");


    [FunctionName(nameof(BlobsCleaner))]
    public async Task Run(
        [TimerTrigger("0 */%BlobsDeleteTimer% * * * *")] TimerInfo myTimer,
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        [DurableClient] IDurableClient durableClient,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        log.LogInformation($"[BlobsCleaner] Start delete zipped blobs at: {DateTime.Now}");
        string deleteQuery = BlobClientExtensions.BuildTagsQuery(status: BlobStatus.Zipped, modifiedTime: DateTime.UtcNow.Subtract(BlobOutdatedThreshold).ToFileTimeUtc());
        long deleteCount = await blobContainerClient.DeleteByTagsAsync(deleteQuery);
        log.LogInformation($"[BlobsCleaner] {deleteCount} zipped blobs deleted successfully");

//         log.LogInformation($"[BlobsCleaner] Start clean duplicate blobs");
//         var entityId = new EntityId(nameof(DuplicateBlobs), DuplicateBlobsEntityName);
//         await durableClient.SignalEntityAsync<IDuplicateBlobs>(entityId, x => x.Remove(DateTime.UtcNow.Subtract(DuplicateBlobsDeleteThreshold)));
//         deleteQuery = BlobClientExtensions.BuildTagsQuery(isDuplicate: true, modifiedTime: DateTime.UtcNow.Subtract(DuplicateBlobsDeleteThreshold).ToFileTimeUtc());
//         deleteCount = await blobContainerClient.DeleteByTagsAsync(deleteQuery);
//         log.LogInformation($"[BlobsCleaner] {deleteCount} duplicate blobs cleaned successfully");
        
        log.LogInformation($"[BlobsCleaner] Start Retry Batched files at blobs: {DateTime.Now}");
        string batchedQuery = BlobClientExtensions.BuildTagsQuery(status: BlobStatus.Batched, modifiedTime: DateTime.UtcNow.Subtract(BatchedBlobsRetryThreshold).ToFileTimeUtc());
        
        IDictionary<string, string> batches = new Dictionary<string, string>();
        await foreach (BlobTags tag in blobContainerClient.QueryByTagsAsync(batchedQuery))
            batches[tag.BatchId] = tag.Namespace;

        log.LogInformation($"[BlobsCleaner] Query batchedQuery:{batchedQuery}");
        log.LogInformation($"[BlobsCleaner] batches: {batches}");
        foreach (KeyValuePair<string, string> kvp in batches)
        {
            log.LogInformation($"[BlobsCleaner] Batch: {kvp}");
            log.LogInformation($"[BlobsCleaner] Retry Batches: {DateTime.Now}, BatchId: {kvp.Key}, Namespace: {kvp.Value}");
            var activity = new ActivityAction() { Namespace = kvp.Value , BatchId = kvp.Key };
            await starter.StartNewAsync(nameof(ZipperOrchestrator), activity);
            log.LogInformation($"[BlobsCleaner] Namespace: {kvp.Value} and BatchId: {kvp.Key} Were Batched blobs started successfully");
        }
        log.LogInformation($"[BlobsCleaner] Batches count: {batches.Count} strated successfully");
    }
}

