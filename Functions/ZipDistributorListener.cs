using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace ImageIngest.Functions;

public class ZipDistributorListener
{
    public static string EventGridSubjectPrefix { get; set; } = Environment.GetEnvironmentVariable("ZipEventGridSubjectPrefix");

    [FunctionName(nameof(ZipDistributorListener))]
    public async Task Run(
        [EventGridTrigger] EventGridEvent blobEvent,
        [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsZipStorage")] BlobContainerClient blobContainerClient,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        string blobName = blobEvent.Subject.Replace(EventGridSubjectPrefix, string.Empty, StringComparison.InvariantCultureIgnoreCase);
        log.LogInformation($"[ZipDistributor] Function triggered on blob {blobName}");
        await starter.StartNewAsync<string>(nameof(ZipDistributorOrchestrator), blobName);
    } 
}
