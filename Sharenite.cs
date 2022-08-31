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
        private readonly IDialogsFactory dialogs;
        
        private ShareniteSettingsViewModel settings { get; set; }
        public override Guid Id { get; } = Guid.Parse("62d53f25-4e62-4f27-a49e-89e73cb1fd48");

        public Sharenite(IPlayniteAPI api) : base(api)
        {
            settings = new ShareniteSettingsViewModel(this, api);
            dialogs = api.Dialogs;
            api.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
            api.Database.Games.ItemUpdated += Games_ItemUpdated;
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
                Description = "Resynchronize library with Sharenite",
                Action = (arguments) => SynchroniseGames()
            };
        }


        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            return new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Description = "Synchronize game with Sharenite",
                    Action = a =>
                    {
                        UpdateGames(args.Games);
                    }
                }
            };
        }

        public void SynchroniseGames()
        {
            var clientApi = new ShareniteAccountClient(this, PlayniteApi);
            var scanRes = dialogs.ActivateGlobalProgress((args) =>
                {
                    clientApi.SynchroniseGames(args).GetAwaiter().GetResult();
                },
                new GlobalProgressOptions("Kicking off a full Sharenite resynchronisation.", true)
                {
                    IsIndeterminate = false
                }
            );

            if (scanRes.Error != null)
            {
                logger.Error(scanRes.Error, "Sharenite synchronization failed.");
                dialogs.ShowErrorMessage("Sharenite synchronization failed." + "\n" + scanRes.Error.Message, "");
            }
        }

        public void UpdateGames(List<Game> games)
        {
            var clientApi = new ShareniteAccountClient(this, PlayniteApi);
            var scanRes = dialogs.ActivateGlobalProgress((args) =>
            {
                clientApi.UpdateGames(args, games).GetAwaiter().GetResult();
            },
                new GlobalProgressOptions("Kicking off a Sharenite games update.", true)
                {
                    IsIndeterminate = false
                }
            );

            if (scanRes.Error != null)
            {
                logger.Error(scanRes.Error, "Sharenite synchronization failed.");
                dialogs.ShowErrorMessage("Sharenite synchronization failed." + "\n" + scanRes.Error.Message, "");
            }
        }
        public void RemoveGames(List<Game> games)
        {
            var clientApi = new ShareniteAccountClient(this, PlayniteApi);
            var scanRes = dialogs.ActivateGlobalProgress((args) =>
            {
                clientApi.DeleteGames(args, games).GetAwaiter().GetResult();
            },
                new GlobalProgressOptions("Kicking off a Sharenite games update.", true)
                {
                    IsIndeterminate = false
                }
            );

            if (scanRes.Error != null)
            {
                logger.Error(scanRes.Error, "Sharenite synchronization failed.");
                dialogs.ShowErrorMessage("Sharenite synchronization failed." + "\n" + scanRes.Error.Message, "");
            }
        }


        public void UpdateGame(Game game)
        {
            var clientApi = new ShareniteAccountClient(this, PlayniteApi);
            var scanRes = dialogs.ActivateGlobalProgress((args) =>
            {
                clientApi.UpdateGame(args, game).GetAwaiter().GetResult();
            },
                new GlobalProgressOptions("Kicking off a Sharenite games update.", true)
                {
                    IsIndeterminate = false
                }
            );

            if (scanRes.Error != null)
            {
                logger.Error(scanRes.Error, "Sharenite synchronization failed.");
                dialogs.ShowErrorMessage("Sharenite synchronization failed." + "\n" + scanRes.Error.Message, "");
            }
        }

        public void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> args)
        {
            if (this.LoadPluginSettings<ShareniteSettings>().keepInSync)
            {
                List<Game> updatedGames = new List<Game>();
                foreach (var updatedItem in args.UpdatedItems)
                {
                    updatedGames.Add(updatedItem.NewData);
                }
                UpdateGames(updatedGames);
            }
        }

        public void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> args)
        {
            if (this.LoadPluginSettings<ShareniteSettings>().keepInSync)
            {
                if (args.AddedItems.Count > 0)
                {
                    UpdateGames(args.AddedItems);
                }
                if (args.RemovedItems.Count > 0)
                {
                    RemoveGames(args.RemovedItems);
                }
            }
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
            if (settings.Settings.resyncAfterBigUpdate01)
            {
                SynchroniseGames();
                settings.BeginEdit();
                settings.Settings.resyncAfterBigUpdate01 = false;
                settings.EndEdit();
            }
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