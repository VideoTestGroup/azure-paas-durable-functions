namespace ImageIngest.Functions;
public class ZipListener
{
    [FunctionName(nameof(ZipListener))]
    public async Task Run(
        [BlobTrigger("zip/{name}.zip", Connection = "AzureWebJobsZipStorage")] BlobClient blobClient,
        [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation($"[ZipListener] Function triggered on blob {blobClient.Name}");
        ActivityAction activity = ActivityAction.ExtractBatchIdAndNamespace(blobClient.Name);
        string deleteQuery = $@"""Status""='{BlobStatus.Zipped}' AND ""Namespace""= '{activity.Namespace}' AND ""BatchId""= '{activity.BatchId}'";
        log.LogInformation($"[ZipListener] Delete by query '{deleteQuery}'. Details {activity}");
        await blobContainerClient.DeleteByQueryAsync(deleteQuery);
    } 
}
