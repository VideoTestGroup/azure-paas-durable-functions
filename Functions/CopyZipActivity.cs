using Azure.Storage.Blobs;
using ImageIngest.Functions.Model;

namespace ImageIngest.Functions;

public class CopyZipActivity
{
    [FunctionName(nameof(CopyZipActivity))]
    public static async Task<bool> Run(
        [ActivityTrigger] CopyZipRequest request,
        ILogger log)
    {
        log.LogInformation($"[CopyZipActivity] Start copy {request.BlobName} to destination - {request.DistributionTarget.TargetName}, containerName - {request.ContainerName}");
        var destClient = new BlobClient(request.DistributionTarget.ConnectionString, request.ContainerName, request.BlobName);
        try
        {
            var copyProcess = await destClient.StartCopyFromUriAsync(request.SourceBlobSasToken);
            await copyProcess.WaitForCompletionAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"[CopyZipActivity] Error in copy zip {request.BlobName} to destination {request.DistributionTarget.TargetName}, containerName - {request.ContainerName}. exMessage: {ex.Message}");
            return false;
        }

        log.LogInformation($"[CopyZipActivity] Finish copy {request.BlobName} to destination - {request.DistributionTarget.TargetName}");
        var destProps = destClient.GetProperties().Value;
        if (destProps.BlobCopyStatus != CopyStatus.Success)
        {
            log.LogError($"[CopyZipActivity] Unsuccessfull copy {request.BlobName} to destination - {request.DistributionTarget.TargetName}, description: {destProps.CopyStatusDescription}");
            return false;
        }

        return true;
    }
}