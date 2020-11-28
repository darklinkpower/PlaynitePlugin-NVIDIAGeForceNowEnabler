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

        public override void OnGameInstalled(Game game)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(Game game)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(Game game)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.
            if (settings.ExecuteOnStartup == true)
            {
                MainMethod(false);
            }
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.
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
                    Description = "Update game features",
                    MenuSection = "@Nvidia GeForce NOW Enabler",
                    Action = args => {
                        MainMethod(true);
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

        public string AddNvidiaAction(Game game, GeforceGame supportedGame, string geforceNowWorkingPath, string geforceNowExecutablePath)
        {
            GameAction geforceNowAction = null;
            if (game.OtherActions != null)
            {
                geforceNowAction = game.OtherActions.Where(x => Regex.IsMatch(x.Arguments, @"--url-route=""#\?cmsId=d+&launchSource=External""")).FirstOrDefault();
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
                var playActionArguments = String.Format("--url-route=\"#?cmsId={0}&launchSource=External`\"", supportedGame.id);
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
            List<GeforceGame> supportedGames = new List<GeforceGame>();

            try
            {
                WebClient webClient = new WebClient();
                string downloadedString = webClient.DownloadString(uri);
                supportedGames = JsonConvert.DeserializeObject<List<GeforceGame>>(downloadedString);
                foreach (var supportedGame in supportedGames)
                {
                    supportedGame.title = Regex.Replace(supportedGame.title, @"[^\p{L}\p{Nd}]", "").ToLower();
                }
                return supportedGames;
            }
            catch (Exception e)
            {
                logger.Error(e, e.Message);
                if (showDialogs == true)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "NVIDIA GeForce NOW Enabler");
                }
                return supportedGames;
            }
        }

        public void MainMethod(bool showDialogs)
        {

            string featureName = "Plugin test feature";
            GameFeature feature = PlayniteApi.Database.Features.Add(featureName);

            string localAppData = Environment.GetEnvironmentVariable("LocalAppData");
            string[] paths = { localAppData, "NVIDIA Corporation", "GeForceNOW", "CEF" };
            string geforceNowWorkingPath = Path.Combine(paths);
            string geforceNowExecutablePath = geforceNowWorkingPath + "\\GeForceNOWStreamer.exe";
            
            var supportedGames = DownloadGameList("https://static.nvidiagrid.net/supported-public-game-list/gfnpc.json", showDialogs);
            if (supportedGames.Count() == 0)
            {
                return;
            }
            var supportedSteamGames = supportedGames.Where(g => g.store == "Steam");
            var supportedEpicGames = supportedGames.Where(g => g.store == "Epic");
            var supportedOriginGames = supportedGames.Where(g => g.store == "Origin");
            var supportedUplayGames = supportedGames.Where(g => g.store == "UPLAY");

            List<Guid> supportedLibraries = new List<Guid>() {
                BuiltinExtensions.GetIdFromExtension(BuiltinExtension.EpicLibrary),
                BuiltinExtensions.GetIdFromExtension(BuiltinExtension.OriginLibrary),
                BuiltinExtensions.GetIdFromExtension(BuiltinExtension.SteamLibrary),
                BuiltinExtensions.GetIdFromExtension(BuiltinExtension.UplayLibrary)
            };

            int enabledGamesCount = 0; 
            int featureAddedCount = 0;
            int featureRemovedCount = 0;
            int playActionAddedCount = 0;
            int playActionRemovedCount = 0;

            var gameDatabase = PlayniteApi.Database.Games.Where(g => supportedLibraries.Contains(g.PluginId));
            foreach (var game in gameDatabase)
            {
                var gameName = Regex.Replace(game.Name, @"[^\p{L}\p{Nd}]", "").ToLower();
                GeforceGame supportedGame = null;
                switch (game.PluginId)
                {
                    case var id when BuiltinExtensions.GetExtensionFromId(id) == BuiltinExtension.SteamLibrary:
                        var steamUrl = String.Format("https://store.steampowered.com/app/{0}", game.GameId);
                        supportedGame = supportedSteamGames.Where(g => g.steamUrl == steamUrl).FirstOrDefault();
                        break;
                    case var id when BuiltinExtensions.GetExtensionFromId(id) == BuiltinExtension.EpicLibrary:
                        supportedGame = supportedEpicGames.Where(g => g.title == gameName).FirstOrDefault();
                        break;
                    case var id when BuiltinExtensions.GetExtensionFromId(id) == BuiltinExtension.OriginLibrary:
                        supportedGame = supportedOriginGames.Where(g => g.title == gameName).FirstOrDefault();
                        break;
                    case var id when BuiltinExtensions.GetExtensionFromId(id) == BuiltinExtension.UplayLibrary:
                        supportedGame = supportedUplayGames.Where(g => g.title == gameName).FirstOrDefault();
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
                        logger.Info(String.Format("NVIDIA GeForce NOW Enabler - Feature removed from {0}", game.Name));
                    }
                }
                
                if (supportedGame != null)
                {
                    enabledGamesCount++;
                    bool featureAdded = AddFeature(game, feature);
                    if (featureAdded == true)
                    {
                        featureAddedCount++;
                        logger.Info(String.Format("NVIDIA GeForce NOW Enabler - Feature added to {0}", game.Name));
                    }
                }

                if (settings.UpdatePlayActions == true)
                {
                    string updatePlayAction = AddNvidiaAction(game, supportedGame, geforceNowWorkingPath, geforceNowExecutablePath);
                    if (updatePlayAction == "playActionAdded")
                    {
                        playActionAddedCount++; 
                        logger.Info(String.Format("NVIDIA GeForce NOW Enabler - Play Action added to {0}", game.Name));
                    }
                    else if (updatePlayAction == "playActionRemoved")
                    {
                        playActionRemovedCount++; 
                        logger.Info(String.Format("NVIDIA GeForce NOW Enabler - Play Action removed from {0}", game.Name));
                    }
                }
            }

            if (showDialogs == true)
            {
                string results = String.Format("NVIDIA GeForce NOW enabled games in library: {0}`n`nAdded \"{1}\" feature to {2} games`nRemoved \"{3}\" feature from {4} games",
                    enabledGamesCount, featureName, playActionAddedCount, featureName, playActionRemovedCount);
                if (settings.UpdatePlayActions == true)
                {
                    results += String.Format("`n`nPlay Action added to {0} games`nPlay Action removed from {1} games",
                        playActionAddedCount, playActionRemovedCount);
                }
                PlayniteApi.Dialogs.ShowMessage(results, "NVIDIA GeForce NOW Enabler");
            }
        }
    }
}