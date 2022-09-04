
namespace ImageIngest.Functions;
public class Collector
{
    public static string AzureWebJobsFTPStorage { get; set; } = System.Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
    public static long ZipBatchSizeMB { get; set; } = long.TryParse(System.Environment.GetEnvironmentVariable("ZipBatchSizeMB"), out long size) ? size : 10485760;


    [FunctionName(nameof(Collector))]
    public static async Task<ActivityAction> Run(
        [ActivityTrigger] ActivityAction activity,
        [Blob("images", Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation($"[Collector] ActivityTrigger triggered Function activity:{activity}");
        List<BlobTags> tags = new List<BlobTags>();
        await foreach (BlobTags tag in blobContainerClient.QueryAsync(t => t.Status == activity.QueryStatus && t.Namespace == activity.Namespace))
        {
            activity.Total += tag.Length;
            tags.Add(tag);
        }

        log.LogInformation($"[Collector] found {tags.Count} blobs in total size {activity.Total.Bytes2Megabytes()}MB(/{ZipBatchSizeMB}MB).\n {string.Join(",", tags.Select(t => $"{t.Name} ({t.Length.Bytes2Megabytes()}MB)"))}");
        if (activity.Total.Bytes2Megabytes() < ZipBatchSizeMB) return activity;

        //Create batch id
        activity.OverrideBatchId = ActivityAction.EnlistBatchId(activity.Namespace);
        activity.OverrideStatus = BlobStatus.Batched;
        await Task.WhenAll(tags.Select(tag =>
            new BlobClient(AzureWebJobsFTPStorage, tag.Container, tag.Name).WriteTagsAsync(tag, t =>
            {
                t.Status = activity.OverrideStatus;
                t.BatchId = activity.OverrideBatchId;
                return t;
            })
        ));
        log.LogInformation($"[Collector] Tags marked {tags.Count} blobs. Status: {activity.OverrideStatus}, OverrideBatchId: {activity.OverrideBatchId}. Files: {string.Join(",", tags.Select(t => $"{t.Name} ({t.Length.Bytes2Megabytes()}MB)"))}");

        return activity;
    }
}