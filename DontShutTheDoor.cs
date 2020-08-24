using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace DvMod.DontShutTheDoor
{
    [EnableReloading]
    public static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();

            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload;

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value != enabled)
            {
                enabled = value;
            }
            return true;
        }

        static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.UnpatchAll(modEntry.Info.Id);
            return true;
        }

        public static void DebugLog(string message)
        {
            mod.Logger.Log(message);
        }
    }

    [HarmonyPatch(typeof(TrainPhysicsLod), "ToggleItems")]
    static class ToggleItemsPatch
    {
        static bool Prefix(TrainPhysicsLod __instance, bool on)
        {
            Main.DebugLog($"on = {on}, carItems = {__instance.carItems}");
            return true;
        }
    }
}
