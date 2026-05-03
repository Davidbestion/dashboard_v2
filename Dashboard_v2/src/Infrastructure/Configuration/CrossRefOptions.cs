namespace Dashboard_v2.Infrastructure.Configuration;

public class CrossRefOptions
{
    // Base URL of the CrossRef API
    public string BaseAddress { get; set; } = "https://api.crossref.org/";

    // Contact email recommended by CrossRef for polite pool identification
    public string? ContactEmail { get; set; }

    // HTTP client timeout in seconds
    public int TimeoutSeconds { get; set; } = 10;

    // Maximum number of retries on transient failures
    public int MaxRetries { get; set; } = 3;

    // Base delay in milliseconds for exponential backoff
    public int BaseDelayMs { get; set; } = 500;

    // Maximum jitter factor (0.5..1.5 applied to computed delay)
    public double JitterFactorMin { get; set; } = 0.5;
    public double JitterFactorMax { get; set; } = 1.5;

    // Handler lifetime in seconds for HttpClientHandler recycling
    public int HandlerLifetimeSeconds { get; set; } = 300;
}
