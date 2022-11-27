using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace ImageIngest.Functions;

public class BlobListener
{
    public static string EventGridSubjectPrefix { get; set; } = System.Environment.GetEnvironmentVariable("EventGridSubjectPrefix");

    [FunctionName(nameof(BlobListener))]
    public async Task Run(
        // [BlobTrigger(ActivityAction.ContainerName + "/{name}", Source = BlobTriggerSource.EventGrid, Connection = "AzureWebJobsFTPStorage")] BlobClient blobClient,
        [EventGridTrigger] EventGridEvent blobEvent, 
        [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        log.LogInformation($"[BlobListener] Function triggered on EventGrid topic subscription. Subject: {blobEvent.Subject}, Prefix: {EventGridSubjectPrefix} Details: {blobEvent}");
        string blobName = blobEvent.Subject.Replace(EventGridSubjectPrefix, string.Empty, StringComparison.InvariantCultureIgnoreCase);
        log.LogInformation($"[BlobListener] extacted blob name: {blobName}");
        BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);
        log.LogInformation($"[BlobListener] BlobClient Blob: {blobClient.Name}, Container: {blobClient.BlobContainerName}, AccountName: {blobClient.AccountName}");
        BlobProperties props = await blobClient.GetPropertiesAsync();
        log.LogInformation($"[BlobListener] BlobProperties: {props}");
        BlobTags tags = new BlobTags(props, blobClient);
        ActivityAction activity = new ActivityAction(tags);

        string blobNameWithoutExt = Path.GetFileNameWithoutExtension(blobName);
        log.LogInformation($"blobNameWithoutExt: {blobNameWithoutExt}");
        activity.Namespace = blobNameWithoutExt.Split("_").LastOrDefault() ?? "P3";
        log.LogInformation($"activity: {activity}");

        Response response = await blobClient.WriteTagsAsync(tags);
        if(response.IsError)
            log.LogError(new EventId(1001), response.ToString());
        log.LogInformation($"[BlobListener] BlobTags saved for blob {blobName}, Tags: {tags}");
        await starter.StartNewAsync<ActivityAction>(nameof(Orchestrator), activity);
    }
}
