using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using UltimateFlea.Configs;

namespace UltimateFlea;

[Injectable(InjectionType.Singleton)]
public class ConfigManager(
    ISptLogger<ConfigManager> logger,
    ModHelper modHelper)
{
    public ModConfig Mod { get; private set; } = new();
    public PoolConfig Pool { get; private set; } = new();
    public PricingConfig Pricing { get; private set; } = new();
    public EconomyConfig Economy { get; private set; } = new();
    public EventsConfig Events { get; private set; } = new();
    public TrendsConfig Trends { get; private set; } = new();
    public LevelLocksConfig Levels { get; private set; } = new();

    public string ModPath { get; private set; } = string.Empty;

    public void Load()
    {
        ModPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        Mod = LoadOrDefault<ModConfig>("config/mod.json");
        Pool = LoadOrDefault<PoolConfig>("config/pool.json");
        Pricing = LoadOrDefault<PricingConfig>("config/pricing.json");
        Economy = LoadOrDefault<EconomyConfig>("config/economy.json");
        Events = LoadOrDefault<EventsConfig>("config/events.json");
        Trends = LoadOrDefault<TrendsConfig>("config/trends.json");
        Levels = LoadOrDefault<LevelLocksConfig>("config/levels.json");

        Validate();
    }

    private T LoadOrDefault<T>(string relativeFile) where T : new()
    {
        try
        {
            return modHelper.GetJsonDataFromFile<T>(ModPath, relativeFile);
        }
        catch (Exception ex)
        {
            logger.Warning($"[UltimateFlea] Could not read '{relativeFile}', using defaults. ({ex.Message})");
            return new T();
        }
    }

    private void Validate()
    {
        var e = Economy;

        e.SimIntervalMinutes = Math.Max(1, e.SimIntervalMinutes);
        e.DecayPerTick = Clamp01(e.DecayPerTick);
        e.SettleSpeed = Clamp01(e.SettleSpeed);
        e.Noise = Math.Max(0, e.Noise);
        e.MinPriceFactor = Math.Max(0.01, e.MinPriceFactor);
        e.MaxPriceFactor = Math.Max(e.MinPriceFactor, e.MaxPriceFactor);
        e.Wipe.StartLengthDays = Math.Max(0, e.Wipe.StartLengthDays);
        e.Wipe.StartMultiplier = Math.Max(0.01, e.Wipe.StartMultiplier);

        if (Pricing.GlobalMultiplier <= 0)
        {
            logger.Warning("[UltimateFlea] globalMultiplier must be > 0, resetting to 1.0");
            Pricing.GlobalMultiplier = 1.0;
        }

        var mode = Pool.Mode?.ToLowerInvariant();
        if (mode != "blacklist" && mode != "whitelist")
        {
            logger.Warning($"[UltimateFlea] Unknown pool mode '{Pool.Mode}', defaulting to 'blacklist'");
            Pool.Mode = "blacklist";
        }

        foreach (var ev in Events.Events)
        {
            if (ev.EndDay < ev.StartDay)
            {
                logger.Warning($"[UltimateFlea] Event '{ev.Id}' has endDay < startDay, swapping");
                (ev.StartDay, ev.EndDay) = (ev.EndDay, ev.StartDay);
            }

            ev.StartDay = Math.Max(0, ev.StartDay);
            ev.EndDay = Math.Max(ev.StartDay, ev.EndDay);
            ev.Multiplier = Math.Max(0.01, ev.Multiplier);
        }

        foreach (var trend in Trends.Trends)
        {
            trend.PeriodDays = Math.Max(0.1, trend.PeriodDays);
            trend.Amplitude = Math.Clamp(trend.Amplitude, 0.0, 0.5);
        }
    }

    private static double Clamp01(double v) => Math.Clamp(v, 0.0, 1.0);
}
