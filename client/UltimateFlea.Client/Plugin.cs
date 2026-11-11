using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UltimateFlea.Client.Patches;

namespace UltimateFlea.Client
{
    [BepInPlugin("com.zloymolodoy.ultimateflea", "UltimateFlea Client", "1.3.2")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> QuickListFromStash;

        private void Awake()
        {
            Log = Logger;

            QuickListFromStash = Config.Bind(
                "Selling",
                "QuickListFromStash",
                true,
                "Show UltimateFlea: List in stash context menu and list immediately");

            new RagfairUpdateSettingsPatch().Enable();
            new SellLevelLockPatch().Enable();
            new BuyLevelLockShowPatch().Enable();
            new BuyLevelLockStatusPatch().Enable();
            new RagfairScreenLockSpritePatch().Enable();
            new CategoryLevelLabelPatch().Enable();
            new CategoryShowPatch().Enable();
            new StashQuickListPatch().Enable();
            new StashQuickListButtonPatch().Enable();
        }
    }
}
