using Polly;
using Polly.CircuitBreaker;

var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));

var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {

            Console.WriteLine($"Tentativa {retryCount} falhou. Tentando novamente em {timeSpan.Seconds} segundos.");
        });



var httpClient = new HttpClient();

for (int i = 0; i < 5; i++)
{
    try
    {
        await circuitBreakerPolicy.ExecuteAsync(async () =>
        {
            var response = await httpClient.GetAsync("https://localhost:7247/WeatherForecast");
            response.EnsureSuccessStatusCode();
            Console.WriteLine("Requisição foi bem sucedida");
        });

        await retryPolicy.ExecuteAsync(async () =>
        {
            var response = await httpClient.GetAsync("https://localhost:7247/WeatherForecast");
            response.EnsureSuccessStatusCode();
            Console.WriteLine("Requisição foi bem sucedida");
        });
    }
    catch (BrokenCircuitException ex)
    {
        Console.WriteLine($"Circuito está aberto. Falha detectada: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Falha de requisição: {ex.Message}");
    }

    //await Task.Delay(5000);
}