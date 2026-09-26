using System;
using EFT;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace UltimateFlea.Client
{
    public static class LevelLockClient
    {
        private const string RulesRoute = "/ultimateflea/levellocks";
        private const string LockedLocaleKey = "ultimateflea/LevelLocked{0}";
        private const string ShortLocaleKey = "ultimateflea/LevelShort{0}";

        public static LevelLockRules Rules { get; private set; } = new LevelLockRules();

        public static Profile Profile { get; set; }

        public static int PlayerLevel => Profile?.Info?.Level ?? 0;

        public static void Load()
        {
            try
            {
                var json = RequestHandler.GetJson(RulesRoute);
                Rules = JsonConvert.DeserializeObject<LevelLockRules>(json) ?? new LevelLockRules();
                Plugin.Log.LogInfo(
                    $"[UltimateFlea] Level locks: enabled={Rules.Enabled}, {Rules.Levels.Count} items, {Rules.Categories.Count} categories");
            }
            catch (Exception ex)
            {
                Rules = new LevelLockRules();
                Plugin.Log.LogWarning($"[UltimateFlea] Could not load level locks from server: {ex.Message}");
            }
        }

        public static bool IsBuyLocked(string tpl, out int requiredLevel)
        {
            return IsLocked(Rules.LockBuying, Rules.Levels, tpl, out requiredLevel);
        }

        public static bool IsSellLocked(string tpl, out int requiredLevel)
        {
            return IsLocked(Rules.LockSelling, Rules.Levels, tpl, out requiredLevel);
        }

        public static bool IsCategoryLocked(string handbookId, out int requiredLevel)
        {
            return IsLocked(true, Rules.Categories, handbookId, out requiredLevel);
        }

        public static string LockedMessage(int requiredLevel)
        {
            var template = LockedLocaleKey.Localized();
            if (string.IsNullOrEmpty(template) || template == LockedLocaleKey || template.StartsWith("ultimateflea/"))
            {
                template = "Торговля предметами данного типа будет доступна с {0} уровня";
            }

            return string.Format(template, requiredLevel);
        }

        public static string ShortLabel(int requiredLevel)
        {
            var template = ShortLocaleKey.Localized();
            if (string.IsNullOrEmpty(template) || template == ShortLocaleKey || template.StartsWith("ultimateflea/"))
            {
                template = "{0} ур.";
            }

            return string.Format(template, requiredLevel);
        }

        private static bool IsLocked(bool mode, System.Collections.Generic.Dictionary<string, int> table, string id, out int requiredLevel)
        {
            requiredLevel = 0;
            if (!Rules.Enabled || !mode || id == null || !table.TryGetValue(id, out requiredLevel))
            {
                return false;
            }

            return PlayerLevel < requiredLevel;
        }
    }
}
