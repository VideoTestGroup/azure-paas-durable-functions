namespace ImageIngest.Functions;
public class ZipperOrchestrator
{
    [FunctionName(nameof(ZipperOrchestrator))]
    public static async Task Run(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        log.LogInformation($"[ZipperOrchestrator] OrchestrationTrigger triggered Function for InstanceId {context.InstanceId}");
        var activity = context.GetInput<ActivityAction>();

        log.LogInformation($"[ZipperOrchestrator] Zipping files for activity: {activity}");
        bool? isSuccessfull = await context.CallActivityAsync<bool?>(nameof(Zipper), activity);
        if (isSuccessfull.HasValue)
        {
            log.LogInformation($"[ZipperOrchestrator] Finish zipping files for acitivty: {activity}. result: {isSuccessfull}");
        }
    }
}
