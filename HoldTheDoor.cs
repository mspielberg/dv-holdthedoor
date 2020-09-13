using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityModManagerNet;

namespace DvMod.HoldTheDoor
{
    public static class Main
    {
        public static UnityModManager.ModEntry mod;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            modEntry.OnToggle = OnToggle;
            return true;
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

        public static void DebugLog(string message)
        {
            mod.Logger.Log(message);
        }
    }

    [HarmonyPatch(typeof(TrainCar), "OnPlayerCarChanged")]
    public static class OnPlayerCarChangedPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Main.DebugLog("Patching TrainCar.OnPlayerCarChanged");
            List<CodeInstruction> insts = new List<CodeInstruction>(instructions);
            Label endLabel = insts[insts.Count - 1].labels[0];
            int i = insts.FindIndex(c => c.Calls(AccessTools.DeclaredMethod(typeof(TrainCar), "UnloadInterior")));
            insts.InsertRange(i - 1, new List<CodeInstruction>() {
                new CodeInstruction(OpCodes.Ldarg_1), // car
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnityEngine.Object), "op_Inequality")),
                new CodeInstruction(OpCodes.Brfalse_S, endLabel),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TrainCar), "IsLoco")),
                new CodeInstruction(OpCodes.Brfalse_S, endLabel)
            });
            return insts;
        }
    }
}
