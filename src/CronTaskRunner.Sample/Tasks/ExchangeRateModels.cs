namespace CronTaskRunner.Sample.Tasks;

public class ExchangeRateData
{
    public decimal CurrentRate { get; set; }
    public decimal PreviousRate { get; set; }
}

public class ExchangeRateComparisonResult
{
    public decimal CurrentRate { get; set; }
    public decimal PreviousRate { get; set; }
    public decimal Diff { get; set; }
    public bool ThresholdExceeded { get; set; }
}
