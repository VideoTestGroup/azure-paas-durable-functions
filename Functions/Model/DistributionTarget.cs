namespace ImageIngest.Functions.Model;

public class DistributionTarget
{
    public string TargetName { get; set; }
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
    public bool? IsRoundRobin { get; set; }
    public int? ContainersCount { get; set; }
}
