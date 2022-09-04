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
        private List<Guid> gameIdsToRemove;
        private List<Guid> gameIdsToUpdate;
        private System.Timers.Timer timer;

        private ShareniteSettingsViewModel settings { get; set; }
        public override Guid Id { get; } = Guid.Parse("62d53f25-4e62-4f27-a49e-89e73cb1fd48");

        public Sharenite(IPlayniteAPI api) : base(api)
        {
            settings = new ShareniteSettingsViewModel(this, api);
            dialogs = api.Dialogs;
            api.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
            api.Database.Games.ItemUpdated += Games_ItemUpdated;
            timer = new System.Timers.Timer();
            timer.Elapsed += (_,__) => handleTimerUpdate();
            timer.AutoReset = false;
            timer.Interval = 5000;
            gameIdsToRemove = new List<Guid>();
            gameIdsToUpdate = new List<Guid>();

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

        private void handleTimerUpdate()
        {            
            timer.Stop();

            // Remove pending games
            List<Game> gamesToRemove = new List<Game>();
            int toRemove = 0;
            foreach (Guid id in gameIdsToRemove)
            {
                Game game = new Game();
                game.Id = id;
                gamesToRemove.Add(game);
                toRemove++;
            }
            gameIdsToRemove.RemoveRange(0, toRemove);
            if (gamesToRemove.Count > 0)
            {
                RemoveGames(gamesToRemove.GroupBy(game => game.Id).Select(group => group.First()).Cast<Game>().ToList());
            }

            // Update pending games
            List<Game> gamesToUpdate = new List<Game>();
            int toUpdate = 0;
            foreach (Guid id in gameIdsToUpdate)
            {
                Game gameToUpdate = PlayniteApi.Database.Games.FirstOrDefault(a => a.Id == id);
                if (gameToUpdate != null)
                {
                    gamesToUpdate.Add(gameToUpdate);
                }
                toUpdate++;
            }
            gameIdsToUpdate.RemoveRange(0, toUpdate);
            if (gamesToUpdate.Count > 0)
            {
                UpdateGames(gamesToUpdate.GroupBy(game => game.Id).Select(group => group.First()).Cast<Game>().ToList());
            }
        }

        private void RestartTimer()
        {
            timer.Stop();
            timer.Start();
        }

        public void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> args)
        {
            if (this.LoadPluginSettings<ShareniteSettings>().keepInSync)
            {
                var list = args.UpdatedItems.Select(item => item.NewData).Select(game => game.Id);
                if (list.Count() > 0)
                {
                    gameIdsToUpdate.AddRange(list);
                }
            }
            RestartTimer();
        }

        public void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> args)
        {
            if (this.LoadPluginSettings<ShareniteSettings>().keepInSync)
            {
                if (args.AddedItems.Count > 0)
                {
                    gameIdsToUpdate.AddRange(args.AddedItems.Select(game => game.Id));
                }
                if (args.RemovedItems.Count > 0)
                {
                    gameIdsToRemove.AddRange(args.RemovedItems.Select(game => game.Id));
                }
                RestartTimer();
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
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
            handleTimerUpdate();
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