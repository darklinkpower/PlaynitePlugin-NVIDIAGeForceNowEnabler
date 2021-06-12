using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace NVIDIAGeForceNowEnabler
{
    public class NVIDIAGeForceNowEnabler : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private NVIDIAGeForceNowEnablerSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("5f2dfd12-5f13-46fe-bcdd-64eb53ace26a");

        public NVIDIAGeForceNowEnabler(IPlayniteAPI api) : base(api)
        {
            settings = new NVIDIAGeForceNowEnablerSettings(this);
        }

        public override void OnApplicationStarted()
        {
            if (settings.ExecuteOnStartup == true)
            {
                MainMethod(false);
            }
        }

        public override void OnLibraryUpdated()
        {
            if (settings.ExecuteOnLibraryUpdate == true)
            {
                MainMethod(false);
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new NVIDIAGeForceNowEnablerSettingsView();
        }
         
        public override List<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs menuArgs)
        {
            return new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = "Update enabled status of games",
                    MenuSection = "@NVIDIA GeForce NOW Enabler",
                    Action = args => {
                        MainMethod(true);
                    }
                },
                new MainMenuItem
                {
                    Description = "Remove Play Action from all games",
                    MenuSection = "@NVIDIA GeForce NOW Enabler",
                    Action = args => {
                        LibraryRemoveAllPlayActions();
                    }
                },
            };
        }
        
        public bool RemoveFeature(Game game, GameFeature feature)
        {
            if (game.FeatureIds != null)
            {
                if (game.FeatureIds.Contains(feature.Id))
                {
                    game.FeatureIds.Remove(feature.Id);
                    PlayniteApi.Database.Games.Update(game);
                    bool featureRemoved = true;
                    return featureRemoved;
                }
                else
                {
                    bool featureRemoved = false; 
                    return featureRemoved;
                }
            }
            else
            {
                bool featureRemoved = false;
                return featureRemoved;
            }
        }

        public bool AddFeature(Game game, GameFeature feature)
        {
            if (game.FeatureIds == null)
            {
                game.FeatureIds = new List<Guid> { feature.Id };
                PlayniteApi.Database.Games.Update(game);
                bool featureAdded = true;
                return featureAdded;
            }
            else if (game.FeatureIds.Contains(feature.Id) == false)
            {
                game.FeatureIds.AddMissing(feature.Id);
                PlayniteApi.Database.Games.Update(game);
                bool featureAdded = true;
                return featureAdded;
            }
            else
            {
                bool featureAdded = false;
                return featureAdded;
            }
        }

        public void AddOtherAction(Game game, GameAction gameAction)
        {
            if (game.OtherActions == null)
            {
                game.OtherActions = new System.Collections.ObjectModel.ObservableCollection<GameAction> { gameAction };
            }
            else
            {
                game.OtherActions.AddMissing(gameAction);
            }
        }

        public string UpdateNvidiaAction(Game game, GeforceGame supportedGame, string geforceNowWorkingPath, string geforceNowExecutablePath)
        {
            GameAction geforceNowAction = null;
            if (game.OtherActions != null)
            {
                geforceNowAction = game.OtherActions.Where(x => x.Arguments != null).Where(x => Regex.IsMatch(x.Arguments, @"--url-route=""#\?cmsId=\d+&launchSource=External&shortName=game_gfn_pc&parentGameId=""")).FirstOrDefault();
            }

            if (supportedGame == null && geforceNowAction != null)
            {
                game.OtherActions.Remove(geforceNowAction);
                PlayniteApi.Database.Games.Update(game);
                string result = "playActionRemoved";
                return result;

            }
            else if (supportedGame != null && geforceNowAction == null)
            {
                GameAction nvidiaGameAction = new GameAction();
                nvidiaGameAction.Name = "Launch in Nvidia GeForce NOW";
                var playActionArguments = String.Format("--url-route=\"#?cmsId={0}&launchSource=External&shortName=game_gfn_pc&parentGameId=\"", supportedGame.Id);
                nvidiaGameAction.Arguments = playActionArguments;
                nvidiaGameAction.Path = geforceNowExecutablePath;
                nvidiaGameAction.WorkingDir = geforceNowWorkingPath;

                AddOtherAction(game, nvidiaGameAction);
                PlayniteApi.Database.Games.Update(game);
                string result = "playActionAdded";
                return result;
            }
            else
            {
                string result = null;
                return result;
            }
        }

        public List<GeforceGame> DownloadGameList(string uri, bool showDialogs)
        {
            var supportedGames = new List<GeforceGame>();

            var progRes = PlayniteApi.Dialogs.ActivateGlobalProgress((a) => {
                using (var webClient = new WebClient())
                {
                    try
                    {
                        webClient.Encoding = Encoding.UTF8;
                        string downloadedString = webClient.DownloadString(uri);
                        supportedGames = JsonConvert.DeserializeObject<List<GeforceGame>>(downloadedString);
                        foreach (var supportedGame in supportedGames)
                        {
                            supportedGame.Title = Regex.Replace(supportedGame.Title, @"[^\p{L}\p{Nd}]", "").ToLower();
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, e.Message);
                        if (showDialogs == true)
                        {
                            PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "NVIDIA GeForce NOW Enabler");
                        }
                    }
                }
            }, new GlobalProgressOptions("Downloading NVIDIA GeForce Now database..."));
            
            return supportedGames;
        }

        public IEnumerable<Game> GetGamesSupportedLibraries()
        {
            List<Guid> supportedLibraries = new List<Guid>() {
                BuiltinExtensions.GetIdFromExtension(BuiltinExtension.EpicLibrary),
                BuiltinExtensions.GetIdFromExtension(BuiltinExtension.OriginLibrary),
                BuiltinExtensions.GetIdFromExtension(BuiltinExtension.SteamLibrary),
                BuiltinExtensions.GetIdFromExtension(BuiltinExtension.UplayLibrary)
            };

            var gameDatabase = PlayniteApi.Database.Games.Where(g => supportedLibraries.Contains(g.PluginId));
            return gameDatabase;
        }

        public void MainMethod(bool showDialogs)
        {
            string featureName = "NVIDIA GeForce NOW";
            GameFeature feature = PlayniteApi.Database.Features.Add(featureName);

            string localAppData = Environment.GetEnvironmentVariable("LocalAppData");
            string geforceNowWorkingPath = Path.Combine(localAppData, "NVIDIA Corporation", "GeForceNOW", "CEF");
            string geforceNowExecutablePath = Path.Combine(geforceNowWorkingPath, "GeForceNOWStreamer.exe");
            
            var supportedGames = DownloadGameList("https://static.nvidiagrid.net/supported-public-game-list/gfnpc.json", showDialogs);
            if (supportedGames.Count() == 0)
            {
                return;
            }
            var supportedSteamGames = supportedGames.Where(g => g.Store == "Steam");
            var supportedEpicGames = supportedGames.Where(g => g.Store == "Epic");
            var supportedOriginGames = supportedGames.Where(g => g.Store == "Origin");
            var supportedUplayGames = supportedGames.Where(g => g.Store == "Ubisoft Connect");

            int enabledGamesCount = 0; 
            int featureAddedCount = 0;
            int featureRemovedCount = 0;
            int playActionAddedCount = 0;
            int playActionRemovedCount = 0;
            int setAsInstalledCount = 0;
            int setAsUninstalledCount = 0;

            var progRes = PlayniteApi.Dialogs.ActivateGlobalProgress((a) => {

            var gameDatabase = GetGamesSupportedLibraries();
            foreach (var game in gameDatabase)
            {
                var gameName = Regex.Replace(game.Name, @"[^\p{L}\p{Nd}]", "").ToLower();
                GeforceGame supportedGame = null;
                switch (BuiltinExtensions.GetExtensionFromId(game.PluginId))
                {
                    case BuiltinExtension.SteamLibrary:
                        var steamUrl = String.Format("https://store.steampowered.com/app/{0}", game.GameId);
                        supportedGame = supportedSteamGames.Where(g => g.SteamUrl == steamUrl).FirstOrDefault();
                        break;
                    case BuiltinExtension.EpicLibrary:
                        supportedGame = supportedEpicGames.Where(g => g.Title == gameName).FirstOrDefault();
                        break;
                    case BuiltinExtension.OriginLibrary:
                        supportedGame = supportedOriginGames.Where(g => g.Title == gameName).FirstOrDefault();
                        break;
                    case BuiltinExtension.UplayLibrary:
                        supportedGame = supportedUplayGames.Where(g => g.Title == gameName).FirstOrDefault();
                        break;
                    default:
                        break;
                }
                
                if (supportedGame == null)
                {
                    bool featureRemoved = RemoveFeature(game, feature);
                    if (featureRemoved == true)
                    {
                        featureRemovedCount++; 
                        logger.Info(String.Format("Feature removed from \"{0}\"", game.Name));

                        if (settings.SetEnabledGamesAsInstalled == true && game.IsInstalled == true)
                        {
                            game.IsInstalled = true;
                            setAsUninstalledCount++;
                            PlayniteApi.Database.Games.Update(game);
                            logger.Info(String.Format("Set \"{0}\" as uninstalled", game.Name));
                        }
                    }
                }
                
                if (supportedGame != null)
                {
                    enabledGamesCount++;
                    bool featureAdded = AddFeature(game, feature);
                    if (featureAdded == true)
                    {
                        featureAddedCount++;
                        logger.Info(String.Format("Feature added to \"{0}\"", game.Name));
                    }
                    if (settings.SetEnabledGamesAsInstalled == true && game.IsInstalled == false)
                    {
                        game.IsInstalled = true;
                        setAsInstalledCount++;
                        PlayniteApi.Database.Games.Update(game);
                        logger.Info(String.Format("Set \"{0}\" as installed", game.Name));
                    }
                }

                if (settings.UpdatePlayActions == true)
                {
                    string updatePlayAction = UpdateNvidiaAction(game, supportedGame, geforceNowWorkingPath, geforceNowExecutablePath);
                    if (updatePlayAction == "playActionAdded")
                    {
                        playActionAddedCount++; 
                        logger.Info(String.Format("Play Action added to \"{0}\"", game.Name));
                    }
                    else if (updatePlayAction == "playActionRemoved")
                    {
                        playActionRemovedCount++; 
                        logger.Info(String.Format("Play Action removed from \"{0}\"", game.Name));
                    }
                }
            } }, new GlobalProgressOptions("Updating NVIDIA GeForce NOW Enabled games"));

            if (showDialogs == true)
            {
                string results = String.Format("NVIDIA GeForce NOW enabled games in library: {0}\n\nAdded \"{1}\" feature to {2} games.\nRemoved \"{3}\" feature from {4} games.",
                    enabledGamesCount, featureName, featureAddedCount, featureName, featureRemovedCount);
                if (settings.UpdatePlayActions == true)
                {
                    results += String.Format("\n\nPlay Action added to {0} games.\nPlay Action removed from {1} games.",
                        playActionAddedCount, playActionRemovedCount);
                }
                if (settings.SetEnabledGamesAsInstalled == true)
                {
                    results += String.Format("\n\nSet {0} games as Installed.\nSet {1} games as uninstalled.", setAsInstalledCount, setAsUninstalledCount);
                }
                PlayniteApi.Dialogs.ShowMessage(results, "NVIDIA GeForce NOW Enabler");
            }
        }

        public void LibraryRemoveAllPlayActions()
        {
            int playActionRemovedCount = 0;
            var gameDatabase = GetGamesSupportedLibraries();
            foreach (var game in gameDatabase)
            {
                GameAction geforceNowAction = null;
                if (game.OtherActions != null)
                {
                    geforceNowAction = game.OtherActions.Where(x => x.Arguments != null).Where(x => Regex.IsMatch(x.Arguments, @"--url-route=""#\?cmsId=\d+&launchSource=External&shortName=game_gfn_pc&parentGameId=""")).FirstOrDefault();
                }

                if (geforceNowAction != null)
                {
                    game.OtherActions.Remove(geforceNowAction);
                    PlayniteApi.Database.Games.Update(game);
                    playActionRemovedCount++;
                    logger.Info(String.Format("Play Action removed from \"{0}\"", game.Name));
                }
            }
            string results = String.Format("Play Action removed from {0} games", playActionRemovedCount);
            PlayniteApi.Dialogs.ShowMessage(results, "NVIDIA GeForce NOW Enabler");
        }
    }
}