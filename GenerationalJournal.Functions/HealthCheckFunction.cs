using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenerationalJournal.Functions;

public class HealthCheckFunction
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthCheckFunction> _logger;

    public HealthCheckFunction(IConfiguration configuration, ILogger<HealthCheckFunction> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [Function(nameof(HealthCheckFunction))]
    public async Task RunAsync(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
        FunctionContext context)
    {
        var healthUrl = _configuration["HealthCheck:ApiUrl"] ?? "http://localhost:5278/health";

        try
        {
            using var response = await Client.GetAsync(healthUrl);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Health check passed for {Url} ({StatusCode}).",
                    healthUrl, (int)response.StatusCode);
            }
            else
            {
                _logger.LogWarning(
                    "Health check failed for {Url} ({StatusCode}).",
                    healthUrl, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check could not reach {Url}.", healthUrl);
        }
    }
}
