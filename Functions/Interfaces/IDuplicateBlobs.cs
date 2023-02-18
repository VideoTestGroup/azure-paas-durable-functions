namespace ImageIngest.Functions.Interfaces;
public interface IDuplicateBlobs
{
    void Add(string blobName, DateTime timestamp);
    void Remove(DateTime timestamp);
    Task<Dictionary<string, DateTime>> Get();
}
