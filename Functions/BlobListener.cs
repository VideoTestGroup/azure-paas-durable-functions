using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using System.Text.RegularExpressions;

namespace ImageIngest.Functions;

public class BlobListener
{
    public static string EventGridSubjectPrefix { get; set; } = Environment.GetEnvironmentVariable("FTPEventGridSubjectPrefix");

    [FunctionName(nameof(BlobListener))]
    public async Task Run(
        [EventGridTrigger] EventGridEvent blobEvent, 
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation($"[BlobListener] Function triggered on EventGrid topic subscription. Subject: {blobEvent.Subject}, Prefix: {EventGridSubjectPrefix} Details: {blobEvent}");
        string blobName = blobEvent.Subject.Replace(EventGridSubjectPrefix, string.Empty, StringComparison.InvariantCultureIgnoreCase);
        log.LogInformation($"[BlobListener] extacted blob name: {blobName}");
        BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);
        log.LogInformation($"[BlobListener] BlobClient Blob: {blobClient.Name}, Container: {blobClient.BlobContainerName}, AccountName: {blobClient.AccountName}");
        BlobProperties props = await blobClient.GetPropertiesAsync();
        BlobTags tags = new BlobTags(props, blobClient);

        string blobNameWithoutExt = Path.GetFileNameWithoutExtension(blobName);
        log.LogInformation($"[BlobListener]: BlobNameWithoutExt: {blobNameWithoutExt}");
        tags.Namespace = GetBlobNamespace(blobNameWithoutExt, log);

        Response response = await blobClient.WriteTagsAsync(tags);
        if (response.IsError)
        {
            log.LogError(new EventId(1001), response.ToString());
        }

        log.LogInformation($"[BlobListener] BlobTags saved for blob {blobName}, Tags: {tags}");
    }

    private static string GetBlobNamespace(string blobName, ILogger log)
    {
        string @namespace = blobName.Split("_").LastOrDefault();
        log.LogInformation($"[BlobListener] got namespace {@namespace} for blob {blobName}");

        if (Regex.IsMatch(@namespace, "^(P|p)[1-4]$"))
        {
            return EnvironmentVariablesExtensions.GetNamespaceVariable(@namespace.ToUpper());
        }
        else
        {
            return EnvironmentVariablesExtensions.GetNamespaceVariable(Environment.GetEnvironmentVariable("DefaultBlobType"));
        }
    }
}
