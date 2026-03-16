using Artisan.CraftingLists;
using Artisan.GameInterop;
using Artisan.RawInformation;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using ImGuiNET;
using System;

namespace Artisan.UI
{
    internal class ProcessingWindow : Window
    {
        public ProcessingWindow() : base("處理清單###ProcessingList", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
        {
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            SizeCondition = ImGuiCond.Appearing;
        }

        public override bool DrawConditions()
        {
            if (CraftingListUI.Processing)
                return true;

            return false;
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

        public unsafe override void Draw()
        {
            if (CraftingListUI.Processing)
            {
                CraftingListFunctions.ProcessList(CraftingListUI.selectedList);

                //if (ImGuiEx.AddHeaderIcon("OpenConfig", FontAwesomeIcon.Cog, new ImGuiEx.HeaderIconOptions() { Tooltip = "Open Config" }))
                //{
                //    P.PluginUi.IsOpen = true;
                //}

                ImGui.Text($"目前處理中：{CraftingListUI.selectedList.Name}");
                ImGui.Separator();
                ImGui.Spacing();
                if (CraftingListUI.CurrentProcessedItem != 0)
                {
                    ImGuiEx.TextV($"製作中：{LuminaSheets.RecipeSheet[CraftingListUI.CurrentProcessedItem].ItemResult.Value.Name.ToDalamudString().ToString()}");
                    ImGuiEx.TextV($"目前項目進度：{CraftingListUI.CurrentProcessedItemCount} / {CraftingListUI.CurrentProcessedItemListCount}");
                    ImGuiEx.TextV($"整體清單進度：{CraftingListFunctions.CurrentIndex + 1} / {CraftingListUI.selectedList.ExpandedList.Count}");

                    string duration = CraftingListFunctions.ListEndTime == TimeSpan.Zero ? "未知" : string.Format("{0}d {1}h {2}m {3}s", CraftingListFunctions.ListEndTime.Days, CraftingListFunctions.ListEndTime.Hours, CraftingListFunctions.ListEndTime.Minutes, CraftingListFunctions.ListEndTime.Seconds);
                    ImGuiEx.TextV($"預估剩餘時間：{duration}");

                }

                if (!CraftingListFunctions.Paused)
                {
                    if (ImGui.Button("暫停"))
                    {
                        CraftingListFunctions.Paused = true;
                        P.TM.Abort();
                        CraftingListFunctions.CLTM.Abort();
                        PreCrafting.Tasks.Clear();
                    }
                }
                else
                {
                    if (ImGui.Button("繼續"))
                    {
                        if (Crafting.CurState is Crafting.State.IdleNormal or Crafting.State.IdleBetween)
                        {
                            var recipe = LuminaSheets.RecipeSheet[CraftingListUI.CurrentProcessedItem];
                            PreCrafting.Tasks.Add((() => PreCrafting.TaskSelectRecipe(recipe), default));
                        }

                        CraftingListFunctions.Paused = false;
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("取消"))
                {
                    CraftingListUI.Processing = false;
                    CraftingListFunctions.Paused = false;
                    P.TM.Abort();
                    CraftingListFunctions.CLTM.Abort();
                    PreCrafting.Tasks.Clear();
                    Crafting.CraftFinished -= CraftingListUI.UpdateListTimer;
                }
            }
        }
    }
}
