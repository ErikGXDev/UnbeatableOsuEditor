using System.Collections.Generic;
using System.Threading;
using MongoDB.Bson;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.IO.Serialization;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UMania.Objects;

namespace osu.Game.Rulesets.UMania.Beatmaps;

public class PassBeatmapConverter : BeatmapConverter<HitObject>
{
    public PassBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
        : base(beatmap, ruleset)
    {
    }

    public override bool CanConvert() => true;

    protected override IEnumerable<HitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
    {
        return [original];
    }

    public new Beatmap<HitObject> ConvertBeatmap(IBeatmap beatmap, CancellationToken token)
    {
        var convertBeatmap = base.ConvertBeatmap(beatmap, token);

        var hitObjects = new List<HitObject>(beatmap.HitObjects);

        // serialize and deserialize all hitobjects
        var serializedHitObjects = new List<HitObject>();
        foreach (var hitObject in hitObjects)
        {
            var samples = hitObject.Samples;
            var serializedSamples = new List<HitSampleInfo>(samples.Count);

            foreach (var sample in samples)
            {
                var serializedSample = sample.Serialize();
                var deserializedSample = serializedSample.Deserialize<HitSampleInfo>();
                serializedSamples.Add(deserializedSample);
            }


            var serialized = hitObject.Serialize();

            if (hitObject is Note note)
            {
                var deserialized = serialized.Deserialize<Note>();
                deserialized.Samples = serializedSamples;
                serializedHitObjects.Add(deserialized);
            }
            else if (hitObject is HoldNote holdNote)
            {
                var deserialized = serialized.Deserialize<HoldNote>();
                serializedHitObjects.Add(deserialized);

            }
            else
            {
                var deserialized = serialized.Deserialize<ManiaHitObject>();
                serializedHitObjects.Add(deserialized);
            }
        }

        convertBeatmap.HitObjects = serializedHitObjects;


        return convertBeatmap;
    }
}
