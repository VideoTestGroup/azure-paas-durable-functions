public class Consts
{
    // IMPORTANT: 
    // When changing ContainerName make sure to change EventGrid's topic filter
    // Subject Begins With: blobServices/default/containers/files

    public const string FTPContainerName = "files3-ms";
    public const string ZipContainerName = "zip3-ms";
    public const string FailedZipsContainerName = "failed-zips3-ms";
}
