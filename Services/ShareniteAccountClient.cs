using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Web;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using Sharenite.Models;
using Newtonsoft.Json;

namespace Sharenite.Services
{
    class ShareniteAccountClient
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI api;
        private readonly string cookiesPath;
        private readonly Sharenite plugin;
        // DEV
        // private const string protocol = "http://";
        // private const string domain = "localhost";
        // private const string domainDev = ":3000";
        // PROD
        private const string protocol = "https://";
        private const string domain = "www.sharenite.link";
        private const string domainDev = "";
        // URLs
        private const string loginUrl = protocol + domain + domainDev + "/users/sign_in";
        private const string homepageUrl = protocol + domain + domainDev + "/";
        private const string gameListUrl = protocol + domain + domainDev + "/api/v1/games";
        public ShareniteAccountClient(Sharenite plugin, IPlayniteAPI api)
        {
            this.api = api;
            this.plugin = plugin;
            cookiesPath = Path.Combine(plugin.GetPluginUserDataPath(), "cookies.bin");
        }

        public void Login()
        {
            var loggedIn = false;


            using (var view = api.WebViews.CreateView(580, 700))
            {
                view.LoadingChanged += (s, e) =>
                {
                    var address = view.GetCurrentAddress();
                    if (address.Equals(homepageUrl))
                    {
                        loggedIn = true;
                        view.Close();
                    }
                };

                view.DeleteDomainCookies(domain);
                view.Navigate(loginUrl);
                view.OpenDialog();
            }

            if (!loggedIn)
            {
                return;
            }

            dumpCookies();

            return;
        }

        private IEnumerable<Playnite.SDK.HttpCookie> dumpCookies()
        {
            var view = api.WebViews.CreateOffscreenView();

            var cookies = view.GetCookies();


            var cookieContainer = new CookieContainer();
            foreach (var cookie in cookies)
            {
                if (cookie.Domain == domain)
                {
                    cookieContainer.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
                }
            }

            WriteCookiesToDisk(cookieContainer);

            view.Dispose();
            return cookies;
        }

        private void WriteCookiesToDisk(CookieContainer cookieJar)
        {
            File.Delete(cookiesPath);
            using (Stream stream = File.Create(cookiesPath))
            {
                try
                {
                    Console.Out.Write("Writing cookies to disk... ");
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cookieJar);
                    Console.Out.WriteLine("Done.");
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine("Problem writing cookies to disk: " + e.GetType());
                }
            }
        }

        private CookieContainer ReadCookiesFromDisk()
        {
            try
            {
                using (Stream stream = File.Open(cookiesPath, FileMode.Open))
                {
                    Console.Out.Write("Reading cookies from disk... ");
                    BinaryFormatter formatter = new BinaryFormatter();
                    Console.Out.WriteLine("Done.");
                    return (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Problem reading cookies from disk: " + e.GetType());
                return new CookieContainer();
            }
        }

        public async Task CheckAuthentication()
        {
            logger.Debug(String.Join("\n", Directory.GetFiles(plugin.GetPluginUserDataPath())));
            if (!File.Exists(cookiesPath))
            {
                throw new Exception("User is not authenticated: token file doesn't exist.");
            }
            else
            {
                if (!await GetIsUserLoggedIn())
                {
                    TryRefreshCookies();
                    if (!await GetIsUserLoggedIn())
                    {
                        throw new Exception("User is not authenticated.");
                    }
                }
            }
        }

        public static string ToJson(object obj, bool formatted = false)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings()
            {
                Formatting = formatted ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Include,
            });
        }

        public async Task SynchroniseGames(GlobalProgressActionArgs args)
        {
            try
            {
                args.Text = "Reading authentication.";
                var cookieContainer = ReadCookiesFromDisk();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                using (var httpClient = new HttpClient(handler))
                {
                    var games = new GamesPost();
                    games.games = new List<GamePost>();
                    var gamesCount = api.Database.Games.Count;
                    args.ProgressMaxValue = gamesCount;
                    args.Text = "Synchronising games (processing 0 out of " + gamesCount + ")";
                    int index = 0;
                    foreach (var game in api.Database.Games)
                    {
                        index++;
                        if (index % 50 == 0)
                        {
                            args.CurrentProgressValue = index;
                            args.Text = "Synchronising games (processing " + index + " out of " + gamesCount + ")";
                        }                        
                        var tempGame = new GamePost();
                        tempGame.name = game.Name;
                        tempGame.added = game.Added;
                        tempGame.community_score = game.CommunityScore;
                        tempGame.critic_score = game.CriticScore;
                        tempGame.description = game.Description;
                        tempGame.favorite = game.Favorite;
                        tempGame.game_id = game.GameId;
                        tempGame.game_started_script = game.GameStartedScript;
                        tempGame.hidden = game.Hidden;
                        tempGame.include_library_plugin_action = game.IncludeLibraryPluginAction;
                        tempGame.install_directory = game.InstallDirectory;
                        tempGame.is_custom_game = game.IsCustomGame;
                        tempGame.is_installed = game.IsInstalled;
                        tempGame.is_installing = game.IsInstalling;
                        tempGame.is_launching = game.IsLaunching;
                        tempGame.is_running = game.IsRunning;
                        tempGame.is_uninstalling = game.IsUninstalling;
                        tempGame.last_activity = game.LastActivity;
                        tempGame.manual = game.Manual;
                        tempGame.modified = game.Modified;
                        tempGame.notes = game.Notes;
                        tempGame.play_count = game.PlayCount;
                        tempGame.playnite_id = game.Id;
                        tempGame.playtime = game.Playtime;
                        tempGame.plugin_id = game.PluginId;
                        tempGame.post_script = game.PostScript;
                        tempGame.pre_script = game.PreScript;
                        //tempGame.release_date = game.ReleaseDate;
                        tempGame.sorting_name = game.SortingName;
                        tempGame.use_global_game_started_script = game.UseGlobalGameStartedScript;
                        tempGame.use_global_post_script = game.UseGlobalPostScript;
                        tempGame.use_global_pre_script = game.UseGlobalPreScript;
                        tempGame.user_score = game.UserScore;
                        tempGame.version = game.Version;
                        games.games.Add(tempGame);
                        if (args.CancelToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    args.Text = "Sending " + gamesCount + " game to Sharenite.";
                    var serializedData = ToJson(games);
                    var buffer = Encoding.UTF8.GetBytes(serializedData);
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var resp = httpClient.PostAsync(gameListUrl, byteContent).GetAwaiter().GetResult();
                    var strResponse = await resp.Content.ReadAsStringAsync();
                    if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new Exception("User is not authenticated.");
                    }
                    else if (resp.StatusCode != HttpStatusCode.Accepted)
                    {
                        ErrorGeneric errorGeneric;
                        Serialization.TryFromJson(strResponse, out errorGeneric);
                        if (errorGeneric != null) {
                            throw new Exception(errorGeneric.error);
                        }
                        else
                        {
                            throw new Exception(strResponse);
                        }
                    }
                }
                return;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                logger.Error(e, "Failed to synchronise games.");
                return;
            }
        }

        public async Task UpdateGames(GlobalProgressActionArgs args, List<Playnite.SDK.Models.Game> databaseGames)
        {
            try
            {
                args.Text = "Reading authentication.";
                var cookieContainer = ReadCookiesFromDisk();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                using (var httpClient = new HttpClient(handler))
                {
                    var games = new GamesPost();
                    games.games = new List<GamePost>();
                    var gamesCount = databaseGames.Count;
                    args.ProgressMaxValue = gamesCount;
                    args.Text = "Synchronising games (processing 0 out of " + gamesCount + ")";
                    int index = 0;
                    foreach (var game in databaseGames)
                    {
                        index++;
                        if (index % 50 == 0)
                        {
                            args.CurrentProgressValue = index;
                            args.Text = "Synchronising games (processing " + index + " out of " + gamesCount + ")";
                        }
                        var tempGame = new GamePost();
                        tempGame.name = game.Name;
                        tempGame.added = game.Added;
                        tempGame.community_score = game.CommunityScore;
                        tempGame.critic_score = game.CriticScore;
                        tempGame.description = game.Description;
                        tempGame.favorite = game.Favorite;
                        tempGame.game_id = game.GameId;
                        tempGame.game_started_script = game.GameStartedScript;
                        tempGame.hidden = game.Hidden;
                        tempGame.include_library_plugin_action = game.IncludeLibraryPluginAction;
                        tempGame.install_directory = game.InstallDirectory;
                        tempGame.is_custom_game = game.IsCustomGame;
                        tempGame.is_installed = game.IsInstalled;
                        tempGame.is_installing = game.IsInstalling;
                        tempGame.is_launching = game.IsLaunching;
                        tempGame.is_running = game.IsRunning;
                        tempGame.is_uninstalling = game.IsUninstalling;
                        tempGame.last_activity = game.LastActivity;
                        tempGame.manual = game.Manual;
                        tempGame.modified = game.Modified;
                        tempGame.notes = game.Notes;
                        tempGame.play_count = game.PlayCount;
                        tempGame.playnite_id = game.Id;
                        tempGame.playtime = game.Playtime;
                        tempGame.plugin_id = game.PluginId;
                        tempGame.post_script = game.PostScript;
                        tempGame.pre_script = game.PreScript;
                        //tempGame.release_date = game.ReleaseDate;
                        tempGame.sorting_name = game.SortingName;
                        tempGame.use_global_game_started_script = game.UseGlobalGameStartedScript;
                        tempGame.use_global_post_script = game.UseGlobalPostScript;
                        tempGame.use_global_pre_script = game.UseGlobalPreScript;
                        tempGame.user_score = game.UserScore;
                        tempGame.version = game.Version;
                        games.games.Add(tempGame);
                        if (args.CancelToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    args.Text = "Sending " + gamesCount + " game to Sharenite.";
                    var serializedData = ToJson(games);
                    var buffer = Encoding.UTF8.GetBytes(serializedData);
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var resp = httpClient.PutAsync(gameListUrl, byteContent).GetAwaiter().GetResult();
                    var strResponse = await resp.Content.ReadAsStringAsync();
                    if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new Exception("User is not authenticated.");
                    }
                    else if (resp.StatusCode != HttpStatusCode.Accepted)
                    {
                        ErrorGeneric errorGeneric;
                        Serialization.TryFromJson(strResponse, out errorGeneric);
                        if (errorGeneric != null)
                        {
                            throw new Exception(errorGeneric.error);
                        }
                        else
                        {
                            throw new Exception(strResponse);
                        }
                    }
                }
                return;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                logger.Error(e, "Failed to synchronise games.");
                return;
            }
        }

        public async Task<bool> GetIsUserLoggedIn()
        {
            if (!File.Exists(cookiesPath))
            {
                return false;
            }

            try
            {
                var cookieContainer = ReadCookiesFromDisk();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                using (var httpClient = new HttpClient(handler))
                {
                    var resp = httpClient.GetAsync(gameListUrl).GetAwaiter().GetResult();
                    var strResponse = await resp.Content.ReadAsStringAsync();
                    if (Serialization.TryFromJson<ErrorUnathorized>(strResponse, out var error) && error.error == "401 Forbidden")
                    {
                        return false;
                    }

                    //if (Serialization.TryFromJson<List<Game>>(strResponse, out var games) && games != null)
                    //{
                        return true;
                    //}
                }
                return false;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                logger.Error(e, "Failed to check if user is authenticated into Sharenite.");
                return false;
            }
        }

        private void TryRefreshCookies()
        {
            string address;
            using (var webView = api.WebViews.CreateOffscreenView())
            {
                webView.LoadingChanged += (s, e) =>
                {
                    address = webView.GetCurrentAddress();
                    webView.Close();
                };

                webView.NavigateAndWait(loginUrl);
            }

            dumpCookies();
        }
    }
}
