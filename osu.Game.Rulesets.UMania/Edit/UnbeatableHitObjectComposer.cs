using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Audio;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.UMania.Edit.Composition;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osuTK;

namespace osu.Game.Rulesets.UMania.Edit;

[Cached]
public partial class UnbeatableHitObjectComposer : ManiaHitObjectComposer
{
    public UnbeatableHitObjectComposer(Ruleset ruleset)
        : base(ruleset)
    {
    }

    protected override IReadOnlyList<CompositionTool> CompositionTools => new CompositionTool[]
    {
        // Unbeatable Note Predicates

        // Normal Notes
        new UbNoteCompositionTool("Note", UbIconType.Note, [2, 3], []),
        new UbHoldNoteCompositionTool("Hold", UbIconType.Hold, [2, 3], []),

        // Dodge
        new UbNoteCompositionTool("Dodge", UbIconType.Dodge, [2, 3], [HitSampleInfo.HIT_WHISTLE]),
        // Blue/Double
        new UbHoldNoteCompositionTool("Double", UbIconType.Double, [2, 3], [HitSampleInfo.HIT_WHISTLE]),
        // Freestyle
        new UbNoteCompositionTool("Freestyle", UbIconType.Freestyle, [5], []),
        // Spam
        new UbHoldNoteCompositionTool("Spam", UbIconType.Spam, [5], [HitSampleInfo.HIT_FINISH]),

        // Flip
        new UbNoteCompositionTool("Flip", UbIconType.Flip, [4], []),
        // Zoom
        new UbNoteCompositionTool("Zoom", UbIconType.Zoom, [4], [HitSampleInfo.HIT_WHISTLE]),

        // Cop
        // new UbNoteCompositionTool("Brawl", UbIconType.Brawl, [2, 3], [], HitSampleInfo.BANK_STRONG),
        // Cop Hold
        // new UbHoldNoteCompositionTool("Brawl Hold", UbIconType.Brawl, [2, 3], [], HitSampleInfo.BANK_STRONG)
    };

    public Bindable<TernaryState> SettingShowAllowedColumns = new Bindable<TernaryState>(TernaryState.True);

    private readonly Bindable<TernaryState> modFlyingNote = new Bindable<TernaryState>();
    private readonly Bindable<TernaryState> modInvisibleNote = new Bindable<TernaryState>();

    private readonly Bindable<TernaryState> modSwapImmediate = new Bindable<TernaryState>();

    public DrawableTernaryButton ModFlyingButton = null!;
    public DrawableTernaryButton ModInvisibleButton = null!;

    public DrawableTernaryButton ModSwapImmediateButton = null!;

    // Create a dictionary that maps each button to a list of tool names that should have it enabled
    public Dictionary<DrawableTernaryButton, List<string>> ModButtonToolMap =
        new Dictionary<DrawableTernaryButton, List<string>>();

    protected override IEnumerable<Drawable> CreateTernaryButtons()
    {
        return new Drawable[]
        {
            ModFlyingButton = new DrawableTernaryButton
            {
                Current = modFlyingNote,
                Description = "Flying",
                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.Wind },
            },
            ModInvisibleButton = new DrawableTernaryButton
            {
                Current = modInvisibleNote,
                Description = "Invisible",
                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.EyeSlash },
            },
            ModSwapImmediateButton = new DrawableTernaryButton
            {
                Current = modSwapImmediate,
                Description = "Swap Immediate",
                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.ExchangeAlt },
            },
        };

        //return base.CreateTernaryButtons();
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        ModButtonToolMap = new Dictionary<DrawableTernaryButton, List<string>>
        {
            { ModFlyingButton, new List<string> { "Note", "Hold", "Dodge", "Double", "Spam", "Freestyle" } },
            { ModInvisibleButton, new List<string> { "Note", "Hold", "Double", "Spam", "Freestyle" } },
            { ModSwapImmediateButton, new List<string> { "Flip" } },
        };

        LeftToolbox.Add(new EditorToolboxGroup("unbeatable")
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children = new[]
                {
                    new DrawableTernaryButton
                    {
                        Current = SettingShowAllowedColumns,
                        Description = "Use column hints",
                        CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.Lightbulb },
                    }
                }
            },
        });
    }

    protected override void Update()
    {
        base.Update();

        if (BlueprintContainer.CurrentTool != null)
        {
            var tool = BlueprintContainer.CurrentTool;

            string name = tool.Name;

            foreach (var kvp in ModButtonToolMap)
            {
                var button = kvp.Key;
                var toolsWithButton = kvp.Value;

                bool shouldEnable = toolsWithButton.Contains(name);

                if (button.Enabled.Value != shouldEnable)
                {
                    button.Enabled.Value = toolsWithButton.Contains(name);
                }
            }

            var stage = Playfield.Stages[0];

            var columns = stage.Columns;

            var toolColumns = new List<int>();

            if (tool is UbNoteCompositionTool ubNTool)
            {
                toolColumns = ubNTool.Columns;
            }
            else if (tool is UbHoldNoteCompositionTool ubHTool)
            {
                toolColumns = ubHTool.Columns;
            }

            if (tool is UbNoteCompositionTool or UbHoldNoteCompositionTool &&
                SettingShowAllowedColumns.Value == TernaryState.True)
            {
                foreach (var col in columns)
                {
                    if (!toolColumns.Contains(col.Index))
                    {
                        //col.Colour = Colour4.Gray;
                        col.FlashColour(Colour4.Gray.Lighten(0.35f), 200);
                    }
                    else
                    {
                        //col.Colour = Colour4.White;
                        col.FlashColour(Colour4.White, 200);
                    }
                }
            }
            else
            {
                foreach (var col in columns)
                {
                    col.FlashColour(Colour4.White, 200);
                }
            }
        }
    }
}
