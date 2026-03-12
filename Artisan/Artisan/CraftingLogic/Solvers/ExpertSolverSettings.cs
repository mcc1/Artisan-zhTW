using Artisan.CraftingLogic.CraftData;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Dalamud.Interface.Components;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using System;
using static Artisan.RawInformation.AddonExtensions;

namespace Artisan.CraftingLogic.Solvers;

public class ExpertSolverSettings
{
    public bool MaxIshgardRecipes;
    public bool UseReflectOpener;
    public bool MuMeIntensiveGood = true; // if true, we allow spending mume on intensive (400p) rather than rapid (500p) if good condition procs
    public bool MuMeIntensiveMalleable = false; // if true and we have malleable during mume, use intensive rather than hoping for rapid
    public bool MuMeIntensiveLastResort = true; // if true and we're on last step of mume, use intensive (forcing via H&S if needed) rather than hoping for rapid (unless we have centered)
    public bool MuMePrimedManip = false; // if true, allow using primed manipulation after veneration is up on mume
    public bool MuMeAllowObserve = false; // if true, observe rather than use actions during unfavourable conditions to conserve durability
    public int MuMeMinStepsForManip = 2; // if this or less rounds are remaining on mume, don't use manipulation under favourable conditions
    public int MuMeMinStepsForVene = 1; // if this or less rounds are remaining on mume, don't use veneration
    public int MidMinIQForHSPrecise = 10; // min iq stacks where we use h&s+precise; 10 to disable
    public bool MidBaitPliantWithObservePreQuality = true; // if true, when very low on durability and without manip active during pre-quality phase, we use observe rather than normal manip
    public bool MidBaitPliantWithObserveAfterIQ = true; // if true, when very low on durability and without manip active after iq has 10 stacks, we use observe rather than normal manip or inno+finnesse
    public bool MidPrimedManipPreQuality = true; // if true, allow using primed manipulation during pre-quality phase
    public bool MidPrimedManipAfterIQ = true; // if true, allow using primed manipulation during after iq has 10 stacks
    public bool MidKeepHighDuraUnbuffed = true; // if true, observe rather than use actions during unfavourable conditions to conserve durability when no buffs are active
    public bool MidKeepHighDuraVeneration = false; // if true, observe rather than use actions during unfavourable conditions to conserve durability when veneration is active
    public bool MidAllowVenerationGoodOmen = true; // if true, we allow using veneration during iq phase if we lack a lot of progress on good omen
    public bool MidAllowVenerationAfterIQ = true; // if true, we allow using veneration after iq is fully stacked if we still lack a lot of progress
    public bool MidAllowIntensiveUnbuffed = false; // if true, we allow spending good condition on intensive if we still need progress when no buffs are active
    public bool MidAllowIntensiveVeneration = false; // if true, we allow spending good condition on intensive if we still need progress when veneration is active
    public bool MidAllowPrecise = true; // if true, we allow spending good condition on precise touch if we still need iq
    public bool MidAllowSturdyPreсise = false; // if true,we consider sturdy+h&s+precise touch a good move for building iq
    public bool MidAllowCenteredHasty = true; // if true, we consider centered hasty touch a good move for building iq (85% reliability)
    public bool MidAllowSturdyHasty = true; // if true, we consider sturdy hasty touch a good move for building iq (50% reliability), otherwise we use combo
    public bool MidAllowGoodPrep = true; // if true, we consider prep touch a good move for finisher under good+inno+gs
    public bool MidAllowSturdyPrep = true; // if true, we consider prep touch a good move for finisher under sturdy+inno
    public bool MidGSBeforeInno = true; // if true, we start quality combos with gs+inno rather than just inno
    public bool MidFinishProgressBeforeQuality = false; // if true, at 10 iq we first finish progress before starting on quality
    public bool MidObserveGoodOmenForTricks = false; // if true, we'll observe on good omen where otherwise we'd use tricks on good
    public bool FinisherBaitGoodByregot = true; // if true, use careful observations to try baiting good byregot
    public bool EmergencyCPBaitGood = false; // if true, we allow spending careful observations to try baiting good for tricks when we really lack cp
    public bool UseMaterialMiracle = false;

    public ExpertSolverSettings()
    {
        // 移除构造函数中的图标加载
    }

    public bool Draw()
    {
        ImGui.TextWrapped($"專家配方解算器並不是標準解算器的替代品。它僅用於專家配方。");
        ImGui.TextWrapped($"該解算器僅適用於製作日誌中標記為專家配方的食譜。");
        bool changed = false;
        ImGui.Indent();
        if (ImGui.CollapsingHeader("起手設定"))
        {
            changed |= ImGui.Checkbox($"使用 [{Skills.Reflect.NameOfAction()}] 代替 [{Skills.MuscleMemory.NameOfAction()}] 作為起手", ref UseReflectOpener);
            changed |= ImGui.Checkbox($"如果在使用 [{Skills.MuscleMemory.NameOfAction()}] 後處於 [{Condition.Good.ToLocalizedString()}] {ConditionString}，使用 [{Skills.IntensiveSynthesis.NameOfAction()}]（400%）而不是 [{Skills.RapidSynthesis.NameOfAction()}]（500%）", ref MuMeIntensiveGood);
            changed |= ImGui.Checkbox($"如果在 [{Skills.MuscleMemory.NameOfAction()}] 期間出現 [{Condition.Malleable.ToLocalizedString()}] {ConditionString}，使用 [{Skills.HeartAndSoul.NameOfAction()}] + [{Skills.IntensiveSynthesis.NameOfAction()}]", ref MuMeIntensiveMalleable);
            changed |= ImGui.Checkbox($"如果在 [{Skills.MuscleMemory.NameOfAction()}] 的最後一步時不是 [{Condition.Centered.ToLocalizedString()}] {ConditionString}，使用 [{Skills.IntensiveSynthesis.NameOfAction()}]（如果可以，強制使用 [{Skills.HeartAndSoul.NameOfAction()}]）", ref MuMeIntensiveLastResort);
            changed |= ImGui.Checkbox($"如果 [{Skills.Veneration.NameOfAction()}] 還處於啟用狀態，在 [{Condition.Primed.ToLocalizedString()}] {ConditionString}時使用 [{Skills.Manipulation.NameOfAction()}]。", ref MuMePrimedManip);
            changed |= ImGui.Checkbox($"在出現不利狀態時使用 [{Skills.Observe.NameOfAction()}]，而不是使用 [{Skills.RapidSynthesis.NameOfAction()}] 來消耗 {DurabilityString}。", ref MuMeAllowObserve);
            ImGui.Text($"僅當 [{Skills.MuscleMemory.NameOfAction()}] 的剩餘步驟超過此數量時，才允許使用 [{Skills.Manipulation.NameOfAction()}]。");
            ImGui.PushItemWidth(250);
            changed |= ImGui.SliderInt("###MumeMinStepsForManip", ref MuMeMinStepsForManip, 0, 5);
            ImGui.Text($"僅當 [{Skills.MuscleMemory.NameOfAction()}] 的剩餘步驟超過此數量時，才允許使用 [{Skills.Veneration.NameOfAction()}]。");
            ImGui.PushItemWidth(250);
            changed |= ImGui.SliderInt("###MuMeMinStepsForVene", ref MuMeMinStepsForVene, 0, 5);
        }
        if (ImGui.CollapsingHeader("主循環設定"))
        {
            ImGui.Text($"[{Buffs.InnerQuiet.NameOfBuff()}] 至少有多少層時才為 [{Skills.PreciseTouch.NameOfAction()}] 使用 [{Skills.HeartAndSoul.NameOfAction()}]（10為禁用）");
            ImGui.PushItemWidth(250);
            changed |= ImGui.SliderInt($"###MidMinIQForHSPrecise", ref MidMinIQForHSPrecise, 0, 10);
            changed |= ImGui.Checkbox($"低 {DurabilityString} 時，優先選擇 [{Skills.Observe.NameOfAction()}]，而不是在非-[{Condition.Pliant.ToLocalizedString()}] [{Skills.Manipulation.NameOfAction()}] 時使用它，在 {Buffs.InnerQuiet.NameOfBuff()} 有 10 層之前", ref MidBaitPliantWithObservePreQuality);
            changed |= ImGui.Checkbox($"低 {DurabilityString} 時，優先選擇 [{Skills.Observe.NameOfAction()}]，而不是在非-[{Condition.Pliant.ToLocalizedString()}] [{Skills.Manipulation.NameOfAction()}] / [{Skills.Innovation.NameOfAction()}]+[{Skills.TrainedFinesse.NameOfAction()}] 時使用它，在 {Buffs.InnerQuiet.NameOfBuff()} 有 10 層之後", ref MidBaitPliantWithObserveAfterIQ);
            changed |= ImGui.Checkbox($"在 [{Condition.Primed.ToLocalizedString()}] {ConditionString} 下使用 [{Skills.Manipulation.NameOfAction()}]，於 {Buffs.InnerQuiet.NameOfBuff()} 有 10 層之前", ref MidPrimedManipPreQuality);
            changed |= ImGui.Checkbox($"在 [{Condition.Primed.ToLocalizedString()}] {ConditionString} 下使用 [{Skills.Manipulation.NameOfAction()}]，於 {Buffs.InnerQuiet.NameOfBuff()} 有 10 層之後，且有足夠的 CP 可有效利用 {DurabilityString}", ref MidPrimedManipAfterIQ);
            changed |= ImGui.Checkbox($"在不利的 {ConditionString} 下允許使用 [{Skills.Observe.NameOfAction()}]，且目前沒有增益效果", ref MidKeepHighDuraUnbuffed);
            changed |= ImGui.Checkbox($"在不利的 {ConditionString} 下，若已有 {Buffs.Veneration.NameOfBuff()}，則允許使用 [{Skills.Observe.NameOfAction()}]", ref MidKeepHighDuraVeneration);
            changed |= ImGui.Checkbox($"如果我們在 [{Condition.GoodOmen.ToLocalizedString()}] 上還有大量 {ProgressString} 赤字（超過 [{Skills.IntensiveSynthesis.NameOfAction()}] 可補足的量），則允許使用 [{Skills.Veneration.NameOfAction()}]", ref MidAllowVenerationGoodOmen);
            changed |= ImGui.Checkbox($"在 {Buffs.InnerQuiet.NameOfBuff()} 有 10 層之後，如果我們在 [{Condition.GoodOmen.ToLocalizedString()}] 上還有大量 {ProgressString} 赤字（超過 [{Skills.RapidSynthesis.NameOfAction()}] 可補足的量），則允許使用 [{Skills.Veneration.NameOfAction()}]", ref MidAllowVenerationAfterIQ);
            changed |= ImGui.Checkbox($"在沒有增益效果的情況下，花費 [{Condition.Good.ToLocalizedString()}] {ConditionString} 來使用 [{Skills.IntensiveSynthesis.NameOfAction()}]，如果我們還需要更多 {ProgressString}", ref MidAllowIntensiveUnbuffed);
            changed |= ImGui.Checkbox($"在 [{Skills.Veneration.NameOfAction()}] 下，花費 [{Condition.Good.ToLocalizedString()}] {ConditionString} 來使用 [{Skills.IntensiveSynthesis.NameOfAction()}]，如果我們還需要更多 {ProgressString}", ref MidAllowIntensiveVeneration);
            changed |= ImGui.Checkbox($"如果我們還需要更多 {Buffs.InnerQuiet.NameOfBuff()} 層，花費 [{Condition.Good.ToLocalizedString()}] {ConditionString} 來使用 [{Skills.PreciseTouch.NameOfAction()}]", ref MidAllowPrecise);
            changed |= ImGui.Checkbox($"考慮在 [{Condition.Sturdy.ToLocalizedString()}] {ConditionString} 下使用 [{Skills.HeartAndSoul.NameOfAction()}] + [{Skills.PreciseTouch.NameOfAction()}]，作為累積 {Buffs.InnerQuiet.NameOfBuff()} 層的良好選擇", ref MidAllowSturdyPreсise);
            changed |= ImGui.Checkbox($"考慮在 [{Condition.Centered.ToLocalizedString()}] {ConditionString} 下使用 [{Skills.HastyTouch.NameOfAction()}]，作為累積 {Buffs.InnerQuiet.NameOfBuff()} 層的良好選擇（85% 成功率，10 {DurabilityString}）", ref MidAllowCenteredHasty);
            changed |= ImGui.Checkbox($"考慮在 [{Condition.Sturdy.ToLocalizedString()}] {ConditionString} 下使用 [{Skills.HastyTouch.NameOfAction()}]，作為累積 {Buffs.InnerQuiet.NameOfBuff()} 層的良好選擇（50% 成功率，5 {DurabilityString}）", ref MidAllowSturdyHasty);
            changed |= ImGui.Checkbox($"在 [{Condition.Good.ToLocalizedString()}] {ConditionString} + {Buffs.Innovation.NameOfBuff()} + {Buffs.GreatStrides.NameOfBuff()} 的情況下，考慮使用 [{Skills.PreparatoryTouch.NameOfAction()}]，前提是我們有足夠的 {DurabilityString}", ref MidAllowGoodPrep);
            changed |= ImGui.Checkbox($"在 [{Condition.Sturdy.ToLocalizedString()}] {ConditionString} + {Buffs.Innovation.NameOfBuff()} 的情況下，考慮使用 [{Skills.PreparatoryTouch.NameOfAction()}]，前提是我們有足夠的 {DurabilityString}", ref MidAllowSturdyPrep);
            changed |= ImGui.Checkbox($"在 [{Skills.Innovation.NameOfAction()}] + {QualityString} 组合之前使用 [{Skills.GreatStrides.NameOfAction()}]", ref MidGSBeforeInno);
            changed |= ImGui.Checkbox($"在開始 {QualityString} 階段之前完成 {ProgressString}", ref MidFinishProgressBeforeQuality);
            changed |= ImGui.Checkbox($"在 [{Condition.GoodOmen.ToLocalizedString()}] {ConditionString} 下使用 [{Skills.Observe.NameOfAction()}]，如果我們原本會在 [{Condition.Good.ToLocalizedString()}] {ConditionString} 上使用 [{Skills.TricksOfTrade.NameOfAction()}]", ref MidObserveGoodOmenForTricks);
        }
        ImGui.Unindent();
        changed |= ImGui.Checkbox("充分利用伊修加德重建配方，而不是只滿足最大品質斷點。", ref MaxIshgardRecipes);
        ImGuiComponents.HelpMarker("這將嘗試最大限度地提高品質，以獲得更多的技巧點。");
        changed |= ImGui.Checkbox($"終結技：使用 {Skills.CarefulObservation.NameOfAction()}，為 {Condition.Good.ToLocalizedString()} {ConditionString} 爭取一次 {Skills.ByregotsBlessing.NameOfAction()}", ref FinisherBaitGoodByregot);
        changed |= ImGui.Checkbox($"緊急情況：如果製作力不夠了，使用 {Skills.CarefulObservation.NameOfAction()}，為 [{Condition.Good.ToLocalizedString()}] {ConditionString} 爭取一次 {Skills.TricksOfTrade.NameOfAction()}", ref EmergencyCPBaitGood);
        changed |= ImGui.Checkbox($"在宇宙探索中使用材料奇蹟", ref UseMaterialMiracle);
        if (ImGuiEx.ButtonCtrl("重設高難度配方設定到預設狀態"))
        {
            P.Config.ExpertSolverConfig = new();
            changed |= true;
        }
        return changed;
    }
}
