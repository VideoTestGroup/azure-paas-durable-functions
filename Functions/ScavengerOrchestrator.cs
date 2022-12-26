using Azure.Storage.Blobs;

namespace ImageIngest.Functions;
public class ScavengerOrchestrator
{
    public static string AzureWebJobsFTPStorage { get; set; } = System.Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");

    public static TimeSpan ScavengerOutdatedThreshold { get; set; } =
        TimeSpan.TryParse(System.Environment.GetEnvironmentVariable("ScavengerOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);


    [FunctionName(nameof(ScavengerOrchestrator))]
    public static async Task Run(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation($"[ScavengerOrchestrator] OrchestrationTrigger triggered Function from [Scavenger] for InstanceId {context.InstanceId}");
        List<BlobTags> blobs = new List<BlobTags>();
        await foreach (BlobTags tags in blobContainerClient.QueryAsync(t =>
            t.Status == BlobStatus.Pending &&
            t.Modified < DateTime.UtcNow.Add(ScavengerOutdatedThreshold).ToFileTimeUtc()))
        {
            log.LogInformation($"[ScavengerOrchestrator] Found pending old file {tags.Name}, tags: {tags.Tags}");
            blobs.Add(tags);
        }

        if (blobs.Count < 1)
        {
            log.LogInformation($"[ScavengerOrchestrator] Not Found pending files");
            return;
        }

        log.LogInformation($"[ScavengerOrchestrator] Found {blobs.Count} pending old files");
        var blobsByNamespace = blobs.GroupBy(b => b.Namespace);
        foreach (var blobsNamespace in blobsByNamespace)
        {
            ActivityAction activity = new ActivityAction();
            // Create batch id
            activity.OverrideBatchId = ActivityAction.EnlistBatchId(blobsNamespace.Key);
            activity.OverrideStatus = BlobStatus.Batched;
            await Task.WhenAll(blobsNamespace.Select(tag =>
                new BlobClient(AzureWebJobsFTPStorage, tag.Container, tag.Name).WriteTagsAsync(tag, null, t =>
                {
                    t.Status = activity.OverrideStatus;
                    t.BatchId = activity.OverrideBatchId;
                })
            ));

            log.LogInformation($"[ScavengerOrchestrator] Tags marked {blobsNamespace.Count()} blobs.\nSActivity: {activity}.\nFiles: {string.Join(",", blobsNamespace.Select(t => $"{t.Name} ({t.Length.Bytes2Megabytes()}MB)"))}");
            log.LogInformation($"[ScavengerOrchestrator] Zipping files. ActivityAction {activity}");
            await context.CallActivityAsync<ActivityAction>(nameof(Zipper), activity);
            log.LogInformation($"[ScavengerOrchestrator] Zip file stored successsfuly {activity}");
        }
    }
}
