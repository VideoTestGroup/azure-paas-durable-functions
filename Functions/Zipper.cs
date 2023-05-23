using Azure;
using Azure.Storage.Blobs.Models;
using Ionic.Zip;

namespace ImageIngest.Functions;
//public static class Zipper
 public static class Zipper
{
    private static string AzureWebJobsFTPStorage => Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
    private static TimeSpan LeaseDuration => TimeSpan.Parse(Environment.GetEnvironmentVariable("LeaseDuration"));

    [FunctionName(nameof(Zipper))]
   //public async Task Run(
    public static async Task Run(
   //   public async Task<bool?> Run(
     //   [ActivityTrigger] ActivityAction activity,
        [ServiceBusTrigger("batches", Connection = "ServiceBusConnection")]
//               [ServiceBusTrigger("batches", Connection = "ServiceBusConnection", AutoCompleteMessages=true)]
            TagBatchQueueItem myQueueItem,
            Int32 deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient ftpClient,
        [Blob(Consts.ZipContainerName, Connection = "AzureWebJobsZipStorage")] BlobContainerClient zipClient,
        // [CosmosDB(
        //     databaseName: "FilesLog",
        //     containerName: "files",
        //     Connection = "CosmosDBConnection")]IAsyncCollector<FileLog> fileLogOut,
       ILogger logger
        )
    {
        logger.LogInformation($"[Zipper] Service Bus trigger function Processed blobs myQueueItem: {myQueueItem}");
        List<BatchJob> jobs = new List<BatchJob>();
        List<TaggedBlobItem> blobs = new List<TaggedBlobItem>();

        foreach (BlobTags tags in myQueueItem.Tags)
        {
            tags.Container = myQueueItem.Container;
            BatchJob job = new BatchJob(tags);
            jobs.Add(job);
        }

        if (jobs.Count < 1) //Must throw for the Service Bus retry mechanism
            throw new ArgumentOutOfRangeException("myQueueItem", $"[Zipper] No blobs found for activity: ");

        // download file streams
        await Task.WhenAll(jobs.Select(job => job.BlobClient.DownloadToAsync(job.Stream)
            .ContinueWith(r => logger.LogInformation($"[Zipper] DownloadToAsync {job.BlobClient.Name}, length: {job.Stream.Length}, Success: {r.IsCompletedSuccessfully}, Exception: {r.Exception?.Message}"))
        ));

        bool isZippedSuccessfull = true;
        logger.LogInformation($"[Zipper] Downloaded {jobs.Count} blobs. Files: {string.Join(",", jobs.Select(j => $"{j.Name} ({j.Stream.Length})"))}");
        try
        {
            using (MemoryStream zipStream = new MemoryStream())
            {
                using (ZipFile zip = new ZipFile())
                {
                    foreach (BatchJob job in jobs)
                    {
                        try
                        {
                            if (job.Stream == null)
                            {
                                logger.LogError($"[Zipper] Package zip Cannot compress part, no stream created: {job}");
                                job.Tags.Status = BlobStatus.Error;
                                job.Tags.Text = "Zip failed, No stream downloaded";
                                // FileLog log = new FileLog(job.Name, 99)
                                // {
                                //     tags = job.Tags,
                                //     container = job.Tags.Container,
                                //     message = job.Tags.Text
                                // };
                                // await fileLogOut.AddAsync(log);
                                continue;
                            }

                            job.Stream.Position = 0;
                            zip.AddEntry(job.Name, job.Stream);
                            // FileLog log1 = new FileLog(job.Name, 99)
                            // {
                            //     tags = job.Tags,
                            //     container = job.Tags.Container,
                            // };
                            // await fileLogOut.AddAsync(log1);

                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"[Zipper] Error in job {job.Name}, ActivityDetails: , exception: {ex.Message}");
                            job.Tags.Status = BlobStatus.Error;
                            job.Tags.Text = "Exception in create blob part in zip";
                        }
                    }

                    zip.Save(zipStream);
                }

                logger.LogInformation($"[Zipper] Creating zip stream: {myQueueItem.BatchId}.zip");
                zipStream.Position = 0;
                var zipBlobClient = zipClient.GetBlobClient($"{myQueueItem.BatchId}.zip");

                var isExist = await zipBlobClient.ExistsAsync();

                // Sometimes azure trigger the same batchId (right now dont know why).
                // So we check if the blob is already exists, if true we ignore this execution with the batchId.
                if (isExist.Value)
                {
                    logger.LogWarning($"[Zipper] Zip with batchId {myQueueItem.BatchId} already exists. ignoring this execution, ActivityDetails: ");
                    return;// null;
                }

                await zipClient.GetBlobClient($"{myQueueItem.BatchId}.zip").UploadAsync(zipStream);
                logger.LogInformation($"[Zipper] CopyToAsync zip file zipStream: {zipStream.Length}, activity: ");
            }
        }
        catch (Exception ex)
        {
            isZippedSuccessfull = false;
            logger.LogError(ex, $"{ex.Message} ActivityDetails: ");
        }

        try
        {
            await Task.WhenAll(jobs.Select(job =>
           job.BlobClient.WriteTagsAsync(job.Tags, t => t.Status = isZippedSuccessfull && t.Status != BlobStatus.Error ? BlobStatus.Zipped : BlobStatus.Error)));

            //Achi delete the files
            await Task.WhenAll(jobs.Select(job =>
                job.BlobClient.DeleteAsync()));


            logger.LogInformation($"[Zipper] files DELETED {jobs.Count} blobs. Files: {string.Join(",", jobs.Select(t => $"{t.Name} ({t.Tags.Length.Bytes2Megabytes()}MB)"))}");

            if(!isZippedSuccessfull)
                throw new Exception($"[Zipper] Unsuccessfull zipping process, the files will be marked in Status=Error, Details: {myQueueItem}");

            logger.LogInformation($"[Zipper] Zip file completed, post creation marking blobs. Details: {myQueueItem}");
        }
        catch (Exception ex) 
        { // Must throw exception for the Service Bus
            throw ex;
        }
    }
}
