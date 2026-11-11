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
            MainMenuAwaiter.MainMenuInitializing += RegisterMenuButton;
            Log.Info("Duplicati loaded. Open Mods > Duplicati to scan for duplicate maps.");
        }

        private static int _registrations;
        
        private static void RegisterMenuButton()
        {
            try
            {
                UnregisterMenuButton();

                _flowCoordinator = BeatSaberUI.CreateFlowCoordinator<DuplicatiFlowCoordinator>();
                _menuButton = new MenuButton("Duplicati", ShowFlowCoordinator);
                MenuButtons.Instance.RegisterButton(_menuButton);

                _registrations++;
                Log.Info("Registered the Duplicati menu button (menu build #" + _registrations + ").");
            }
            catch (Exception ex)
            {
                Log.Error("Could not register the Duplicati menu button: " + ex);
            }
        }
        
        private static void UnregisterMenuButton()
        {
            if (_menuButton == null)
                return;

            try
            {
                if (MenuButtons.Instance != null)
                    MenuButtons.Instance.UnregisterButton(_menuButton);
            }
            catch (Exception ex)
            {
                Log.Warn("Could not unregister the previous Duplicati menu button (continuing): " + ex.Message);
            }
            finally
            {
                _menuButton = null;
            }
        }

        private static void ShowFlowCoordinator()
        {
            try
            {
                if (_flowCoordinator == null)
                    _flowCoordinator = BeatSaberUI.CreateFlowCoordinator<DuplicatiFlowCoordinator>();

                BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_flowCoordinator);
            }
            catch (Exception ex)
            {
                Log.Error("Could not open the Duplicati menu: " + ex);
            }
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            MainMenuAwaiter.MainMenuInitializing -= RegisterMenuButton;
            UnregisterMenuButton();
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
