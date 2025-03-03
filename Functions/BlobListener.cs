using System.Text.RegularExpressions;

namespace ImageIngest.Functions;

public class BlobListener
{
    public static string EventGridSubjectPrefix { get; set; } = Environment.GetEnvironmentVariable("FTPEventGridSubjectPrefix");
    public static string DuplicateBlobsEntityName = Environment.GetEnvironmentVariable("DuplicateBlobsEntityName");

    [FunctionName(nameof(BlobListener))]
    public async Task Run(
        [ServiceBusTrigger("camsftpfr", Connection = "ServiceBusConnection", AutoCompleteMessages=true)]
            EventGridItem myQueueItem,
            Int32 deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        [DurableClient] IDurableClient durableClient,
        [CosmosDB(
            databaseName: "FilesLog",
            containerName: "files",
            Connection = "CosmosDBConnection")]IAsyncCollector<FileLog> fileLogOut,
        ILogger logger)
    {
        //log.LogInformation($"[BlobListener] Function triggered on EventGrid topic subscription. Subject: {blobEvent.Subject}, Prefix: {EventGridSubjectPrefix} Details: {blobEvent}");
        logger.LogInformation($"[BlobListener] Function triggered on Service Bus Queue. myQueueItem.Subject: {myQueueItem.Subject}, myQueueItem: {myQueueItem}, deliveryCount: {deliveryCount} enqueuedTimeUtc: {enqueuedTimeUtc}, messageId: {messageId}");
        string blobName = myQueueItem.Subject.Replace(EventGridSubjectPrefix, string.Empty, StringComparison.InvariantCultureIgnoreCase);

        FileLog log = new FileLog(blobName, deliveryCount)
        {
            container = Consts.FTPContainerName,
            eventGrid = myQueueItem,
            queueItem = new QueueItem() { deliveryCount = deliveryCount, enqueuedTimeUtc = enqueuedTimeUtc, messageId = messageId }
        };
        await fileLogOut.AddAsync(log);
        logger.LogInformation($"[BlobListener] first record was registered: {blobName}, deliveryCount: {deliveryCount}");

        BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

        try
        {
            var isBlobExist = await blobClient.ExistsAsync();
            if (!isBlobExist.Value)
            {
                string msg = $"[BlobListener] blob: {blobName} is not exist so ignore the trigger";
                logger.LogWarning(msg);
                log.message = msg;
                await fileLogOut.AddAsync(log);
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[BlobListener] Error check blob: {blobClient.Name} ExistsAsync");
        }

        //var entityId = new EntityId(nameof(DuplicateBlobs), DuplicateBlobsEntityName);
        //var duplicatesBlobsState = await durableClient.ReadEntityStateAsync<DuplicateBlobs>(entityId);

        //if (duplicatesBlobsState.EntityExists &&
        //    duplicatesBlobsState.EntityState.DuplicateBlobsDic != null &&
        //    duplicatesBlobsState.EntityState.DuplicateBlobsDic.ContainsKey(blobName))
        //{
        //    logger.LogWarning($"[BlobListener] blob: {blobClient.Name} is already handled so ignoring");
        //    Response<GetBlobTagResult> res = await blobClient.GetTagsAsync();
        //    var blobTags = new BlobTags(res.Value.Tags);
        //    blobTags.IsDuplicate = true;
        //    await blobClient.WriteTagsAsync(blobTags);
        //    return;
        //}
        
        //await durableClient.SignalEntityAsync<IDuplicateBlobs>(entityId, x => x.Add(new DuplicateBlob() { BlobName = blobName, Timestamp = DateTime.UtcNow }));

        BlobProperties props = await blobClient.GetPropertiesAsync();
        BlobTags tags = new BlobTags(props, blobClient);

        string blobNameWithoutExt = Path.GetFileNameWithoutExtension(blobName);
        tags.Namespace = GetBlobNamespace(blobNameWithoutExt);

        Response response = await blobClient.WriteTagsAsync(tags);
        if (response.IsError)
        {
            logger.LogError(new EventId(1001), response.ToString());
            log.message = response.ToString();
            await fileLogOut.AddAsync(log);
        }


        logger.LogInformation($"[BlobListener] seconed record is about to be registered{blobName}");
        await fileLogOut.AddAsync(new FileLog(blobName, deliveryCount)
        {
            container = Consts.FTPContainerName,
            eventGrid = myQueueItem,
            queueItem = new QueueItem() { deliveryCount = deliveryCount, enqueuedTimeUtc = enqueuedTimeUtc, messageId = messageId },
            tags = tags,
            properties = props

        });

        logger.LogInformation($"[BlobListener] BlobTags saved for blob {blobName}, Tags: {tags}");
    }

    private static string GetBlobNamespace(string blobName)
    {
        string @namespace = blobName.Split("_").LastOrDefault();

        if (Regex.IsMatch(@namespace, "^(P|p)[1-4]$"))
            return EnvironmentVariablesExtensions.GetNamespaceVariable(@namespace.ToUpper());
        else
            return EnvironmentVariablesExtensions.GetNamespaceVariable(Environment.GetEnvironmentVariable("DefaultBlobType"));
    }
}
