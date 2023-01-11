using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ImageIngest.Functions.Model;
using System.Collections.Generic;

namespace ImageIngest.Functions;

public class ZipFailedActivity
{
    private static string AzureWebJobsZipStorage => Environment.GetEnvironmentVariable("AzureWebJobsZipStorage");

    [FunctionName(nameof(ZipFailedActivity))]
    public static async Task Run(
        [ActivityTrigger] ZipFailedParams failedParams,
        [Blob(Consts.FailedZipsContainerName + "/{failedParams.BlobName}", Connection = "AzureWebJobsZipStorage")] BlobClient zipFailedClient,
        ILogger log)
    {
        log.LogInformation($"[ZipFailedActivity] start handle failed zip {failedParams.BlobName}, failedTargets: {failedParams.FailedTargets}");
        var blobClient = new BlobClient(AzureWebJobsZipStorage, Consts.ZipContainerName, failedParams.BlobName);
        try
        {
            await blobClient.SetTagsAsync(new Dictionary<string, string>() { { "FailedTargets", failedParams.FailedTargets } });
            var sourceBlobSasToken = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddMinutes(10));
            var copyProcess = await zipFailedClient.StartCopyFromUriAsync(sourceBlobSasToken);
            await copyProcess.WaitForCompletionAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"[ZipFailedActivity] Error in handle failed zip {failedParams.BlobName}, failedTargets: {failedParams.FailedTargets}");
        }

        log.LogInformation($"[ZipFailedActivity] finish handle failed zip {failedParams.BlobName}");
    }
}