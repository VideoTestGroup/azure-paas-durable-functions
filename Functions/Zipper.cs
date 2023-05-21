using Azure;
using Azure.Storage.Blobs.Models;
using Ionic.Zip;

namespace ImageIngest.Functions;
public static class Zipper
// public class Zipper
{
    private static string AzureWebJobsFTPStorage => Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
    private static TimeSpan LeaseDuration => TimeSpan.Parse(Environment.GetEnvironmentVariable("LeaseDuration"));

    [FunctionName(nameof(Zipper))]
    public static async Task<bool?> Run(
   // public static async Task Run(
   //   public async Task Run(
     //   [ActivityTrigger] ActivityAction activity,
        [ServiceBusTrigger("batches", Connection = "ServiceBusConnection", AutoCompleteMessages=true)]
            TagBatchQueueItem myQueueItem,
            Int32 deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
        [Blob(Consts.FTPContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient ftpClient,
        [Blob(Consts.ZipContainerName, Connection = "AzureWebJobsZipStorage")] BlobContainerClient zipClient,
        [CosmosDB(
            databaseName: "FilesLog",
            containerName: "files",
            Connection = "CosmosDBConnection")]IAsyncCollector<FileLog> fileLogOut,
        ILogger logger)
    {
        logger.LogInformation($"[Zipper] ActivityTrigger trigger function Processed blob\n activity:  ");
        List<BatchJob> jobs = new List<BatchJob>();
        List<TaggedBlobItem> blobs = new List<TaggedBlobItem>();



        foreach (var FileName in myQueueItem.FileNames)
        {

            // Get a reference to the blob by its name
            BlobClient blobClient = ftpClient.GetBlobClient(FileName);

            // Fetch the blob properties
            BlobProperties blobProperties = await blobClient.GetPropertiesAsync();

            // Access the tags
            // Access the tags
            IDictionary<string, string> metadata = blobProperties.Metadata;

            // Convert the metadata to Dictionary<string, string>
            Dictionary<string, string> Blobtags = new Dictionary<string, string>(metadata);



            BlobTags tags = new BlobTags(metadata, FileName);
            BatchJob job = new BatchJob(tags);
            jobs.Add(job);
        }


        //string query = $"Status = 'Batched' AND BatchId = '{myQueueItem}'";

        //await foreach (TaggedBlobItem taggedBlobItem in ftpClient.FindBlobsByTagsAsync(query))
        //{
        //    BlobTags tags = new BlobTags(taggedBlobItem);
        //    BatchJob job = new BatchJob(tags);
        //    jobs.Add(job);
        //}





        if (jobs.Count < 1)
        {
            logger.LogWarning($"[Zipper] No blobs found for activity: ");
            return null;
        }

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
                                FileLog log = new FileLog(job.Name, 99)
                                {
                                    tags = job.Tags,
                                    container = job.Tags.Container,
                                    message = job.Tags.Text
                                };
                                await fileLogOut.AddAsync(log);
                                continue;
                            }

                            job.Stream.Position = 0;
                            zip.AddEntry(job.Name, job.Stream);
                            FileLog log1 = new FileLog(job.Name, 99)
                            {
                                tags = job.Tags,
                                container = job.Tags.Container,
                            };
                            await fileLogOut.AddAsync(log1);

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

                logger.LogInformation($"[Zipper] Creating zip stream: {myQueueItem}.zip");
                zipStream.Position = 0;
                var zipBlobClient = zipClient.GetBlobClient($"{myQueueItem}.zip");

                try
                {
                    var isExist = await zipBlobClient.ExistsAsync();

                    // Sometimes azure trigger the same batchId (right now dont know why).
                    // So we check if the blob is already exists, if true we ignore this execution with the batchId.
                    if (isExist.Value)
                    {
                        logger.LogWarning($"[Zipper] Zip with batchId {myQueueItem} already exists. ignoring this execution, ActivityDetails: ");
                        return null;
                    }
                }
                catch (Exception ex )
                {
                    logger.LogError(ex, $"[Zipper] Error check zip: {myQueueItem} ExistsAsync");
                }

                await zipClient.GetBlobClient($"{myQueueItem}.zip").UploadAsync(zipStream);
                logger.LogInformation($"[Zipper] CopyToAsync zip file zipStream: {zipStream.Length}, activity: ");
            }
        }
        catch (Exception ex)
        {
            isZippedSuccessfull = false;
            logger.LogError(ex, $"{ex.Message} ActivityDetails: ");
        }

        logger.LogInformation($"[Zipper] Zip file completed, post creation marking blobs. Activity: ");

        try
        {
            await Task.WhenAll(jobs.Select(job =>
           job.BlobClient.WriteTagsAsync(job.Tags, t => t.Status = isZippedSuccessfull && t.Status != BlobStatus.Error ? BlobStatus.Zipped : BlobStatus.Error)));

            //Achi delete the files
            await Task.WhenAll(jobs.Select(job =>
                job.BlobClient.DeleteAsync()));


            logger.LogInformation($"[Zipper] files DELETED {jobs.Count} blobs. Files: {string.Join(",", jobs.Select(t => $"{t.Name} ({t.Tags.Length.Bytes2Megabytes()}MB)"))}");

            return isZippedSuccessfull;

        }
        catch (Exception ex)
        {

            isZippedSuccessfull = false;
            logger.LogError(ex, $"[Zipper] failed updating ZIPPED tag and deleting the files {ex.Message} ActivityDetails: ");
        }
        return isZippedSuccessfull;

    }
}
