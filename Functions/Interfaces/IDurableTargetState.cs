namespace ImageIngest.Functions.Interfaces;

public interface IDurableTargetState
{
    int CurrentValue { get; set; }
    void Increment();
    void ChangeValue(int maxSize);
    void Reset();
}
