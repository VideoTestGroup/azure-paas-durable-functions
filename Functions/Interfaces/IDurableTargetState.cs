namespace ImageIngest.Functions.Interfaces;

public interface IDurableTargetState
{
    int CurrentValue { get; set; }
    void Increment();
    void Reset();
    int GetNext(int maxSize);
}
