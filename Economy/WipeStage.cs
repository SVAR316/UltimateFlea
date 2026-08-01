using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Servers;

namespace UltimateFlea.Economy;

// Early-wipe multiplier from RegistrationDate, not from mod install time.
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

        var ageDays = GetWipeAgeDays();
        if (ageDays is null)
        {
            return 1.0;
        }

        var progress = Math.Clamp(ageDays.Value / wipe.StartLengthDays, 0.0, 1.0);
        var multiplier = wipe.StartMultiplier + (1.0 - wipe.StartMultiplier) * progress;

        if (configManager.Mod.Debug)
        {
            logger.Debug($"[UltimateFlea] Wipe stage: ageDays={ageDays.Value:F2}, progress={progress:F2}, mult={multiplier:F3}");
        }

        return multiplier;
    }

    /// <summary>
    /// Days since the oldest PMC RegistrationDate. Null if no profile has one.
    /// Shared by wipe, market events, and category trends.
    /// </summary>
    public double? GetWipeAgeDays()
    {
        var registrationDate = GetOldestRegistrationDate();
        if (registrationDate is null)
        {
            return null;
        }

        var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var ageSeconds = nowSeconds - registrationDate.Value;
        if (ageSeconds <= 0)
        {
            return 0.0;
        }

        return ageSeconds / 86400.0;
    }

    // Oldest PMC so the economy does not jump when multiple profiles exist.
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
