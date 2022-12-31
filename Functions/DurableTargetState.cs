namespace ImageIngest.Functions;

[JsonObject(MemberSerialization.OptIn)]
public class DurableTargetState: IDurableTargetState
{
    [JsonProperty("value")]
    public int CurrentValue { get; set; }

    public void Increment() => this.CurrentValue += 1;

    public void Reset() => this.CurrentValue = 1;

    public int GetNext(int maxSize)
    {
        int currentValue = this.CurrentValue;
        if (currentValue < maxSize)
        {
            Increment();
        }
        else
        {
            Reset();
        }

        return currentValue;
    }


    [FunctionName(nameof(DurableTargetState))]
    public static Task Run([EntityTrigger] IDurableEntityContext ctx)
    {
        if (!ctx.HasState)
        {
            ctx.SetState(1);
        }

        return ctx.DispatchAsync<DurableTargetState>();
    }
}