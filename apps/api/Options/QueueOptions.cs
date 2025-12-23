namespace Api.Options;

public class QueueOptions
{
    public int MaxConcurrency { get; set; } = 2;

    public int MaxRetries { get; set; } = 3;

    public int QueueCapacity { get; set; } = 100;
}
