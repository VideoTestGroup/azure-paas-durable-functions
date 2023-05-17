namespace ImageIngest.Functions;

public class BlobsCleaner
{
    public static TimeSpan BlobOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(Environment.GetEnvironmentVariable("BlobOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

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
        string deleteQuery = $"Status = 'Zipped'";
        long deleteCount = await blobContainerClient.DeleteByTagsAsync(deleteQuery);
        log.LogInformation($"[BlobsCleaner] {deleteCount} zipped blobs deleted successfully");
        
        // // TODO: reafactor FindBlobsByTagsAsync for reuse
        // log.LogInformation($"[BlobsCleaner] Start Retry Batched files at blobs: {DateTime.Now}");
        // IDictionary<string, string> batches = new Dictionary<string, string>();
        // string query = $"Status = 'Batched' AND Modified < '{DateTime.UtcNow.Subtract(BatchedBlobsRetryThreshold).ToFileTimeUtc()}'";
        // List<TaggedBlobItem> blobs = new List<TaggedBlobItem>();
        // await foreach (TaggedBlobItem taggedBlobItem in blobContainerClient.FindBlobsByTagsAsync(query))
        // {
        //     BlobTags tag = new BlobTags(taggedBlobItem);
        //     batches[tag.BatchId] = tag.Namespace; 
        // }

        // log.LogInformation($"[BlobsCleaner] batches count: {batches.Count}");
        // foreach (KeyValuePair<string, string> kvp in batches)
        // {
        //     log.LogInformation($"[BlobsCleaner] Retry Batches: {DateTime.Now}, BatchId: {kvp.Key}, Namespace: {kvp.Value}");
        //     var activity = new ActivityAction() { Namespace = kvp.Value , BatchId = kvp.Key };
        //     await starter.StartNewAsync(nameof(ZipperOrchestrator), activity);
        //     log.LogInformation($"[BlobsCleaner] Namespace: {kvp.Value} and BatchId: {kvp.Key} Were Batched blobs started successfully");
        // }
        // log.LogInformation($"[BlobsCleaner] Batches count: {batches.Count} strated successfully");
    }
}

