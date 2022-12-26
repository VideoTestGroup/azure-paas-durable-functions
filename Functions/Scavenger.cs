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
            await starter.StartNewAsync(nameof(ScavengerOrchestrator));
        }
    }
}

