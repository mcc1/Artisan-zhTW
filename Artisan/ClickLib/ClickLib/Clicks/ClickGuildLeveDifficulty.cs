using System;

using ClickLib.Attributes;
using ClickLib.Bases;

namespace ClickLib.Clicks;

/// <summary>
/// Addon GuildLeve.
/// </summary>
public sealed unsafe class ClickGuildLeveDifficulty : ClickBase<ClickGuildLeveDifficulty>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClickGuildLeveDifficulty"/> class.
    /// </summary>
    /// <param name="addon">Addon pointer.</param>
    public ClickGuildLeveDifficulty(IntPtr addon = default)
        : base("GuildLeveDifficulty", addon)
    {
    }

    public static implicit operator ClickGuildLeveDifficulty(IntPtr addon) => new(addon);

    /// <summary>
    /// Instantiate this click using the given addon.
    /// </summary>
    /// <param name="addon">Addon to reference.</param>
    /// <returns>A click instance.</returns>
    public static ClickGuildLeveDifficulty Using(IntPtr addon) => new(addon);

    /// <summary>
    /// Click the confirm button.
    /// </summary>
    /// <param name="difficulty">Default difficulty.</param>
    [ClickName("difficulty_confirm")]
    public void Confirm(int difficulty)
    {
        this.FireCallback(0, difficulty);
        this.HideAddon();
    }

    /// <summary>
    /// Click the cancel button.
    /// </summary>
    [ClickName("difficulty_cancel")]
    public void Cancel()
        => this.FireCallback(-1);
}
