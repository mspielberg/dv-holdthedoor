using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityModManagerNet;

namespace DvMod.HoldTheDoor
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

    [HarmonyPatch(typeof(TrainCar), "OnPlayerCarChanged")]
    static class OnPlayerCarChangedPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //Main.DebugLog($"before:{instructions.Aggregate("",(a,b)=>a+"\n"+b)}");
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
            //Main.DebugLog($"after:{insts.Aggregate("",(a,b)=>a+"\n"+b)}");
            return insts;
        }
    }
}
