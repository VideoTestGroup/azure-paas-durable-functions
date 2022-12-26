using System.Diagnostics;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace ImageIngest.Functions
{
    public class Scavenger
    {
        public static string AzureWebJobsFTPStorage { get; set; } = System.Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");

        public int ScavengerPageSize { get; set; } =
            int.TryParse(System.Environment.GetEnvironmentVariable("ScavengerPageSize"), out int size) ? size : 10485760;

        public TimeSpan ScavengerOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(System.Environment.GetEnvironmentVariable("ScavengerOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

        [FunctionName(nameof(Scavenger))]
        public async Task Run(
            [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"[Scavenger] Timer trigger function executed at: {DateTime.Now}");
            List<string> namespaces = new List<string>() { "P1", "P2", "P3", "P4" };
            foreach (var @namespace in namespaces)
            {
                log.LogInformation($"[Scavenger] trigger Orchestrator for namespace: {@namespace}");
                await starter.StartNewAsync(nameof(Orchestrator),
                    new ActivityAction() { QueryStatus = BlobStatus.Pending, Namespace = @namespace });
                log.LogInformation($"[Scavenger] finish Orchestrator for namespace: {@namespace}");
            }
        }
    }
}

