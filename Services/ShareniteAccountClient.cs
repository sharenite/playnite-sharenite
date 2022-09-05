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
using Newtonsoft.Json.Serialization;

namespace Sharenite.Services
{
    class ShareniteAccountClient
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI api;
        private readonly string cookiesPath;
        private readonly Sharenite plugin;
        // DEV
        //private const string protocol = "http://";
        //private const string domain = "localhost";
        //private const string domainDev = ":3000";
        // PROD
        private const string protocol = "https://";
        private const string domain = "www.sharenite.link";
        private const string domainDev = "";
        // URLs
        private const string loginUrl = protocol + domain + domainDev + "/users/sign_in";
        private const string homepageUrl = protocol + domain + domainDev + "/";
        private const string gameListUrl = protocol + domain + domainDev + "/api/v1/games";
        private const string gameDeleteUrl = protocol + domain + domainDev + "/api/v1/games/delete";

        private DefaultContractResolver contractResolver;
        public ShareniteAccountClient(Sharenite plugin, IPlayniteAPI api)
        {
            this.api = api;
            this.plugin = plugin;
            cookiesPath = Path.Combine(plugin.GetPluginUserDataPath(), "cookies.bin");
            contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };
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
                throw new Exception("Problem reading cookies from disk: " + e.GetType());
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

        public string ToJson(object obj, bool formatted = false)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                Formatting = formatted ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Include,
                ContractResolver = contractResolver,
            });
        }

        public async Task SynchroniseGames(GlobalProgressActionArgs args)
        {
            args.Text = "Reading authentication.";
            var cookieContainer = ReadCookiesFromDisk();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                var games = new GamesPost();
                games.games = api.Database.Games.ToList();
                var gamesCount = api.Database.Games.Count;
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

        public async Task UpdateGames(GlobalProgressActionArgs args, List<Playnite.SDK.Models.Game> databaseGames)
        {
            args.Text = "Reading authentication.";
            var cookieContainer = ReadCookiesFromDisk();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                var games = new GamesPost();
                games.games = databaseGames;
                var gamesCount = databaseGames.Count;
                args.Text = "Sending " + gamesCount + " games to Sharenite.";
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

        public async Task DeleteGames(GlobalProgressActionArgs args, List<Playnite.SDK.Models.Game> databaseGames)
        {
            args.Text = "Reading authentication.";
            var cookieContainer = ReadCookiesFromDisk();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                var games = new GamesPost();
                games.games = databaseGames;
                var gamesCount = databaseGames.Count;    
                args.Text = "Removing " + gamesCount + " games from Sharenite.";
                var serializedData = ToJson(games);
                var buffer = Encoding.UTF8.GetBytes(serializedData);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var resp = httpClient.PutAsync(gameDeleteUrl, byteContent).GetAwaiter().GetResult();
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

        public async Task UpdateGame(GlobalProgressActionArgs args, Playnite.SDK.Models.Game databaseGame)
        {
            args.Text = "Reading authentication.";
            var cookieContainer = ReadCookiesFromDisk();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                var game = new GamePutOld();
                game.game = new GamePostOld();
                args.Text = "Reading game playtime";
                game.game.name = databaseGame.Name;
                game.game.added = databaseGame.Added;
                game.game.community_score = databaseGame.CommunityScore;
                game.game.critic_score = databaseGame.CriticScore;
                game.game.description = databaseGame.Description;
                game.game.favorite = databaseGame.Favorite;
                game.game.game_id = databaseGame.GameId;
                game.game.game_started_script = databaseGame.GameStartedScript;
                game.game.hidden = databaseGame.Hidden;
                game.game.include_library_plugin_action = databaseGame.IncludeLibraryPluginAction;
                game.game.install_directory = databaseGame.InstallDirectory;
                game.game.is_custom_game = databaseGame.IsCustomGame;
                game.game.is_installed = databaseGame.IsInstalled;
                game.game.is_installing = databaseGame.IsInstalling;
                game.game.is_launching = databaseGame.IsLaunching;
                game.game.is_running = databaseGame.IsRunning;
                game.game.is_uninstalling = databaseGame.IsUninstalling;
                game.game.last_activity = databaseGame.LastActivity;
                game.game.manual = databaseGame.Manual;
                game.game.modified = databaseGame.Modified;
                game.game.notes = databaseGame.Notes;
                game.game.play_count = databaseGame.PlayCount;
                game.game.playnite_id = databaseGame.Id;
                game.game.playtime = databaseGame.Playtime;
                game.game.plugin_id = databaseGame.PluginId;
                game.game.post_script = databaseGame.PostScript;
                game.game.pre_script = databaseGame.PreScript;
                //tempGame.release_date = game.ReleaseDate;
                game.game.sorting_name = databaseGame.SortingName;
                game.game.use_global_game_started_script = databaseGame.UseGlobalGameStartedScript;
                game.game.use_global_post_script = databaseGame.UseGlobalPostScript;
                game.game.use_global_pre_script = databaseGame.UseGlobalPreScript;
                game.game.user_score = databaseGame.UserScore;
                game.game.version = databaseGame.Version;
                if (args.CancelToken.IsCancellationRequested)
                {
                    return;
                }

                args.Text = "Sending Playtime game to Sharenite.";
                var serializedData = ToJson(game);
                var buffer = Encoding.UTF8.GetBytes(serializedData);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var resp = httpClient.PutAsync(gameListUrl + "/" + databaseGame.Id, byteContent).GetAwaiter().GetResult();
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
