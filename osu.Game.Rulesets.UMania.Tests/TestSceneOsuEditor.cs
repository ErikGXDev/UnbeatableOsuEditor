using NUnit.Framework;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.UMania.Tests;

[TestFixture]
public partial class TestSceneOsuEditor : EditorTestScene
{
    protected override Ruleset CreateEditorRuleset() => new UManiaRuleset();
}
