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
        // [CosmosDB(
        //     databaseName: "FilesLog",
        //     containerName: "files",
        //     Connection = "CosmosDBConnection")]IAsyncCollector<FileLog> fileLogOut,
        ILogger logger)
    {
        logger.LogInformation($"[BlobListener] Function triggered on Service Bus Queue. myQueueItem.Subject: {myQueueItem.Subject}, myQueueItem: {myQueueItem}, deliveryCount: {deliveryCount} enqueuedTimeUtc: {enqueuedTimeUtc}, messageId: {messageId}");
        string blobName = myQueueItem.Subject.Replace(EventGridSubjectPrefix, string.Empty, StringComparison.InvariantCultureIgnoreCase);

        // FileLog log = new FileLog(blobName, deliveryCount)
        // {
        //     container = Consts.FTPContainerName,
        //     eventGrid = myQueueItem,
        //     queueItem = new QueueItem() { deliveryCount = deliveryCount, enqueuedTimeUtc = enqueuedTimeUtc, messageId = messageId }
        // };
        // await fileLogOut.AddAsync(log);
        logger.LogInformation($"[BlobListener] first record was registered: {blobName}, deliveryCount: {deliveryCount}");

        //TODO: replace binding to use BlobClient instead of BlobContainerClient and then BlobClient
        BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

        try
        {
            var isBlobExist = await blobClient.ExistsAsync();
            if (!isBlobExist.Value)
            {
                string msg = $"[BlobListener] blob: {blobName} is not exist so ignore the trigger";
                logger.LogWarning(msg);
                // log.message = msg;
                // await fileLogOut.AddAsync(log);
                return;
            }
        }
        catch (Exception ex)
        {
            // Must throw error, otherwise the Service Bus will treat the queue item as completed successfully
            string error = $"[BlobListener] Error check blob: {blobClient.Name} ExistsAsync";
            throw new Exception(error, ex);
        }

        BlobProperties props = await blobClient.GetPropertiesAsync();
        BlobTags tags = new BlobTags(props, blobClient);

        string blobNameWithoutExt = Path.GetFileNameWithoutExtension(blobName);
        tags.Namespace = GetBlobNamespace(blobNameWithoutExt);

        Response response = await blobClient.WriteTagsAsync(tags);
        if (response.IsError)
        {
            logger.LogError(new EventId(1001), response.ToString());
            // log.message = response.ToString();
            // await fileLogOut.AddAsync(log);
            throw new RequestFailedException($"[BlobListener] Failed writing tags. Details: {response.ToString()}");
        }


        logger.LogInformation($"[BlobListener] seconed record is about to be registered: {blobName}");
        // await fileLogOut.AddAsync(new FileLog(blobName, deliveryCount)
        // {
        //     container = Consts.FTPContainerName,
        //     eventGrid = myQueueItem,
        //     queueItem = new QueueItem() { deliveryCount = deliveryCount, enqueuedTimeUtc = enqueuedTimeUtc, messageId = messageId },
        //     tags = tags,
        //     properties = props
        // });

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
