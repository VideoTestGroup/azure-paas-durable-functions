namespace ImageIngest.Functions;

public class DurableTargetStates
{
    [FunctionName(nameof(DurableTargetStates))]
    public static void Run([EntityTrigger] IDurableEntityContext ctx)
    {
        if (!ctx.HasState)
        {
            ctx.SetState(1);
        }

        switch (ctx.OperationName)
        {
            case "GetNext":
                int currentValue = ctx.GetState<int>();
                int maxSize = ctx.GetInput<int>();
                if (currentValue < maxSize)
                {
                    ctx.SetState(currentValue + 1);
                }
                else
                {
                    ctx.SetState(1);
                }

                ctx.Return(currentValue);
                break;
            default:
                throw new ArgumentException("Unsupported operation");
        }
    }
}