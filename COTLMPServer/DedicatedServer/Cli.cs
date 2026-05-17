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

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.DedicatedServer
{
    internal static class Cli
    {
        internal static bool HasOptions = true;
        internal static bool HasConfigFile = true;
        internal static string ConfigFile = "ServerSettings.json";
        internal static string ServerLibFile = "COTLMPServer.dll";
        internal static string PathLocation = Directory.GetCurrentDirectory();
        internal static Version ServerVersion = Assembly.GetExecutingAssembly().GetName().Version;

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
             * valid mode names and the password mustn't have white spaces).
             */
            if (!string.IsNullOrEmpty(Options.GameMode))
            {
                if (!string.Equals(Options.GameMode, "Standard") &&
                    !string.Equals(Options.GameMode, "Boss Fight") &&
                    !string.Equals(Options.GameMode, "Deathmatch") &&
                    !string.Equals(Options.GameMode, "Zombies"))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(Options.Password))
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
            COTLMPServer.DedicatedServer.ConsoleOutput.Log("The server has been started! Type \u001b[94mquit\x1b[97m into the console to gracefully shutdown the server.");
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
                COTLMPServer.DedicatedServer.ConsoleOutput.Fatal("Failed to create the server, at least one of the option parameters aren't valid:\n" +
                                                                 $"\n--->  Port number expected between 1 and 65535 range, got {Options.PortNumber}" +
                                                                 "\n--->  Server name expected to be non-null" +
                                                                 $"\n--->  Maximum players expected to be up to 12, got {Options.MaxPlayers}" +
                                                                 $"\n--->  Game mode expected to be within supported game modes, got {Options.GameMode}" +
                                                                 "\n--->  Password expected to not have white spaces\n");
                return;
            }

            /*
             * The server config file never existed, create one based on the provided
             * server options. Always default the game mode to Standard if no mode name
             * was given when parsing the arguments.
             */
            AbsolutePath = Path.Combine(PathLocation, ConfigFile);
            if (!HasConfigFile)
            {
                COTLMPServer.DedicatedServer.ConsoleOutput.Log($"No \x1b[94m{ConfigFile}\x1b[97m file could be found, creating server configuration file on first run...");
                if (string.IsNullOrEmpty(Options.GameMode))
                {
                    Options.GameMode = "Standard";
                }
                else
                {
                    /*
                     * HACK: Always hardcode the game mode to Standard if other mode was submitted.
                     * Because we don't support any other game modes other than the standard one....
                     */
                    if (!string.Equals(Options.GameMode, "Standard"))
                    {
                        COTLMPServer.DedicatedServer.ConsoleOutput.Warning($"{Options.GameMode} is currently not supported as a game mode, defaulting to Standard...");
                        Options.GameMode = "Standard";
                    }
                }

                Config = new ServerConfig
                {
                    PortNumber = Options.PortNumber,
                    ServerName = Options.ServerName,
                    MaxPlayers = Options.MaxPlayers,
                    Password = Options.Password,
                    GameMode = Options.GameMode
                };

                JsonData = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(AbsolutePath, JsonData);

                COTLMPServer.DedicatedServer.ConsoleOutput.Log($"Starting server with name \x1b[92m{Config.ServerName}\x1b[97m (Version: \x1b[92m{ServerVersion}\x1b[97m)");
                StartServer(Config);
                return;
            }

            /* The server config file exists, deserealize the data from it */
            Config = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText(AbsolutePath));
            if (Config == null)
            {
                COTLMPServer.DedicatedServer.ConsoleOutput.Fatal("Failed to create the server, couldn't read the server configuration file (might be corrupt)." +
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
                    if (string.IsNullOrEmpty(Options.GameMode))
                    {
                        Options.GameMode = "Standard";
                    }
                    else
                    {
                        /*
                         * HACK: Always hardcode the game mode to Standard if other mode was submitted.
                         * Because we don't support any other game modes other than the standard one....
                         */
                        if (!string.Equals(Options.GameMode, "Standard"))
                        {
                            COTLMPServer.DedicatedServer.ConsoleOutput.Warning($"{Options.GameMode} is currently not supported as a game mode, defaulting to Standard...");
                            Options.GameMode = "Standard";
                        }
                    }

                    Config.GameMode = Options.GameMode;
                }

                JsonData = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(AbsolutePath, JsonData);
            }

            COTLMPServer.DedicatedServer.ConsoleOutput.Log($"Starting server with name \x1b[92m{Config.ServerName}\x1b[97m (Version: \x1b[92m{ServerVersion}\x1b[97m)");
            StartServer(Config);
        }

        /// <summary>
        /// Parses the server argument options and initializes the server based on the passed options.
        /// </summary>
        /// <param name = "Arguments">The command line arguments passed from the main entry point.</param>
        public static void Initialize(string[] Arguments)
        {
            /*
             * The server config file doesn't exist and the user passed server options.
             * This is a first time run, acknowldedge that and create the config file.
             * Otherwise bail out.
             */
            if (!File.Exists(Path.Combine(PathLocation, ConfigFile)))
            {
                if (Arguments.Length == 0)
                {
                    COTLMPServer.DedicatedServer.ConsoleOutput.Fatal($"Failed to create the server, no server options were provided and {ConfigFile} configuration file could not be found!");
                    return;
                }

                HasConfigFile = false;
            }

            /*
             * The user has passed no arguments but the server config file is present.
             * Load the said file and start the server based on that.
             */
            if (Arguments.Length == 0)
            {
                HasOptions = false;
            }

            /* Bail out if the core COTLMP Server library is missing or tampered */
            if (!File.Exists(Path.Combine(PathLocation, ServerLibFile)))
            {
                COTLMPServer.DedicatedServer.ConsoleOutput.Fatal($"Failed to create the server, the {ServerLibFile} library is either corrupt or missing.\n" +
                                                                 "Please re-install the COTLMP Dedicated Server!");
                return;
            }

            /* Pass down the option arguments to the parser and call the parser callback */
            CommandLine.Parser.Default.ParseArguments<DedicatedServerOptions>(Arguments)
                .WithParsed<DedicatedServerOptions>(SetupConfigServer);
        }
    }
}

/* EOF */
