using System;
using System.IO;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using Duplicati.Configuration;
using Duplicati.Core;
using Duplicati.UI;
using HMUI;
using IPA;
using IPA.Config.Stores;
using IPA.Utilities;
using BeatSaberMarkupLanguage.Util;
using IPALogger = IPA.Logging.Logger;

namespace Duplicati
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }
        internal static PluginConfig Settings { get; private set; }

        private static MenuButton _menuButton;
        private static DuplicatiFlowCoordinator _flowCoordinator;

        [Init]
        public Plugin(IPALogger logger, IPA.Config.Config config)
        {
            Log = logger;
            Settings = config.Generated<PluginConfig>();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            // BSML raises this after its main-menu objects (including MenuButtons) are ready.
            // This deliberately does not scan or modify the song library at startup.
            MainMenuAwaiter.MainMenuInitializing += RegisterMenuButton;
            Log.Info("Duplicati loaded. Open Mods > Duplicati to scan for duplicate maps.");
        }

        private static void RegisterMenuButton()
        {
            try
            {
                if (_menuButton != null)
                    return;

                _flowCoordinator = BeatSaberUI.CreateFlowCoordinator<DuplicatiFlowCoordinator>();
                _menuButton = new MenuButton("Duplicati", ShowFlowCoordinator);
                MenuButtons.Instance.RegisterButton(_menuButton);
                Log.Info("Registered the Duplicati menu button.");
            }
            catch (Exception ex)
            {
                Log.Error("Could not register the Duplicati menu button: " + ex);
            }
        }

        private static void ShowFlowCoordinator()
        {
            if (_flowCoordinator == null)
                return;

            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_flowCoordinator);
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            MainMenuAwaiter.MainMenuInitializing -= RegisterMenuButton;

            if (_menuButton != null && MenuButtons.Instance != null)
            {
                MenuButtons.Instance.UnregisterButton(_menuButton);
                _menuButton = null;
            }
        }

        internal static string CustomLevelsPath
        {
            get { return Path.Combine(UnityGame.InstallPath, "Beat Saber_Data", "CustomLevels"); }
        }

        internal static string QuarantinePath
        {
            get { return Path.Combine(UnityGame.UserDataPath, "Duplicati", "Removed"); }
        }

        internal sealed class IpaLog : Core.ILog
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
