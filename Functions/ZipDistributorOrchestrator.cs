using Azure.Storage.Sas;

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
        List<Task<CopyZipResponse>> tasks = new List<Task<CopyZipResponse>>();

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

                try
                {
                    int containerNum = 0;
                    containerNum = await context.CallEntityAsync<int>(entityId, "GetNext", distributionTarget.ContainersCount.Value);
                    containerName += containerNum.ToString();
                    log.LogInformation($"[ZipDistributorOrchestrator] Recived container index for - {distributionTarget.TargetName} successfully. containerName: {containerName}");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"[ZipDistributorOrchestrator] Failed to get container index for - {distributionTarget.TargetName}");
                }
            }

            log.LogInformation($"[ZipDistributorOrchestrator] Trigger CopyZipActivity for distribution target - {distributionTarget.TargetName}");
            tasks.Add(context.CallActivityAsync<CopyZipResponse>(nameof(CopyZipActivity), new CopyZipRequest()
            {
                BlobName = blobName,
                ContainerName = containerName,
                DistributionTarget = distributionTarget,
            }));
        }

        var results = await Task.WhenAll(tasks);
        if (results.Any(res => !res.IsSuccessfull))
        {
            string failedTargets = string.Join(",", results.Where(res => !res.IsSuccessfull).Select(res => res.TargetName));
            log.LogWarning($"[ZipDistributorOrchestrator] zip {blobName} not successfully copy to all destinations, failed in {failedTargets}");
            await context.CallActivityAsync(nameof(ZipFailedActivity), new ZipFailedParams() { BlobName = blobName, FailedTargets = failedTargets } );
        }
        else
        {
            log.LogInformation($"[ZipDistributorOrchestrator] zip {blobName} successfully copy to all destinations");
        }

        await context.CallActivityAsync(nameof(ZipDeleteActivity), blobName);
    }
}
