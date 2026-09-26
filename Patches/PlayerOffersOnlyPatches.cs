using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators.Ragfair;
using SPTarkov.Server.Core.Models.Common;

namespace UltimateFlea.Patches;

[Injectable(TypePriority = OnLoadOrder.Preload)]
public class PlayerOffersOnlyPatchesLoad(ConfigManager configManager) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(configManager.ModPath))
        {
            configManager.Load();
        }

        PlayerOffersOnlyGate.Bind(configManager);
        new DisableDynamicOffersPatch().Enable();
        new DisableTraderFleaOffersPatch().Enable();
        return Task.CompletedTask;
    }
}

public class DisableDynamicOffersPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagfairOfferGenerator).GetMethod(
            nameof(RagfairOfferGenerator.GenerateDynamicOffers),
            BindingFlags.Instance | BindingFlags.Public)!;
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return !PlayerOffersOnlyGate.Enabled;
    }
}

public class DisableTraderFleaOffersPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagfairOfferGenerator).GetMethod(
            nameof(RagfairOfferGenerator.GenerateFleaOffersForTrader),
            BindingFlags.Instance | BindingFlags.Public,
            null,
            [typeof(MongoId)],
            null)!;
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return !PlayerOffersOnlyGate.Enabled;
    }
}

internal static class PlayerOffersOnlyGate
{
    private static ConfigManager? _config;

    public static void Bind(ConfigManager configManager) => _config = configManager;

    public static bool Enabled => _config?.Mod.PlayerOffersOnly == true;
}
