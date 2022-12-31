using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace ImageIngest.Functions;
public class ZipDistributorOrchestrator
{
    private static List<DistributionTarget> DistributionTargets { get; } =
        JsonConvert.DeserializeObject<List<DistributionTarget>>(Environment.GetEnvironmentVariable("DistributionTargets"));

    private static string AzureWebJobsZipStorage => Environment.GetEnvironmentVariable("AzureWebJobsZipStorage");

    [FunctionName(nameof(ZipDistributorOrchestrator))]
    public static async Task Run(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        string blobName = context.GetInput<string>();
        log.LogInformation($"[ZipDistributorOrchestrator] Triggered Function for zip: {blobName}, InstanceId {context.InstanceId}");
        var blobClient = new BlobClient(AzureWebJobsZipStorage, "zip", blobName);
        var sourceBlobSasToken = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddMinutes(5));

        await Task.WhenAll(DistributionTargets.Select(async distributionTarget =>
        {
            log.LogInformation($"[ZipDistributorOrchestrator] Start distribution target - {distributionTarget.TargetName}");
            string containerName = distributionTarget.ContainerName;
            if (distributionTarget.IsRoundRobin.HasValue &&
                distributionTarget.IsRoundRobin.Value &&
                distributionTarget.ContainersCount.HasValue)
            {
                var entityId = new EntityId(nameof(DurableTargetState), distributionTarget.TargetName);
                using (await context.LockAsync(entityId))
                {
                    int containerNum = await context.CallEntityAsync<int>(entityId, "GetNext");
                    containerName += containerNum.ToString();
                }
            }

            log.LogInformation($"[ZipDistributorOrchestrator] Start copy {blobClient.Name} to destination - {distributionTarget.TargetName}, containerName - {containerName}");
            var destClient = new BlobClient(distributionTarget.ConnectionString, containerName, blobClient.Name);
            var copyProcess = await destClient.StartCopyFromUriAsync(sourceBlobSasToken);
            await copyProcess.WaitForCompletionAsync();

            log.LogInformation($"[ZipDistributorOrchestrator] Finish copy {blobClient.Name} to destination - {distributionTarget.TargetName}");

            var destProps = destClient.GetProperties().Value;
            if (destProps.BlobCopyStatus != CopyStatus.Success)
            {
                log.LogError($"[ZipDistributorOrchestrator] Unsuccessfull copy {blobClient.Name} to destination - {distributionTarget.TargetName}, description: {destProps.CopyStatusDescription}");
                // TODO - Log and mark the zip not successfull.
            }
        }));

        // TODO - Delete zip if all successfull.
        // TODO - Handle errors
    }
}
