using System.Reflection;
using System.Text.Json;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using UltimateFlea.Configs;
using UltimateFlea.Util;

namespace UltimateFlea;

[Injectable(InjectionType.Singleton)]
public class ConfigManager(
    ISptLogger<ConfigManager> logger,
    ModHelper modHelper)
{
    /// <summary>
    /// One registry row per config file. Load, read, save, validate and the name list are all
    /// driven from <see cref="Slots"/>, so a new config only has to be added here.
    /// </summary>
    private sealed record ConfigSlot(
        string Name,
        string FileName,
        bool RestartRecommended,
        Func<ConfigManager, object> Get,
        Action<ConfigManager, object> Set,
        Func<ConfigManager, object> ReadFile,
        Func<JsonElement, object> FromBody,
        Action<ConfigManager, object> Validate);

    private static ConfigSlot Slot<T>(
        string name,
        string fileName,
        bool restartRecommended,
        Func<ConfigManager, T> get,
        Action<ConfigManager, T> set,
        Action<ConfigManager, T>? validate = null)
        where T : class, new()
        => new(
            name,
            fileName,
            restartRecommended,
            m => get(m),
            (m, v) => set(m, (T)v),
            m => m.ReadConfigFile<T>(fileName),
            body => DeserializeBody<T>(body),
            (m, v) => validate?.Invoke(m, (T)v));

    private static readonly ConfigSlot[] Slots =
    [
        Slot<ModConfig>("mod", "config/mod.json", false,
            m => m.Mod, (m, v) => m.Mod = v, (m, v) => m.ValidateMod(v)),
        Slot<PoolConfig>("pool", "config/pool.json", true,
            m => m.Pool, (m, v) => m.Pool = v, (m, v) => m.ValidatePool(v)),
        Slot<PricingConfig>("pricing", "config/pricing.json", true,
            m => m.Pricing, (m, v) => m.Pricing = v, (m, v) => m.ValidatePricing(v)),
        Slot<EconomyConfig>("economy", "config/economy.json", true,
            m => m.Economy, (m, v) => m.Economy = v, (m, v) => m.ValidateEconomy(v)),
        Slot<EventsConfig>("events", "config/events.json", false,
            m => m.Events, (m, v) => m.Events = v, (m, v) => m.ValidateEvents(v)),
        Slot<TrendsConfig>("trends", "config/trends.json", false,
            m => m.Trends, (m, v) => m.Trends = v, (m, v) => m.ValidateTrends(v)),
        Slot<LevelLocksConfig>("levels", "config/levels.json", false,
            m => m.Levels, (m, v) => m.Levels = v),
    ];

    // Publishing happens under this lock so two writers (web save vs. reload) never interleave.
    // The fields themselves are volatile: a reader on the tick thread always sees whole objects.
    private readonly object _publishGate = new();

    private volatile ModConfig _mod = new();
    private volatile PoolConfig _pool = new();
    private volatile PricingConfig _pricing = new();
    private volatile EconomyConfig _economy = new();
    private volatile EventsConfig _events = new();
    private volatile TrendsConfig _trends = new();
    private volatile LevelLocksConfig _levels = new();

    public ModConfig Mod { get => _mod; private set => _mod = value; }
    public PoolConfig Pool { get => _pool; private set => _pool = value; }
    public PricingConfig Pricing { get => _pricing; private set => _pricing = value; }
    public EconomyConfig Economy { get => _economy; private set => _economy = value; }
    public EventsConfig Events { get => _events; private set => _events = value; }
    public TrendsConfig Trends { get => _trends; private set => _trends = value; }
    public LevelLocksConfig Levels { get => _levels; private set => _levels = value; }

    public string ModPath { get; private set; } = string.Empty;

    public string[] ConfigNames { get; } = Slots.Select(s => s.Name).ToArray();

    public void Load()
    {
        ModPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        LoadAllFromDisk();
    }

    public void ReloadFromDisk()
    {
        if (string.IsNullOrEmpty(ModPath))
        {
            Load();
            return;
        }

        LoadAllFromDisk();
    }

    public object GetConfigObject(string name)
    {
        var slot = FindSlot(name) ?? throw new ArgumentException($"Unknown config '{name}'");
        return slot.Get(this);
    }

    public bool TrySaveConfig(string name, JsonElement body, out string error, out bool restartRecommended)
    {
        error = string.Empty;
        restartRecommended = false;

        var slot = FindSlot(name);
        if (slot is null)
        {
            error = $"Unknown config '{name}'";
            return false;
        }

        restartRecommended = slot.RestartRecommended;

        if (string.IsNullOrEmpty(ModPath))
        {
            error = "Mod path is not known yet, config was not loaded";
            logger.Error($"[UltimateFlea] Refusing to save {slot.FileName}: mod path is not known yet.");
            return false;
        }

        object staged;
        try
        {
            // Build and validate a fresh instance; the live config stays untouched until the
            // file on disk is actually written.
            staged = slot.FromBody(body);
            slot.Validate(this, staged);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            logger.Error($"[UltimateFlea] Invalid payload for {slot.FileName}: {ex.Message}", ex);
            return false;
        }

        if (!JsonFileStore.WriteAtomic(
                Path.Combine(ModPath, slot.FileName),
                staged,
                JsonFileStore.Config,
                logger,
                slot.FileName,
                out error))
        {
            return false;
        }

        lock (_publishGate)
        {
            slot.Set(this, staged);
        }

        logger.Success($"[UltimateFlea] Saved {slot.FileName}");
        return true;
    }

    private void LoadAllFromDisk()
    {
        var staged = new Dictionary<string, object>(Slots.Length, StringComparer.Ordinal);

        foreach (var slot in Slots)
        {
            var value = slot.ReadFile(this);
            slot.Validate(this, value);
            staged[slot.Name] = value;
        }

        lock (_publishGate)
        {
            foreach (var slot in Slots)
            {
                slot.Set(this, staged[slot.Name]);
            }
        }
    }

    private static ConfigSlot? FindSlot(string name) =>
        Slots.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    private T ReadConfigFile<T>(string relativeFile) where T : class, new() =>
        JsonFileStore.ReadOrDefault<T, ConfigManager>(
            Path.Combine(ModPath, relativeFile),
            JsonFileStore.Config,
            logger,
            relativeFile) ?? new T();

    private static T DeserializeBody<T>(JsonElement body)
    {
        var value = body.Deserialize<T>(JsonFileStore.Config);
        return value ?? throw new InvalidOperationException("Empty JSON body");
    }

    private void ValidateMod(ModConfig mod)
    {
        mod.PriceSource = string.IsNullOrWhiteSpace(mod.PriceSource) ? "local" : mod.PriceSource.Trim().ToLowerInvariant();
        if (mod.PriceSource is not ("local" or "tarkovdev" or "tarkovmarket"))
        {
            logger.Warning($"[UltimateFlea] Unknown priceSource '{mod.PriceSource}', expected local, tarkovdev or tarkovmarket.");
        }

        // An old mod.json has no liveFlea: derive it so behaviour is unchanged, and store the
        // concrete value so the settings UI and the next save always see a real boolean.
        mod.LiveFlea = mod.ResolveLiveFlea();
        mod.TarkovMarketApiKey = mod.TarkovMarketApiKey?.Trim() ?? string.Empty;
        mod.PriceFetchTimeoutSeconds = Math.Clamp(mod.PriceFetchTimeoutSeconds, 10, 1800);
    }

    private void ValidateEconomy(EconomyConfig e)
    {
        e.SimIntervalMinutes = Math.Max(1, e.SimIntervalMinutes);

        // Negative elasticity would invert demand and supply, flipping the entire market.
        e.PriceElasticity = ClampWarn(e.PriceElasticity, 0.0, 10.0, "priceElasticity");
        e.DemandPerBuy = ClampWarn(e.DemandPerBuy, 0.0, 1.0, "demandPerBuy");
        e.SupplyPerSell = ClampWarn(e.SupplyPerSell, 0.0, 1.0, "supplyPerSell");

        e.DecayPerTick = Clamp01(e.DecayPerTick);
        e.SettleSpeed = Clamp01(e.SettleSpeed);
        e.Noise = Math.Max(0, e.Noise);
        e.MinPriceFactor = Math.Max(0.01, e.MinPriceFactor);
        e.MaxPriceFactor = Math.Max(e.MinPriceFactor, e.MaxPriceFactor);
        e.Wipe.StartLengthDays = Math.Max(0, e.Wipe.StartLengthDays);
        e.Wipe.StartMultiplier = Math.Max(0.01, e.Wipe.StartMultiplier);
    }

    private void ValidatePricing(PricingConfig pricing)
    {
        if (pricing.GlobalMultiplier <= 0)
        {
            logger.Warning("[UltimateFlea] globalMultiplier must be > 0, resetting to 1.0");
            pricing.GlobalMultiplier = 1.0;
        }
    }

    private void ValidatePool(PoolConfig pool)
    {
        var mode = pool.Mode?.ToLowerInvariant();
        if (mode != "blacklist" && mode != "whitelist")
        {
            logger.Warning($"[UltimateFlea] Unknown pool mode '{pool.Mode}', defaulting to 'blacklist'");
            pool.Mode = "blacklist";
        }
    }

    private void ValidateEvents(EventsConfig events)
    {
        foreach (var ev in events.Events)
        {
            if (ev.EndDay < ev.StartDay)
            {
                logger.Warning($"[UltimateFlea] Event '{ev.Id}' has endDay < startDay, swapping");
                (ev.StartDay, ev.EndDay) = (ev.EndDay, ev.StartDay);
            }

            ev.StartDay = Math.Max(0, ev.StartDay);
            ev.EndDay = Math.Max(ev.StartDay, ev.EndDay);
            ev.Multiplier = Math.Max(0.01, ev.Multiplier);

            // endDay is an exclusive bound, so an empty range matches no day at all.
            if (ev.EndDay <= ev.StartDay)
            {
                logger.Warning(
                    $"[UltimateFlea] Event '{ev.Id}' has endDay <= startDay ({ev.StartDay}..{ev.EndDay}), it will never fire.");
            }
        }
    }

    private void ValidateTrends(TrendsConfig trends)
    {
        foreach (var trend in trends.Trends)
        {
            trend.PeriodDays = Math.Max(0.1, trend.PeriodDays);
            trend.Amplitude = Math.Clamp(trend.Amplitude, 0.0, 0.5);
        }
    }

    private double ClampWarn(double value, double min, double max, string name)
    {
        var clamped = Math.Clamp(value, min, max);
        if (clamped != value)
        {
            logger.Warning($"[UltimateFlea] {name} must be between {min} and {max}, clamping {value} to {clamped}");
        }

        return clamped;
    }

    private static double Clamp01(double v) => Math.Clamp(v, 0.0, 1.0);
}
