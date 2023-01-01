namespace ImageIngest.Functions.Model;

public class Consts
{
    // IMPORTANT: 
    // When changing ContainerName make sure to change EventGrid's topic filter
    // Subject Begins With: blobServices/default/containers/files

    public const string FTPContainerName = "files";
    public const string ZipContainerName = "zip";
}
