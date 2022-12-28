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
        string batchId = Path.GetFileNameWithoutExtension(blobClient.Name);
        string deleteQuery = $@"""Status""='{BlobStatus.Zipped}' AND ""BatchId""= '{batchId}'";
        log.LogInformation($"[ZipListener] Delete by query '{deleteQuery}', BatchId = {batchId}");
        await blobContainerClient.DeleteByQueryAsync(deleteQuery);
    } 
}
