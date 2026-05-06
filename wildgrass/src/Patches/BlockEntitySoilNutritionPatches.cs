using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Wildgrass;

[HarmonyPatch]
static class BlockEntitySoilNutrition_UpdatePatch
{
    static void WildgrassWeed(IBlockAccessor blockAccessor, int blockId, BlockPos abovePos)
    {
        Block block = blockAccessor.GetBlock(blockId);
        if(block.FirstCodePart() == "tallgrass") {
            var api = Traverse.Create(block).Field("api").GetValue<ICoreAPI>();
            var genWildgrassSystem = api.ModLoader.GetModSystem<GenWildgrass>();

            var climate = blockAccessor.GetClimateAt(abovePos, EnumGetClimateMode.WorldGenValues);

            float rainRel = climate.Rainfall;
            float tempRel = climate.Temperature;
            float forestRel = climate.ForestDensity;
            var species = genWildgrassSystem.SpeciesForPos(abovePos, rainRel, tempRel, forestRel);

            if(species != null) {
                blockAccessor.SetBlock(species.BlockIds[0], abovePos);
                return;
            }
        }
        blockAccessor.SetBlock(blockId, abovePos);
    }

    internal static MethodInfo TargetMethod()
    {
        var closures = AccessTools.FirstInner(
            typeof(BlockEntitySoilNutrition),
            t => t.Name.Contains("DisplayClass29_0"));
        return AccessTools.Method(closures, "<beginIntervalledUpdate>b__1");
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        try {
            var codeMatcher = new CodeMatcher(instructions).Start();
            var pos = codeMatcher
                .MatchStartForward(
                    CodeMatch.Calls( () => default(IBlockAccessor).SetBlock(default, default) )                    
                )
                .ThrowIfInvalid("Failed patch BlockEntitySoilNutrition.beginIntervalledUpdate delegate b__1")
                .Repeat((cm) =>
                {
                    cm.RemoveInstruction();
                    cm.InsertAndAdvance(
                        CodeInstruction.Call(() => WildgrassWeed(default, default, default)));
                }
                );


            var cminstructions = codeMatcher.Instructions();
            return cminstructions;
        } catch(Exception e) {
            WildgrassCore.Instance.api.Logger.Error($"Exception patching BlockEntitySoilNutrition.beginIntervalledUpdate delegate b__1: {e}");
            return instructions;
        }
    }
}