using System;

namespace ImageIngest.Functions;

public class Collector
{
    public List<string> Namespaces { get; set; } = Environment.GetEnvironmentVariable("Namespaces").Split(",").ToList();
    public static string AzureWebJobsFTPStorage { get; set; } = Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
    public static long ZipBatchMinSizeMB { get; set; } = long.TryParse(Environment.GetEnvironmentVariable("ZipBatchMinSizeMB"), out long size) ? size : 10;
    public static long ZipBatchMaxSizeMB { get; set; } = long.TryParse(Environment.GetEnvironmentVariable("ZipBatchMaxSizeMB"), out long size) ? size : 20;
    public static int MaxZipsPerExecution { get; set; } = int.TryParse(Environment.GetEnvironmentVariable("MaxZipsPerExecution"), out int size) ? size : 100;

    public static TimeSpan BlobOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(Environment.GetEnvironmentVariable("BlobOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

    [FunctionName(nameof(Collector))]
    public async Task Run(
        [TimerTrigger("*/%CollectorTimer% * * * * *")] TimerInfo myTimer,
        [ServiceBus("batches", Connection = "ServiceBusConnection")] IAsyncCollector<TagBatchQueueItem> collector,
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient containerClient,
        ILogger log)
    {
        log.LogInformation($"[Collector] executed at: {DateTime.Now} ");
        // Runs each namespace seperately:
        await Task.WhenAll(Namespaces.Select(@namespace => CollectorRun(@namespace, containerClient, collector, log)));
    }

    public async Task CollectorRun(string @namespace, BlobContainerClient containerClient, IAsyncCollector<TagBatchQueueItem> collector, ILogger log)
    {
        List<BlobTags> tags = new List<BlobTags>();
        bool hasOutdateBlobs = false;
        long totalSize = 0;
        long TotalBatches = 0;

        log.LogInformation($"[Collector {@namespace}] start run for namespace: {@namespace}");

        try
        {
            IAsyncEnumerable<BlobTags> allBlobs = containerClient.QueryByTagsAsync($"Status = 'Pending' AND Namespace = '{@namespace}'");
            await foreach (BlobTags tag in allBlobs)
            {
                if (TotalBatches > MaxZipsPerExecution)
                    break;

                totalSize += tag.Length;
                hasOutdateBlobs |= tag.Modified < DateTime.UtcNow.Subtract(BlobOutdatedThreshold).ToFileTimeUtc();
                tags.Add(tag);


                if (totalSize.Bytes2Megabytes() > ZipBatchMaxSizeMB)
                {

                    // DO

                    log.LogInformation($"[Collector {@namespace}] found {tags.Count} blobs in total size {totalSize.Bytes2Megabytes()}MB(/{ZipBatchMinSizeMB}MB).\n {string.Join(",", tags.Select(t => $"{t.Name} ({t.Length.Bytes2Megabytes()}MB)"))}");
                    
                    // Create batch id
                    string batchId = ActivityAction.CreateBatchId(@namespace, tags.Count);

                    // Mark blobs as batched with batchId
                    await Task.WhenAll(tags.Select(tag =>
                        new BlobClient(AzureWebJobsFTPStorage, tag.Container, tag.Name).WriteTagsAsync(tag, t =>
                        {
                            t.Status = BlobStatus.Batched;
                            t.BatchId = batchId;
                        })
                    ));

                    log.LogInformation($"[Collector {@namespace}] Tags marked {tags.Count} blobs.\n BatchId: {batchId} \n TotalSize: {totalSize}.\nFiles: {string.Join(",", tags.Select(t => $"{t.Name} ({t.Length.Bytes2Megabytes()}MB)"))}");

                    var activity = new TagBatchQueueItem() { Namespace = @namespace, BatchId = batchId, Container = tags[0].Container, Tags = tags.ToArray() };

                    await collector.AddAsync(activity);
                    await collector.FlushAsync();

                    tags.Clear();
                    totalSize = 0;
                    TotalBatches++;
                }
            }

            // if we finished the loop,
            // and the last items didnt add up to xMB, it will be handled in next loop 





            //await foreach (BlobTags tag in containerClient.QueryAsync(t => t.Status == BlobStatus.Pending && t.Namespace == @namespace))
            //{
            //    log.LogInformation($"namespace:{@namespace} blob status ");           
            //    totalSize += tag.Length;
            //    hasOutdateBlobs |= tag.Modified < DateTime.UtcNow.Subtract(BlobOutdatedThreshold).ToFileTimeUtc();
            //    tags.Add(tag);

            //    if (totalSize.Bytes2Megabytes() > ZipBatchMaxSizeMB)
            //    {
            //        break;
            //    }
            //}

            //if (totalSize.Bytes2Megabytes() < ZipBatchMinSizeMB && !hasOutdateBlobs)
            //{
            //    return;
            //}

           
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"[Collector {@namespace}] failed, exMessage: {ex.Message}");
        }
    }
}
