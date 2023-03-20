using Azure.Storage.Blobs;
using ImageIngest.Functions.Model;

namespace ImageIngest.Functions;

public class CopyZipActivity
{
    [FunctionName(nameof(CopyZipActivity))]
    public static async Task<CopyZipResponse> Run(
        [ActivityTrigger] CopyZipRequest request,
        [Blob(Consts.ZipContainerName + "/{request.BlobName}", FileAccess.Read, Connection = "AzureWebJobsZipStorage")] Stream blobInput,
        ILogger log)
    {
        log.LogInformation($"[CopyZipActivity] Start copy {request.BlobName} to destination - {request.DistributionTarget.TargetName}, containerName - {request.ContainerName}");
        var destClient = new BlobClient(request.DistributionTarget.ConnectionString, request.ContainerName, request.BlobName);
        //var destClient = new BlobClient(new Uri(request.BlobName), new AzureSasCredential(request.DistributionTarget.ConnectionString), new BlobClientOptions());
        var response = new CopyZipResponse() { TargetName = request.DistributionTarget.TargetName };
        
        //BlobContainerClient targetContainerClient = new BlobContainerClient(new Uri(request.DistributionTarget.ConnectionString));
            //BlobClient destClient = targetContainerClient.GetBlobClient(request.BlobName);
            //BlobUploadOptions options = new BlobUploadOptions
            //{
                //AccessTier = AccessTier.Hot,
            //};
            
        
        try
        {
            await destClient.UploadAsync(blobInput, overwrite: true);
            //BlobContentInfo info = await destClient.UploadAsync(blobInput, options);
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"[CopyZipActivity] Error in copy zip {request.BlobName} to destination {request.DistributionTarget.TargetName}, containerName - {request.ContainerName}. exMessage: {ex.Message}");
            response.IsSuccessfull = false;
            return response;
        }

        log.LogInformation($"[CopyZipActivity] Finish copy {request.BlobName} to destination - {request.DistributionTarget.TargetName}");
        response.IsSuccessfull = true;
        return response;
    }
}
