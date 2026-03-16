using Artisan.CraftingLogic;
using Artisan.CraftingLogic.Solvers;
using Artisan.GameInterop;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Numerics;

namespace Artisan.UI
{
    internal class MacroEditor : Window
    {
        private MacroSolverSettings.Macro SelectedMacro;
        private bool renameMode = false;
        private string renameMacro = "";
        private int selectedStepIndex = -1;
        private bool Raweditor = false;
        private static string _rawMacro = string.Empty;
        private bool raphael_cache = false;

        public MacroEditor(MacroSolverSettings.Macro macro, bool raphael_cache = false) : base($"巨集編輯器###{macro.ID}", ImGuiWindowFlags.None)
        {
            this.raphael_cache = raphael_cache;
            SelectedMacro = macro;
            selectedStepIndex = macro.Steps.Count - 1;
            this.IsOpen = true;
            P.ws.AddWindow(this);
            this.Size = new Vector2(600, 600);
            this.SizeCondition = ImGuiCond.Appearing;
            ShowCloseButton = true;

            Crafting.CraftStarted += OnCraftStarted;
        }

        public override void PreDraw()
        {
            if (!P.Config.DisableTheme)
            {
                P.Style.Push();
                P.StylePushed = true;
            }

        }

        public override void PostDraw()
        {
            if (P.StylePushed)
            {
                P.Style.Pop();
                P.StylePushed = false;
            }
        }

        public override void OnClose()
        {
            Crafting.CraftStarted -= OnCraftStarted;
            base.OnClose();
            P.ws.RemoveWindow(this);
        }

        public override void Draw()
        {
            if (SelectedMacro.ID != 0)
            {
                if (!renameMode)
                {
                    ImGui.TextUnformatted($"選中的巨集：{SelectedMacro.Name}");
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
                    {
                        renameMode = true;
                    }
                }
                else
                {
                    renameMacro = SelectedMacro.Name!;
                    if (ImGui.InputText("", ref renameMacro, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        SelectedMacro.Name = renameMacro;
                        P.Config.Save();

                        renameMode = false;
                        renameMacro = String.Empty;
                    }
                }
                if (ImGui.Button("刪除巨集（按住 Ctrl）") && ImGui.GetIO().KeyCtrl)
                {
                    if (raphael_cache)
                    {
                        var copy = P.Config.RaphaelSolverCacheV3.Where(kv => kv.Value == SelectedMacro);
                        //really should be just one but is it for sure??
                        foreach (var kv in copy)
                        {
                            P.Config.RaphaelSolverCacheV3.TryRemove(kv);
                        }
                    }
                    else
                    {
                        P.Config.MacroSolverConfig.Macros.Remove(SelectedMacro);
                        foreach (var e in P.Config.RecipeConfigs)
                            if (e.Value.SolverType == typeof(MacroSolverDefinition).FullName && e.Value.SolverFlavour == SelectedMacro.ID)
                                P.Config.RecipeConfigs.Remove(e.Key); // TODO: do we want to preserve other configs?..
                    }
                    P.Config.Save();
                    SelectedMacro = new();
                    selectedStepIndex = -1;

                    this.IsOpen = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("原始編輯器"))
                {
                    _rawMacro = string.Join("\r\n", SelectedMacro.Steps.Select(x => $"{x.Action.NameOfAction()}"));
                    Raweditor = !Raweditor;
                }

                ImGui.SameLine();
                var exportButton = ImGuiHelpers.GetButtonSize("匯出巨集");
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - exportButton.X);

                if (ImGui.Button("匯出巨集###ExportButton"))
                {
                    ImGui.SetClipboardText(JsonConvert.SerializeObject(SelectedMacro));
                    Notify.Success("巨集已複製到剪貼簿");
                }

                ImGui.Spacing();
                if (ImGui.Checkbox("如果品質已達 100%，則跳過品質技能", ref SelectedMacro.Options.SkipQualityIfMet))
                {
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker("一旦品質達到100%，巨集將跳過所有提升品質的技能，包括增益技能。");
                ImGui.SameLine();
                if (ImGui.Checkbox("若不是低品質，則跳過觀察", ref SelectedMacro.Options.SkipObservesIfNotPoor))
                {
                    P.Config.Save();
                }


                if (ImGui.Checkbox("升級加工技能", ref SelectedMacro.Options.UpgradeQualityActions))
                    P.Config.Save();
                ImGuiComponents.HelpMarker("若出現高品質或最高品質，且你的巨集目前正處於提升品質的步驟上（不包含比爾格的祝福），則會自動將技能升級為「集中加工」。");
                ImGui.SameLine();

                if (ImGui.Checkbox("升級製作技能", ref SelectedMacro.Options.UpgradeProgressActions))
                    P.Config.Save();
                ImGuiComponents.HelpMarker("若出現高品質或最高品質，且你的巨集目前正處於提升進展的步驟，則會自動將技能升級為「集中製作」。");

                ImGui.PushItemWidth(150f);
                if (ImGui.InputInt("最低作業精度", ref SelectedMacro.Options.MinCraftsmanship))
                    P.Config.Save();
                ImGuiComponents.HelpMarker("若在選用此巨集時未達最低作業精度，Artisan 將不會開始製作。");

                ImGui.PushItemWidth(150f);
                if (ImGui.InputInt("最低加工精度", ref SelectedMacro.Options.MinControl))
                    P.Config.Save();
                ImGuiComponents.HelpMarker("若在選用此巨集時未達最低加工精度，Artisan 將不會開始製作。");

                ImGui.PushItemWidth(150f);
                if (ImGui.InputInt("最低製作力", ref SelectedMacro.Options.MinCP))
                    P.Config.Save();
                ImGuiComponents.HelpMarker("若在選用此巨集時未達最低製作力，Artisan 將不會開始製作。");

                if (!Raweditor)
                {
                    if (ImGui.Button($"插入新技能：({Skills.BasicSynthesis.NameOfAction()})"))
                    {
                        SelectedMacro.Steps.Insert(selectedStepIndex + 1, new() { Action = Skills.BasicSynthesis });
                        ++selectedStepIndex;
                        P.Config.Save();
                    }

                    if (selectedStepIndex >= 0)
                    {
                        if (ImGui.Button($"插入新技能 - 與上一步相同：({SelectedMacro.Steps[selectedStepIndex].Action.NameOfAction()})"))
                        {
                            SelectedMacro.Steps.Insert(selectedStepIndex + 1, new() { Action = SelectedMacro.Steps[selectedStepIndex].Action });
                            ++selectedStepIndex;
                            P.Config.Save();
                        }
                    }


                    ImGui.Columns(2, "actionColumns", true);
                    ImGui.SetColumnWidth(0, 220f.Scale());
                    ImGuiEx.ImGuiLineCentered("###MacroActions", () => ImGuiEx.TextUnderlined("巨集技能"));
                    ImGui.Indent();
                    for (int i = 0; i < SelectedMacro.Steps.Count; i++)
                    {
                        var step = SelectedMacro.Steps[i];
                        var selectedAction = ImGui.Selectable($"{i + 1}. {(step.Action == Skills.None ? "Artisan Recommendation" : step.Action.NameOfAction())}{(step.HasExcludeCondition ? " | " : "")}{(step.HasExcludeCondition && step.ReplaceOnExclude ? step.ReplacementAction.NameOfAction() : step.HasExcludeCondition ? "Skip" : "")}###selectedAction{i}", i == selectedStepIndex);
                        if (selectedAction)
                            selectedStepIndex = i;
                    }
                    ImGui.Unindent();
                    if (selectedStepIndex >= 0)
                    {
                        var step = SelectedMacro.Steps[selectedStepIndex];

                        ImGui.NextColumn();
                        ImGuiEx.CenterColumnText($"選中的技能：{(step.Action == Skills.None ? "Artisan 建议" : step.Action.NameOfAction())}", true);
                        if (selectedStepIndex > 0)
                        {
                            ImGui.SameLine();
                            if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowLeft))
                            {
                                selectedStepIndex--;
                            }
                        }

                        if (selectedStepIndex < SelectedMacro.Steps.Count - 1)
                        {
                            ImGui.SameLine();
                            if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowRight))
                            {
                                selectedStepIndex++;
                            }
                        }

                        ImGui.Dummy(new Vector2(0, 0));
                        ImGui.SameLine();
                        if (ImGui.Checkbox($"此技能跳過升級", ref step.ExcludeFromUpgrade))
                            P.Config.Save();

                        ImGui.Spacing();
                        ImGuiEx.CenterColumnText($"跳過條件", true);

                        ImGui.BeginChild("ConditionalExcludes", new Vector2(ImGui.GetContentRegionAvail().X, step.HasExcludeCondition ? 200f : 100f), false, ImGuiWindowFlags.AlwaysAutoResize);
                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
                        ImGui.Columns(3, null, false);
                        if (ImGui.Checkbox($"普通", ref step.ExcludeNormal))
                            P.Config.Save();
                        if (ImGui.Checkbox($"低品質", ref step.ExcludePoor))
                            P.Config.Save();
                        if (ImGui.Checkbox($"高品質", ref step.ExcludeGood))
                            P.Config.Save();
                        if (ImGui.Checkbox($"最高品質", ref step.ExcludeExcellent))
                            P.Config.Save();

                        ImGui.NextColumn();

                        if (ImGui.Checkbox($"安定", ref step.ExcludeCentered))
                            P.Config.Save();
                        if (ImGui.Checkbox($"結實", ref step.ExcludeSturdy))
                            P.Config.Save();
                        if (ImGui.Checkbox($"高效", ref step.ExcludePliant))
                            P.Config.Save();
                        if (ImGui.Checkbox($"大進展", ref step.ExcludeMalleable))
                            P.Config.Save();

                        ImGui.NextColumn();

                        if (ImGui.Checkbox($"長持續", ref step.ExcludePrimed))
                            P.Config.Save();
                        if (ImGui.Checkbox($"高品質", ref step.ExcludeGoodOmen))
                            P.Config.Save();

                        ImGui.Columns(1);
                        ImGui.PopStyleVar();

                        if (step.HasExcludeCondition)
                        {
                            ImGuiEx.CenterColumnText($"排除選項", true);
                            if (ImGui.Checkbox($"不要略過時，改為使用：", ref step.ReplaceOnExclude))
                                P.Config.Save();

                            if (step.ReplaceOnExclude)
                            {
                                if (ImGui.BeginCombo("###Select Replacement", step.ReplacementAction.NameOfAction()))
                                {
                                    if (ImGui.Selectable($"Artisan 建議"))
                                    {
                                        step.ReplacementAction = Skills.None;
                                        P.Config.Save();
                                    }

                                    ImGuiComponents.HelpMarker("使用適合的預設解算器建議，也就是一般配方使用標準配方解算器，專家配方使用專家配方解算器。");

                                    if (ImGui.Selectable($"加工連段"))
                                    {
                                        step.ReplacementAction = Skills.TouchCombo;
                                        P.Config.Save();
                                    }

                                    ImGuiComponents.HelpMarker("會根據實際上一個使用的技能，自動接上三段式加工連段中適合的步驟。對於升級品質技能或依條件略過時特別有用。");

                                    if (ImGui.Selectable($"加工連段（精修加工路線）"))
                                    {
                                        step.ReplacementAction = Skills.TouchComboRefined;
                                        P.Config.Save();
                                    }

                                    ImGuiComponents.HelpMarker($"與另一種加工連段類似，會根據前一個使用的技能，在基本加工與精修加工之間切換。");

                                    ImGui.Separator();

                                    foreach (var opt in Enum.GetValues(typeof(Skills)).Cast<Skills>().OrderBy(y => y.NameOfAction()))
                                    {
                                        if (ImGui.Selectable(opt.NameOfAction()))
                                        {
                                            step.ReplacementAction = opt;
                                            P.Config.Save();
                                        }
                                    }

                                    ImGui.EndCombo();
                                }
                            }
                        }
                        ImGui.EndChild();

                        if (ImGui.Button("刪除技能（按住 Ctrl）") && ImGui.GetIO().KeyCtrl)
                        {
                            SelectedMacro.Steps.RemoveAt(selectedStepIndex);
                            P.Config.Save();
                            if (selectedStepIndex == SelectedMacro.Steps.Count)
                                selectedStepIndex--;
                        }

                        if (ImGui.BeginCombo("###ReplaceAction", "替換技能"))
                        {
                            if (ImGui.Selectable($"Artisan 建議"))
                            {
                                step.Action = Skills.None;
                                P.Config.Save();
                            }

                            ImGuiComponents.HelpMarker("使用適合的預設解算器建議，也就是一般配方使用標準配方解算器，專家配方使用專家配方解算器。");

                            if (ImGui.Selectable($"加工連攜"))
                            {
                                step.Action = Skills.TouchCombo;
                                P.Config.Save();
                            }

                            ImGuiComponents.HelpMarker("這將根據最後實際使用的技能使用3步加工連攜的適當步驟。對於提高品質的技能或跳過條件非常有用。");

                            if (ImGui.Selectable($"加工連段（精修加工路線）"))
                            {
                                step.Action = Skills.TouchComboRefined;
                                P.Config.Save();
                            }

                            ImGuiComponents.HelpMarker($"與另一種加工連段類似，會根據前一個使用的技能，在基本加工與精修加工之間切換。");

                            ImGui.Separator();

                            foreach (var opt in Enum.GetValues(typeof(Skills)).Cast<Skills>().OrderBy(y => y.NameOfAction()))
                            {
                                if (ImGui.Selectable(opt.NameOfAction()))
                                {
                                    step.Action = opt;
                                    P.Config.Save();
                                }
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.Text("重新排序技能");
                        if (selectedStepIndex > 0)
                        {
                            ImGui.SameLine();
                            if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUp))
                            {
                                SelectedMacro.Steps.Reverse(selectedStepIndex - 1, 2);
                                selectedStepIndex--;
                                P.Config.Save();
                            }
                        }

                        if (selectedStepIndex < SelectedMacro.Steps.Count - 1)
                        {
                            ImGui.SameLine();
                            if (selectedStepIndex == 0)
                            {
                                ImGui.Dummy(new Vector2(22));
                                ImGui.SameLine();
                            }

                            if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowDown))
                            {
                                SelectedMacro.Steps.Reverse(selectedStepIndex, 2);
                                selectedStepIndex++;
                                P.Config.Save();
                            }
                        }

                    }
                    ImGui.Columns(1);
                }
                else
                {
                    ImGui.Text($"巨集操作（一個操作一行）");
                    ImGuiComponents.HelpMarker("您可以像複製普通遊戲巨集一樣直接複製/黏貼巨集，也可以每行單獨列出每個動作。\n例如：\n/ac 堅信\n等同於\n堅信\n您也可以使用 *（星號）或「Artisan Recommendation」插入 Artisan 建議作為步驟。");
                    ImGui.InputTextMultiline("###MacroEditor", ref _rawMacro, 10000000, new Vector2(ImGui.GetContentRegionAvail().X - 30f, ImGui.GetContentRegionAvail().Y - 30f));
                    if (ImGui.Button("保存"))
                    {
                        var steps = MacroUI.ParseMacro(_rawMacro);
                        if (steps.Count > 0 && !SelectedMacro.Steps.SequenceEqual(steps))
                        {
                            selectedStepIndex = steps.Count - 1;
                            SelectedMacro.Steps = steps;
                            P.Config.Save();
                            DuoLog.Information($"巨集已更新");
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("保存並關閉"))
                    {
                        var steps = MacroUI.ParseMacro(_rawMacro);
                        if (steps.Count > 0 && !SelectedMacro.Steps.SequenceEqual(steps))
                        {
                            selectedStepIndex = steps.Count - 1;
                            SelectedMacro.Steps = steps;
                            P.Config.Save();
                            DuoLog.Information($"巨集已更新");
                        }

                        Raweditor = !Raweditor;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("關閉"))
                    {
                        Raweditor = !Raweditor;
                    }
                }


                ImGuiEx.ImGuiLineCentered("MTimeHead", delegate
                {
                    ImGuiEx.TextUnderlined($"巨集時長估算");
                });
                ImGuiEx.ImGuiLineCentered("MTimeArtisan", delegate
                {
                    ImGuiEx.Text($"Artisan 巨集耗時：{MacroUI.GetMacroLength(SelectedMacro)} 秒");
                });
                ImGuiEx.ImGuiLineCentered("MTimeTeamcraft", delegate
                {
                    ImGuiEx.Text($"普通巨集耗時：{MacroUI.GetTeamcraftMacroLength(SelectedMacro)} 秒");
                });
            }
            else
            {
                selectedStepIndex = -1;
            }
        }

        private void OnCraftStarted(Lumina.Excel.Sheets.Recipe recipe, CraftState craft, StepState initialStep, bool trial) => IsOpen = false;
    }
}
