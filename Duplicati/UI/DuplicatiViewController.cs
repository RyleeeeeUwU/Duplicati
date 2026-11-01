using System;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Duplicati.Core;
using TMPro;

namespace Duplicati.UI
{
    [ViewDefinition("Duplicati.UI.Duplicati.bsml")]
    internal sealed class DuplicatiViewController : BSMLAutomaticViewController
    {
        [UIComponent("statusText")]
        private TextMeshProUGUI _statusText;

        private SynchronizationContext _uiContext;
        private int _scanRunning;

        public bool PermanentlyDelete
        {
            get { return Plugin.Settings.PermanentlyDelete; }
            set { Plugin.Settings.PermanentlyDelete = value; }
        }

        public bool DryRun
        {
            get { return Plugin.Settings.DryRun; }
            set { Plugin.Settings.DryRun = value; }
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            // Capture Unity's synchronization context so worker-thread results can safely
            // update TextMeshPro on the game's main thread.
            _uiContext = SynchronizationContext.Current;
            if (_statusText != null)
                _statusText.text = "Ready. Select an option, then scan your songs.";
        }

        [UIAction("StartScan")]
        private void StartScan()
        {
            if (Interlocked.CompareExchange(ref _scanRunning, 1, 0) != 0)
                return;

            SynchronizationContext uiContext = SynchronizationContext.Current ?? _uiContext;
            if (uiContext == null)
            {
                Interlocked.Exchange(ref _scanRunning, 0);
                SetStatus("Unable to start: the Unity UI context is unavailable. Relaunch Beat Saber and try again.");
                Plugin.Log.Error("Could not start cleanup because no Unity SynchronizationContext was available.");
                return;
            }

            bool permanentlyDelete = PermanentlyDelete;
            bool dryRun = DryRun;
            string customLevelsPath = Plugin.CustomLevelsPath;
            string quarantinePath = Plugin.QuarantinePath;

            SetStatus("Scanning... This will take a while, don't panic :3");
            Plugin.Log.Info("User requested duplicate scan" + (dryRun ? " (dry run)" : string.Empty) + ".");

            Task.Run(() =>
            {
                string finalStatus;
                try
                {
                    var options = new CleanOptions
                    {
                        DryRun = dryRun,
                        PermanentlyDelete = permanentlyDelete,
                        QuarantineDirectory = quarantinePath,
                    };

                    CleanSummary summary = LibraryCleaner.Run(
                        customLevelsPath,
                        options,
                        new Plugin.IpaLog(Plugin.Log));

                    if (summary.CustomLevelsFolderMissing)
                    {
                        finalStatus = "CustomLevels folder not found. Nothing was changed.";
                    }
                    else if (dryRun)
                    {
                        finalStatus = string.Format(
                            "Dry run complete: found {0} duplicates. No files were changed.",
                            summary.DuplicatesFound);
                    }
                    else
                    {
                        finalStatus = string.Format(
                            "Successfully removed {0} duplicates.", summary.Removed);

                        if (summary.Failed > 0)
                            finalStatus += string.Format(" {0} could not be removed.", summary.Failed);
                    }

                    Plugin.Log.Info(string.Format(
                        "Done in {0:0.0}s: {1} levels scanned, {2} skipped, {3} duplicate sets, " +
                        "{4} duplicate copies found, {5} removed, {6} failed.",
                        summary.Elapsed.TotalSeconds, summary.LevelsScanned, summary.LevelsSkipped,
                        summary.DuplicateGroups, summary.DuplicatesFound, summary.Removed, summary.Failed));
                }
                catch (Exception ex)
                {
                    finalStatus = "Scan failed. Check the Beat Saber log for details.";
                    Plugin.Log.Error("Duplicati scan failed: " + ex);
                }
                try
                {
                    uiContext.Post(_ =>
                    {
                        SetStatus(finalStatus);
                        Interlocked.Exchange(ref _scanRunning, 0);
                    }, null);
                }
                catch (Exception ex)
                {
                    Interlocked.Exchange(ref _scanRunning, 0);
                    Plugin.Log.Error("Could not post the scan result to the Duplicati UI: " + ex);
                }
            });
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }
    }
}
