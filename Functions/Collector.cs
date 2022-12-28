namespace ImageIngest.Functions;

public class Collector
{
    public static string AzureWebJobsFTPStorage { get; set; } = Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
    public static long ZipBatchMinSizeMB { get; set; } = long.TryParse(Environment.GetEnvironmentVariable("ZipBatchMinSizeMB"), out long size) ? size : 10;
    public static long ZipBatchMaxSizeMB { get; set; } = long.TryParse(Environment.GetEnvironmentVariable("ZipBatchMaxSizeMB"), out long size) ? size : 20;

    public static TimeSpan BlobOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(Environment.GetEnvironmentVariable("BlobOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

    [FunctionName(nameof(Collector))]
    public static async Task<string> Run(
        [ActivityTrigger] string @namespace,
        [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient containerClient,
        ILogger log)
    {

        List<BlobTags> tags = new List<BlobTags>();
        bool hasOutdateBlobs = false;
        long totalSize = 0;

        log.LogInformation($"[Collector] ActivityTrigger triggered Function namespace: {@namespace}");

        await foreach (BlobTags tag in containerClient.QueryAsync(t => t.Status == BlobStatus.Pending && t.Namespace == @namespace))
        {
            log.LogInformation($"[Collector] found relevant blob {tag.Name}");
            totalSize += tag.Length;
            hasOutdateBlobs |= tag.Modified < DateTime.UtcNow.Subtract(BlobOutdatedThreshold).ToFileTimeUtc();
            tags.Add(tag);

            if (totalSize.Bytes2Megabytes() > ZipBatchMaxSizeMB)
            {
                break;
            }
        }

        log.LogInformation($"[Collector] found {tags.Count} blobs in total size {totalSize.Bytes2Megabytes()}MB(/{ZipBatchMinSizeMB}MB).\n {string.Join(",", tags.Select(t => $"{t.Name} ({t.Length.Bytes2Megabytes()}MB)"))}");
        if (totalSize.Bytes2Megabytes() < ZipBatchMinSizeMB && !hasOutdateBlobs)
        {
            return null;
        }

        // Create batch id
        string batchId = ActivityAction.CreateBatchId(@namespace);
        await Task.WhenAll(tags.Select(tag =>
            new BlobClient(AzureWebJobsFTPStorage, tag.Container, tag.Name).WriteTagsAsync(tag, null, t =>
            {
                t.Status = BlobStatus.Batched;
                t.BatchId = batchId;
            })
        ));

        log.LogInformation($"[Collector] Tags marked {tags.Count} blobs.\n BatchId: {batchId} \n TotalSize: {totalSize}.\nFiles: {string.Join(",", tags.Select(t => $"{t.Name} ({t.Length.Bytes2Megabytes()}MB)"))}");

        return batchId;
    }
}