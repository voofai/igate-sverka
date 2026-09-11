namespace CronTaskRunner.Sample.Services;

public interface IExchangeRateProvider
{
    Task<decimal> GetCurrentRateAsync(CancellationToken cancellationToken);
    Task<decimal> GetLastSavedRateAsync(CancellationToken cancellationToken);
}
