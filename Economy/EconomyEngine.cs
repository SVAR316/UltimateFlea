using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using UltimateFlea.Pricing;

namespace UltimateFlea.Economy;

[Injectable(InjectionType.Singleton)]
public class EconomyEngine(
    ISptLogger<EconomyEngine> logger,
    ConfigManager configManager,
    PriceSourceRouter priceSource,
    EconomyStateStore stateStore,
    WipeStage wipeStage,
    PriceApplier priceApplier,
    DatabaseServer databaseServer)
{
    private readonly Random _random = new();

    private readonly Dictionary<MongoId, ItemMarketState> _state = new();
    private readonly HashSet<MongoId> _fixed = new();

    public void Initialize()
    {
        priceSource.Refresh();

        var pricing = configManager.Pricing;
        var items = databaseServer.GetTables().Templates.Items;

        var fixedPrices = ToMongoIdMap(pricing.FixedPrices);
        var itemMultipliers = ToMongoIdMap(pricing.ItemMultipliers);
        var categoryMultipliers = ToMongoIdMap(pricing.CategoryMultipliers);

        var persisted = stateStore.Load();

        _state.Clear();
        _fixed.Clear();

        // fixedPrices всегда в пуле, даже если CanSellOnRagfair ещё false
        foreach (var (tpl, fixedPrice) in fixedPrices)
        {
            if (!items.ContainsKey(tpl))
            {
                logger.Warning($"[UltimateFlea] Fixed price TPL not found in item DB: {tpl}");
                continue;
            }

            _fixed.Add(tpl);
            _state[tpl] = BuildMarketState(tpl, fixedPrice, persisted, preferCurrent: false);
        }

        foreach (var (tpl, item) in items)
        {
            if (_state.ContainsKey(tpl))
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

            _state[tpl] = BuildMarketState(tpl, source.Value * mult, persisted, preferCurrent: true);
        }

        logger.Success($"[UltimateFlea] Economy initialized for {_state.Count} items ({_fixed.Count} fixed-price)");

        ApplyAll();
    }

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
            if (preferCurrent && !_fixed.Contains(tpl))
            {
                market.CurrentPrice = prev.CurrentPrice;
            }
        }

        return market;
    }

    public void Tick()
    {
        var cfg = configManager.Economy;
        var wipeMult = wipeStage.GetMultiplier();

        foreach (var (tpl, market) in _state)
        {
            if (_fixed.Contains(tpl))
            {
                continue;
            }

            market.Demand *= 1.0 - cfg.DecayPerTick;
            market.Supply *= 1.0 - cfg.DecayPerTick;

            var pressure = cfg.PriceElasticity * (market.Demand - market.Supply);
            var noise = (_random.NextDouble() * 2.0 - 1.0) * cfg.Noise;
            var target = market.BasePrice * wipeMult * (1.0 + pressure + noise);

            market.CurrentPrice += (target - market.CurrentPrice) * cfg.SettleSpeed;

            var min = market.BasePrice * cfg.MinPriceFactor;
            var max = market.BasePrice * cfg.MaxPriceFactor;
            market.CurrentPrice = Math.Clamp(market.CurrentPrice, min, max);
        }

        ApplyAll();
        Persist();
    }

    public void RecordBuy(MongoId tpl)
    {
        if (_state.TryGetValue(tpl, out var market) && !_fixed.Contains(tpl))
        {
            market.Demand = Math.Clamp(market.Demand + configManager.Economy.DemandPerBuy, -1.0, 1.0);
        }
    }

    public void RecordSell(MongoId tpl)
    {
        if (_state.TryGetValue(tpl, out var market) && !_fixed.Contains(tpl))
        {
            market.Supply = Math.Clamp(market.Supply + configManager.Economy.SupplyPerSell, -1.0, 1.0);
        }
    }

    private void ApplyAll()
    {
        foreach (var (tpl, market) in _state)
        {
            priceApplier.SetPrice(tpl, market.CurrentPrice);
        }
    }

    private void Persist()
    {
        var snapshot = _state.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
        stateStore.Save(snapshot);
    }

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
}
