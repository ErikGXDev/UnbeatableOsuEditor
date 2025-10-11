// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.UMania.Objects;

namespace osu.Game.Rulesets.UMania.Beatmaps
{
    public class UManiaBeatmapConverter : BeatmapConverter<ManiaHitObject>
    {
        public UManiaBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
            : base(beatmap, ruleset)
        {
        }

        protected override Beatmap<ManiaHitObject> CreateBeatmap()
        {
            ManiaBeatmap beatmap = new ManiaBeatmap(new StageDefinition(TotalColumns));

            return beatmap;
        }

        // todo: Check for conversion types that should be supported (ie. Beatmap.HitObjects.Any(h => h is IHasXPosition))
        // https://github.com/ppy/osu/tree/master/osu.Game/Rulesets/Objects/Types
        public override bool CanConvert() => Beatmap.HitObjects.Any(h => h is IHasXPosition);

        protected override IEnumerable<ManiaHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap,
                                                                        CancellationToken cancellationToken)
        {
            if (original is ManiaHitObject hitObject)
            {
                yield return hitObject;

                yield break;
            }

            if (original is IHasXPosition hitObjectXPosition)

            {
                var columnIndex = GetColumn(hitObjectXPosition.X);

                if (original is IHasDuration hitObjectDuration)
                {
                    yield return new HoldNote
                    {
                        StartTime = original.StartTime,
                        Samples = original.Samples,
                        EndTime = hitObjectDuration.EndTime,
                        PlaySlidingSamples = true,
                    };

                    yield break;
                }

                yield return new Note
                {
                    StartTime = original.StartTime,
                    Samples = original.Samples,
                    Column = columnIndex,
                };
            }
        }

        protected int TotalColumns => 6;

        protected int GetColumn(float position /*, bool allowSpecial = false*/)
        {
            /*if (allowSpecial && TotalColumns == 8)
            {
                const float local_x_divisor = 512f / 7;
                return Math.Clamp((int)MathF.Floor(position / local_x_divisor), 0, 6) + 1;
            }*/

            float localXDivisor = 512f / TotalColumns;
            return Math.Clamp((int)MathF.Floor(position / localXDivisor), 0, TotalColumns - 1);
        }
    }
}
