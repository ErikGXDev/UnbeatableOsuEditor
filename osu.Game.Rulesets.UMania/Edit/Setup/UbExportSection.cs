// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.OSD;
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

        [Resolved(canBeNull: true)] private OnScreenDisplay onScreenDisplay { get; set; }

        public void ExportToUnbeatable() => Task.Run(exportToUnbeatable);

        private void exportToUnbeatable()
        {
            Logger.Log("Exporting to Unbeatable...");

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = workingBeatmap.BeatmapSetInfo;

            // Export the .osu file
            Logger.Log(Beatmap.HitObjects.Count + " hitobjects found.");

            PassBeatmapConverter passConverter =
                new PassBeatmapConverter(Beatmap, Beatmap.BeatmapInfo.Ruleset.CreateInstance());

            var playableBeatmap = passConverter.ConvertBeatmap(Beatmap, CancellationToken.None);

            UbBeatmapEncoder encoder = new UbBeatmapEncoder(playableBeatmap, null);

            var beatmapStream = new MemoryStream();
            var sw = new StreamWriter(beatmapStream, Encoding.UTF8, 1024);

            encoder.EncodeB(sw);

            sw.Flush();
            // Audio file
            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);
            if (audioFile == null)
            {
                showToast("Export failed", "Audio file not found in beatmap set.");
                return;
            }

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

                    showToast("Export successful", "Sent to Unbeatable!");
                }
            });
        }


        public void ExportToZip() => Task.Run(exportToZip);


        private IBeatmap[] getBeatmapsFromSet(BeatmapSetInfo beatmapSet)
        {
            var beatmaps = new IBeatmap[beatmapSet.Beatmaps.Count];

            for (int i = 0; i < beatmapSet.Beatmaps.Count; i++)
            {
                var beatmapInfo = beatmapSet.Beatmaps[i];
                var beatmap = beatmapManager.GetWorkingBeatmap(beatmapInfo).Beatmap;
                beatmaps[i] = beatmap;
            }

            return beatmaps;
        }

        private MemoryStream getBeatmapStream(IBeatmap beatmap)
        {
            // Export the .osu file
            Logger.Log(beatmap.HitObjects.Count + " hitobjects found.");

            PassBeatmapConverter passConverter =
                new PassBeatmapConverter(beatmap, beatmap.BeatmapInfo.Ruleset.CreateInstance());

            var playableBeatmap = passConverter.ConvertBeatmap(beatmap, CancellationToken.None);

            UbBeatmapEncoder encoder = new UbBeatmapEncoder(playableBeatmap, null);

            var beatmapStream = new MemoryStream();
            var sw = new StreamWriter(beatmapStream, Encoding.UTF8, 1024);

            encoder.EncodeB(sw);

            sw.Flush();

            return beatmapStream;
        }

        private void exportToZip()
        {

            if (string.IsNullOrEmpty(exportFolderSelector.SelectedDirectory.Value))
            {
                showToast("Export failed", "No export folder selected.");
                return;
            }

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = Beatmap.BeatmapInfo.BeatmapSet;

            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);

            var baseFilename = "";

            string artist = Beatmap.Metadata.Artist ?? "Unknown";
            string title = Beatmap.Metadata.Title ?? "Song";
            string author = Beatmap.Metadata.Author.Username ?? "Unknown";
            string difficulty = Beatmap.BeatmapInfo.DifficultyName ?? "Easy";

            if (beatmapSet.Beatmaps.Count > 1)
            {
                baseFilename = $"{artist} - {title} ({author})".GetValidFilename();
            }
            else
            {
                baseFilename = $"{artist} - {title} ({author}) [{difficulty}]".GetValidFilename();
            }

            // Create the .zip file
            string zipFilename = baseFilename + ".zip";

            using (var zipStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {

                    var beatmaps = getBeatmapsFromSet(beatmapSet);

                    foreach (var beatmap in beatmaps)
                    {
                        var stream = getBeatmapStream(beatmap);

                        var newDifficulty = beatmap.BeatmapInfo.DifficultyName ?? "Easy";

                        var beatmapName = $"{artist} - {title} ({author}) [{newDifficulty}]".GetValidFilename();
                        var beatmapEntry = archive.CreateEntry(beatmapName + $".osu", CompressionLevel.Optimal);

                        using (var entryStream = beatmapEntry.Open())
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.CopyTo(entryStream);
                        }

                        stream.Dispose();
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


                // show file save dialog

                var directory = exportFolderSelector.SelectedDirectory.Value;

                var savePath = Path.Combine(directory, zipFilename);



                using (var fs = File.Create(Path.Combine(directory, zipFilename)))
                {
                    zipStream.Seek(0, SeekOrigin.Begin);
                    zipStream.CopyTo(fs);
                }
            }


            Logger.Log($"Exporting to {zipFilename}...");

            showToast("Export successful", $"Saved as {zipFilename}");
        }

        public void ExportToFolder() => Task.Run(exportToFolder);

        private void exportToFolder()
        {
            if (string.IsNullOrEmpty(exportFolderSelector.SelectedDirectory.Value))
            {
                showToast("Export failed", "No export folder selected.");
                return;
            }

            var workingBeatmap = editor.Beatmap.Value;

            var beatmapSet = Beatmap.BeatmapInfo.BeatmapSet;

            string audioFilename = Beatmap.Metadata.AudioFile;

            var audioFile = beatmapSet.GetFile(audioFilename);

            var baseFilename = "";

            string artist = Beatmap.Metadata.Artist ?? "Unknown";
            string title = Beatmap.Metadata.Title ?? "Song";
            string author = Beatmap.Metadata.Author.Username ?? "Unknown";
            string difficulty = Beatmap.BeatmapInfo.DifficultyName ?? "Easy";

            if (beatmapSet.Beatmaps.Count > 1)
            {
                baseFilename = $"{artist} - {title} ({author})".GetValidFilename();
            }
            else
            {
                baseFilename = $"{artist} - {title} ({author}) [{difficulty}]".GetValidFilename();
            }

            var directory = exportFolderSelector.SelectedDirectory.Value;

            var beatmaps = getBeatmapsFromSet(beatmapSet);

            foreach (var beatmap in beatmaps)
            {
                var stream = getBeatmapStream(beatmap);

                var newDifficulty = beatmap.BeatmapInfo.DifficultyName ?? "Easy";

                var beatmapName = $"{artist} - {title} ({author}) [{newDifficulty}]".GetValidFilename();
                var beatmapPath = Path.Combine(directory, beatmapName + $".osu");

                using (var fs = File.Create(beatmapPath))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fs);
                }

                stream.Dispose();
            }

            // Only add audio file if it exists
            if (audioFile != null)
            {
                var audioStream = workingBeatmap.GetStream(audioFile.File.GetStoragePath());
                if (audioStream != null)
                {
                    var audioPath = Path.Combine(directory, audioFilename);

                    using (var fs = File.Create(audioPath))
                    {
                        audioStream.Seek(0, SeekOrigin.Begin);
                        audioStream.CopyTo(fs);
                    }

                    audioStream.Dispose();
                }
            }

            Logger.Log($"Exporting to folder {directory}...");

            showToast("Export successful", $"Saved to folder {baseFilename}");
        }


        public void ExportMap()
        {

            showToast("Exporting...", "Please wait...");
            if (exportModeBindable.Value == ExportMode.Zip)
            {
                ExportToZip();
            }
            else
            {
                ExportToFolder();
            }
        }


        private partial class BeatmapEditorToast : Toast
        {
            public BeatmapEditorToast(LocalisableString value, string beatmapDisplayName)
                : base(InputSettingsStrings.EditorSection, value, beatmapDisplayName)
            {
            }
        }

        private void showToast(string title, string message)
        {
            onScreenDisplay?.Display(new BeatmapEditorToast(title, message));
        }

        private Bindable<ExportMode> exportModeBindable = new Bindable<ExportMode>(ExportMode.Zip);
        private UbExportFolderSelector exportFolderSelector;

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
                    Caption = "Export your beatmap locally for easy sharing",
                    ButtonText = "Export map",
                    Action = ExportMap,
                },
                new FormEnumDropdown<ExportMode>
                {
                    Caption = "Export as",
                    Current = exportModeBindable,
                },
                exportFolderSelector = new UbExportFolderSelector(false, [@".qetiqpuqloekglxmbnmnbfkworitzuokwjfbmvncvmbndf"]) // some extension that is unlikely to be chosen, so only folders are visible
                {
                    Caption = "Export folder",
                    PlaceholderText = "Select folder to export Unbeatable beatmaps to",
                },

            };
        }

        enum ExportMode
        {
            [Description("Package (.zip file)")]
            Zip,

            [Description("Uncompressed files")]
            Folder
        }

    }


}
