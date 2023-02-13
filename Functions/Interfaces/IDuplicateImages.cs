namespace ImageIngest.Functions.Interfaces;

public interface IDuplicateImages
{
    void Add(IngestImage image);
    void Remove(DateTime timestamp);
    Task<Dictionary<string, IngestImage>> Get();
}
