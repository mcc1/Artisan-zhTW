using Artisan.Autocraft;
using Artisan.CraftingLists;
using Artisan.CraftingLogic;
using Artisan.FCWorkshops;
using Artisan.RawInformation;
using Artisan.RawInformation.Character;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Lumina.Excel.Sheets;
using PunishLib.ImGuiMethods;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ThreadLoadImageHandler = ECommons.ImGuiMethods.ThreadLoadImageHandler;

namespace Artisan.UI
{
    unsafe internal class PluginUI : Window
    {
        public event EventHandler<bool>? CraftingWindowStateChanged;


        private bool visible = false;
        public OpenWindow OpenWindow { get; set; }

        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        private bool craftingVisible = false;
        public bool CraftingVisible
        {
            get { return this.craftingVisible; }
            set { if (this.craftingVisible != value) CraftingWindowStateChanged?.Invoke(this, value); this.craftingVisible = value; }
        }

        public PluginUI() : base($"{P.Name} {P.GetType().Assembly.GetName().Version}###Artisan")
        {
            this.RespectCloseHotkey = false;
            this.SizeConstraints = new()
            {
                MinimumSize = new(250, 100),
                MaximumSize = new(9999, 9999)
            };
            P.ws.AddWindow(this);
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

        public void Dispose()
        {

        }

        public override void Draw()
        {
            if (DalamudInfo.IsOnStaging())
            {
                var scale = ImGui.GetIO().FontGlobalScale;
                ImGui.GetIO().FontGlobalScale = scale * 1.5f;
                using (var f = ImRaii.PushFont(ImGui.GetFont()))
                {
                    ImGuiEx.TextWrapped($"聽著，兄弟，你現在用的是Dalamud的測試版，遇到任何問題都可能是在Dalamud測試版上特有的，跟Artisan無關。這個插件不是為測試版開發的，所以除非問題出現在Dalamud正式版中，否則別指望我會修復。");
                    ImGui.Separator();

                    ImGui.Spacing();
                    ImGui.GetIO().FontGlobalScale = scale;
                }

            }
            var region = ImGui.GetContentRegionAvail();
            var itemSpacing = ImGui.GetStyle().ItemSpacing;

            var topLeftSideHeight = region.Y;

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f.Scale(), 0));
            try
            {
                ShowEnduranceMessage();

                using (var table = ImRaii.Table($"ArtisanTableContainer", 2, ImGuiTableFlags.Resizable))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);

                    ImGui.TableNextColumn();

                    var regionSize = ImGui.GetContentRegionAvail();

                    ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
                    using (var leftChild = ImRaii.Child($"###ArtisanLeftSide", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                    {
                        var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/artisan-icon.png");

                        if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var logo))
                        {
                            ImGuiEx.LineCentered("###ArtisanLogo", () =>
                            {
                                ImGui.Image(logo.ImGuiHandle, new(100f.Scale(), 100f.Scale()));
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text($"真棒！你是第69個發現這個秘密的人。");
                                    ImGui.EndTooltip();
                                }
                            });

                        }
                        ImGui.Spacing();
                        ImGui.Separator();

                        if (ImGui.Selectable("插件概述", OpenWindow == OpenWindow.Overview))
                        {
                            OpenWindow = OpenWindow.Overview;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("插件設定", OpenWindow == OpenWindow.Main))
                        {
                            OpenWindow = OpenWindow.Main;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("耐力模式", OpenWindow == OpenWindow.Endurance))
                        {
                            OpenWindow = OpenWindow.Endurance;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("巨集", OpenWindow == OpenWindow.Macro))
                        {
                            OpenWindow = OpenWindow.Macro;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("Raphael 快取", OpenWindow == OpenWindow.RaphaelCache))
                        {
                            OpenWindow = OpenWindow.RaphaelCache;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("配方分配器", OpenWindow == OpenWindow.Assigner))
                        {
                            OpenWindow = OpenWindow.Assigner;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("製作清單", OpenWindow == OpenWindow.Lists))
                        {
                            OpenWindow = OpenWindow.Lists;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("清單生成器", OpenWindow == OpenWindow.SpecialList))
                        {
                            OpenWindow = OpenWindow.SpecialList;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("部隊工房", OpenWindow == OpenWindow.FCWorkshop))
                        {
                            OpenWindow = OpenWindow.FCWorkshop;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("模擬器", OpenWindow == OpenWindow.Simulator))
                        {
                            OpenWindow = OpenWindow.Simulator;
                        }
                        ImGui.Spacing();
                        if (ImGui.Selectable("關於", OpenWindow == OpenWindow.About))
                        {
                            OpenWindow = OpenWindow.About;
                        }


#if DEBUG
                        ImGui.Spacing();
                        if (ImGui.Selectable("调试", OpenWindow == OpenWindow.Debug))
                        {
                            OpenWindow = OpenWindow.Debug;
                        }
                        ImGui.Spacing();
#endif

                    }

                    ImGui.PopStyleVar();
                    ImGui.TableNextColumn();
                    using (var rightChild = ImRaii.Child($"###ArtisanRightSide", Vector2.Zero, false))
                    {
                        switch (OpenWindow)
                        {
                            case OpenWindow.Main:
                                DrawMainWindow();
                                break;
                            case OpenWindow.Endurance:
                                Endurance.Draw();
                                break;
                            case OpenWindow.Lists:
                                CraftingListUI.Draw();
                                break;
                            case OpenWindow.About:
                                AboutTab.Draw("Artisan");
                                break;
                            case OpenWindow.Debug:
                                DebugTab.Draw();
                                break;
                            case OpenWindow.Macro:
                                MacroUI.Draw();
                                break;
                            case OpenWindow.RaphaelCache:
                                RaphaelCacheUI.Draw();
                                break;
                            case OpenWindow.Assigner:
                                AssignerUI.Draw();
                                break;
                            case OpenWindow.FCWorkshop:
                                FCWorkshopUI.Draw();
                                break;
                            case OpenWindow.SpecialList:
                                SpecialLists.Draw();
                                break;
                            case OpenWindow.Overview:
                                DrawOverview();
                                break;
                            case OpenWindow.Simulator:
                                SimulatorUI.Draw();
                                break;
                            case OpenWindow.None:
                                break;
                            default:
                                break;
                        }
                        ;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            ImGui.PopStyleVar();
        }

        private void DrawOverview()
        {
            var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/artisan.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var logo))
            {
                ImGuiEx.LineCentered("###ArtisanTextLogo", () =>
                {
                    ImGui.Image(logo.ImGuiHandle, new Vector2(logo.Width, 100f.Scale()));
                });
            }

            ImGuiEx.LineCentered("###ArtisanOverview", () =>
            {
                ImGuiEx.TextUnderlined("Artisan - 概述");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"首先感謝你下載我的這個小小的生產插件。自2022年6月以來，我一直在開發Artisan，這是我的一系列插件中的代表作。");
            ImGuiEx.TextWrapped($"這是免費內容，不應該透過其他管道花錢取得。");
            ImGui.Spacing();
            ImGuiEx.TextWrapped($"在開始使用Artisan之前，我們應該先了解一下插件的工作原理。一旦你了解了幾個關鍵因素，Artisan就很容易使用。");

            ImGui.Spacing();
            ImGuiEx.LineCentered("###ArtisanModes", () =>
            {
                ImGuiEx.TextUnderlined("製作模式");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan 具有\"自動製作模式\"，該模式僅接受內建解算器給出的建議，並代替你自動操作。" +
"預設情況下，它會以遊戲允許的間隔速度使用製作技能，這比使用遊戲內的巨集更快。" +
"使用它並沒有繞過任何形式的遊戲限制，但如果你打算讓製作速度慢一些，也可以設定延遲。" +
"啟用此選項不影響 Artisan 預設使用的建議生成過程。");

            var automode = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/AutoMode.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(automode, out var example))
            {
                ImGuiEx.LineCentered("###AutoModeExample", () =>
                {
                    ImGui.Image(example.ImGuiHandle, new Vector2(example.Width, example.Height));
                });
            }

            ImGuiEx.TextWrapped($"若未啟用自動模式，你還可以使用另外兩種模式：「半自動模式」與「全手動模式」。" +
                                $"\"半自動模式\"將在你開始製作作業時，出現在一個彈出的小視窗中。");

            var craftWindowExample = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/ThemeCraftingWindowExample.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(craftWindowExample, out example))
            {
                ImGuiEx.LineCentered("###CraftWindowExample", () =>
                {
                    ImGui.Image(example.ImGuiHandle, new Vector2(example.Width, example.Height));
                });
            }

            ImGuiEx.TextWrapped($"點擊「執行建議的操作」按鈕後，便會使用插件目前建議的製作技能。" +
                $"這被認為是半自動的，因為你仍然需要每一步點擊一次操作按鈕，但不必去快速鍵欄上找到相應的技能。" +
                $"\"全手動模式\"是透過正常按下快速鍵欄上的技能來執行的。" +
                $"預設情況下，你將得到輔助；如果你將技能放在快速鍵欄上，Artisan 也會對相應技能進行高亮提示（可在設定中停用）。");

            var outlineExample = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/OutlineExample.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(outlineExample, out example))
            {
                ImGuiEx.LineCentered("###OutlineExample", () =>
                {
                    ImGui.Image(example.ImGuiHandle, new Vector2(example.Width, example.Height));
                });
            }

            ImGui.Spacing();
            ImGuiEx.LineCentered("###ArtisanSuggestions", () =>
            {
                ImGuiEx.TextUnderlined("解算器/巨集");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"預設情況下，Artisan 會為你提供下一步製作技能的建議。然而，這個解算器並不完美，無法取代一套合適的生產裝備。" +
                $"除了啟用Artisan之外，你無需執行任何操作。" +
                $"\r\n\r\n" +
                $"若你正在嘗試處理預設解算器無法完成的製作，Artisan 可讓你建立巨集來取代預設解算器。" +
                $"Artisan 巨集的好處是不受長度限制，可以在遊戲允許的間隔範圍內執行巨集中的技能，並且還能設定一些額外條件，在巨集運行過程中作出調整。");

            ImGui.Spacing();
            ImGuiEx.TextUnderlined($"點擊此處前往「巨集」選單。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                OpenWindow = OpenWindow.Macro;
            }
            ImGui.Spacing();
            ImGuiEx.TextWrapped($"建立巨集後，必須先將其指派給配方才能使用。這可透過「配方視窗」的下拉選單輕鬆完成。預設情況下，它會附加在遊戲內製作筆記視窗的右上角，但也可以在設定中取消附加。");


            var recipeWindowExample = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/RecipeWindowExample.png");

            if (ThreadLoadImageHandler.TryGetTextureWrap(recipeWindowExample, out example))
            {
                ImGuiEx.LineCentered("###RecipeWindowExample", () =>
                {
                    ImGui.Image(example.ImGuiHandle, new Vector2(example.Width, example.Height));
                });
            }


            ImGuiEx.TextWrapped($"從下拉框中選擇已建立的巨集。" +
                $"當你製作這個物品時，技能建議會改為使用你巨集中的內容。");


            ImGui.Spacing();
            ImGuiEx.LineCentered("###Endurance", () =>
            {
                ImGuiEx.TextUnderlined("耐力模式");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan具有名為\"耐力模式\"的功能，其實就是\"自動重複模式\"文藝一點的說法。" +
                $"耐力模式的工作原理是從遊戲內製作筆記中選擇一個配方並啟用該功能。" +
                $"接著，只要素材足夠，你的角色就會嘗試連續製作該物品多次。" +
                $"\r\n\r\n" +
                $"其餘功能應該都很直觀，因為耐力模式還能管理食物、藥水、手冊、修理，以及製作過程中的素材提取。" +
                $"裝備修理功能僅支援使用暗物質，不支援使用修理 NPC。");

            ImGui.Spacing();
            ImGuiEx.TextUnderlined($"點擊此處前往耐力模式。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                OpenWindow = OpenWindow.Endurance;
            }

            ImGui.Spacing();
            ImGuiEx.LineCentered("###Lists", () =>
            {
                ImGuiEx.TextUnderlined("製作清單");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"Artisan還能夠生成一個物品清單，並讓它開始自動製作清單內的每個物品。" +
                $"製作清單有很多強大的工具來簡化從素材到最終產品的過程。" +
                $"它還支援在 Artisan 和 Teamcraft 之間匯入匯出。");

            ImGui.Spacing();
            ImGuiEx.TextUnderlined($"點擊此處前往製作清單。");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
            {
                OpenWindow = OpenWindow.Lists;
            }

            ImGui.Spacing();
            ImGuiEx.LineCentered("###Questions", () =>
            {
                ImGuiEx.TextUnderlined("有其他疑問嗎？");
            });
            ImGui.Spacing();

            ImGuiEx.TextWrapped($"如果你對此處未提及的內容有疑問，可以前往我們的 Discord 伺服器或 GitHub 頁面。");
            ImGui.SameLine(ImGui.GetCursorPosX(), 1.5f);
            ImGuiEx.TextUnderlined($"Discord 伺服器");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsItemClicked())
                {
                    Util.OpenLink("https://discord.gg/Zzrcc8kmvy");
                }
            }

            ImGuiEx.TextWrapped($"你也可以來我們的");
            ImGui.SameLine(ImGui.GetCursorPosX(), 2f);
            ImGuiEx.TextUnderlined($"GitHub 頁面");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.IsItemClicked())
                {
                    Util.OpenLink("https://github.com/PunishXIV/Artisan");
                }
            }

        }

        public static void DrawMainWindow()
        {
            ImGui.TextWrapped($"在這裡，你可以調整一些會影響 Artisan 使用方式的設定。其中部分設定可在製作過程中直接開關。");
            ImGui.TextWrapped($"若要使用 Artisan 的技能建議高亮提示，請將所有已解鎖技能放入可見的快速鍵欄。");
            bool autoEnabled = P.Config.AutoMode;
            bool delayRec = P.Config.DelayRecommendation;
            bool failureCheck = P.Config.DisableFailurePrediction;
            int maxQuality = P.Config.MaxPercentage;
            bool useTricksGood = P.Config.UseTricksGood;
            bool useTricksExcellent = P.Config.UseTricksExcellent;
            bool useSpecialist = P.Config.UseSpecialist;
            //bool showEHQ = P.Config.ShowEHQ;
            //bool useSimulated = P.Config.UseSimulatedStartingQuality;
            bool disableGlow = P.Config.DisableHighlightedAction;
            bool disableToasts = P.Config.DisableToasts;

            ImGui.Separator();

            if (ImGui.CollapsingHeader("常規設定"))
            {
                if (ImGui.Checkbox("自動製作模式", ref autoEnabled))
                {
                    P.Config.AutoMode = autoEnabled;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"自動使用內建解算器建議的每個技能。");
                if (autoEnabled)
                {
                    if (ImGui.Checkbox($"重複巨集延遲", ref P.Config.ReplicateMacroDelay))
                    {
                        P.Config.Save();
                    }

                    if (!P.Config.ReplicateMacroDelay)
                    {
                        var delay = P.Config.AutoDelay;
                        ImGui.PushItemWidth(200);
                        if (ImGui.SliderInt("運行延遲（毫秒）###ActionDelay", ref delay, 0, 1000))
                        {
                            if (delay < 0) delay = 0;
                            if (delay > 1000) delay = 1000;

                            P.Config.AutoDelay = delay;
                            P.Config.Save();
                        }
                    }
                }

                bool requireFoodPot = P.Config.AbortIfNoFoodPot;
                if (ImGui.Checkbox("強制使用消耗品", ref requireFoodPot))
                {
                    P.Config.AbortIfNoFoodPot = requireFoodPot;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker("Artisan 會自動使用已設定的食物、指南或藥品；若找不到，則不會開始製作。");

                if (ImGui.Checkbox("在製作練習時使用消耗品", ref P.Config.UseConsumablesTrial))
                {
                    P.Config.Save();
                }

                if (ImGui.Checkbox("在簡易製作作業時使用消耗品", ref P.Config.UseConsumablesQuickSynth))
                {
                    P.Config.Save();
                }

                ImGui.Indent();
                if (ImGui.CollapsingHeader("預設消耗品"))
                {
                    bool changed = false;
                    changed |= P.Config.DefaultConsumables.DrawFood();
                    changed |= P.Config.DefaultConsumables.DrawPotion();
                    changed |= P.Config.DefaultConsumables.DrawManual();
                    changed |= P.Config.DefaultConsumables.DrawSquadronManual();

                    if (changed)
                    {
                        P.Config.Save();
                    }
                }
                ImGui.Unindent();

                if (ImGui.Checkbox($"優先使用修理工而不是自己修理", ref P.Config.PrioritizeRepairNPC))
                {
                    P.Config.Save();
                }

                ImGuiComponents.HelpMarker("啟用自動修理時，會優先嘗試尋找附近的修理工進行修理；若附近沒有修理工，且你符合自行修理的條件，則會改為自行修理。");

                if (ImGui.Checkbox($"無法修理時，禁用耐久模式", ref P.Config.DisableEnduranceNoRepair))
                    P.Config.Save();

                ImGuiComponents.HelpMarker($"達到修理閾值後，如果無法自行修理，且也無法透過 NPC 修理，則自動停用耐力模式。");

                if (ImGui.Checkbox($"無法修理時暫停製作清單進度", ref P.Config.DisableListsNoRepair))
                    P.Config.Save();

                ImGuiComponents.HelpMarker($"達到修理閾值後，如果無法自行修理，且也無法透過 NPC 修理，則自動暫停目前清單進度。");

                bool requestStop = P.Config.RequestToStopDuty;
                bool requestResume = P.Config.RequestToResumeDuty;
                int resumeDelay = P.Config.RequestToResumeDelay;

                if (ImGui.Checkbox("在任務進入出發準備時，自動關閉耐力模式或暫停清單製作。", ref requestStop))
                {
                    P.Config.RequestToStopDuty = requestStop;
                    P.Config.Save();
                }

                if (requestStop)
                {
                    if (ImGui.Checkbox("在離開副本後，自動恢復關閉耐力模式或暫停清單製作。", ref requestResume))
                    {
                        P.Config.RequestToResumeDuty = requestResume;
                        P.Config.Save();
                    }

                    if (requestResume)
                    {
                        if (ImGui.SliderInt("延遲恢復（秒）", ref resumeDelay, 5, 60))
                        {
                            P.Config.RequestToResumeDelay = resumeDelay;
                        }
                    }
                }

                if (ImGui.Checkbox("禁止自動裝備製作所需物品。", ref P.Config.DontEquipItems))
                    P.Config.Save();

                if (ImGui.Checkbox("耐力模式製作完成後播放提示音", ref P.Config.PlaySoundFinishEndurance))
                    P.Config.Save();

                if (ImGui.Checkbox($"清單製作完成後播放提示音", ref P.Config.PlaySoundFinishList))
                    P.Config.Save();

                if (P.Config.PlaySoundFinishEndurance || P.Config.PlaySoundFinishList)
                {
                    if (ImGui.SliderFloat("音量", ref P.Config.SoundVolume, 0f, 1f, "%.2f"))
                        P.Config.Save();
                }

                if (ImGuiEx.ButtonCtrl("Reset Cosmic Exploration Crafting Configs"))
                {
                    var copy = P.Config.RecipeConfigs;
                    foreach (var c in copy)
                    {
                        if (Svc.Data.GetExcelSheet<Recipe>().GetRow(c.Key).Number == 0)
                            P.Config.RecipeConfigs.Remove(c.Key);
                    }
                }
            }
            if (ImGui.CollapsingHeader("巨集設定"))
            {
                if (ImGui.Checkbox("若無法使用該技能，則跳過該步驟。", ref P.Config.SkipMacroStepIfUnable))
                    P.Config.Save();

                if (ImGui.Checkbox($"阻止Artisan在巨集完成後繼續。", ref P.Config.DisableMacroArtisanRecommendation))
                    P.Config.Save();
            }
            if (ImGui.CollapsingHeader("解算器設定"))
            {
                if (ImGui.Checkbox($"使用 {Skills.TricksOfTrade.NameOfAction()} - {LuminaSheets.AddonSheet[227].Text.ToString()}", ref useTricksGood))
                {
                    P.Config.UseTricksGood = useTricksGood;
                    P.Config.Save();
                }
                ImGui.SameLine();
                if (ImGui.Checkbox($"使用 {Skills.TricksOfTrade.NameOfAction()} - {LuminaSheets.AddonSheet[228].Text.ToString()}", ref useTricksExcellent))
                {
                    P.Config.UseTricksExcellent = useTricksExcellent;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker(
                    $"這兩個選項允許你在出現「{LuminaSheets.AddonSheet[227].Text.ToString()}」或「{LuminaSheets.AddonSheet[228].Text.ToString()}」狀態時，優先使用「{Skills.TricksOfTrade.NameOfAction()}」。" +
                    $"這會取代「{Skills.PreciseTouch.NameOfAction()}」與「{Skills.IntensiveSynthesis.NameOfAction()}」的使用時機。" +
                    $"無論如何設定，在尚未學會前或特定狀況下，仍會使用「{Skills.TricksOfTrade.NameOfAction()}」。");
                if (ImGui.Checkbox("使用專家技能", ref useSpecialist))
                {
                    P.Config.UseSpecialist = useSpecialist;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker("若目前職業具備專家資格，便可使用會消耗「能工巧匠圖紙」的技能。\n「設計變動」會取代「觀察」。");
                ImGui.TextWrapped("最大品質%%");
                ImGuiComponents.HelpMarker($"一旦品質達到以下百分比，Artisan將會專注於推動進展。");
                if (ImGui.SliderInt("###SliderMaxQuality", ref maxQuality, 0, 100, $"%d%%"))
                {
                    P.Config.MaxPercentage = maxQuality;
                    P.Config.Save();
                }

                ImGui.Text($"收藏品收藏價值斷點");
                ImGuiComponents.HelpMarker("一旦收藏品達到以下斷點，解算器將停止推進品質。");

                if (ImGui.RadioButton($"低檔", P.Config.SolverCollectibleMode == 1))
                {
                    P.Config.SolverCollectibleMode = 1;
                    P.Config.Save();
                }
                ImGui.SameLine();
                if (ImGui.RadioButton($"中檔", P.Config.SolverCollectibleMode == 2))
                {
                    P.Config.SolverCollectibleMode = 2;
                    P.Config.Save();
                }
                ImGui.SameLine();
                if (ImGui.RadioButton($"最高檔", P.Config.SolverCollectibleMode == 3))
                {
                    P.Config.SolverCollectibleMode = 3;
                    P.Config.Save();
                }

                if (ImGui.Checkbox($"提升品质起手，使用 ({Skills.Reflect.NameOfAction()})", ref P.Config.UseQualityStarter))
                    P.Config.Save();
                ImGuiComponents.HelpMarker($"在耐久度較低的製作中更有利。");

                //if (ImGui.Checkbox("Low Stat Mode", ref P.Config.LowStatsMode))
                //    P.Config.Save();

                //ImGuiComponents.HelpMarker("This swaps out Waste Not II & Groundwork for Prudent Synthesis");

                ImGui.TextWrapped($"{Skills.PreparatoryTouch.NameOfAction()} - 最大{Buffs.InnerQuiet.NameOfBuff()}層数");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker($"將僅使用 {Skills.PreparatoryTouch.NameOfAction()} 來提高 {Buffs.InnerQuiet.NameOfBuff()} 的層數。這有助於調整製作力用量。");
                if (ImGui.SliderInt($"###MaxIQStacksPrepTouch", ref P.Config.MaxIQPrepTouch, 0, 10))
                    P.Config.Save();

                if (ImGui.Checkbox($"Use Material Miracle when available", ref P.Config.UseMaterialMiracle))
                    P.Config.Save();

                ImGuiComponents.HelpMarker($"這將在增益持續時間內將標準配方求解器切換到專家求解器。由於這是一個定時增益，而不是具有層數的永久增益，因此不會給您提供正確的模擬器結果，我們無法真正正確模擬它。");

                if (P.Config.UseMaterialMiracle)
                {
                    ImGui.Indent();
                    if (ImGui.Checkbox($"Use more than once per craft.", ref P.Config.MaterialMiracleMulti))
                        P.Config.Save();

                    ImGui.Unindent();
                }

            }
            bool openExpert = false;
            if (ImGui.CollapsingHeader("專家配方解算器設定"))
            {
                openExpert = true;
                if (P.Config.ExpertSolverConfig.Draw())
                    P.Config.Save();
            }
            if (!openExpert)
            {
                // 移除图标显示代码
            }

            if (ImGui.CollapsingHeader("Raphael 求解器設定"))
            {
                if (P.Config.RaphaelSolverConfig.Draw())
                    P.Config.Save();
            }

            using (ImRaii.Disabled())
            {
                if (ImGui.CollapsingHeader("腳本解算器設定（目前已停用）"))
                {
                    if (P.Config.ScriptSolverConfig.Draw())
                        P.Config.Save();
                }
            }
            if (ImGui.CollapsingHeader("介面設定"))
            {
                if (ImGui.Checkbox("禁用技能快速鍵欄提示", ref disableGlow))
                {
                    P.Config.DisableHighlightedAction = disableGlow;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker("禁用後就不會在快速鍵欄上用矩形框標記建議使用的技能了。");

                if (ImGui.Checkbox($"禁用技能螢幕提示", ref disableToasts))
                {
                    P.Config.DisableToasts = disableToasts;
                    P.Config.Save();
                }

                ImGuiComponents.HelpMarker("禁用後就不會在螢幕中間用文字和圖示提示建議使用的技能了。");

                bool lockMini = P.Config.LockMiniMenuR;
                if (ImGui.Checkbox("保持迷你選單吸附至製作筆記視窗。", ref lockMini))
                {
                    P.Config.LockMiniMenuR = lockMini;
                    P.Config.Save();
                }

                if (!P.Config.LockMiniMenuR)
                {
                    if (ImGui.Checkbox($"固定迷你選單位置", ref P.Config.PinMiniMenu))
                    {
                        P.Config.Save();
                    }
                }

                if (ImGui.Button("重設製作清單迷你選單位置"))
                {
                    AtkResNodeFunctions.ResetPosition = true;
                }

                if (ImGui.Checkbox($"擴展搜尋欄功能", ref P.Config.ReplaceSearch))
                {
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"擴充配方選單中的搜尋欄，提供即時結果與點擊開啟配方的功能。");

                bool hideQuestHelper = P.Config.HideQuestHelper;
                if (ImGui.Checkbox($"隱藏任務助手", ref hideQuestHelper))
                {
                    P.Config.HideQuestHelper = hideQuestHelper;
                    P.Config.Save();
                }

                bool hideTheme = P.Config.DisableTheme;
                if (ImGui.Checkbox("禁用自訂主題", ref hideTheme))
                {
                    P.Config.DisableTheme = hideTheme;
                    P.Config.Save();
                }
                ImGui.SameLine();

                if (IconButtons.IconTextButton(FontAwesomeIcon.Clipboard, "複製主題"))
                {
                    ImGui.SetClipboardText("DS1H4sIAAAAAAAACq1YS3PbNhD+Kx2ePR6AeJG+xXYbH+KOJ3bHbW60REusaFGlKOXhyX/v4rEACEqumlY+ECD32/cuFn7NquyCnpOz7Cm7eM1+zy5yvfnDPL+fZTP4at7MHVntyMi5MGTwBLJn+HqWLZB46Ygbx64C5kQv/nRo8xXQ3AhZZRdCv2jdhxdHxUeqrJO3Ftslb5l5u/Fa2rfEvP0LWBkBPQiSerF1Cg7wApBn2c5wOMv2juNn9/zieH09aP63g+Kqyr1mI91mHdj5mj3UX4bEG+b5yT0fzRPoNeF1s62e2np+EuCxWc+7z5cLr1SuuCBlkTvdqBCEKmaQxCHJeZmXnFKlgMHVsmnnEZ5IyXMiFUfjwt6yCHvDSitx1212m4gHV0QURY4saMEYl6Q4rsRl18/rPuCZQ+rFJxeARwyAJb5fVmD4NBaJEK3eL331UscuAgflOcY0J5zLUioHpHmhCC0lCuSBwU23r3sfF/0N0wKdoxcGFqHezYZmHypJIkgiSCJIalc8NEM7Utb6ErWlwngt9aUoFRWSB3wilRUl5SRwISUFvhJt9lvDrMgLIjgLzK66tq0228j0H+R3W693l1UfmUd9kqA79MKn9/2sB9lPI8hbofb073vdh1BbQYRgqKzfGbTfTWVqHmnMOcXUpI6BXhzGJjEQCNULmy4x9GpZz1a3Vb8KqaIDz4RPVGZin6dlZPKDSS29baAyRqYfzVGnr0ekaaowTbEw9MLjLnfD0GGT1unHSSlKr2lRyqLA2qU5ESovi6m+lkvqYiZ1/ygxyqrgjDKF8Yr2lp1pd4R7dokhvOBUQk37TCVKQbX4TMVtyuymruKWJCURVEofClYWbNpWCQfFifDwsWnYyXXS8ZxDOI+H0uLToPzrhKg3VV8N3amt1dP/t5goW/E85pg2pB8N8sd623yr3/dNOPYVstELg9cLA8zFCJKapQpEYkPVi9CMA/L/Uv8hrk1hmg9WKKMQXyIxnGFrm6i06MkhBHlIiQ8rI0xx4k/rsLWBsWpbTmmhqFIypcvUHTRgQ859V/bbKaPf1s/dbBcfD0R6NnCWwg/dS3lB4MfQMSrnCY9EK8qEw9uUl4YdHjRQRVFTuu5mq2a9uOvrfVOH0SDHqtXxMjDfi1RA/fyyGb7G5y5KdJg8EnTXdsOHZl1vQyJJQrlCQTDsEBi80HdhO+VwrEP48hwdTRp202yHbgGzhRfu03/UCA4gjglDd44mUT2D2i4UH9coSy8mfjEYN54NfbcOOIZnn15M7YqAH5rFEmdl3eJ8r0N5E9zH0fz71nQQyN+1/zSP6yR2A/l93dazoY6n5DdyiumWc91Xi+u+2zxU/aI+Jipq2QD5tdrfgO3t2P5jcqz9gLEXAEjgFHzcMJUgr5uXyDQsNSxZtCvX81s3r1qLOw0EztC3ORiEs4vssu9W9fqn2263HqpmncFF016PqklGjh1kjQ2NUyUJH08mcIk9gSrqn+jg0XFoqeqTrmDPwQv+PDEr6wl3oljaxcRSRTCyMc/lJJ/lAcnNhMr3WWZ+ES3exrXE+HJ2yNOrowkb97A2cExdXcrYjaFToVDfGSMqnCaDa0pi/vzNMyLG/wQEyzmzfhx7KAwJUn93Fz6v5shD8B+DRAG4Oh+QHYapovAd3/OEQzuiDSdE4c8wjJHh7iiBFFozvP3+NxT8RWGlEQAA");
                    Notify.Success("主題數據已複製到剪貼簿");
                }

                if (ImGui.Checkbox("停用 Allagan Tools 插件與清單的整合", ref P.Config.DisableAllaganTools))
                    P.Config.Save();

                if (ImGui.Checkbox("禁用Artisan上下文選單選項", ref P.Config.HideContextMenus))
                    P.Config.Save();

                ImGuiComponents.HelpMarker("若未停用，當你右鍵點擊製作筆記中的配方時，彈出的右鍵選單中會出現額外選項。");

                ImGui.Indent();
                if (ImGui.CollapsingHeader("模擬器設定"))
                {
                    if (ImGui.Checkbox("隱藏配方視窗模擬器結果", ref P.Config.HideRecipeWindowSimulator))
                        P.Config.Save();

                    if (ImGui.SliderFloat("模擬器技能圖示尺寸", ref P.Config.SimulatorActionSize, 5f, 70f))
                    {
                        P.Config.Save();
                    }
                    ImGuiComponents.HelpMarker("設定模擬器分頁中技能圖示的顯示比例。");

                    if (ImGui.Checkbox("啟用手動模式滑鼠懸停預覽", ref P.Config.SimulatorHoverMode))
                        P.Config.Save();

                    if (ImGui.Checkbox($"隱藏技能提示視窗", ref P.Config.DisableSimulatorActionTooltips))
                        P.Config.Save();

                    ImGuiComponents.HelpMarker("在手動模式下滑鼠懸停在技能上時，描述提示視窗將不會顯示。");
                }
                ImGui.Unindent();
            }
            if (ImGui.CollapsingHeader("清單設定"))
            {
                ImGui.TextWrapped($"當建立製作清單時，這些設定會自動生效。");

                if (ImGui.Checkbox("跳過你已經擁有了足夠數量的物品", ref P.Config.DefaultListSkip))
                {
                    P.Config.Save();
                }

                if (ImGui.Checkbox("自動精製魔晶石", ref P.Config.DefaultListMateria))
                {
                    P.Config.Save();
                }

                if (ImGui.Checkbox("自動修理", ref P.Config.DefaultListRepair))
                {
                    P.Config.Save();
                }

                if (P.Config.DefaultListRepair)
                {
                    ImGui.TextWrapped($"修理臨界值");
                    ImGui.SameLine();
                    if (ImGui.SliderInt("###SliderRepairDefault", ref P.Config.DefaultListRepairPercent, 0, 100, $"%d%%"))
                    {
                        P.Config.Save();
                    }
                }

                if (ImGui.Checkbox("設定新加入的物品製作模式為快速製作", ref P.Config.DefaultListQuickSynth))
                {
                    P.Config.Save();
                }

                if (ImGui.Checkbox($@"加入到列表後重設“加入次數”。", ref P.Config.ResetTimesToAdd))
                    P.Config.Save();

                ImGui.PushItemWidth(100);
                if (ImGui.InputInt("透過選單加入的次數", ref P.Config.ContextMenuLoops))
                {
                    if (P.Config.ContextMenuLoops <= 0)
                        P.Config.ContextMenuLoops = 1;

                    P.Config.Save();
                }

                ImGui.PushItemWidth(400);
                if (ImGui.SliderFloat("製作間隔延遲", ref P.Config.ListCraftThrottle2, 0f, 2f, "%.1f"))
                {
                    if (P.Config.ListCraftThrottle2 < 0f)
                        P.Config.ListCraftThrottle2 = 0f;

                    if (P.Config.ListCraftThrottle2 > 2f)
                        P.Config.ListCraftThrottle2 = 2f;

                    P.Config.Save();
                }

                ImGui.Indent();
                if (ImGui.CollapsingHeader("素材表設定"))
                {
                    ImGuiEx.TextWrapped(ImGuiColors.DalamudYellow, $"如果已經查看過清單的素材表，則「所有欄位設定」將不會生效。");

                    if (ImGui.Checkbox($@"預設隐藏""背包"" 列", ref P.Config.DefaultHideInventoryColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設隱藏“雇員”列", ref P.Config.DefaultHideRetainerColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設隱藏“剩餘需求”列", ref P.Config.DefaultHideRemainingColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設隱藏“來源”列", ref P.Config.DefaultHideCraftableColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設隱藏“可製作數量”列", ref P.Config.DefaultHideCraftableCountColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設隱藏“製作目標”列", ref P.Config.DefaultHideCraftItemsColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設隱藏“類型”列", ref P.Config.DefaultHideCategoryColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設隱藏“採集區域”列", ref P.Config.DefaultHideGatherLocationColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設隱藏“ID”列", ref P.Config.DefaultHideIdColumn))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設啟用“僅顯示HQ製作”", ref P.Config.DefaultHQCrafts))
                        P.Config.Save();

                    if (ImGui.Checkbox($"預設啟用“顏色驗證”", ref P.Config.DefaultColourValidation))
                        P.Config.Save();

                    if (ImGui.Checkbox($"從 Universalis 取得價格（載入時間較慢）", ref P.Config.UseUniversalis))
                        P.Config.Save();

                    if (P.Config.UseUniversalis)
                    {
                        if (ImGui.Checkbox($"將 Universalis 限制為目前大區", ref P.Config.LimitUnversalisToDC))
                            P.Config.Save();

                        if (ImGui.Checkbox($"僅按需取得價格", ref P.Config.UniversalisOnDemand))
                            P.Config.Save();

                        ImGuiComponents.HelpMarker("你必須點擊按鈕，才能取得每項商品的價格。");
                    }
                }

                ImGui.Unindent();
            }
        }

        private void ShowEnduranceMessage()
        {
            if (!P.Config.ViewedEnduranceMessage)
            {
                P.Config.ViewedEnduranceMessage = true;
                P.Config.Save();

                ImGui.OpenPopup("EndurancePopup");

                var windowSize = new Vector2(512 * ImGuiHelpers.GlobalScale,
                    ImGui.GetTextLineHeightWithSpacing() * 13 + 2 * ImGui.GetFrameHeightWithSpacing() * 2f);
                ImGui.SetNextWindowSize(windowSize);
                ImGui.SetNextWindowPos((ImGui.GetIO().DisplaySize - windowSize) / 2);

                using var popup = ImRaii.Popup("EndurancePopup",
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Modal);
                if (!popup)
                    return;

                ImGui.TextWrapped($@"I have been receiving quite a number of messages regarding ""buggy"" Endurance mode not setting ingredients anymore. As of the previous update, the old functionality of Endurance has been moved to a new setting.");
                ImGui.Dummy(new Vector2(0));

                var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/EnduranceNewSetting.png");

                if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var img))
                {
                    ImGuiEx.ImGuiLineCentered("###EnduranceNewSetting", () =>
                    {
                        ImGui.Image(img.ImGuiHandle, new Vector2(img.Width, img.Height));
                    });
                }

                ImGui.Spacing();

                ImGui.TextWrapped($"This change was made to bring back the very original behaviour of Endurance mode. If you do not care about your ingredient ratio, please make sure to enable Max Quantity Mode.");

                ImGui.SetCursorPosY(windowSize.Y - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y);
                if (ImGui.Button("關閉", -Vector2.UnitX))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    public enum OpenWindow
    {
        None = 0,
        Main = 1,
        Endurance = 2,
        Macro = 3,
        Lists = 4,
        About = 5,
        Debug = 6,
        FCWorkshop = 7,
        SpecialList = 8,
        Overview = 9,
        Simulator = 10,
        RaphaelCache = 11,
        Assigner = 12,
    }
}
