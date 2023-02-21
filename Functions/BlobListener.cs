using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using System.Text.RegularExpressions;

namespace ImageIngest.Functions;

public class BlobListener
{
    public static string EventGridSubjectPrefix { get; set; } = Environment.GetEnvironmentVariable("FTPEventGridSubjectPrefix");
    public static string DuplicateBlobsEntityName = Environment.GetEnvironmentVariable("DuplicateBlobsEntityName");

    [FunctionName(nameof(BlobListener))]
    public async Task Run(
        [EventGridTrigger] EventGridEvent blobEvent, 
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        [DurableClient] IDurableClient durableClient,
        ILogger log)
    {
        log.LogInformation($"[BlobListener] Function triggered on EventGrid topic subscription. Subject: {blobEvent.Subject}, Prefix: {EventGridSubjectPrefix} Details: {blobEvent}");
        string blobName = blobEvent.Subject.Replace(EventGridSubjectPrefix, string.Empty, StringComparison.InvariantCultureIgnoreCase);
        BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

        try
        {
            var isBlobExist = await blobClient.ExistsAsync();
            if (!isBlobExist.Value)
            {
                log.LogWarning($"[BlobListener] blob: {blobClient.Name} is not exist so ignore the trigger");
                return;
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"[BlobListener] Error check blob: {blobClient.Name} ExistsAsync");
        }

        var entityId = new EntityId(nameof(DuplicateBlobs), DuplicateBlobsEntityName);
        var duplicatesBlobsState = await durableClient.ReadEntityStateAsync<DuplicateBlobs>(entityId);

        if (duplicatesBlobsState.EntityExists &&
            duplicatesBlobsState.EntityState.DuplicateBlobsDic != null &&
            duplicatesBlobsState.EntityState.DuplicateBlobsDic.ContainsKey(blobName))
        {
            log.LogWarning($"[BlobListener] blob: {blobClient.Name} is already handled so ignoring");
            Response<GetBlobTagResult> res = await blobClient.GetTagsAsync();
            var blobTags = new BlobTags(res.Value.Tags);
            blobTags.IsDuplicate = true;
            await blobClient.WriteTagsAsync(blobTags);
            return;
        }

        await durableClient.SignalEntityAsync<IDuplicateBlobs>(entityId, x => x.Add(new DuplicateBlob() { BlobName = blobName, Timestamp = DateTime.UtcNow }));
        BlobProperties props = await blobClient.GetPropertiesAsync();

        BlobTags tags = new BlobTags(props, blobClient);

        string blobNameWithoutExt = Path.GetFileNameWithoutExtension(blobName);
        tags.Namespace = GetBlobNamespace(blobNameWithoutExt);

        Response response = await blobClient.WriteTagsAsync(tags);
        if (response.IsError)
        {
            log.LogError(new EventId(1001), response.ToString());
        }

        log.LogInformation($"[BlobListener] BlobTags saved for blob {blobName}, Tags: {tags}");
    }

    private static string GetBlobNamespace(string blobName)
    {
        string @namespace = blobName.Split("_").LastOrDefault();

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
