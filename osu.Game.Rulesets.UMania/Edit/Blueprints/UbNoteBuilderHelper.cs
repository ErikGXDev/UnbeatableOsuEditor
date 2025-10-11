// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit.Components.TernaryButtons;

namespace osu.Game.Rulesets.UMania.Edit.Blueprints
{
    public class UbNoteBuilderHelper
    {
        private UnbeatableHitObjectComposer composer;
        private HitObject hitObject;

        public UbNoteBuilderHelper(UnbeatableHitObjectComposer composer, HitObject hitObject)
        {
            this.composer = composer;
            this.hitObject = hitObject;
        }

        public void ApplyEverything(List<string> hitSampleInfos, string mainBank)
        {
            ApplySamples(hitSampleInfos);
            ApplyMainBank(mainBank);

            ApplyModifierSample(composer.ModInvisibleButton, HitSampleInfo.HIT_CLAP);
            ApplyModifierMainBank(composer.ModFlyingButton, HitSampleInfo.BANK_SOFT);

            ApplyModifierSample(composer.ModSwapImmediateButton, HitSampleInfo.HIT_CLAP);

            foreach (var nest in hitObject.NestedHitObjects)
            {
                nest.Samples = hitObject.Samples;
            }

            Logger.Log("Applied everything on " + hitObject.GetType());
        }

        public void ApplySamples(List<string> samples)
        {
            var hitSamples = new List<HitSampleInfo>();

            hitSamples = hitObject.Samples.ToList();

            foreach (string sample in samples)
            {
                HitSampleInfo sampleInfo = hitObject.CreateHitSampleInfo(sample);
                hitSamples.Add(sampleInfo);
            }

            hitObject.Samples = hitSamples;
        }

        public void ApplyModifierSample(DrawableTernaryButton modButton, string sample)
        {
            if (modButton.Current.Value == TernaryState.True && modButton.Enabled.Value)
            {
                HitSampleInfo sampleInfo = hitObject.CreateHitSampleInfo(sample).With(newVolume: 100);
                hitObject.Samples.Add(sampleInfo);
            }
        }

        public void ApplyModifierMainBank(DrawableTernaryButton modButton, string bank)
        {
            if (modButton.Current.Value == TernaryState.True && modButton.Enabled.Value)
            {
                ApplyMainBank(bank);
            }
        }

        public void ApplyModifierAdditionBank(DrawableTernaryButton modButton, string bank)
        {
            if (modButton.Current.Value == TernaryState.True && modButton.Enabled.Value)
            {
                ApplyAdditionBank(bank);
            }
        }

        public void ApplyMainBank(string bank)
        {
            var normalSample = hitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);

            if (normalSample == null)
            {
                hitObject.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_NORMAL, bank, string.Empty, 100));
                return;
            }

            var index = hitObject.Samples.IndexOf(normalSample);

            hitObject.Samples[index] = new HitSampleInfo(normalSample.Name,
                bank,
                normalSample.Suffix,
                100);
        }

        public void ApplyAdditionBank(string bank)
        {
            var additionSample = hitObject.Samples.FirstOrDefault(s => s.Name != HitSampleInfo.HIT_NORMAL);

            if (additionSample == null)
                return;

            var index = hitObject.Samples.IndexOf(additionSample);

            hitObject.Samples[index] = new HitSampleInfo(additionSample.Name,
                bank,
                additionSample.Suffix,
                additionSample.Volume);
        }

        public HitSampleInfo GetMainSample()
        {
            var normalSample = hitObject.Samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);

            return normalSample ?? new HitSampleInfo(HitSampleInfo.HIT_NORMAL, "normal", string.Empty, 100);
        }

        public bool HasAdditionSample(string sample)
        {
            return hitObject.Samples.Any(s => s.Name == sample);
        }

        public UbIconType InferObjectTypeIcon()
        {
            if (hitObject is ManiaHitObject maniaHitObject)
            {
                Logger.Log("Inferring icon type " + hitObject.GetType());

                int column = maniaHitObject.Column;

                if (hitObject is HeadNote or HoldNote)
                {
                    if (column == 5)
                    {
                        return UbIconType.Spam;
                    }

                    /*
                    hitObject.Samples.ForEach(s =>
                        Logger.Log($"Sample: {s.Name}, Bank: {s.Bank}, Suffix: {s.Suffix}, Volume: {s.Volume}"));
                        */

                    if (HasAdditionSample(HitSampleInfo.HIT_WHISTLE))
                    {
                        return UbIconType.Double;
                    }

                    return UbIconType.Hold;
                }

                if (hitObject is Note)
                {
                    if (column == 5)
                    {
                        return UbIconType.Freestyle;
                    }

                    if (column == 4)
                    {
                        if (HasAdditionSample(HitSampleInfo.HIT_WHISTLE))
                        {
                            return UbIconType.Zoom;
                        }
                        else
                        {
                            return UbIconType.Flip;
                        }
                    }

                    if (HasAdditionSample(HitSampleInfo.HIT_WHISTLE))
                    {
                        return UbIconType.Dodge;
                    }

                    return UbIconType.Note;
                }
            }

            return UbIconType.Note;
        }

        public List<UbIconType> InferObjectModifierIcons()
        {
            var icons = new List<UbIconType>();

            if (hitObject is ManiaHitObject maniaHitObject)
            {
                int column = maniaHitObject.Column;

                if (HasAdditionSample(HitSampleInfo.HIT_CLAP))
                {
                    if (column == 4)
                    {
                        icons.Add(UbIconType.ModSwapImmediate);
                    }
                    else
                    {
                        icons.Add(UbIconType.ModInvisible);
                    }
                }

                if (GetMainSample().Bank == HitSampleInfo.BANK_SOFT)
                {
                    icons.Add(UbIconType.ModFlying);
                }
            }

            return icons;
        }
    }
}
