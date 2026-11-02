using System.Collections.Concurrent;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using UltimateFlea.Pricing;

namespace UltimateFlea.Economy;

[Injectable(InjectionType.Singleton)]
public class EconomyEngine(
    ISptLogger<EconomyEngine> logger,
    ConfigManager configManager,
    PriceSourceRouter priceSource,
    EconomyStateStore stateStore,
    TradeStatsStore tradeStats,
    WipeStage wipeStage,
    MarketEventService marketEvents,
    PriceTrendService priceTrends,
    PriceApplier priceApplier,
    TemplateTable templates)
{
    private readonly Random _random = new();

    // Mutated by the tick thread and by trade request threads, enumerated by dashboard
    // (Kestrel) threads: every shared container here must be safe for concurrent use.
    private readonly ConcurrentDictionary<MongoId, ItemMarketState> _state = new();

    // Immutable once published; swapped as a whole by Initialize.
    private volatile ItemCatalog _catalog = ItemCatalog.Empty;

    public void Initialize()
    {
        priceSource.Refresh();

        var pricing = configManager.Pricing;
        var items = templates.Items;

        var fixedPrices = ToMongoIdMap(pricing.FixedPrices);
        var itemMultipliers = ToMongoIdMap(pricing.ItemMultipliers);
        var categoryMultipliers = ToMongoIdMap(pricing.CategoryMultipliers);

        var persisted = stateStore.Load();

        var fixedSet = new HashSet<MongoId>();
        var info = new Dictionary<MongoId, ItemInfo>();
        var next = new Dictionary<MongoId, ItemMarketState>();

        // Fixed prices are always in the pool, even when CanSellOnRagfair is still false.
        foreach (var (tpl, fixedPrice) in fixedPrices)
        {
            if (!items.TryGetValue(tpl, out var fixedItem))
            {
                logger.Warning($"[UltimateFlea] Fixed price TPL not found in item DB: {tpl}");
                continue;
            }

            fixedSet.Add(tpl);
            info[tpl] = BuildInfo(tpl, fixedItem, items);
            next[tpl] = BuildMarketState(tpl, fixedPrice, persisted, preferCurrent: false);
        }

        foreach (var (tpl, item) in items)
        {
            if (next.ContainsKey(tpl))
            {
                continue;
            }

            if (item.Properties?.CanSellOnRagfair != true)
            {
                continue;
            }

            var source = priceSource.GetBasePrice(tpl);
            if (source is null or <= 0)
            {
                continue;
            }

            var mult = pricing.GlobalMultiplier;
            if (itemMultipliers.TryGetValue(tpl, out var im)) mult *= im;
            if (categoryMultipliers.TryGetValue(item.Parent, out var cm)) mult *= cm;

            info[tpl] = BuildInfo(tpl, item, items);
            next[tpl] = BuildMarketState(tpl, source.Value * mult, persisted, preferCurrent: true);
        }

        _state.Clear();
        foreach (var (tpl, market) in next)
        {
            _state[tpl] = market;
        }

        _catalog = new ItemCatalog(info, fixedSet);

        logger.Success($"[UltimateFlea] Economy initialized for {_state.Count} items ({fixedSet.Count} fixed-price)");

        ApplyAll(onlyFloating: false);
    }

    public EconomySnapshot GetSnapshot()
    {
        var catalog = _catalog;
        var count = 0;
        var factorSum = 0.0;
        var factorCount = 0;

        foreach (var market in _state.Values)
        {
            count++;
            var basePrice = market.BasePrice;
            if (basePrice > 0)
            {
                factorSum += market.CurrentPrice / basePrice;
                factorCount++;
            }
        }

        double? avgFactor = count == 0 ? null : factorCount == 0 ? 1.0 : factorSum / factorCount;

        return new EconomySnapshot(
            ItemCount: count,
            FixedCount: catalog.Fixed.Count,
            AverageFactor: avgFactor,
            WipeAgeDays: wipeStage.GetWipeAgeDays(),
            WipeMultiplier: wipeStage.GetMultiplier());
    }

    /// <summary>
    /// Consistent, allocation-once view of the live market for the dashboard.
    /// Reads every mutable double into a local so a concurrent tick cannot tear a row.
    /// </summary>
    public IReadOnlyList<MarketItemSnapshot> SnapshotItems()
    {
        var catalog = _catalog;
        var rows = new List<MarketItemSnapshot>(_state.Count);

        foreach (var (tpl, market) in _state)
        {
            var basePrice = market.BasePrice;
            var currentPrice = market.CurrentPrice;
            var demand = market.Demand;
            var supply = market.Supply;

            catalog.Info.TryGetValue(tpl, out var meta);

            rows.Add(new MarketItemSnapshot(
                Tpl: tpl.ToString(),
                Name: meta?.Name ?? tpl.ToString(),
                Parent: meta?.ParentKey ?? string.Empty,
                ParentName: meta?.ParentName ?? string.Empty,
                BasePrice: basePrice,
                CurrentPrice: currentPrice,
                Demand: demand,
                Supply: supply,
                IsFixed: catalog.Fixed.Contains(tpl)));
        }

        return rows;
    }

    /// <summary>Live state keyed by TPL, in the same shape as data/economy_state.json.</summary>
    public Dictionary<string, ItemMarketState> SnapshotState()
    {
        var snapshot = new Dictionary<string, ItemMarketState>(_state.Count);
        foreach (var (tpl, market) in _state)
        {
            snapshot[tpl.ToString()] = new ItemMarketState
            {
                BasePrice = market.BasePrice,
                CurrentPrice = market.CurrentPrice,
                Demand = market.Demand,
                Supply = market.Supply,
            };
        }

        return snapshot;
    }

    public readonly record struct EconomySnapshot(
        int ItemCount,
        int FixedCount,
        double? AverageFactor,
        double? WipeAgeDays,
        double WipeMultiplier);

    private ItemMarketState BuildMarketState(
        MongoId tpl,
        double basePrice,
        Dictionary<string, ItemMarketState> persisted,
        bool preferCurrent)
    {
        var market = new ItemMarketState
        {
            BasePrice = basePrice,
            CurrentPrice = basePrice,
            Demand = 0,
            Supply = 0
        };

        if (persisted.TryGetValue(tpl.ToString(), out var prev))
        {
            market.Demand = prev.Demand;
            market.Supply = prev.Supply;
            if (preferCurrent)
            {
                market.CurrentPrice = prev.CurrentPrice;
            }
        }

        return market;
    }

    private static ItemInfo BuildInfo(MongoId tpl, TemplateItem item, Dictionary<MongoId, TemplateItem> items)
    {
        var parent = item.Parent;
        var parentName = items.TryGetValue(parent, out var parentItem)
            ? Display(parentItem.Name, parent)
            : parent.ToString();

        return new ItemInfo(
            TplKey: tpl.ToString(),
            Name: Display(item.Name, tpl),
            Parent: parent,
            ParentKey: parent.ToString(),
            ParentName: parentName);
    }

    private static string Display(string? name, MongoId fallback) =>
        string.IsNullOrWhiteSpace(name) ? fallback.ToString() : name;

    public void Tick()
    {
        var cfg = configManager.Economy;
        var catalog = _catalog;

        // Wipe age walks every profile, so resolve it once per tick instead of per item.
        var ageDays = wipeStage.GetWipeAgeDays();
        var wipeMult = wipeStage.GetMultiplier();
        var eventTable = marketEvents.BuildTable(ageDays);
        var trendTable = priceTrends.BuildTable(ageDays ?? 0.0);

        foreach (var (tpl, market) in _state)
        {
            if (catalog.Fixed.Contains(tpl))
            {
                continue;
            }

            if (!catalog.Info.TryGetValue(tpl, out var meta))
            {
                continue;
            }

            market.Demand *= 1.0 - cfg.DecayPerTick;
            market.Supply *= 1.0 - cfg.DecayPerTick;

            var eventMult = eventTable.Get(meta.TplKey, meta.ParentKey);
            var trendMult = trendTable.Get(meta.ParentKey);

            var pressure = cfg.PriceElasticity * (market.Demand - market.Supply);
            var noise = (_random.NextDouble() * 2.0 - 1.0) * cfg.Noise;
            var target = market.BasePrice * wipeMult * eventMult * trendMult * (1.0 + pressure + noise);

            var price = market.CurrentPrice + (target - market.CurrentPrice) * cfg.SettleSpeed;

            var min = market.BasePrice * cfg.MinPriceFactor;
            var max = market.BasePrice * cfg.MaxPriceFactor;
            market.CurrentPrice = Math.Clamp(price, Math.Min(min, max), Math.Max(min, max));
        }

        ApplyAll(onlyFloating: true);
        Persist();
        tradeStats.Flush();
    }

    public void RecordBuy(MongoId tpl)
    {
        tradeStats.RecordBuy();
        if (_state.TryGetValue(tpl, out var market) && !_catalog.Fixed.Contains(tpl))
        {
            market.Demand = Math.Clamp(market.Demand + configManager.Economy.DemandPerBuy, -1.0, 1.0);
        }
    }

    public void RecordSell(MongoId tpl)
    {
        tradeStats.RecordSell();
        if (_state.TryGetValue(tpl, out var market) && !_catalog.Fixed.Contains(tpl))
        {
            market.Supply = Math.Clamp(market.Supply + configManager.Economy.SupplyPerSell, -1.0, 1.0);
        }
    }

    /// <param name="onlyFloating">
    /// Fixed-price items never move after Initialize, so the per-tick pass skips them.
    /// </param>
    private void ApplyAll(bool onlyFloating)
    {
        var fixedSet = _catalog.Fixed;
        foreach (var (tpl, market) in _state)
        {
            if (onlyFloating && fixedSet.Contains(tpl))
            {
                continue;
            }

            priceApplier.SetPrice(tpl, market.CurrentPrice);
        }
    }

    private void Persist() => stateStore.Save(SnapshotState());

    private Dictionary<MongoId, double> ToMongoIdMap(Dictionary<string, double> raw)
    {
        var map = new Dictionary<MongoId, double>();
        foreach (var (key, value) in raw)
        {
            if (!string.IsNullOrWhiteSpace(key) && MongoId.IsValidMongoId(key))
            {
                map[new MongoId(key)] = value;
            }
            else
            {
                logger.Warning($"[UltimateFlea] Ignoring invalid TPL in pricing config: '{key}'");
            }
        }

        return map;
    }

    private sealed record ItemInfo(
        string TplKey,
        string Name,
        MongoId Parent,
        string ParentKey,
        string ParentName);

    private sealed class ItemCatalog(
        Dictionary<MongoId, ItemInfo> info,
        HashSet<MongoId> fixedItems)
    {
        public static readonly ItemCatalog Empty = new([], []);

        public IReadOnlyDictionary<MongoId, ItemInfo> Info { get; } = info;
        public IReadOnlySet<MongoId> Fixed { get; } = fixedItems;
    }
}

/// <summary>One row of the live market, safe to hand to the dashboard layer.</summary>
public readonly record struct MarketItemSnapshot(
    string Tpl,
    string Name,
    string Parent,
    string ParentName,
    double BasePrice,
    double CurrentPrice,
    double Demand,
    double Supply,
    bool IsFixed)
{
    public double Factor => BasePrice > 0 ? CurrentPrice / BasePrice : 1.0;
}
