using Azure.Storage.Sas;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json.Linq;

namespace ImageIngest.Functions;

public class ZipDistributorListener
{
    [FunctionName(nameof(ZipDistributorListener))]
    public async Task Run(
        // TODO - Change to event grid trigger.
        [BlobTrigger("zip/{name}.zip", Connection = "AzureWebJobsZipStorage")] BlobClient blobClient,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        log.LogInformation($"[ZipDistributor] Function triggered on blob {blobClient.Name}");
        await starter.StartNewAsync<string>(nameof(ZipDistributorOrchestrator), blobClient.Name);
    } 
}
