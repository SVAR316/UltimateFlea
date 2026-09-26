using BepInEx;
using BepInEx.Logging;
using UltimateFlea.Client.Patches;

namespace UltimateFlea.Client
{
    [BepInPlugin("com.zloymolodoy.ultimateflea.client", "UltimateFlea Client", "1.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;

            new RagfairUpdateSettingsPatch().Enable();
            new SellLevelLockPatch().Enable();
            new BuyLevelLockShowPatch().Enable();
            new BuyLevelLockStatusPatch().Enable();
            new RagfairScreenLockSpritePatch().Enable();
            new CategoryLevelLabelPatch().Enable();
            new CategoryShowPatch().Enable();
        }
    }
}
