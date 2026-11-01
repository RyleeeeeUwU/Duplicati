using System;
using System.IO;
using Duplicati.Configuration;
using Duplicati.Core;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Utilities;
using IPALogger = IPA.Logging.Logger;

namespace Duplicati
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }
        internal static PluginConfig Settings { get; private set; }

        [Init]
        public Plugin(IPALogger logger, Config config)
        {
            Log = logger;
            Settings = config.Generated<PluginConfig>();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            if (!Settings.Enabled)
            {
                Log.Info("Duplicati is disabled in UserData/Duplicati.json.");
                return;
            }

            try
            {
                string customLevels = Path.Combine(UnityGame.InstallPath, "Beat Saber_Data", "CustomLevels");

                var options = new CleanOptions
                {
                    DryRun = Settings.DryRun,
                    PermanentlyDelete = Settings.PermanentlyDelete,
                    QuarantineDirectory = Path.Combine(UnityGame.UserDataPath, "Duplicati", "Removed"),
                };

                Log.Info("Scanning " + customLevels + (options.DryRun ? " (dry run)" : string.Empty));
                CleanSummary summary = LibraryCleaner.Run(customLevels, options, new IpaLog(Log));

                Log.Info(string.Format(
                    "Done in {0:0.0}s: {1} levels scanned, {2} skipped, {3} duplicate sets, {4} removed, {5} failed.",
                    summary.Elapsed.TotalSeconds, summary.LevelsScanned, summary.LevelsSkipped,
                    summary.DuplicateGroups, summary.Removed, summary.Failed));
            }
            catch (Exception ex)
            {
                // A cleanup problem must never take the game down.
                Log.Error("Duplicati failed: " + ex);
            }
        }

        [OnExit]
        public void OnApplicationQuit()
        {
        }

        private sealed class IpaLog : Core.ILog
        {
            private readonly IPALogger _logger;

            public IpaLog(IPALogger logger)
            {
                _logger = logger;
            }

            public void Info(string message)
            {
                _logger.Info(message);
            }

            public void Warn(string message)
            {
                _logger.Warn(message);
            }
        }
    }
}
