// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.UMania.Beatmaps;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Setup;
using WebSocketSharp;
using Logger = osu.Framework.Logging.Logger;

namespace osu.Game.Rulesets.UMania.Edit.Setup
{
    public partial class UbExportSection : SetupSection
    {
        public override LocalisableString Title => "Unbeatable";

        [Resolved] private Editor editor { get; set; } = null!;

        [Resolved] private BeatmapManager beatmapManager { get; set; } = null!;

        public void ExportToUnbeatable()
        {
            Logger.Log("Exporting to Unbeatable...");

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = workingBeatmap.BeatmapSetInfo;

            // Export the .osu file
            Logger.Log(Beatmap.HitObjects.Count + " hitobjects found.");

            PassBeatmapConverter passConverter = new PassBeatmapConverter(Beatmap, Beatmap.BeatmapInfo.Ruleset.CreateInstance());

            var playableBeatmap = passConverter.ConvertBeatmap(Beatmap, CancellationToken.None);

            UbBeatmapEncoder encoder = new UbBeatmapEncoder(playableBeatmap, null);

            var beatmapStream = new MemoryStream();
            var sw = new StreamWriter(beatmapStream, Encoding.UTF8, 1024);

            encoder.Encode(sw);

            sw.Flush();
            // Audio file
            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);
            if (audioFile == null)
                return;
            var audioStream = workingBeatmap.GetStream(audioFile.File.GetStoragePath());

            // Temp folder
            string tempPath = Path.Combine(Path.GetTempPath());

            // Save files to temp folder
            string beatmapPath = Path.Combine(tempPath, "temp.osu");

            using (var fs = File.Create(beatmapPath))
            {
                beatmapStream.Seek(0, SeekOrigin.Begin);
                beatmapStream.CopyTo(fs);
            }

            string audioPath = Path.Combine(tempPath, audioFilename);

            using (var fs = File.Create(audioPath))
            {
                audioStream.Seek(0, SeekOrigin.Begin);
                audioStream.CopyTo(fs);
            }

            beatmapStream.Dispose();
            audioStream.Dispose();

            Task.Run(() =>
            {
                using (var ws = new WebSocket("ws://localhost:5080"))
                {
                    ws.Connect();
                    ws.Send("play " + beatmapPath);
                }
            });
        }

        public void ExportToZip()
        {
            string artist = Beatmap.Metadata.Artist ?? "Unknown";
            string title = Beatmap.Metadata.Title ?? "Song";
            string author = Beatmap.Metadata.Author.Username ?? "Unknown";
            string difficulty = Beatmap.BeatmapInfo.DifficultyName ?? "Easy";
            string baseFilename = $"{artist} - {title} ({author}) [{difficulty}]".GetValidFilename();

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = workingBeatmap.BeatmapSetInfo;


            // Export the .osu file
            Logger.Log(Beatmap.HitObjects.Count + " hitobjects found.");

            PassBeatmapConverter passConverter = new PassBeatmapConverter(Beatmap, Beatmap.BeatmapInfo.Ruleset.CreateInstance());

            var playableBeatmap = passConverter.ConvertBeatmap(Beatmap, CancellationToken.None);

            UbBeatmapEncoder encoder = new UbBeatmapEncoder(playableBeatmap, null);

            var beatmapStream = new MemoryStream();
            var sw = new StreamWriter(beatmapStream, Encoding.UTF8, 1024);

            encoder.Encode(sw);

            sw.Flush();


            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);

            // Create the .zip file
            string zipFilename = baseFilename + ".zip";

            using (var zipStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    var beatmapEntry = archive.CreateEntry(baseFilename + $".osu", CompressionLevel.Optimal);

                    using (var entryStream = beatmapEntry.Open())
                    {
                        beatmapStream.Seek(0, SeekOrigin.Begin);
                        beatmapStream.CopyTo(entryStream);
                    }

                    // Only add audio file if it exists
                    if (audioFile != null)
                    {
                        var audioStream = workingBeatmap.GetStream(audioFile.File.GetStoragePath());
                        if (audioStream != null)
                        {
                            var audioEntry = archive.CreateEntry(audioFilename, CompressionLevel.Optimal);

                            using (var entryStream = audioEntry.Open())
                            {
                                audioStream.Seek(0, SeekOrigin.Begin);
                                audioStream.CopyTo(entryStream);
                            }

                            audioStream.Dispose();
                        }
                    }
                }

                zipStream.Seek(0, SeekOrigin.Begin);

                // Save the .zip file
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");

                using (var fs = File.Create(Path.Combine(directory, zipFilename)))
                {
                    zipStream.Seek(0, SeekOrigin.Begin);
                    zipStream.CopyTo(fs);
                }
            }

            beatmapStream.Dispose();

            Logger.Log($"Exporting to {zipFilename}...");
        }


        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new FormButton
                {
                    Caption = "Test your map in Unbeatable (Through Websocket)",
                    ButtonText = "Test Beatmap",
                    Action = ExportToUnbeatable,
                },
                new FormButton
                {
                    Caption = "Export your map to a .zip file for easy sharing",
                    ButtonText = "Export to .zip",
                    Action = ExportToZip,
                },
            };
        }
    }
}
