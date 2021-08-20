using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityModManagerNet;

namespace DvMod.HoldTheDoor
{
    public static class Main
    {
        public static UnityModManager.ModEntry? mod;
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
                mod!.Logger.Log(message());
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

    public static class Patches
    {
        public static readonly List<TrainCar> carsWithLoadedInteriors = new List<TrainCar>();

        [HarmonyPatch(typeof(TrainCar), nameof(TrainCar.LoadInterior))]
        public static class LoadInteriorPatch
        {
            public static void Postfix(TrainCar __instance)
            {
                carsWithLoadedInteriors.Remove(__instance);
                carsWithLoadedInteriors.Add(__instance);
                Main.DebugLog(() => $"carsWithLoadedInteriors={string.Join(",", carsWithLoadedInteriors.Select(car => car.ID))}");
                if (carsWithLoadedInteriors.Count > Main.settings.maxInteriors)
                {
                    var carToUnload = carsWithLoadedInteriors[0];
                    Main.DebugLog(() => $"Unloading interior for {carToUnload.ID}");
                    carsWithLoadedInteriors.RemoveAt(0);
                    carToUnload.UnloadInterior();
                }
            }
        }

        [HarmonyPatch(typeof(TrainPhysicsLod), nameof(TrainPhysicsLod.SetLod))]
        public static class SetLodPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var inst in instructions)
                {
                    if (inst.Calls(AccessTools.DeclaredMethod(typeof(TrainCar), nameof(TrainCar.UnloadInterior))))
                        yield return new CodeInstruction(OpCodes.Pop).MoveLabelsFrom(inst);
                    else
                        yield return inst;
                }
            }
        }
    }
}
