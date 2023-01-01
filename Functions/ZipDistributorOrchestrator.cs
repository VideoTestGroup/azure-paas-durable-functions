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
        var blobClient = new BlobClient(AzureWebJobsZipStorage, Consts.ZipContainerName, blobName);
        var sourceBlobSasToken = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddMinutes(5));

        List<Task<bool>> tasks = new List<Task<bool>>();

        // TODO - Handle errors is target that will not affect other targets.
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

                log.LogInformation($"[ZipDistributorOrchestrator] Recived container index for - {distributionTarget.TargetName} successfully. containerName: {containerName}");
            }

            log.LogInformation($"[ZipDistributorOrchestrator] Trigger CopyZipActivity for distribution target - {distributionTarget.TargetName}");
            tasks.Add(context.CallActivityAsync<bool>(nameof(CopyZipActivity), new CopyZipRequest()
            {
                BlobName = blobName,
                ContainerName = containerName,
                DistributionTarget = distributionTarget,
                SourceBlobSasToken = sourceBlobSasToken
            }));
        }

        var results = await Task.WhenAll(tasks);
        if (results.Any(res => !res))
        {
            log.LogWarning($"[ZipDistributorOrchestrator] zip {blobName} not successfully copy to all destinations");
            // TODO - Maybe mark the zip as error or something.
        }
        else
        {
            log.LogInformation($"[ZipDistributorOrchestrator] zip {blobName} successfully copy to all destinations, start delete zip");
            await context.CallActivityAsync(nameof(ZipDeleteActivity), blobName);
        }
    }
}
