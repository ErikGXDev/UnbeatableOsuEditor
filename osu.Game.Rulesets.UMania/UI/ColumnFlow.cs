// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Layout;
using osu.Game.Rulesets.UMania.Beatmaps;
using osu.Game.Rulesets.UMania.Configuration;
using osu.Game.Rulesets.UMania.Skinning;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.UMania.UI
{
    /// <summary>
    /// A <see cref="Drawable"/> which flows its contents according to the <see cref="Column"/>s in a <see cref="Stage"/>.
    /// Content can be added to individual columns via <see cref="SetContentForColumn"/>.
    /// </summary>
    /// <typeparam name="TContent">The type of content in each column.</typeparam>
    public partial class ColumnFlow<TContent> : CompositeDrawable
        where TContent : Drawable
    {
        /// <summary>
        /// All contents added to this <see cref="ColumnFlow{TContent}"/>.
        /// </summary>
        public TContent[] Content { get; }

        private readonly FillFlowContainer<Container<TContent>> columns;
        private readonly StageDefinition stageDefinition;

        public new bool Masking
        {
            get => base.Masking;
            set => base.Masking = value;
        }

        private readonly LayoutValue layout = new LayoutValue(Invalidation.DrawSize);

        public ColumnFlow(StageDefinition stageDefinition)
        {
            this.stageDefinition = stageDefinition;
            Content = new TContent[stageDefinition.Columns];

            AutoSizeAxes = Axes.X;

            Masking = true;

            InternalChild = columns = new FillFlowContainer<Container<TContent>>
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
            };

            for (int i = 0; i < stageDefinition.Columns; i++)
                columns.Add(new Container<TContent> { RelativeSizeAxes = Axes.Y });

            AddLayout(layout);
        }

        [Resolved]
        private ISkinSource skin { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(ManiaRulesetConfigManager? rulesetConfig)
        {
            skin.SourceChanged += invalidateLayout;
        }

        protected override void Update()
        {
            base.Update();

            if (!layout.IsValid)
            {
                updateColumnSize();
                layout.Validate();
            }
        }

        /// <summary>
        /// Sets the content of one of the columns of this <see cref="ColumnFlow{TContent}"/>.
        /// </summary>
        /// <param name="column">The index of the column to set the content of.</param>
        /// <param name="content">The content.</param>
        public void SetContentForColumn(int column, TContent content)
        {
            Content[column] = columns[column].Child = content;
        }

        private void invalidateLayout() => layout.Invalidate();

        private void updateColumnSize()
        {
            for (int i = 0; i < stageDefinition.Columns; i++)
            {
                float leftSpacing = skin.GetConfig<ManiaSkinConfigurationLookup, float>(
                                            new ManiaSkinConfigurationLookup(LegacyManiaSkinConfigurationLookups.LeftColumnSpacing, i))
                                        ?.Value ?? Stage.COLUMN_SPACING;

                float rightSpacing = skin.GetConfig<ManiaSkinConfigurationLookup, float>(
                                             new ManiaSkinConfigurationLookup(LegacyManiaSkinConfigurationLookups.RightColumnSpacing, i))
                                         ?.Value ?? Stage.COLUMN_SPACING;

                columns[i].Margin = new MarginPadding { Left = leftSpacing, Right = rightSpacing };

                float? width = skin.GetConfig<ManiaSkinConfigurationLookup, float>(
                                       new ManiaSkinConfigurationLookup(LegacyManiaSkinConfigurationLookups.ColumnWidth, i))
                                   ?.Value;

                bool isSpecialColumn = stageDefinition.IsSpecialColumn(i);

                // only used by default skin (legacy skins get defaults set in LegacyManiaSkinConfiguration)
                width ??= isSpecialColumn ? Column.SPECIAL_COLUMN_WIDTH : Column.COLUMN_WIDTH;

                columns[i].Width = width.Value;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (skin.IsNotNull())
                skin.SourceChanged -= invalidateLayout;
        }
    }
}
