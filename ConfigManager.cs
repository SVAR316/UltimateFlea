using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
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

    public string ModPath { get; private set; } = string.Empty;

    public void Load()
    {
        ModPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        Mod = LoadOrDefault<ModConfig>("config/mod.json");
        Pool = LoadOrDefault<PoolConfig>("config/pool.json");
        Pricing = LoadOrDefault<PricingConfig>("config/pricing.json");
        Economy = LoadOrDefault<EconomyConfig>("config/economy.json");

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
    }

    private static double Clamp01(double v) => Math.Clamp(v, 0.0, 1.0);
}
