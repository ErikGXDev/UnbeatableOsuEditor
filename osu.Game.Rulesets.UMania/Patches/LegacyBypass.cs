using HarmonyLib;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.UMania.Patches;

[HarmonyPatch(typeof(LegacyBeatmapEncoder), MethodType.Constructor, new[] { typeof(IBeatmap), typeof(ISkin) })]
public class LegacyBypass
{
    public static void Prefix(IBeatmap beatmap, ISkin? skin)
    {
        if (beatmap.BeatmapInfo.Ruleset.ShortName == "umania")
        {
            beatmap.BeatmapInfo.Ruleset.OnlineID = 3;
        }
    }

    public static void Postfix(IBeatmap beatmap, ISkin? skin)
    {
        if (beatmap.BeatmapInfo.Ruleset.ShortName == "umania")
        {
            beatmap.BeatmapInfo.Ruleset.OnlineID = -1;
        }
    }
}
