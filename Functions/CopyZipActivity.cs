using Azure.Storage.Blobs;
using ImageIngest.Functions.Model;

namespace ImageIngest.Functions;

public class CopyZipActivity
{
    [FunctionName(nameof(CopyZipActivity))]
    public static async Task<CopyZipResponse> Run(
        [ActivityTrigger] CopyZipRequest request,
        ILogger log)
    {
        log.LogInformation($"[CopyZipActivity] Start copy {request.BlobName} to destination - {request.DistributionTarget.TargetName}, containerName - {request.ContainerName}");
        var destClient = new BlobClient(request.DistributionTarget.ConnectionString, request.ContainerName, request.BlobName);
        var response = new CopyZipResponse() { TargetName = request.DistributionTarget.TargetName };

        try
        {
            var copyProcess = await destClient.StartCopyFromUriAsync(request.SourceBlobSasToken);
            await copyProcess.WaitForCompletionAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"[CopyZipActivity] Error in copy zip {request.BlobName} to destination {request.DistributionTarget.TargetName}, containerName - {request.ContainerName}. exMessage: {ex.Message}");
            response.IsSuccessfull = false;
            return response;
        }

        log.LogInformation($"[CopyZipActivity] Finish copy {request.BlobName} to destination - {request.DistributionTarget.TargetName}");
        var destProps = destClient.GetProperties().Value;
        if (destProps.BlobCopyStatus != CopyStatus.Success)
        {
            log.LogError($"[CopyZipActivity] Unsuccessfull copy {request.BlobName} to destination - {request.DistributionTarget.TargetName}, description: {destProps.CopyStatusDescription}");
            response.IsSuccessfull = false;
            return response;
        }

        response.IsSuccessfull = true;
        return response;
    }
}