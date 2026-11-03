using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.Trading;
using EFT.UI;
using EFT.UI.Ragfair;
using EFT.Utilities;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace UltimateFlea.Client.Patches
{
    internal static class QuickListState
    {
        public const string InteractionId = "ultimateflea_list";
        public const string RoubleTpl = "5449016a4bdc2d6f028b456f";
        public const double CacheTtlSeconds = 60d;

        private struct PriceEntry
        {
            public double Price;
            public DateTime At;
        }

        private static readonly Dictionary<string, PriceEntry> PriceCache = new Dictionary<string, PriceEntry>();

        // Only one context menu is open at a time; track the live dynamic entry.
        public static object CurrentInteraction;
        public static Transform CurrentButton;
        public static string CurrentButtonText;

        private static bool _reorderLogged;
        private static bool _iconLogged;

        public static bool IsRussian()
        {
            try
            {
                var culture = LocalizationManager.Instance?.Culture;
                if (!string.IsNullOrEmpty(culture))
                {
                    return culture.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { /* fall through */ }

            try
            {
                return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
            }
            catch
            {
                return false;
            }
        }

        public static string L(string ru, string en)
        {
            return IsRussian() ? ru : en;
        }

        public static string FormatMoney(double value)
        {
            return $"{value:N0} ₽";
        }

        public static double NormalizePrice(double avg)
        {
            return Math.Max(1d, Math.Round(avg));
        }

        public static string NeutralLabel()
        {
            return L("Выставить на барахолку", "List on flea");
        }

        public static string PriceLabel(double price)
        {
            return L("Выставить за ", "List for ") + FormatMoney(price);
        }

        public static bool TryGetFreshPrice(string tpl, out double price)
        {
            price = 0d;
            PriceEntry entry;
            if (tpl != null && PriceCache.TryGetValue(tpl, out entry)
                && (DateTime.UtcNow - entry.At).TotalSeconds < CacheTtlSeconds)
            {
                price = entry.Price;
                return true;
            }

            return false;
        }

        // Stale entries are fine for an initial label; a refresh is requested anyway.
        public static bool TryGetAnyPrice(string tpl, out double price)
        {
            price = 0d;
            PriceEntry entry;
            if (tpl != null && PriceCache.TryGetValue(tpl, out entry))
            {
                price = entry.Price;
                return true;
            }

            return false;
        }

        public static void RequestPrice(RagFair ragfair, string tpl, Action<double> onPrice)
        {
            ragfair.GetMarketPrices(tpl, prices =>
            {
                try
                {
                    var price = NormalizePrice(prices.avg);
                    PriceCache[tpl] = new PriceEntry { Price = price, At = DateTime.UtcNow };
                    onPrice(price);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[UltimateFlea] Price callback failed: {ex}");
                }
            });
        }

        public static void ApplyLabelToLiveButton(object interaction, string label)
        {
            if (interaction == null || !ReferenceEquals(interaction, CurrentInteraction))
            {
                return;
            }

            if (CurrentButton == null) // Unity null check: button destroyed
            {
                return;
            }

            var upper = label.ToUpper();
            foreach (var text in CurrentButton.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && string.Equals(text.text, CurrentButtonText, StringComparison.Ordinal))
                {
                    text.text = upper;
                    CurrentButtonText = upper;
                    return;
                }
            }
        }

        // ContextMenuButton keeps the enum name of its interaction (key.ToUpper(), e.g. "ADDOFFER")
        // in the private _iconLabel text. That is language independent, unlike the caption.
        private static readonly FieldInfo IconLabelField = AccessTools.Field(typeof(ContextMenuButton), "_iconLabel");

        /// <summary>
        /// Places the entry in the slot of the game's own "Add offer" button (right above it), which is
        /// where the reference quick-sell mod ends up because it rewrites that very button. Anchors are
        /// tried from most to least reliable and the last resort leaves the entry at the bottom, so the
        /// result never depends on a fixed menu layout (other plugins may add entries too).
        /// </summary>
        public static void MoveToPreferredSlot(Transform button, Transform container)
        {
            try
            {
                var index = FindIndexByKey(container, button, "ADDOFFER");
                if (index < 0)
                {
                    index = FindIndexByKey(container, button, "DISCARD");
                }

                if (index < 0)
                {
                    index = FindDiscardIndexByText(container, button);
                }

                if (index < 0)
                {
                    LogReorderOnce("No Add offer or Discard entry found in context menu; leaving entry at bottom");
                    return;
                }

                button.SetSiblingIndex(index);
            }
            catch (Exception ex)
            {
                LogReorderOnce("Reorder failed: " + ex.Message);
            }
        }

        private static int FindIndexByKey(Transform container, Transform button, string key)
        {
            if (IconLabelField == null)
            {
                return -1;
            }

            for (var i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                if (child == button)
                {
                    continue;
                }

                foreach (var menuButton in child.GetComponentsInChildren<ContextMenuButton>(true))
                {
                    var label = menuButton == null ? null : IconLabelField.GetValue(menuButton) as TMP_Text;
                    if (label != null && string.Equals(label.text, key, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        // Legacy fallback for builds where the key label is unavailable: match the localized caption.
        private static int FindDiscardIndexByText(Transform container, Transform button)
        {
            var candidates = new List<string>();
            foreach (var key in new[] { "Discard", "DISCARD", "Dispose", "DISPOSE" })
            {
                candidates.Add(key.Localized());
            }

            for (var i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                if (child == button)
                {
                    continue;
                }

                foreach (var text in child.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text == null || string.IsNullOrEmpty(text.text))
                    {
                        continue;
                    }

                    foreach (var c in candidates)
                    {
                        if (!string.IsNullOrEmpty(c)
                            && string.Equals(text.text.Trim(), c.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Context menu icons are plain sprites at "Characteristics/Icons/&lt;EItemInfoButton name&gt;"
        /// (InteractionButtonsContainer.Show loads them the same way). The game's own "AddOffer"
        /// entry is the flea-market action, so we reuse its sprite to look native. Fails soft: a
        /// null icon just means the button renders without one.
        /// </summary>
        public static Sprite ResolveIcon()
        {
            try
            {
                var sprite = ResourcesCache.Pop<Sprite>("Characteristics/Icons/" + EItemInfoButton.AddOffer);
                if (sprite != null) // Unity null check
                {
                    return sprite;
                }

                LogIconOnce("Icon sprite 'AddOffer' not found; entry shown without icon");
            }
            catch (Exception ex)
            {
                LogIconOnce("Icon resolve failed: " + ex.Message);
            }

            return null;
        }

        private static void LogIconOnce(string message)
        {
            if (_iconLogged)
            {
                return;
            }

            _iconLogged = true;
            Plugin.Log.LogDebug("[UltimateFlea] " + message);
        }

        private static void LogReorderOnce(string message)
        {
            if (_reorderLogged)
            {
                return;
            }

            _reorderLogged = true;
            Plugin.Log.LogDebug("[UltimateFlea] " + message);
        }
    }

    public class StashQuickListPatch : ModulePatch
    {
        private static readonly MethodInfo AddDynamic = AccessTools.Method(
            typeof(ContextInteractions<EItemInfoButton>),
            "method_2");

        private static readonly FieldInfo DynamicInteractionsField = AccessTools.Field(
            typeof(ContextInteractions<EItemInfoButton>),
            "_dynamicInteractions");

        private static readonly FieldInfo KeyField = AccessTools.Field(typeof(DynamicContextInteraction), "Key");

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Constructor(
                typeof(InventoryItemContextInteractions),
                new[]
                {
                    typeof(ItemContext),
                    typeof(ItemUiContext),
                    typeof(CompoundItem[]),
                    typeof(Action),
                });
        }

        [PatchPostfix]
        public static void Postfix(InventoryItemContextInteractions __instance, ItemContext itemContext)
        {
            try
            {
                if (!Plugin.QuickListFromStash.Value || AddDynamic == null || itemContext?.Item == null)
                {
                    return;
                }

                var item = itemContext.Item;
                var tpl = item.TemplateId.ToString();

                double cached;
                var label = QuickListState.TryGetAnyPrice(tpl, out cached)
                    ? QuickListState.PriceLabel(cached)
                    : QuickListState.NeutralLabel();

                AddDynamic.Invoke(__instance, new object[]
                {
                    QuickListState.InteractionId,
                    label,
                    (Action)(() => ListNow(item)),
                    QuickListState.ResolveIcon(),
                });

                RefreshLabel(__instance, tpl, label);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[UltimateFlea] Quick list menu entry failed: {ex}");
            }
        }

        private static void RefreshLabel(object instance, string tpl, string shownLabel)
        {
            try
            {
                object interaction = null;
                var dict = DynamicInteractionsField?.GetValue(instance) as IDictionary;
                if (dict != null && dict.Contains(QuickListState.InteractionId))
                {
                    interaction = dict[QuickListState.InteractionId];
                }

                double fresh;
                if (QuickListState.TryGetFreshPrice(tpl, out fresh))
                {
                    return;
                }

                var ragfair = ItemUiContext.Instance?.Session?.RagFair;
                if (ragfair == null)
                {
                    return;
                }

                QuickListState.RequestPrice(ragfair, tpl, price =>
                {
                    var newLabel = QuickListState.PriceLabel(price);
                    if (newLabel == shownLabel)
                    {
                        return;
                    }

                    if (interaction != null && KeyField != null)
                    {
                        KeyField.SetValue(interaction, newLabel);
                    }

                    QuickListState.ApplyLabelToLiveButton(interaction, newLabel);
                });
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[UltimateFlea] Quick list price label failed: {ex}");
            }
        }

        private static void ListNow(Item item)
        {
            try
            {
                var ui = ItemUiContext.Instance;
                if (ui?.Session?.RagFair == null)
                {
                    Warn(QuickListState.L("Барахолка недоступна", "Ragfair unavailable"));
                    return;
                }

                var ragfair = ui.Session.RagFair;
                var inventoryController = AccessTools.Field(typeof(ItemUiContext), "_inventoryController")
                    ?.GetValue(ui) as InventoryController;

                if (inventoryController == null)
                {
                    Warn(QuickListState.L("Контроллер инвентаря недоступен", "Inventory controller missing"));
                    return;
                }

                if (!RagFair.CanBeSelectedAtRagfair(item, inventoryController, out var error))
                {
                    Warn(string.IsNullOrEmpty(error)
                        ? QuickListState.L("Этот предмет нельзя выставить", "Cannot list this item")
                        : error);
                    return;
                }

                var tpl = item.TemplateId.ToString();
                double cached;
                if (QuickListState.TryGetFreshPrice(tpl, out cached))
                {
                    ListAt(ui, ragfair, item, cached);
                    return;
                }

                QuickListState.RequestPrice(ragfair, tpl, price =>
                {
                    try
                    {
                        ListAt(ui, ragfair, item, price);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogError($"[UltimateFlea] Quick list failed: {ex}");
                        Warn(ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[UltimateFlea] Quick list failed: {ex}");
                Warn(ex.Message);
            }
        }

        private static void ListAt(ItemUiContext ui, RagFair ragfair, Item item, double price)
        {
            var tax = Mathf.CeilToInt((float)PriceCalculator.CalculateTaxPrice(
                item,
                item.StackObjectsCount,
                price,
                sellInOnePiece: false));

            var requirement = new BarterTemplate
            {
                _tpl = QuickListState.RoubleTpl,
                count = price,
                onlyFunctional = false,
            };

            ui.Session.RagfairAddOffer(
                false,
                new[] { item.Id },
                new[] { requirement },
                (IResult result) =>
                {
                    if (result == null || result.Failed)
                    {
                        Warn(result?.Error ?? QuickListState.L("Не удалось выставить", "List failed"));
                        return;
                    }

                    NotificationManager.DisplayMessageNotification(
                        QuickListState.L("Выставлено: ", "Listed: ") + QuickListState.FormatMoney(price)
                        + QuickListState.L(" | Налог: ", " | Tax: ") + QuickListState.FormatMoney(tax),
                        ENotificationDurationType.Default,
                        ENotificationIconType.RagFair,
                        null);

                    try { ragfair.FilterMyOffers(); }
                    catch { /* ignore */ }
                });
        }

        private static void Warn(string message)
        {
            NotificationManager.DisplayWarningNotification(message, ENotificationDurationType.Default);
        }
    }

    /// <summary>
    /// Dynamic entries are always built after the standard ones (below the destructive "Discard").
    /// Once our button exists, move it next to the native Add offer entry and remember it so the price can be patched in.
    /// </summary>
    public class StashQuickListButtonPatch : ModulePatch
    {
        private static readonly FieldInfo ButtonsContainerField = AccessTools.Field(
            typeof(InteractionButtonsContainer),
            "_buttonsContainer");

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionButtonsContainer), "CreateDynamicContextButton");
        }

        [PatchPostfix]
        public static void Postfix(InteractionButtonsContainer __instance, DynamicContextInteraction __0)
        {
            try
            {
                if (__0 == null || __0.Id != QuickListState.InteractionId)
                {
                    return;
                }

                var container = ButtonsContainerField?.GetValue(__instance) as Transform;
                if (container == null || container.childCount == 0)
                {
                    return;
                }

                var button = container.GetChild(container.childCount - 1);
                QuickListState.CurrentInteraction = __0;
                QuickListState.CurrentButton = button;
                QuickListState.CurrentButtonText = __0.Key == null ? null : __0.Key.ToUpper();

                QuickListState.MoveToPreferredSlot(button, container);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"[UltimateFlea] Quick list button patch failed: {ex.Message}");
            }
        }
    }
}
