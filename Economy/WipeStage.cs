using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using UltimateFlea.Configs;

namespace UltimateFlea.Economy;

// Множитель раннего вайпа от RegistrationDate, не от установки мода.
[Injectable(InjectionType.Singleton)]
public class WipeStage(
    ISptLogger<WipeStage> logger,
    ConfigManager configManager,
    SaveServer saveServer)
{
    public double GetMultiplier()
    {
        var wipe = configManager.Economy.Wipe;
        if (!wipe.Enabled || wipe.StartLengthDays <= 0)
        {
            return 1.0;
        }

        var registrationDate = GetOldestRegistrationDate();
        if (registrationDate is null)
        {
            return 1.0;
        }

        var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var ageSeconds = nowSeconds - registrationDate.Value;
        if (ageSeconds <= 0)
        {
            return 1.0;
        }

        var ageDays = ageSeconds / 86400.0;
        var progress = Math.Clamp(ageDays / wipe.StartLengthDays, 0.0, 1.0);
        var multiplier = wipe.StartMultiplier + (1.0 - wipe.StartMultiplier) * progress;

        if (configManager.Mod.Debug)
        {
            logger.Debug($"[UltimateFlea] Wipe stage: ageDays={ageDays:F2}, progress={progress:F2}, mult={multiplier:F3}");
        }

        return multiplier;
    }

    // Берём самый старый PMC. Если профилей несколько, экономика не скачет.
    private long? GetOldestRegistrationDate()
    {
        long? oldest = null;

        foreach (var (_, profile) in saveServer.GetProfiles())
        {
            var reg = profile.CharacterData?.PmcData?.Info?.RegistrationDate;
            if (reg is > 0)
            {
                long regSeconds = reg.Value;
                oldest = oldest is null ? regSeconds : Math.Min(oldest.Value, regSeconds);
            }
        }

        return oldest;
    }
}
