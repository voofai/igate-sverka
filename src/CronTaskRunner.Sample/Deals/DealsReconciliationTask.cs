using System.Text;
using CronTaskRunner.Core.Abstractions;
using CronTaskRunner.Sample.Services;
using Microsoft.Extensions.Logging;

namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Пример реализации реальной задачи сверки: тянет одни и те же сделки
/// из нескольких источников (БД), сравнивает их между собой по ключу
/// DealId, оформляет расхождения в HTML-отчёт и отправляет его на почту.
/// Источники (IDealsSource) и их количество настраиваются через DI —
/// сама задача не знает, сколько их и что это за БД.
/// </summary>
public sealed class DealsReconciliationTask : ScheduledTaskBase<IReadOnlyList<IReadOnlyCollection<DealRecord>>, DealsReconciliationResult>
{
    private readonly IReadOnlyList<IDealsSource> _sources;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<DealsReconciliationTask> _logger;

    public DealsReconciliationTask(
        IEnumerable<IDealsSource> sources,
        IEmailSender emailSender,
        ILogger<DealsReconciliationTask> logger)
    {
        _sources = sources.ToList();
        _emailSender = emailSender;
        _logger = logger;

        if (_sources.Count < 2)
        {
            throw new InvalidOperationException(
                $"{nameof(DealsReconciliationTask)} требует минимум 2 источника сделок (зарегистрировано: {_sources.Count})");
        }
    }

    public override string Name => "DealsReconciliation";

    protected override async Task<IReadOnlyList<IReadOnlyCollection<DealRecord>>> GetDataAsync(CancellationToken cancellationToken)
    {
        var perSourceDeals = new List<IReadOnlyCollection<DealRecord>>(_sources.Count);

        foreach (var source in _sources)
        {
            _logger.LogInformation("[{Task}] Загружаем сделки из {Source}", Name, source.SourceName);
            perSourceDeals.Add(await source.GetDealsAsync(cancellationToken));
        }

        return perSourceDeals;
    }

    protected override Task<DealsReconciliationResult> HandleAsync(
        IReadOnlyList<IReadOnlyCollection<DealRecord>> perSourceDeals, CancellationToken cancellationToken)
    {
        var dealIds = perSourceDeals
            .SelectMany(deals => deals.Select(d => d.DealId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var discrepancies = new List<DealDiscrepancy>();

        foreach (var dealId in dealIds)
        {
            var bySource = new Dictionary<string, DealRecord?>();
            for (var i = 0; i < _sources.Count; i++)
            {
                bySource[_sources[i].SourceName] = perSourceDeals[i]
                    .FirstOrDefault(d => string.Equals(d.DealId, dealId, StringComparison.OrdinalIgnoreCase));
            }

            var found = bySource.Values.Where(v => v is not null).Select(v => v!).ToList();

            if (found.Count < _sources.Count)
            {
                var missingIn = bySource.Where(kv => kv.Value is null).Select(kv => kv.Key);
                discrepancies.Add(new DealDiscrepancy
                {
                    DealId = dealId,
                    Kind = DiscrepancyKind.MissingInSomeSources,
                    BySource = bySource,
                    Description = $"Отсутствует в: {string.Join(", ", missingIn)}",
                });
                continue;
            }

            var reference = found[0];
            var mismatched = found.Skip(1).Any(d =>
                d.Amount != reference.Amount ||
                !string.Equals(d.Currency, reference.Currency, StringComparison.OrdinalIgnoreCase) ||
                d.TradeDate.Date != reference.TradeDate.Date);

            if (mismatched)
            {
                discrepancies.Add(new DealDiscrepancy
                {
                    DealId = dealId,
                    Kind = DiscrepancyKind.FieldMismatch,
                    BySource = bySource,
                    Description = "Суммы/валюта/дата расходятся между источниками",
                });
            }
        }

        var result = new DealsReconciliationResult
        {
            TotalDealsChecked = dealIds.Count,
            Discrepancies = discrepancies,
        };

        _logger.LogInformation(
            "[{Task}] Проверено сделок: {Total}, расхождений: {Count}",
            Name, result.TotalDealsChecked, result.Discrepancies.Count);

        return Task.FromResult(result);
    }

    protected override async Task ProcessResultAsync(DealsReconciliationResult result, CancellationToken cancellationToken)
    {
        if (!result.HasDiscrepancies)
        {
            _logger.LogInformation("[{Task}] Расхождений нет, письмо не отправляется", Name);
            return;
        }

        var html = BuildHtmlReport(result);

        await _emailSender.SendAsync(
            to: "reconciliation-alerts@company.com",
            subject: $"Сверка сделок: найдено расхождений — {result.Discrepancies.Count}",
            body: html,
            cancellationToken,
            isHtml: true);
    }

    private string BuildHtmlReport(DealsReconciliationResult result)
    {
        var sb = new StringBuilder();
        sb.Append("<html><body>");
        sb.Append($"<h2>Сверка сделок</h2>");
        sb.Append($"<p>Проверено сделок: {result.TotalDealsChecked}. Расхождений: {result.Discrepancies.Count}.</p>");
        sb.Append("<table border='1' cellpadding='4' cellspacing='0'>");
        sb.Append("<tr><th>DealId</th><th>Тип</th><th>Описание</th>");
        foreach (var source in _sources)
        {
            sb.Append($"<th>{System.Net.WebUtility.HtmlEncode(source.SourceName)}</th>");
        }
        sb.Append("</tr>");

        foreach (var d in result.Discrepancies)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(d.DealId)}</td>");
            sb.Append($"<td>{d.Kind}</td>");
            sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(d.Description)}</td>");

            foreach (var source in _sources)
            {
                var record = d.BySource.TryGetValue(source.SourceName, out var r) ? r : null;
                var cell = record is null
                    ? "—"
                    : $"{record.Amount} {record.Currency} ({record.TradeDate:yyyy-MM-dd})";
                sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(cell)}</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</table></body></html>");
        return sb.ToString();
    }
}
