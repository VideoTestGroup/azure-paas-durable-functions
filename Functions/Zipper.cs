using System.IO.Packaging;

namespace ImageIngest.Functions;
public static class Zipper
{
    private static string AzureWebJobsFTPStorage => Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
    private static TimeSpan LeaseDuration => TimeSpan.Parse(Environment.GetEnvironmentVariable("LeaseDuration"));

    [FunctionName(nameof(Zipper))]
    public static async Task<bool?> Run(
        [ActivityTrigger] ActivityAction activity,
        [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient client,
        [Blob("zip", Connection = "AzureWebJobsZipStorage")] BlobContainerClient zipClient,
        ILogger log)
    {
        log.LogInformation($"[Zipper] ActivityTrigger trigger function Processed blob\n activity: {activity}");
        log.LogInformation($"[Zipper] QueryAsync activity: {activity}");
        List<BatchJob> jobs = new List<BatchJob>();
        await foreach (BlobTags tags in client.QueryAsync(t =>
            t.Status == BlobStatus.Batched &&
            t.BatchId == activity.BatchId &&
            t.Namespace == activity.Namespace))
        {
            BatchJob job = new BatchJob(tags);
            jobs.Add(job);
        }

        if (jobs.Count < 1)
        {
            log.LogWarning($"[Zipper] No blobs found for activity: {activity}");
            return null;
        }

        // TODO - Fix bug in lease
        // download file streams
        await Task.WhenAll(jobs.Select(job => job.LeaseClient.AcquireAsync(LeaseDuration).ContinueWith(j => job.Lease = j.Result, TaskContinuationOptions.ExecuteSynchronously)));
        await Task.WhenAll(jobs.Select(job => job.BlobClient.DownloadToAsync(job.Stream, new BlobDownloadToOptions { Conditions = new BlobRequestConditions { LeaseId = job.Lease.LeaseId } })
            .ContinueWith(r => log.LogInformation($"[Zipper] DownloadToAsync {job.BlobClient.Name}, length: {job.Stream.Length}, Success: {r.IsCompletedSuccessfully}, Exception: {r.Exception?.Message}"))
        ));

        bool isZippedSuccessfull = true;
        log.LogInformation($"[Zipper] Downloaded {jobs.Count} blobs. Files: {string.Join(",", jobs.Select(j => $"{j.Name} ({j.Stream.Length})"))}");
        string currentJobName = string.Empty;
        try
        {
            using (MemoryStream zipStream = new MemoryStream())
            {
                using (Package zip = Package.Open(zipStream, FileMode.CreateNew))
                {
                    foreach (var job in jobs)
                    {
                        currentJobName = job.Name;
                        if (null == job.Stream)
                        {
                            log.LogError($"[Zipper] Package zip Cannot compress part, no stream created: {job}");
                            job.Tags.Status = BlobStatus.Error;
                            job.Tags.Text = "Zip failed, No stream downloaded";
                            continue;
                        }

                        string destFilename = "/" + Path.GetFileName(job.Name);
                        Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                        PackagePart part = zip.CreatePart(uri, "", CompressionOption.NotCompressed);
                        using (Stream dest = part.GetStream())
                            {
                                job.Stream.Position = 0;
                                job.Stream.CopyTo(dest);
                            }
                    }
                }

                log.LogInformation($"[Zipper] Creating zip stream: {activity.BatchId}.zip");
                zipStream.Position = 0;
                await zipClient.GetBlobClient($"{activity.BatchId}.zip").UploadAsync(zipStream);
                log.LogInformation($"[Zipper] CopyToAsync zip file zipStream: {zipStream.Length}, activity: {activity}");
            }
        }
        catch (Exception ex)
        {
            isZippedSuccessfull = false;
            log.LogError(ex, $"{ex.Message} ActivityDetails: {activity}");
            var job = jobs.FirstOrDefault(j => j.Name == currentJobName);
            if (null != job)
            {
                job.Tags.Status = BlobStatus.Error;
                job.Tags.Text = ex.Message;
            }
        }

        log.LogInformation($"[Zipper] Zip file completed, post creation marking blobs for deletion. Activity: {activity}");
        await Task.WhenAll(jobs.Select(job => job.BlobClient
            .WriteTagsAsync(job.Tags, job.Lease.LeaseId, t => t.Status = isZippedSuccessfull ? BlobStatus.Zipped : BlobStatus.Error)
            .ContinueWith(t => job.LeaseClient.ReleaseAsync())));

        log.LogInformation($"[Zipper] Tags marked {jobs.Count} blobs. Status: {(isZippedSuccessfull ? BlobStatus.Zipped : BlobStatus.Error)}, Activity: {activity}. Files: {string.Join(",", jobs.Select(t => $"{t.Name} ({t.Tags.Length.Bytes2Megabytes()}MB)"))}");
        return isZippedSuccessfull;
    }
}
