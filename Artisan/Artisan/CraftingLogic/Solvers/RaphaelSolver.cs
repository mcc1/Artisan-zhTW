using Artisan.GameInterop;
using Artisan.RawInformation;
using Artisan.UI;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Artisan.CraftingLogic.Solvers
{
    public class RaphaelSolverDefintion : ISolverDefinition
    {
        public Solver Create(CraftState craft, int flavour)
        {
            var key = RaphaelCache.GetKey(craft);
            if (RaphaelCache.HasSolution(craft, out var output))
            {
                return new MacroSolver(output!, craft);
            }
            return craft.CraftExpert ? new ExpertSolver() : new StandardSolver(false);
        }

        public IEnumerable<ISolverDefinition.Desc> Flavours(CraftState craft)
        {
            if (RaphaelCache.HasSolution(craft, out var solution))
                yield return new(this, 3, 0, $"Raphael 配方求解器");
        }
    }

    internal static class RaphaelCache
    {
        internal static readonly ConcurrentDictionary<string, Tuple<CancellationTokenSource, Task>> Tasks = [];
        [NonSerialized]
        public static Dictionary<string, RaphaelSolutionConfig> TempConfigs = new();

        public static void Build(CraftState craft, RaphaelSolutionConfig config)
        {
            var key = GetKey(craft);

            if (CLIExists() && !Tasks.ContainsKey(key))
            {
                P.Config.RaphaelSolverCacheV3.TryRemove(key, out _);

                Svc.Log.Information("啟動 Raphael 程序");

                var manipulation = craft.UnlockedManipulation ? "--manipulation" : "";
                var itemText = $"--recipe-id {craft.RecipeId}";
                var extraArgsBuilder = new StringBuilder();

                extraArgsBuilder.Append($"--initial {craft.InitialQuality} "); // must always have a space after

                if (config.EnsureReliability)
                {
                    Svc.Log.Error("已启用确保可靠性，这可能需要较长时间。启用后不提供任何支持。");
                    extraArgsBuilder.Append($"--adversarial "); // must always have a space after
                }

                if (config.BackloadProgress)
                {
                    extraArgsBuilder.Append($"--backload-progress "); // must always have a space after
                }

                if (config.HeartAndSoul)
                {
                    extraArgsBuilder.Append($"--heart-and-soul "); // must always have a space after
                }

                if (config.QuickInno)
                {
                    extraArgsBuilder.Append($"--quick-innovation "); // must always have a space after
                }

                if (P.Config.RaphaelSolverConfig.MaximumThreads > 0)
                {
                    extraArgsBuilder.Append($"--threads {P.Config.RaphaelSolverConfig.MaximumThreads} "); // must always have a space after
                }

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Join(Path.GetDirectoryName(Svc.PluginInterface.AssemblyLocation.FullName), "raphael-cli.exe"),
                        Arguments = $"solve {itemText} {manipulation} --level {craft.StatLevel} --stats {craft.StatCraftsmanship} {craft.StatControl} {craft.StatCP} {extraArgsBuilder} --output-variables ids", // Command to execute
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                Svc.Log.Information(process.StartInfo.Arguments);

                var cts = new CancellationTokenSource();
                cts.Token.Register(() => { process.Kill(); Tasks.Remove(key, out var _); });
                cts.CancelAfter(TimeSpan.FromMinutes(P.Config.RaphaelSolverConfig.TimeOutMins));

                var task = Task.Run(() =>
                {
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd().Trim();
                    if (process.ExitCode != 0)
                    {
                        DuoLog.Error(error.Split('\r', '\n')[1]);
                        cts.Cancel();
                        return;
                    }
                    var rng = new Random();
                    var ID = rng.Next(50001, 10000000);
                    while (P.Config.RaphaelSolverCacheV3.Any(kv => kv.Value.ID == ID))
                        ID = rng.Next(50001, 10000000);

                    var cleansedOutput = output.Replace("[", "").Replace("]", "").Replace("\"", "").Split(", ").Select(x => int.TryParse(x, out int n) ? n : 0);
                    P.Config.RaphaelSolverCacheV3[key] = new MacroSolverSettings.Macro()
                    {
                        ID = ID,
                        Name = key,
                        Steps = MacroUI.ParseMacro(cleansedOutput),
                        Options = new()
                        {
                            SkipQualityIfMet = false,
                            UpgradeProgressActions = false,
                            UpgradeQualityActions = false,
                            MinCP = craft.StatCP,
                            MinControl = craft.StatControl,
                            MinCraftsmanship = craft.StatCraftsmanship,
                        }
                    };

                    cts.Token.ThrowIfCancellationRequested();
                    if (P.Config.RaphaelSolverCacheV3[key] == null || P.Config.RaphaelSolverCacheV3[key].Steps.Count == 0)
                    {
                        Svc.Log.Error($"Raphael 无法生成有效的宏。这可能是以下原因之一：" +
                            $"\n- 如果您不在运行 Windows，Raphael 可能与您的操作系统不兼容。" +
                            $"\n- 您取消了生成过程。" +
                            $"\n- Raphael 在找不到结果后放弃了。{(P.Config.RaphaelSolverConfig.AutoGenerate ? "\n自动生成将因此被禁用。" : "")}");
                        P.Config.RaphaelSolverConfig.AutoGenerate = false;
                        cts.Cancel();
                        return;
                    }


                    if (P.Config.RaphaelSolverConfig.AutoSwitch)
                    {
                        if (!P.Config.RaphaelSolverConfig.AutoSwitchOnAll)
                        {
                            Svc.Log.Debug("切换到 Raphael 求解器");
                            var opt = CraftingProcessor.GetAvailableSolversForRecipe(craft, true).FirstOrNull(x => x.Name == $"Raphael 配方求解器");
                            if (opt is not null)
                            {
                                var config = P.Config.RecipeConfigs.GetValueOrDefault(craft.Recipe.RowId) ?? new();
                                config.SolverType = opt?.Def.GetType().FullName!;
                                config.SolverFlavour = (int)(opt?.Flavour);
                                P.Config.RecipeConfigs[craft.Recipe.RowId] = config;
                            }
                        }
                        else
                        {
                            var crafts = AllValidCrafts(key, craft.Recipe.CraftType.RowId).ToList();
                            Svc.Log.Debug($"将求解器应用到 {crafts.Count()} 个配方。");
                            var opt = CraftingProcessor.GetAvailableSolversForRecipe(craft, true).FirstOrNull(x => x.Name == $"Raphael 配方求解器");
                            if (opt is not null)
                            {
                                var config = P.Config.RecipeConfigs.GetValueOrDefault(craft.Recipe.RowId) ?? new();
                                config.SolverType = opt?.Def.GetType().FullName!;
                                config.SolverFlavour = (int)(opt?.Flavour);
                                foreach (var c in crafts)
                                {
                                    Svc.Log.Debug($"将 {c.Recipe.RowId} ({c.Recipe.ItemResult.Value.Name}) 切换到 Raphael 求解器");
                                    P.Config.RecipeConfigs[c.Recipe.RowId] = config;
                                }
                            }
                        }
                    }
                    P.Config.Save();
                    Tasks.Remove(key, out var _);
                }, cts.Token);

                Tasks.TryAdd(key, new(cts, task));
            }
        }

        public static string GetKey(CraftState craft)
        {
            return $"{craft.CraftLevel}/{craft.CraftProgress}/{craft.CraftQualityMax}/{craft.CraftDurability}-{craft.StatCraftsmanship}/{craft.StatControl}/{craft.StatCP}-{(craft.CraftExpert ? "Expert" : "Standard")}/{craft.InitialQuality}";
        }

        public static IEnumerable<CraftState> AllValidCrafts(string key, uint craftType)
        {
            var stats = KeyParts(key);
            var recipes = LuminaSheets.RecipeSheet.Values.Where(x => x.CraftType.RowId == craftType && x.RecipeLevelTable.Value.ClassJobLevel == stats.Level);
            foreach (var recipe in recipes)
            {
                var state = Crafting.BuildCraftStateForRecipe(default, Job.CRP + recipe.CraftType.RowId, recipe);
                if (stats.Prog == state.CraftProgress &&
                    stats.Qual == state.CraftQualityMax &&
                    stats.Dur == state.CraftDurability)
                    yield return state;
            }
        }

        public static (int Level, int Prog, int Qual, int Dur, int Initial, int Crafts, int Control, int CP) KeyParts(string key)
        {
            var parts = key.Split('/');

            int.TryParse(parts[0], out var lvl);
            int.TryParse(parts[1], out var prog);
            int.TryParse(parts[2], out var qual);
            int.TryParse(parts[3].Split('-')[0], out var dur);
            int.TryParse(parts[3].Split('-')[1], out var crafts);
            int.TryParse(parts[4], out var ctrl);
            int.TryParse(parts[5].Split('-')[0], out var cp);
            int.TryParse(parts[6], out var initial);

            return (lvl, prog, qual, dur, initial, crafts, ctrl, cp);
        }

        public static bool HasSolution(CraftState craft, out MacroSolverSettings.Macro? raphaelSolutionConfig)
        {
            foreach (var solution in P.Config.RaphaelSolverCacheV3.OrderByDescending(x => KeyParts(x.Key).Control))
            {
                if (solution.Value.Steps.Count == 0) continue;

                var solKey = KeyParts(solution.Key);

                if (solKey.Level == craft.CraftLevel &&
                    solKey.Prog == craft.CraftProgress &&
                    solKey.Qual == craft.CraftQualityMax &&
                    solKey.Crafts == craft.StatCraftsmanship &&
                    solKey.Control <= craft.StatControl &&
                    solKey.Initial == craft.InitialQuality &&
                    solKey.CP <= craft.StatCP)
                {
                    raphaelSolutionConfig = solution.Value;
                    return true;
                }
            }
            raphaelSolutionConfig = null;
            return false;
        }

        public static bool InProgress(CraftState craft) => Tasks.TryGetValue(GetKey(craft), out var _);

        public static bool InProgressAny() => Tasks.Any();

        internal static bool CLIExists()
        {
            return File.Exists(Path.Join(Path.GetDirectoryName(Svc.PluginInterface.AssemblyLocation.FullName), "raphael-cli.exe"));
        }

        public static bool DrawRaphaelDropdown(CraftState craft, bool liveStats = true)
        {
            bool changed = false;
            var config = P.Config.RecipeConfigs.GetValueOrDefault(craft.RecipeId) ?? new();
            if (CLIExists())
            {
                var hasSolution = HasSolution(craft, out var solution);
                var key = GetKey(craft);

                if (!TempConfigs.ContainsKey(key))
                {
                    TempConfigs.Add(key, new());
                    TempConfigs[key].EnsureReliability = P.Config.RaphaelSolverConfig.AllowEnsureReliability;
                    TempConfigs[key].BackloadProgress = P.Config.RaphaelSolverConfig.AllowBackloadProgress;
                    TempConfigs[key].HeartAndSoul = P.Config.RaphaelSolverConfig.ShowSpecialistSettings && craft.Specialist;
                    TempConfigs[key].QuickInno = P.Config.RaphaelSolverConfig.ShowSpecialistSettings && craft.Specialist;
                }

                if (hasSolution)
                {
                    var opt = CraftingProcessor.GetAvailableSolversForRecipe(craft, true).FirstOrNull(x => x.Name == $"Raphael 配方求解器");
                    var solverIsRaph = config.SolverType == opt?.Def.GetType().FullName!;
                    var curStats = CharacterStats.GetCurrentStats();
                    //Svc.Log.Debug($"{curStats.Craftsmanship}/{craft.StatCraftsmanship} - {curStats.Control}/{craft.StatControl} - {curStats.CP}/{craft.StatCP}");
                    if (liveStats && craft.StatCraftsmanship != curStats.Craftsmanship && solverIsRaph)
                    {
                        var craftsmanshipError = curStats.Craftsmanship - craft.StatCraftsmanship > 0 ? $"(超出 {curStats.Craftsmanship - craft.StatCraftsmanship}) " : "";
                        ImGuiEx.Text(ImGuiColors.DalamudRed, $"您目前的製作力 {craftsmanshipError}與產生的結果不符。\n由於可能提前完成，此求解器在匹配之前不會被使用。\n(您可能需要套用正確的增益效果)");
                    }

                    if (!solverIsRaph)
                    {
                        if (liveStats)
                        {
                            ImGuiEx.TextCentered($"已產生 Raphael 解決方案。（點擊切換查看）");
                            if (ImGui.IsItemClicked())
                            {
                                config.SolverType = opt?.Def.GetType().FullName!;
                                config.SolverFlavour = (int)(opt?.Flavour);
                                changed = true;
                            }
                        }
                        else
                        {
                            ImGuiEx.TextCentered($"已產生 Raphael 解決方案。");
                        }
                    }
                }
                else
                {
                    if (liveStats && P.Config.RaphaelSolverConfig.AutoGenerate && CraftingProcessor.GetAvailableSolversForRecipe(craft, true).Any())
                    {
                        if (!craft.CraftExpert || (craft.CraftExpert && P.Config.RaphaelSolverConfig.GenerateOnExperts))
                            Build(craft, TempConfigs[key]);
                    }
                }

                ImGui.Separator();
                var inProgress = InProgress(craft);
                var raphChanges = false;

                if (inProgress)
                    ImGui.BeginDisabled();

                if (P.Config.RaphaelSolverConfig.AllowEnsureReliability)
                    raphChanges |= ImGui.Checkbox($"確保可靠性##{key}Reliability", ref TempConfigs[key].EnsureReliability);
                if (P.Config.RaphaelSolverConfig.AllowBackloadProgress)
                    raphChanges |= ImGui.Checkbox($"後置進度##{key}Progress", ref TempConfigs[key].BackloadProgress);
                if (P.Config.RaphaelSolverConfig.ShowSpecialistSettings && craft.Specialist)
                    raphChanges |= ImGui.Checkbox($"允許使用專心致志##{key}HS", ref TempConfigs[key].HeartAndSoul);
                if (P.Config.RaphaelSolverConfig.ShowSpecialistSettings && craft.Specialist)
                    raphChanges |= ImGui.Checkbox($"允許使用快速改革##{key}QI", ref TempConfigs[key].QuickInno);

                changed |= raphChanges;

                if (inProgress)
                    ImGui.EndDisabled();

                if (!inProgress)
                {
                    if (ImGui.Button("建立 Raphael 解決方案", new Vector2(ImGui.GetContentRegionAvail().X, 25f.Scale())))
                    {
                        Build(craft, TempConfigs[key]);
                    }
                }
                else
                {
                    if (ImGui.Button("取消 Raphael 產生", new Vector2(ImGui.GetContentRegionAvail().X, 25f.Scale())))
                    {
                        Tasks.TryRemove(key, out var task);
                        task.Item1.Cancel();
                    }
                }

                if (TempConfigs[key].EnsureReliability && ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("已啟用確保品質，由於可能造成的問題，啟用後不提供任何支援。");
                    ImGui.EndTooltip();
                }

                if (TempConfigs[key].HeartAndSoul || TempConfigs[key].QuickInno)
                {
                    ImGui.Text("已啟用專家技能，這會顯著降低求解器速度。");
                }

                if (inProgress)
                {
                    ImGuiEx.TextCentered("生成中...");
                }
            }

            return changed;
        }
    }

    public class RaphaelSolverSettings
    {
        public bool AllowEnsureReliability = false;
        public bool AllowBackloadProgress = false;
        public bool ShowSpecialistSettings = false;
        public bool ExactCraftsmanship = false;
        public bool AutoGenerate = false;
        public bool AutoSwitch = false;
        public bool AutoSwitchOnAll = false;
        public int MaximumThreads = 0;
        public bool GenerateOnExperts = false;
        public int TimeOutMins = 1;

        public bool Draw()
        {
            bool changed = false;

            ImGui.Indent();
            ImGui.TextWrapped($"Raphael 設定會影響效能與系統記憶體耗用。若可用記憶體較少，請盡量不要更動設定，建議至少保留 2GB 可用記憶體。");

            if (ImGui.SliderInt("最大執行緒數", ref MaximumThreads, 0, Environment.ProcessorCount))
            {
                P.Config.Save();
            }
            ImGuiEx.TextWrapped("預設會使用所有可用資源，但在較低階的機器上，你可能需要減少 CPU 使用量來換取較低的運算速度。（0 = 全部）");

            changed |= ImGui.Checkbox("在巨集生成中確保 100% 可靠性", ref AllowEnsureReliability);
            ImGui.PushTextWrapPos(0);
            ImGui.TextColored(new System.Numerics.Vector4(255, 0, 0, 1), "確保可靠性不一定總是有效，且非常耗用 CPU 和記憶體，建議至少保留 16GB 以上的可用記憶體。啟用此選項後將不提供任何支援");
            ImGui.PopTextWrapPos();
            changed |= ImGui.Checkbox("在巨集生成中允許後置進度", ref AllowBackloadProgress);
            changed |= ImGui.Checkbox("在可用時顯示專家選項", ref ShowSpecialistSettings);
            changed |= ImGui.Checkbox($"若尚未建立有效解決方案，則自動產生解決方案。", ref AutoGenerate);

            if (AutoGenerate)
            {
                ImGui.Indent();
                changed |= ImGui.Checkbox($"在專家配方上生成", ref GenerateOnExperts);
                ImGui.Unindent();
            }

            changed |= ImGui.Checkbox($"一旦建立解決方案，自動切換到 Raphael 求解器。", ref AutoSwitch);

            if (AutoSwitch)
            {
                ImGui.Indent();
                changed |= ImGui.Checkbox($"應用到所有有效製作", ref AutoSwitchOnAll);
                ImGui.Unindent();
            }

            changed |= ImGui.SliderInt("解決方案生成超時", ref TimeOutMins, 1, 15);

            ImGuiComponents.HelpMarker($"若解決方案產生時間超過此分鐘數，將取消產生任務。");

            if (ImGui.Button($"清除 raphael 宏缓存 (当前存储 {P.Config.RaphaelSolverCacheV3.Count} 个)"))
            {
                P.Config.RaphaelSolverCacheV3.Clear();
                changed |= true;
            }

            ImGui.Unindent();
            return changed;
        }
    }

    public class RaphaelSolutionConfig
    {
        public bool EnsureReliability = false;
        public bool BackloadProgress = false;
        public bool HeartAndSoul = false;
        public bool QuickInno = false;
        public string Macro = string.Empty;

        public int MinCP = 0;
        public int MinControl = 0;
        public int ExactCraftsmanship = 0;

        [NonSerialized]
        public bool HasChanges = false;
    }
}
