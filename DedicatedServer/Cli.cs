/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Core dedicated server CLI parser
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using System;
using System.IO;
using System.Reflection;
using CommandLine;
using Newtonsoft.Json;
using static COTLMPServer.Data.GameModes;

/* CLASSES & CODE *************************************************************/

namespace DedicatedServer
{
    internal static class Cli
    {
        internal static ConsoleLogger Logger;
        internal static bool HasOptions = true;
        internal static bool HasConfigFile = true;
        internal const string ConfigFile = "ServerSettings.json";
        internal static string PathLocation = Directory.GetCurrentDirectory();
        internal static Version ServerVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Initializes the server configuration options to defaults.
        /// The dedicated server writes the defaults only if the user hasn't provided any other server option.
        /// </summary>
        /// <returns>Returns the default server config data to the caller.</returns>
        private static ServerConfig InitializeConfigDefaults()
        {
            ServerConfig Config;

            Config = new ServerConfig
            {
                PortNumber = 36963,
                ServerName = "Cult of The Lamb Server",
                MaxPlayers = 12,
                Password = null,
                GameMode = 0
            };

            return Config;
        }

        /// <summary>
        /// Validates the server argument options passed by the parser.
        /// </summary>
        /// <param name = "Options">The dedicated server options class of which option parameters are to be validated.</param>
        /// <returns>Returns true if the options are valid, false otherwise.</returns>
        private static bool ValidateParams(DedicatedServerOptions Options)
        {
            int CharIndex;

            /* There's no options passed to validate */
            if (!HasOptions)
            {
                return true;
            }

            /* Validate the required options */
            if ((Options.PortNumber == 0 || Options.PortNumber > 65535) ||
                string.IsNullOrEmpty(Options.ServerName) ||
                (Options.MaxPlayers == 0 || Options.MaxPlayers > 12))
            {
                return false;
            }

            /*
             * Validate the optional options (game mode must be one of the
             * valid mode values and the password mustn't have white spaces).
             */
            if (Options.GameMode > (uint)GameMode.Zombies)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Options.Password))
            {
                for (CharIndex = 0; CharIndex < Options.Password.Length; CharIndex++)
                {
                    if (Char.IsWhiteSpace(Options.Password[CharIndex]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Starts the standalone server.
        /// </summary>
        /// <param name = "Config">An object to a server configuration class that contains server data used to start the server.</param>
        private static void StartServer(ServerConfig Config)
        {
            // TODO: Implement this when the Server class interface is implemented
            // FIXME: Log to the console the IP address (from COTLMPServer) of the server being started
            Logger.LogInfo("The server has been started! Type \u001b[94mquit\x1b[97m into the console to gracefully shutdown the server.");
        }

        /// <summary>
        /// Creates (or loads) the server configuration file upon server startup.
        /// </summary>
        /// <param name = "Options">The dedicated server options needed to start the create the server config file.</param>
        private static void SetupConfigServer(DedicatedServerOptions Options)
        {
            ServerConfig Config;
            string AbsolutePath, JsonData;

            /* Bail out if the server options are not valid */
            if (!ValidateParams(Options))
            {
                Logger.LogFatal("Failed to create the server, at least one of the option parameters aren't valid:\n" +
                                $"\n--->  Port number expected between 1 and 65535 range, got {Options.PortNumber}" +
                                "\n--->  Server name expected to be non-null" +
                                $"\n--->  Maximum players expected to be up to 12, got {Options.MaxPlayers}" +
                                $"\n--->  Game mode expected to be within supported game modes, got {Options.GameMode}" +
                                "\n--->  Password expected to not have white spaces\n");
                return;
            }

            /*
             * The server config file never existed, create one based on the provided
             * server options. Or write down our defaults in case no options were provided.
             */
            AbsolutePath = Path.Combine(PathLocation, ConfigFile);
            if (!HasConfigFile)
            {
                if (!HasOptions)
                {
                    Config = InitializeConfigDefaults();
                }
                else
                {
                    Logger.LogInfo($"No \x1b[94m{ConfigFile}\x1b[97m file could be found, creating server configuration file with provided options on first run...");

                    /*
                     * HACK: Always hardcode the game mode to Standard if other mode was submitted.
                     * Because we don't support any other game modes other than the standard one....
                     */
                    if (Options.GameMode != (uint)GameMode.Standard)
                    {
                        Logger.LogWarning($"{TranslateGameModeToString((GameMode)Options.GameMode)} is currently not supported as a game mode, defaulting to Standard...");
                        Options.GameMode = (uint)GameMode.Standard;
                    }

                    Config = new ServerConfig
                    {
                        PortNumber = Options.PortNumber,
                        ServerName = Options.ServerName,
                        MaxPlayers = Options.MaxPlayers,
                        Password = Options.Password,
                        GameMode = Options.GameMode
                    };
                }

                JsonData = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(AbsolutePath, JsonData);

                Logger.LogInfo($"Starting server with name \x1b[92m{Config.ServerName}\x1b[97m (Version: \x1b[92m{ServerVersion}\x1b[97m)");
                StartServer(Config);
                return;
            }

            /* The server config file exists, deserealize the data from it */
            Config = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText(AbsolutePath));
            if (Config == null)
            {
                Logger.LogFatal("Failed to create the server, couldn't read the server configuration file (might be corrupt)." +
                                $"Please delete the {ConfigFile} file and start the server with new server options!");
                return;
            }

            /*
             * The general rule is to always start the server using the data from the loaded
             * config file but the parsed options (if the user ever passed them) might diverge
             * from that of the ones from the file. So overwrite the config file with whatever
             * has been parsed and use the newly overwritten data.
             */
            if (HasOptions)
            {
                if (Config.PortNumber != Options.PortNumber)
                {
                    Config.PortNumber = Options.PortNumber;
                }

                if (Config.ServerName != Options.ServerName)
                {
                    Config.ServerName = Options.ServerName;
                }

                if (Config.MaxPlayers != Options.MaxPlayers)
                {
                    Config.MaxPlayers = Options.MaxPlayers;
                }

                if (Config.Password != Options.Password)
                {
                    Config.Password = Options.Password;
                }

                if (Config.GameMode != Options.GameMode)
                {
                    /*
                     * HACK: Always hardcode the game mode to Standard if other mode was submitted.
                     * Because we don't support any other game modes other than the standard one....
                     */
                    if (Options.GameMode != (uint)GameMode.Standard)
                    {
                        Logger.LogWarning($"{TranslateGameModeToString((GameMode)Options.GameMode)} is currently not supported as a game mode, defaulting to Standard...");
                        Options.GameMode = (uint)GameMode.Standard;
                    }

                    Config.GameMode = Options.GameMode;
                }

                JsonData = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(AbsolutePath, JsonData);
            }

            Logger.LogInfo($"Starting server with name \x1b[92m{Config.ServerName}\x1b[97m (Version: \x1b[92m{ServerVersion}\x1b[97m)");
            StartServer(Config);
        }

        /// <summary>
        /// Parses the server argument options and initializes the server based on the passed options.
        /// </summary>
        /// <param name = "Arguments">The command line arguments passed from the main entry point.</param>
        public static void Initialize(string[] Arguments)
        {
            Logger = new ConsoleLogger();

            /*
             * The server config file doesn't exist and the user did not pass server options.
             * In this case create the config file using the defaults we provide.
             * Otherwise use the server options passed by the user to write down the config file.
             */
            if (!File.Exists(Path.Combine(PathLocation, ConfigFile)))
            {
                HasConfigFile = false;

                if (Arguments.Length == 0)
                {
                    Logger.LogInfo($"No \x1b[94m{ConfigFile}\x1b[97m file could be found, creating server configuration file with defaults on first run...");
                    HasOptions = false;
                }
            }
            else
            {
                /*
                 * The user has passed no arguments but the server config file is present.
                 * Load the said file and start the server based on that.
                 */
                if (Arguments.Length == 0)
                {
                    HasOptions = false;
                }
            }

            /* Pass down the option arguments to the parser and call the parser callback */
            CommandLine.Parser.Default.ParseArguments<DedicatedServerOptions>(Arguments)
                .WithParsed<DedicatedServerOptions>(SetupConfigServer);
        }
    }
}

/* EOF */
