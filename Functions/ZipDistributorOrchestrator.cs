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
                var watch = System.Diagnostics.Stopwatch.StartNew();
                log.LogInformation($"[Timer] Started Critical Section: Lock Stopwatch: {watch.ElapsedMilliseconds}, entity id {entityId}");
//                using (await context.LockAsync(entityId))
                int containerNum = 0;
//                List<Task> tasks = new List<Task>();
                try
                {
                    watch.Stop();
                    log.LogInformation($"[Timer] Entered Critical Section: Lock Stopwatch: {watch.ElapsedMilliseconds}, entity id {entityId}");
                    containerNum = await context.CallEntityAsync<int>(entityId, "GetNext", distributionTarget.ContainersCount.Value);
//                    Task task = context.CallEntityAsync<int>(entityId, "GetNext", distributionTarget.ContainersCount.Value);
//                    tasks.Add(task);
                    containerName += containerNum.ToString();
                    log.LogInformation($"[Distribution] Success to {containerName}{containerNum}, distributionTarget: {distributionTarget}")                    
                }
                catch(Exception ex)
                {
                    log.LogError(ex, $"[Distribution] Failed to {containerName}{containerNum}, distributionTarget: {distributionTarget}")
                }
                if(watch.IsRunning)
                {
                    watch.Stop();
                    log.LogInformation($"[Timer] Skipped Critical Section: Lock Stopwatch: {watch.ElapsedMilliseconds}, entity id {entityId}");
                }
                
                log.LogInformation($"[ZipDistributorOrchestrator] Recived container index for - {distributionTarget.TargetName} successfully. containerName: {containerName}");
            }

            log.LogInformation($"[ZipDistributorOrchestrator] Trigger CopyZipActivity for distribution target - {distributionTarget.TargetName}");
            tasks.Add(context.CallActivityAsync<CopyZipResponse>(nameof(CopyZipActivity), new CopyZipRequest()
            {
                BlobName = blobName,
                ContainerName = containerName,
                DistributionTarget = distributionTarget,
            }));
        }
//      await Task.WhenAll(tasks.ToArray());

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
