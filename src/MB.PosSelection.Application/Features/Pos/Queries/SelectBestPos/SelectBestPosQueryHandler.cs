using MB.PosSelection.Application.Dtos;
using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Domain.Interfaces;
using MediatR;
using System.Diagnostics;
using MB.PosSelection.Application.Common.Exceptions;

namespace MB.PosSelection.Application.Features.Pos.Queries.SelectBestPos
{
    public class SelectBestPosQueryHandler : IRequestHandler<SelectBestPosQuery, PosSelectionResult>
    {
        private readonly IPosRateRepository _repository;
        private readonly IEnumerable<ICostCalculationStrategy> _strategies;
        private readonly IEnumerable<IPosSelectionRule> _rules;
        private readonly IPosMetricsService _metrics;

        public SelectBestPosQueryHandler(
            IPosRateRepository repository,
            IEnumerable<ICostCalculationStrategy> strategies,
            IEnumerable<IPosSelectionRule> rules,
            IPosMetricsService metrics)
        {
            _repository = repository;
            _strategies = strategies;
            _rules = rules.OrderBy(r => r.ExecutionOrder);
            _metrics = metrics;
        }

        public async Task<PosSelectionResult> Handle(SelectBestPosQuery query, CancellationToken cancellationToken)
        {
            _metrics.IncrementTotalRequests();

            var stopwatch = Stopwatch.StartNew();
            var request = query.Request;

            // Akıllı Index'i Getir (Cache/Memory'den O(1) erişimle gelir)
            var rateIndex = await _repository.GetRatesIndexAsync();

            // O(1) Hızında Arama Yap. Doğrudan hash map'ten çekiyoruz.
            var matchingRates = rateIndex.FindRates(
                request.Currency,
                request.Installment,
                request.CardType,
                request.CardBrand
            );

            var totalMatches = matchingRates.Count;

            // Maliyet Hesapla
            var candidates = matchingRates
                .Select(rate =>
                {
                    // Strateji seçimi (TRY/USD)
                    var strategy = _strategies.FirstOrDefault(s => s.SupportedCurrency == rate.Currency)
                                   ?? _strategies.First(s => s.SupportedCurrency == Domain.Enums.Currency.TRY);

                    var cost = strategy.CalculateCost(request.Amount, rate.CommissionRate, rate.MinFee);
                    return new PosCandidate(rate, cost, request.Amount + cost);
                })
                .ToList();

            if (!candidates.Any())
            {
                throw new NotFoundException("Kriterlere uygun POS bulunamadı.");
            }

            // Kural Motorunu Çalıştır (Sıralama)
            candidates.Sort((x, y) =>
            {
                foreach (var rule in _rules)
                {
                    // 1. Kurala bak (Cost). Eşit değilse sonucu dön.
                    // Eşitse (0 dönerse), döngü devam eder ve 2. Kurala (Priority) geçer
                    int result = rule.Compare(x, y);
                    if (result != 0) return result;
                }
                return 0;
            });

            var winner = candidates.First();

            // "Neden Kazandı?" Analizi
            string reason = "OnlyOption";
            if (candidates.Count > 1)
            {
                var runnerUp = candidates[1];
                foreach (var rule in _rules)
                {
                    if (rule.Compare(winner, runnerUp) != 0)
                    {
                        reason = rule.RuleName;
                        break;
                    }
                }
            }

            stopwatch.Stop();

            var responseDto = new PosSelectionResponseDto
            {
                PosName = winner.Ratio.PosName,
                CardType = string.IsNullOrWhiteSpace(winner.Ratio.CardType.ToString()) ? null : winner.Ratio.CardType.ToString(),
                CardBrand = string.IsNullOrWhiteSpace(winner.Ratio.CardBrand) ? null : winner.Ratio.CardBrand,

                Installment = winner.Ratio.Installment,
                Currency = winner.Ratio.Currency.ToString(),
                CommissionRate = winner.Ratio.CommissionRate,
                Price = winner.Price,
                PayableTotal = winner.PayableTotal
            };

            var meta = new PosSelectionMeta
            {
                EvaluatedCount = totalMatches,
                SelectionReason = reason,
                WinnerPosName = winner.Ratio.PosName,
                ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };

            return new PosSelectionResult(responseDto, meta);
        }
    }
}
