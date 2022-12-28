namespace ImageIngest.Functions
{
    public class Scavenger
    {
        public static string AzureWebJobsFTPStorage { get; set; } = Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
        public List<string> Namespaces { get; set; } = Environment.GetEnvironmentVariable("Namespaces").Split(",").ToList();

        [FunctionName(nameof(Scavenger))]
        public async Task Run(
            [TimerTrigger("*/%ScavengerTimer% * * * * *")] TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"[Scavenger] Timer trigger function executed at: {DateTime.Now}");
            await Task.WhenAll(Namespaces.Select(@namespace =>
            {
                log.LogInformation($"[Scavenger] trigger Orchestrator for namespace: {@namespace}");
                return starter.StartNewAsync<string>(nameof(Orchestrator), @namespace).ContinueWith((res) =>
                {
                    log.LogInformation($"[Scavenger] finish Orchestrator for namespace: {@namespace}");
                });
            }));
        }
    }
}

