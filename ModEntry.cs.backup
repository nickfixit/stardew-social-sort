using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CustomSocialSort
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance = null!;
        internal Harmony Harmony = null!;

        private const string SaveKey = "npc-custom-order-v1";
        internal List<string> NpcCustomOrder = new();

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Harmony = new Harmony(this.ModManifest.UniqueID);

            // events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnToTitle;

            // patches
            Patches.SocialPagePatches.Apply(Harmony);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // nothing required here for now
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            try
            {
                var data = this.Helper.Data.ReadSaveData<Models.SaveData>(SaveKey);
                NpcCustomOrder = data?.Order ?? new List<string>();
                Monitor.Log($"Loaded custom order list ({NpcCustomOrder.Count} names).", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to load save data: {ex}", LogLevel.Warn);
                NpcCustomOrder = new List<string>();
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            try
            {
                this.Helper.Data.WriteSaveData(SaveKey, new Models.SaveData { Order = NpcCustomOrder });
                Monitor.Log($"Saved custom order list ({NpcCustomOrder.Count} names).", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to save data: {ex}", LogLevel.Warn);
            }
        }

        private void OnReturnToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            NpcCustomOrder.Clear();
        }

        /// <summary>Ensure our order list includes exactly the NPCs present, keeping custom order where possible.</summary>
        internal void NormalizeOrderAgainst(IEnumerable<string> currentNpcNames)
        {
            var set = new HashSet<string>(currentNpcNames);
            // keep only existing
            NpcCustomOrder.RemoveAll(n => !set.Contains(n));
            // append any missing (preserving current game order)
            foreach (var n in currentNpcNames)
                if (!NpcCustomOrder.Contains(n))
                    NpcCustomOrder.Add(n);
        }

        internal int IndexOf(string npcInternalName)
        {
            int idx = NpcCustomOrder.IndexOf(npcInternalName);
            return idx >= 0 ? idx : int.MaxValue; // unknown -> bottom
        }

        internal void MoveName(string name, int newIndex)
        {
            NpcCustomOrder.Remove(name);
            newIndex = Math.Clamp(newIndex, 0, NpcCustomOrder.Count);
            NpcCustomOrder.Insert(newIndex, name);
        }
    }
}