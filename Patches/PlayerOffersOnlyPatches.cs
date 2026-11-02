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

// Both patches stay separate, discoverable classes (Harmony needs that); only the target lookup
// and the prefix body are shared through PlayerOffersOnlyGate.
public class DisableDynamicOffersPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return PlayerOffersOnlyGate.GeneratorMethod(nameof(RagfairOfferGenerator.GenerateDynamicOffers));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return PlayerOffersOnlyGate.AllowGeneration();
    }
}

public class DisableTraderFleaOffersPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return PlayerOffersOnlyGate.GeneratorMethod(
            nameof(RagfairOfferGenerator.GenerateFleaOffersForTrader),
            [typeof(MongoId)]);
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return PlayerOffersOnlyGate.AllowGeneration();
    }
}

internal static class PlayerOffersOnlyGate
{
    private static ConfigManager? _config;

    public static void Bind(ConfigManager configManager) => _config = configManager;

    public static bool Enabled => _config?.Mod.PlayerOffersOnly == true;

    // Shared prefix body: skip generation only while player-offers-only mode is on.
    public static bool AllowGeneration() => !Enabled;

    public static MethodBase GeneratorMethod(string name, Type[]? parameterTypes = null)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

        return parameterTypes is null
            ? typeof(RagfairOfferGenerator).GetMethod(name, flags)!
            : typeof(RagfairOfferGenerator).GetMethod(name, flags, null, parameterTypes, null)!;
    }
}
