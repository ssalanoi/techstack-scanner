namespace Api.Options;

public class LlmOptions
{
    public const string SectionName = "Ollama";

    public string Host { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "llama3.2";

    public int TimeoutSeconds { get; set; } = 60;

    public int MaxTokens { get; set; } = 1000;
}
