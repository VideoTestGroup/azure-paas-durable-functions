using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace ImageIngest.Functions
{
    public class Scavenger
    {
        public static string AzureWebJobsFTPStorage { get; set; } = System.Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");

        public int ScavengerPageSize { get; set; } =
            int.TryParse(System.Environment.GetEnvironmentVariable("ScavengerPageSize"), out int size) ? size : 10485760;

        public TimeSpan ScavengerOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(System.Environment.GetEnvironmentVariable("ScavengerOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

        [FunctionName(nameof(Scavenger))]
        public async Task Run(
            [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
            [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"[Scavenger] Timer trigger function executed at: {DateTime.Now}");
            List<BlobTags> blobs = new List<BlobTags>();
            await foreach (BlobTags tags in blobContainerClient.QueryAsync(t =>
                t.Status == BlobStatus.Pending &&
                t.Modified < DateTime.UtcNow.Add(ScavengerOutdatedThreshold).ToFileTimeUtc()))
            {
                log.LogInformation($"[Scavenger] Found pending old file {tags.Name}, tags: {tags.Tags}");
                blobs.Add(tags);
            }

            if (blobs.Count < 1)
            {
                log.LogInformation($"[Scavenger] Not Found pending files");
                return;
            }

            log.LogInformation($"[Scavenger] Found {blobs.Count} pending old files");
            var blobsByNamespace = blobs.GroupBy(b => b.Namespace);
            foreach (var blobsNamespace in blobsByNamespace)
            {
                ActivityAction activity = new ActivityAction();
                //Create batch id
                activity.OverrideBatchId = ActivityAction.EnlistBatchId(blobsNamespace.Key);
                activity.OverrideStatus = BlobStatus.Batched;
                await Task.WhenAll(blobsNamespace.Select(tag =>
                    new BlobClient(AzureWebJobsFTPStorage, tag.Container, tag.Name).WriteTagsAsync(tag, null, t =>
                    {
                        t.Status = activity.OverrideStatus;
                        t.BatchId = activity.OverrideBatchId;
                    })
                ));
                log.LogInformation($"[Scavenger] Tags marked {blobsNamespace.Count()} blobs.\nSActivity: {activity}.\nFiles: {string.Join(",", blobsNamespace.Select(t => $"{t.Name} ({t.Length.Bytes2Megabytes()}MB)"))}");

                log.LogInformation($"[Scavenger] Zipping files. ActivityAction {activity}");
                await starter.StartNewAsync<ActivityAction>(nameof(Zipper), activity);
                log.LogInformation($"[Scavenger] Zip file stored successsfuly {activity}");
            }
        }
    }
}

