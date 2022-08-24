using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using Sharenite.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Sharenite
{
    public class Sharenite : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ShareniteSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("62d53f25-4e62-4f27-a49e-89e73cb1fd48");

        public Sharenite(IPlayniteAPI api) : base(api)
        {
            settings = new ShareniteSettingsViewModel(this, api);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        // To add new main menu items override GetMainMenuItems
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                Description = "Sharenite",
                Action = (arguments) => SynchroniseGames()
            };
        }

        public void SynchroniseGames()
        {
            var clientApi = new ShareniteAccountClient(this, PlayniteApi);
            clientApi.SynchroniseGames();
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new ShareniteSettingsView();
        }
    }
}