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
        List<Task> tasks = new List<Task>();

        foreach (var distributionTarget in DistributionTargets)
        {
            log.LogInformation($"[ZipDistributorOrchestrator] Start distribution target - {distributionTarget.TargetName}");
            string containerName = distributionTarget.ContainerName;
            if (distributionTarget.IsRoundRobin.HasValue &&
                distributionTarget.IsRoundRobin.Value &&
                distributionTarget.ContainersCount.HasValue)
            {
                log.LogInformation($"[ZipDistributorOrchestrator] Get container index for - {distributionTarget.TargetName}");
                var entityId = new EntityId(nameof(DurableTargetState), distributionTarget.TargetName);
                using (await context.LockAsync(entityId))
                {
                    int containerNum = await context.CallEntityAsync<int>(entityId, "GetNext", distributionTarget.ContainersCount.Value);
                    containerName += containerNum.ToString();
                }

                log.LogInformation($"[ZipDistributorOrchestrator] Recived container index for- {distributionTarget.TargetName} successfully");
            }

            log.LogInformation($"[ZipDistributorOrchestrator] Trigger CopyZipActivity for distribution target - {distributionTarget.TargetName}");
            tasks.Add(context.CallActivityAsync(nameof(CopyZipActivity), new CopyZipRequest()
            {
                BlobName = blobName,
                ContainerName = containerName,
                DistributionTarget = distributionTarget,
                SourceBlobSasToken = sourceBlobSasToken
            }));
        }

        await Task.WhenAll(tasks);
        // TODO - Delete zip if all successfull.
        // TODO - Handle errors
    }
}
