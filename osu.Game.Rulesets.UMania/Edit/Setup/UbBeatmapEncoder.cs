// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.UMania.Edit.Setup
{
    public partial class UbBeatmapEncoder : LegacyBeatmapEncoder
    {
        public UbBeatmapEncoder(IBeatmap beatmap, ISkin? skin)
            : base(beatmap, skin)
        {
            foreach (HitObject hitObject in beatmap.HitObjects.ToList())
            {
                var normalSample = hitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);

                if (normalSample != null)
                {
                    var newBank = "zero";

                    switch (normalSample.Bank.ToLowerInvariant())
                    {
                        case HitSampleInfo.BANK_NORMAL:
                            newBank = "zero";
                            break;

                        case HitSampleInfo.BANK_SOFT:
                            newBank = HitSampleInfo.BANK_NORMAL;
                            break;

                        case HitSampleInfo.BANK_DRUM:
                            newBank = HitSampleInfo.BANK_SOFT;
                            break;

                        case HitSampleInfo.BANK_STRONG:
                            newBank = HitSampleInfo.BANK_DRUM;
                            break;

                        default:
                            newBank = "zero";
                            break;
                    }

                    int index = hitObject.Samples.IndexOf(normalSample);
                    hitObject.Samples[index] = new HitSampleInfo(
                        normalSample.Name,
                        newBank,
                        normalSample.Suffix,
                        normalSample.Volume
                    );

                    hitObject.StartTime = (int)hitObject.StartTime;

                    if (hitObject is IHasDuration hasDuration && hitObject is not IHasPath)
                        hasDuration.Duration = Math.Floor(hasDuration.EndTime) - Math.Floor(hitObject.StartTime);
                }
            }
        }
    }
}
