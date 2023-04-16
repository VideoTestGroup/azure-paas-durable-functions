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
        var items = new List<BlobTags>(); //await blobContainerClient.QueryByTagsAsync(batchedQuery);
       
        await foreach (BlobTags tag in blobContainerClient.QueryByTagsAsync(batchedQuery))
            items.Add(tag);

        log.LogInformation($"[BlobsCleaner] blobContainerClient : {item.BatchId}");
        //var result = items.GroupBy(x => x.BatchId, x=>x.Namespace).Select(group => new { BatchId = group.Key, Namespace = group.FirstOrDefault() }) .ToList();
        var result = items.GroupBy(x => x.BatchId).Select(group => new { BatchId = group.Key}).ToList();

        log.LogInformation($"[BlobsCleaner] Query batchedQuery:{batchedQuery}");
        log.LogInformation($"[BlobsCleaner] Query result:{result.Count}");
        foreach (var item in result)
        {
            log.LogInformation($"[BlobsCleaner] Grouped Items: {item}");
            var batch = items.FirstOrDefault(x => x.BatchId == item.BatchId);
            log.LogInformation($"[BlobsCleaner] Retry Batches: {DateTime.Now}, Namespace: {batch.Namespace}, BatchId: {item.BatchId}");
            var activity = new ActivityAction() { Namespace = batch.Namespace , BatchId = item.BatchId };
            await starter.StartNewAsync(nameof(ZipperOrchestrator), activity);
            log.LogInformation($"[BlobsCleaner] Namespace {batch.Namespace} and {item.BatchId} Were Batched blobs started successfully");
        }
        log.LogInformation($"[BlobsCleaner] Batches count: {result.Count} and files count: {items.Count} Batch blobs strated successfully");
    }
}

