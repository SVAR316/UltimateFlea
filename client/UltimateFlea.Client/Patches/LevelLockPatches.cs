using System.Reflection;
using EFT;
using EFT.HandBook;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateFlea.Client.Patches
{
    public class RagfairUpdateSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RagFair), nameof(RagFair.UpdateSettings));
        }

        [PatchPostfix]
        public static void Postfix(Profile profile)
        {
            LevelLockClient.Profile = profile;
            LevelLockClient.Load();
        }
    }

    public class SellLevelLockPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RagFair), nameof(RagFair.CanBeSelectedAtRagfair));
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref bool __result, ref string error)
        {
            if (!__result || item == null)
            {
                return;
            }

            if (LevelLockClient.IsSellLocked(item.TemplateId.ToString(), out var required))
            {
                __result = false;
                error = LevelLockClient.LockedMessage(required);
            }
        }
    }

    public class BuyLevelLockShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OfferView), nameof(OfferView.Show));
        }

        [PatchPostfix]
        public static void Postfix(OfferView __instance)
        {
            CategoryLockIcon.CacheFromOffer(__instance);
            OfferLevelLockUi.Apply(__instance);
        }
    }

    // AvailableOfferStatus always sets the quest tooltip for Locked offers — replace with level text.
    public class BuyLevelLockStatusPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(OfferView), nameof(OfferView.AvailableOfferStatus));
        }

        [PatchPostfix]
        public static void Postfix(OfferView __instance)
        {
            OfferLevelLockUi.Apply(__instance);
        }
    }

    public class RagfairScreenLockSpritePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RagfairScreen), nameof(RagfairScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix(RagfairScreen __instance)
        {
            CategoryLockIcon.CacheFromGameObject(__instance._offerButtonLockIcon);
        }
    }

    internal static class OfferLevelLockUi
    {
        public static void Apply(OfferView view)
        {
            var offer = view?.Offer;
            if (offer?.Item == null || view.IsTraderOffer)
            {
                return;
            }

            if (!LevelLockClient.IsBuyLocked(offer.Item.TemplateId.ToString(), out var required))
            {
                return;
            }

            offer.Locked = true;

            if (view._lockedButton != null && !view._lockedButton.activeSelf)
            {
                view._lockedButton.SetActive(true);
            }

            var message = LevelLockClient.LockedMessage(required);
            var tooltip = ItemUiContext.Instance?.Tooltip;
            if (tooltip != null && view._hoverTooltipArea != null)
            {
                view._hoverTooltipArea.Init(tooltip, message, rawText: true);
                view._hoverTooltipArea.SetMessageText(message, rawText: true);
            }
        }
    }

    // Game lock sprite next to "(810) 30 ур." — no emoji.
    internal static class CategoryLockIcon
    {
        private const string ObjectName = "UltimateFleaLockIcon";
        private const float IconSize = 18f;
        private static Sprite _sprite;

        public static void CacheFromOffer(OfferView view)
        {
            if (_sprite != null || view == null)
            {
                return;
            }

            CacheFromGameObject(view._lockedButton);
        }

        public static void CacheFromGameObject(GameObject root)
        {
            if (_sprite != null || root == null)
            {
                return;
            }

            var images = root.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image != null && image.sprite != null)
                {
                    _sprite = image.sprite;
                    return;
                }
            }
        }

        public static void Apply(NodeBaseView view, bool locked)
        {
            if (view?.CategoryChildCount == null)
            {
                return;
            }

            var parent = view.CategoryChildCount.transform.parent;
            var existing = parent.Find(ObjectName);

            if (!locked || _sprite == null)
            {
                if (existing != null)
                {
                    existing.gameObject.SetActive(false);
                }

                return;
            }

            Image image;
            if (existing == null)
            {
                var go = new GameObject(ObjectName);
                go.AddComponent<RectTransform>();
                go.AddComponent<LayoutElement>();
                var imageComp = go.AddComponent<Image>();
                go.transform.SetParent(parent, false);
                go.transform.SetSiblingIndex(view.CategoryChildCount.transform.GetSiblingIndex());

                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(IconSize, IconSize);

                var layout = go.GetComponent<LayoutElement>();
                layout.minWidth = IconSize;
                layout.preferredWidth = IconSize;
                layout.minHeight = IconSize;
                layout.preferredHeight = IconSize;

                image = imageComp;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else
            {
                existing.gameObject.SetActive(true);
                image = existing.GetComponent<Image>();
            }

            image.sprite = _sprite;
            image.color = Color.white;
        }
    }

    public class CategoryLevelLabelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NodeBaseView), nameof(NodeBaseView.OffersCountUpdatedHandler));
        }

        [PatchPostfix]
        public static void Postfix(NodeBaseView __instance, HandbookNode node, EViewListType ___ViewListType)
        {
            ApplyCategoryLabel(__instance, node, ___ViewListType);
        }

        public static void ApplyCategoryLabel(NodeBaseView view, HandbookNode node, EViewListType viewListType)
        {
            if (view == null || view.CategoryChildCount == null || node?.Data == null
                || viewListType == EViewListType.Handbook)
            {
                return;
            }

            var data = node.Data;
            var locked = data.Item != null
                ? LevelLockClient.IsBuyLocked(data.Item.TemplateId.ToString(), out var required)
                : LevelLockClient.IsCategoryLocked(data.Id, out required);

            CategoryLockIcon.Apply(view, locked);

            if (!locked)
            {
                return;
            }

            var count = node.Count > 0 ? $"({node.Count})  " : string.Empty;
            view.CategoryChildCount.text = $"{count}{LevelLockClient.ShortLabel(required)}";
            view.CategoryChildCount.gameObject.SetActive(true);
        }
    }

    public class CategoryShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NodeBaseView), nameof(NodeBaseView.Show));
        }

        [PatchPostfix]
        public static void Postfix(NodeBaseView __instance, HandbookNode node, EViewListType viewListType)
        {
            CategoryLevelLabelPatch.ApplyCategoryLabel(__instance, node, viewListType);
        }
    }
}
