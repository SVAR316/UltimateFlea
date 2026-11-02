using SPTarkov.DI.Annotations;
using UltimateFlea.Economy;

namespace UltimateFlea.Web;

/// <summary>
/// Turns one market snapshot plus the configs into the dashboard chart payload.
/// Everything is derived from a single snapshot pass so the charts agree with each other.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class ChartDataBuilder(
    ConfigManager configManager,
    EconomyEngine economyEngine,
    WipeStage wipeStage,
    TradeStatsStore tradeStats)
{
    private const int TradeWindowDays = 30;
    private const int MaxPressurePoints = 600;
    private const int MaxCategories = 12;
    private const int MaxHotItems = 10;
    private const int MaxMovers = 8;
    private const int MaxWipePoints = 60;

    private static readonly string[] FactorLabels =
        ["<0.85", "0.85-0.95", "0.95-1.05", "1.05-1.2", "1.2-1.5", "1.5-2.0", ">2.0"];

    public ChartPayload Build()
    {
        var rows = economyEngine.SnapshotItems();
        var ageDays = wipeStage.GetWipeAgeDays();

        var counts = new int[FactorLabels.Length];
        var above = 0;
        var below = 0;
        var atBase = 0;
        var fixedCount = 0;
        var factorSum = 0.0;
        var factorCount = 0;

        var hotDemand = new List<HotItemRow>();
        var hotSupply = new List<HotItemRow>();
        var candidates = new List<MarketItemSnapshot>();
        var categories = new Dictionary<string, CategoryAccumulator>();

        foreach (var row in rows)
        {
            if (row.IsFixed)
            {
                fixedCount++;
            }

            if (row.BasePrice <= 0)
            {
                continue;
            }

            var factor = row.Factor;
            counts[BucketOf(factor)]++;
            factorSum += factor;
            factorCount++;

            if (factor > 1.005) above++;
            else if (factor < 0.995) below++;
            else atBase++;

            if (row.Demand > 0.01)
            {
                hotDemand.Add(new HotItemRow(row.Tpl, ShortTpl(row.Tpl), row.Name, Round(row.Demand), Round(factor)));
            }

            if (row.Supply > 0.01)
            {
                hotSupply.Add(new HotItemRow(row.Tpl, ShortTpl(row.Tpl), row.Name, Round(row.Supply), Round(factor)));
            }

            if (!row.IsFixed && (Math.Abs(row.Demand - row.Supply) > 0.001 || Math.Abs(factor - 1.0) > 0.005))
            {
                candidates.Add(row);
            }

            if (!string.IsNullOrEmpty(row.Parent))
            {
                if (!categories.TryGetValue(row.Parent, out var acc))
                {
                    acc = new CategoryAccumulator(row.ParentName);
                    categories[row.Parent] = acc;
                }

                acc.Add(factor, row.Demand, row.Supply);
            }
        }

        var trades = tradeStats.GetSnapshot(TradeWindowDays);

        return new ChartPayload(
            Summary: new ChartSummary(
                ItemCount: rows.Count,
                FixedCount: fixedCount,
                FloatingCount: rows.Count - fixedCount,
                AvgFactor: factorCount == 0 ? null : Round(factorSum / factorCount),
                AboveBase: above,
                BelowBase: below,
                AtBase: atBase),
            Trades: new TradesChart(
                WindowDays: TradeWindowDays,
                TotalBuys: trades.TotalBuys,
                TotalSells: trades.TotalSells,
                Days: trades.Days.Select(d => new TradeDayRow(d.Day, d.Buys, d.Sells)).ToList()),
            Factors: new FactorChart(FactorLabels, counts),
            TopDemand: hotDemand.OrderByDescending(x => x.Value).Take(MaxHotItems).ToList(),
            TopSupply: hotSupply.OrderByDescending(x => x.Value).Take(MaxHotItems).ToList(),
            Categories: BuildCategories(categories),
            Movers: BuildMovers(candidates),
            Pressure: BuildPressure(candidates),
            Wipe: BuildWipe(ageDays),
            Events: BuildEvents(ageDays));
    }

    private static IReadOnlyList<CategoryRow> BuildCategories(Dictionary<string, CategoryAccumulator> source) =>
        source
            .OrderByDescending(kv => kv.Value.Count)
            .Take(MaxCategories)
            .Select(kv => new CategoryRow(
                Id: kv.Key,
                Name: kv.Value.Name,
                Items: kv.Value.Count,
                AvgFactor: Round(kv.Value.AverageFactor),
                AvgDemand: Round(kv.Value.AverageDemand),
                AvgSupply: Round(kv.Value.AverageSupply)))
            .ToList();

    private static MoversChart BuildMovers(List<MarketItemSnapshot> candidates)
    {
        var ordered = candidates.OrderByDescending(r => r.Factor).ToList();

        var up = ordered
            .Where(r => r.Factor > 1.0)
            .Take(MaxMovers)
            .Select(ToMover)
            .ToList();

        var down = ordered
            .Where(r => r.Factor < 1.0)
            .OrderBy(r => r.Factor)
            .Take(MaxMovers)
            .Select(ToMover)
            .ToList();

        return new MoversChart(up, down);
    }

    private static MoverRow ToMover(MarketItemSnapshot row) => new(
        Tpl: row.Tpl,
        ShortTpl: ShortTpl(row.Tpl),
        Name: row.Name,
        Factor: Round(row.Factor),
        BasePrice: Math.Round(row.BasePrice),
        CurrentPrice: Math.Round(row.CurrentPrice));

    // Even stride instead of a random sample: the scatter stays stable between refreshes.
    private static IReadOnlyList<PressurePoint> BuildPressure(List<MarketItemSnapshot> candidates)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var stride = Math.Max(1, candidates.Count / MaxPressurePoints);
        var points = new List<PressurePoint>(Math.Min(candidates.Count, MaxPressurePoints));

        for (var i = 0; i < candidates.Count && points.Count < MaxPressurePoints; i += stride)
        {
            var row = candidates[i];
            points.Add(new PressurePoint(Round(row.Demand - row.Supply), Round(row.Factor)));
        }

        return points;
    }

    private WipeChart BuildWipe(double? ageDays)
    {
        var wipe = configManager.Economy.Wipe;
        var curve = new List<WipePoint>();

        if (wipe.Enabled && wipe.StartLengthDays > 0)
        {
            // Show the whole ramp plus a little past it, and always past "today".
            var span = Math.Max(wipe.StartLengthDays * 1.25, (ageDays ?? 0.0) * 1.1);
            span = Math.Max(span, 1.0);

            for (var i = 0; i < MaxWipePoints; i++)
            {
                var day = span * i / (MaxWipePoints - 1.0);
                var progress = Math.Clamp(day / wipe.StartLengthDays, 0.0, 1.0);
                var mult = wipe.StartMultiplier + (1.0 - wipe.StartMultiplier) * progress;
                curve.Add(new WipePoint(Round(day), Round(mult)));
            }
        }

        return new WipeChart(
            Enabled: wipe.Enabled,
            AgeDays: ageDays is null ? null : Round(ageDays.Value),
            Multiplier: Round(wipeStage.GetMultiplier()),
            StartLengthDays: wipe.StartLengthDays,
            StartMultiplier: wipe.StartMultiplier,
            Curve: curve);
    }

    private EventsChart BuildEvents(double? ageDays)
    {
        var cfg = configManager.Events;

        var items = cfg.Events
            .Where(e => !string.IsNullOrWhiteSpace(e.Id))
            .OrderBy(e => e.StartDay)
            .Select(e => new EventRow(
                Id: e.Id,
                Label: e.Id.Replace('_', ' '),
                StartDay: e.StartDay,
                EndDay: e.EndDay,
                Multiplier: e.Multiplier,
                Active: cfg.Enabled && ageDays is not null && ageDays.Value >= e.StartDay && ageDays.Value < e.EndDay))
            .ToList();

        var window = items.Count == 0 ? 30.0 : items.Max(e => e.EndDay);
        window = Math.Max(window, ageDays ?? 0.0);
        window = Math.Max(window, 1.0);

        return new EventsChart(cfg.Enabled, ageDays is null ? null : Round(ageDays.Value), Round(window), items);
    }

    private static int BucketOf(double factor) => factor switch
    {
        < 0.85 => 0,
        < 0.95 => 1,
        < 1.05 => 2,
        < 1.2 => 3,
        < 1.5 => 4,
        < 2.0 => 5,
        _ => 6,
    };

    private static string ShortTpl(string tpl) =>
        tpl.Length <= 8 ? tpl : tpl[..4] + ".." + tpl[^4..];

    private static double Round(double value) => Math.Round(value, 3);

    private sealed class CategoryAccumulator(string name)
    {
        private double _factorSum;
        private double _demandSum;
        private double _supplySum;

        public string Name { get; } = name;
        public int Count { get; private set; }

        public double AverageFactor => Count == 0 ? 1.0 : _factorSum / Count;
        public double AverageDemand => Count == 0 ? 0.0 : _demandSum / Count;
        public double AverageSupply => Count == 0 ? 0.0 : _supplySum / Count;

        public void Add(double factor, double demand, double supply)
        {
            _factorSum += factor;
            _demandSum += demand;
            _supplySum += supply;
            Count++;
        }
    }
}
