using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using CustomSocialSort.Util;

namespace CustomSocialSort.Patches
{
    internal static class SocialPagePatches
    {
        // Per SocialPage instance UI state
        private class UiState
        {
            public ClickableTextureComponent? CustomButton;
            public bool CustomActive;
            public SocialPage.SocialEntry? DraggedEntry;
            public int DragOffsetY;
            public int DraggedIndex = -1;
            public int CachedDropIndex = -1;
        }

        private static readonly ConditionalWeakTable<SocialPage, UiState> _state = new();

        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Constructor(typeof(SocialPage), Type.EmptyTypes),
                postfix: new HarmonyMethod(typeof(SocialPagePatches), nameof(CtorPostfixAny))
            );            // also patch other SocialPage ctors (x,y,w,h)
            harmony.Patch(
                original: AccessTools.Constructor(typeof(SocialPage), new[] { typeof(int), typeof(int), typeof(int), typeof(int) }),
                postfix: new HarmonyMethod(typeof(SocialPagePatches), nameof(CtorPostfixAny))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(SocialPage), nameof(SocialPage.performHoverAction)),
                postfix: new HarmonyMethod(typeof(SocialPagePatches), nameof(HoverPostfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(SocialPage), nameof(SocialPage.receiveLeftClick)),
                prefix: new HarmonyMethod(typeof(SocialPagePatches), nameof(LeftClickPrefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(SocialPage), nameof(SocialPage.releaseLeftClick)),
                prefix: new HarmonyMethod(typeof(SocialPagePatches), nameof(ReleaseLeftClickPrefix))
            );

            // We use a Postfix overlay to avoid fully re-drawing the whole menu (safer across versions)
            harmony.Patch(
                original: AccessTools.Method(typeof(SocialPage), nameof(SocialPage.draw)),
                postfix: new HarmonyMethod(typeof(SocialPagePatches), nameof(DrawPostfix))
            );
        }        // --- helpers to access SocialPage internals safely ---

        private static List<SocialPage.SocialEntry>? GetSocialEntries(SocialPage page)
            => Reflector.GetField<List<SocialPage.SocialEntry>>(page, "socialEntries")
               ?? Reflector.GetFirstFieldOfType<List<SocialPage.SocialEntry>>(page);

        private static List<ClickableComponent>? GetCharacterSlots(SocialPage page)
            => Reflector.GetField<List<ClickableComponent>>(page, "characterSlots")
               ?? Reflector.GetFirstFieldOfType<List<ClickableComponent>>(page);

        private static ClickableTextureComponent? GetSortByNameButton(SocialPage page)
            => Reflector.GetField<ClickableTextureComponent>(page, "sortByNameButton");

        // Re-apply order to entries
        private static void ApplyCustomOrderToPage(SocialPage page, UiState state)
        {
            var entries = GetSocialEntries(page);
            if (entries is null) return;

            // ensure save list matches NPCs
            ModEntry.Instance.NormalizeOrderAgainst(entries.Select(e => e.friend.Name));

            entries.Sort((a, b) =>
            {
                int ia = ModEntry.Instance.IndexOf(a.friend.Name);
                int ib = ModEntry.Instance.IndexOf(b.friend.Name);
                if (ia != ib) return ia.CompareTo(ib);
                // stable-ish fallback
                return string.Compare(a.friend.displayName, b.friend.displayName, StringComparison.OrdinalIgnoreCase);
            });

            // try to call internal layout refresh if present (varies by versions)
            Reflector.CallMethod(page, "repositionScroll");
            Reflector.CallMethod(page, "setScrollBarToCurrentIndex");
        }        private static UiState S(SocialPage page) => _state.GetOrCreateValue(page);

        // --- patches ---

        // Patch 1: constructor Postfix — add Custom button & preload order
        private static void CtorPostfixAny(SocialPage __instance)
        {
            var st = S(__instance);

            // Base the new button's position on the Name sort button if available
            var nameBtn = GetSortByNameButton(__instance);
            int btnSize = 64; // common button size used in menu
            Rectangle src = new Rectangle(257, 284, 16, 16); // from Game1.mouseCursors as suggested

            int bx, by;
            if (nameBtn != null)
            {
                bx = nameBtn.bounds.Right + 8;
                by = nameBtn.bounds.Y;
            }
            else
            {
                // fallback: place near top-right corner of the page
                bx = __instance.xPositionOnScreen + __instance.width - (btnSize + 32);
                by = __instance.yPositionOnScreen + 64;
            }

            st.CustomButton = new ClickableTextureComponent(
                new Rectangle(bx, by, btnSize, btnSize),
                Game1.mouseCursors,
                src,
                4f
            )
            {
                hoverText = "Custom Sort"
            };

            // ensure in-memory order is normalized against current entries
            var entries = GetSocialEntries(__instance);
            if (entries != null)
                ModEntry.Instance.NormalizeOrderAgainst(entries.Select(e => e.friend.Name));
        }        // Patch 2: performHoverAction Postfix — hover text for custom button
        private static void HoverPostfix(SocialPage __instance, int x, int y)
        {
            var st = S(__instance);
            if (st.CustomButton != null && st.CustomButton.containsPoint(x, y))
            {
                __instance.hoverText = st.CustomActive ? "Custom Sort (active)" : "Custom Sort";
            }
        }

        // Patch 3: receiveLeftClick Prefix — toggle custom mode and start dragging
        private static bool LeftClickPrefix(SocialPage __instance, int x, int y, bool playSound)
        {
            var st = S(__instance);

            // click on our custom button
            if (st.CustomButton != null && st.CustomButton.containsPoint(x, y))
            {
                st.CustomActive = !st.CustomActive;
                st.DraggedEntry = null;
                st.DraggedIndex = -1;

                if (st.CustomActive)
                {
                    ApplyCustomOrderToPage(__instance, st);
                    Game1.playSound("smallSelect");
                }
                else
                {
                    Game1.playSound("trashcan");
                }
                // handled
                return false;
            }

            if (!st.CustomActive)
                return true; // let vanilla handle clicks

            // if custom is active, detect start-drag on a slot
            var entries = GetSocialEntries(__instance);
            var slots = GetCharacterSlots(__instance);
            if (entries == null || slots == null) return true;

            for (int i = 0; i < Math.Min(entries.Count, slots.Count); i++)
            {
                var slot = slots[i];
                if (slot.containsPoint(x, y))
                {
                    st.DraggedEntry = entries[i];
                    st.DraggedIndex = i;
                    st.DragOffsetY = y - slot.bounds.Y;
                    Game1.playSound("shwip");
                    // We handle this to avoid vanilla opening NPC profile etc.
                    return false;
                }
            }

            return true;
        }        // Patch 4: releaseLeftClick Prefix — drop & persist
        private static bool ReleaseLeftClickPrefix(SocialPage __instance)
        {
            var st = S(__instance);
            if (!st.CustomActive || st.DraggedEntry == null)
                return true; // let vanilla run

            var entries = GetSocialEntries(__instance);
            var slots = GetCharacterSlots(__instance);
            if (entries == null || slots == null) return true;

            // determine drop index under cursor
            int mouseY = Game1.getMouseY();
            int targetIndex = GetDropIndexFromY(slots, mouseY);

            // move in saved order using internal name
            string name = st.DraggedEntry.friend.Name;
            ModEntry.Instance.MoveName(name, targetIndex);

            // apply & save
            ApplyCustomOrderToPage(__instance, st);
            ModEntry.Instance.Helper.Data.WriteSaveData("npc-custom-order-v1", new Models.SaveData
            {
                Order = ModEntry.Instance.NpcCustomOrder
            });

            st.DraggedEntry = null;
            st.DraggedIndex = -1;
            st.CachedDropIndex = -1;

            Game1.playSound("coin");
            // handled
            return false;
        }        private static int GetDropIndexFromY(List<ClickableComponent> slots, int mouseY)
        {
            // pick index by scanning slot tops; if below all, append at end
            for (int i = 0; i < slots.Count; i++)
            {
                var r = slots[i].bounds;
                if (mouseY < r.Center.Y)
                    return i;
            }
            return slots.Count;
        }

        // Patch 5: draw Postfix — overlay: dragged ghost + drop indicator
        private static void DrawPostfix(SocialPage __instance, SpriteBatch b)
        {
            var st = S(__instance);
            if (!st.CustomActive) return;

            var slots = GetCharacterSlots(__instance);
            if (slots == null) return;

            // live drop index under cursor
            int dropIndex = GetDropIndexFromY(slots, Game1.getMouseY());
            st.CachedDropIndex = dropIndex;

            // draw drop indicator line
            if (dropIndex >= 0 && dropIndex <= slots.Count)
            {
                // y position = top of index slot, or bottom if at end
                int y;
                if (dropIndex == slots.Count)
                    y = slots[^1].bounds.Bottom; // after last
                else
                    y = slots[dropIndex].bounds.Top;

                DrawLine(b, x: slots[0].bounds.Left, y: y, width: slots[0].bounds.Width, thickness: 4, color: Color.Gold * 0.9f);
            }            // draw dragged ghost
            if (st.DraggedEntry != null)
            {
                int mx = Game1.getMouseX();
                int my = Game1.getMouseY() - st.DragOffsetY;
                var ghostRect = new Rectangle(
                    slots[0].bounds.Left,
                    my,
                    slots[0].bounds.Width,
                    slots[0].bounds.Height
                );

                // semi-transparent card
                b.Draw(Game1.staminaRect, ghostRect, Color.Black * 0.35f);

                // simple icon + name (keeps overlay safe across versions)
                string display = st.DraggedEntry.friend.displayName;
                b.DrawString(Game1.smallFont, display, new Vector2(ghostRect.X + 12, ghostRect.Y + 8), Color.White);

                // little heart hint
                var heartSrc = new Rectangle(211, 428, 7, 6);
                b.Draw(Game1.mouseCursors, new Rectangle(ghostRect.Right - 28, ghostRect.Y + 10, 21, 18), heartSrc, Color.White * 0.9f);
            }
        }

        private static void DrawLine(SpriteBatch b, int x, int y, int width, int thickness, Color color)
        {
            b.Draw(Game1.staminaRect, new Rectangle(x, y - thickness / 2, width, thickness), color);
        }
    }
}