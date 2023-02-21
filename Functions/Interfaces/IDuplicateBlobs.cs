namespace ImageIngest.Functions.Interfaces;
public interface IDuplicateBlobs
{
    void Add(DuplicateBlob duplicateBlob);
    void Remove(DateTime timestamp);
    Task<Dictionary<string, DateTime>> Get();
}
