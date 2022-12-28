namespace ImageIngest.Functions;

public class BlobTrigger
{
    [FunctionName(nameof(BlobTrigger))]
    public async Task Run(
        [BlobTrigger(ActivityAction.ContainerName + "/{name}", Connection = "AzureWebJobsFTPStorage")] BlobClient blobClient,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        log.LogInformation($"[BlobTrigger] Function triggered blob name: {blobClient.Name}");
        log.LogInformation($"[BlobTrigger] BlobClient Blob: {blobClient.Name}, Container: {blobClient.BlobContainerName}, AccountName: {blobClient.AccountName}");
        //BlobProperties props = await blobClient.GetPropertiesAsync();
        //log.LogInformation($"[BlobTrigger] BlobProperties: {props}");
        //BlobTags tags = new BlobTags(props, blobClient);
        //ActivityAction activity = new ActivityAction(tags);
        //Response response = await blobClient.WriteTagsAsync(tags);
        //if (response.IsError)
        //    log.LogError(new EventId(1001), response.ToString());
        //log.LogInformation($"[BlobTrigger] BlobTags saved for blob {blobClient.Name}, Tags: {tags}");
        //await starter.StartNewAsync<ActivityAction>(nameof(Orchestrator), activity);
    }
}
