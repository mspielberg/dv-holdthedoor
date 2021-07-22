using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityModManagerNet;

namespace DvMod.HoldTheDoor
{
    public static class Main
    {
        public static UnityModManager.ModEntry mod;
        public static Settings settings = new Settings();

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;

            try
            {
                var loaded = Settings.Load<Settings>(modEntry);
                if (loaded.version == modEntry.Info.Version)
                    settings = loaded;
                else
                    settings = new Settings();
            }
            catch
            {
                settings = new Settings();
            }

            modEntry.OnGUI = OnGui;
            modEntry.OnSaveGUI = OnSaveGui;
            modEntry.OnToggle = OnToggle;
            return true;
        }

        private static void OnGui(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        private static void OnSaveGui(UnityModManager.ModEntry modEntry)
        {
            settings.Save();
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Harmony harmony = new Harmony(modEntry.Info.Id);
            if (value)
            {
                harmony.PatchAll();
            }
            else
            {
                harmony.UnpatchAll(modEntry.Info.Id);
            }
            return true;
        }

        public static void DebugLog(Func<string> message)
        {
            if (settings.enableLogging)
                mod.Logger.Log(message());
        }
    }

    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Max interiors", Min = 1)]
        public int maxInteriors = 1;
        [Draw("Enable logging")]
        public bool enableLogging = false;
        public readonly string? version = Main.mod?.Info.Version;

        public void Save()
        {
            Save(this, Main.mod);
        }

        public void OnChange() { }
    }

    [HarmonyPatch(typeof(TrainCar), "OnPlayerCarChanged")]
    public static class OnPlayerCarChangedPatch
    {
        private static List<TrainCar> carsWithLoadedInteriors = new List<TrainCar>();

        public static bool Prefix(TrainCar __instance, TrainCar car)
        {
            if (car == __instance && __instance.interiorPrefab != null)
            {
                carsWithLoadedInteriors.Remove(car);
                carsWithLoadedInteriors.Add(car);
                Main.DebugLog(() => $"carsWithLoadedInteriors={string.Join(",", carsWithLoadedInteriors.Select(car => car.ID))}");
                var excessInteriorCount = carsWithLoadedInteriors.Count - Main.settings.maxInteriors;
                if (excessInteriorCount > 0)
                {
                    for (int i = 0; i < excessInteriorCount; i++)
                    {
                        var carToUnload = carsWithLoadedInteriors[i];
                        Main.DebugLog(() => $"Unloading interior for {carToUnload.ID}");
                        carToUnload.UnloadInterior();
                    }
                    carsWithLoadedInteriors.RemoveRange(0, excessInteriorCount);
                }
                if (!__instance.IsInteriorLoaded)
                    __instance.LoadInterior();
            }
            return false;
        }
    }
}
