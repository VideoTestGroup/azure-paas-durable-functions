using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace ImageIngest.Functions;
public class ZipListener
{
    public static string EventGridSubjectPrefix { get; set; } = Environment.GetEnvironmentVariable("ZipEventGridSubjectPrefix");

    [FunctionName(nameof(ZipListener))]
    public async Task Run(
        [EventGridTrigger] EventGridEvent blobEvent,
        [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        string blobName = blobEvent.Subject.Replace(EventGridSubjectPrefix, string.Empty, StringComparison.InvariantCultureIgnoreCase);
        log.LogInformation($"[ZipListener] Function triggered on blob {blobName}");
        string batchId = Path.GetFileNameWithoutExtension(blobName);
        string deleteQuery = $@"""Status""='{BlobStatus.Zipped}' AND ""BatchId""= '{batchId}'";
        log.LogInformation($"[ZipListener] Delete by query '{deleteQuery}', BatchId = {batchId}");
        await blobContainerClient.DeleteByQueryAsync(deleteQuery);
    } 
}
