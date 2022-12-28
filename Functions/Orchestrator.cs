namespace ImageIngest.Functions;
public class Orchestrator
{
    [FunctionName(nameof(Orchestrator))]
    public static async Task Run(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        log.LogInformation($"[Orchestrator] OrchestrationTrigger triggered Function from [BlobListener] for InstanceId {context.InstanceId}");
        string @namespace = context.GetInput<string>();

        // //1. Get storage sas token    ++++++++++++++++++++++++++++++++++++++
        // activity = await context.CallActivityAsync<ActivityAction>(nameof(Tokenizer), activity);
        // log.LogInformation($"[Orchestrator] ActivityAction with token {activity}");

        // 1. Check for ready batch files 
        string batchId = await context.CallActivityAsync<string>(nameof(Collector), @namespace);

        // 2 Check if batch created
        if (string.IsNullOrWhiteSpace(batchId))
        {
            log.LogInformation($"[Orchestrator] No batch created for namespace {@namespace}");
            return;
        }

        // 3. Zip Files
        log.LogInformation($"[Orchestrator] Zipping files for namespace: {@namespace} with batchId: {batchId}.");
        bool? isSuccessfull = await context.CallActivityAsync<bool?>(nameof(Zipper), new ActivityAction() { Namespace = @namespace, BatchId = batchId });
        if (isSuccessfull.HasValue)
        {
            log.LogInformation($"[Orchestrator] Finish zipping files for namespace: {@namespace} with batchId: {batchId}. result: {isSuccessfull}");
        }
    }
}
