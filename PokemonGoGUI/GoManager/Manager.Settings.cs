﻿using POGOProtos.Enums;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings.Master;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Helpers;
using PokemonGoGUI.Extensions;
using PokemonGoGUI.GoManager.Models;
using PokemonGoGUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGoGUI.GoManager
{
    public partial class Manager
    {
        public async Task<MethodResult<Dictionary<PokemonId, PokemonSettings>>> GetItemTemplates()
        {
            if (PokeSettings != null)
            {
                return new MethodResult<Dictionary<PokemonId, PokemonSettings>>
                {
                    Data = PokeSettings,
                    Message = "Settings already grabbed",
                    Success = true
                };
            }

            try
            {
                if (!_client.LoggedIn)
                {
                    MethodResult result = await Login();

                    if (!result.Success)
                    {
                        return new MethodResult<Dictionary<PokemonId, PokemonSettings>>
                        {
                            Message = result.Message
                        };
                    }
                }

                DownloadItemTemplatesResponse templates = await _client.Download.GetItemTemplates();
                Dictionary<PokemonId, PokemonSettings> pokemonSettings = new Dictionary<PokemonId, PokemonSettings>();

                foreach (DownloadItemTemplatesResponse.Types.ItemTemplate template in templates.ItemTemplates)
                {
                    if(template.PlayerLevel != null)
                    {
                        LevelSettings = template.PlayerLevel;

                        continue;
                    }

                    if (template.PokemonSettings == null)
                    {
                        continue;
                    }

                    pokemonSettings.Add(template.PokemonSettings.PokemonId, template.PokemonSettings);
                }

                PokeSettings = pokemonSettings;

                return new MethodResult<Dictionary<PokemonId, PokemonSettings>>
                {
                    Data = pokemonSettings,
                    Message = "Success",
                    Success = true
                };
            }
            catch (Exception ex)
            {
                LogCaller(new LoggerEventArgs("Failed to get setting templates", LoggerTypes.Exception, ex));

                return new MethodResult<Dictionary<PokemonId, PokemonSettings>>
                {
                    Message = "Failed to get setting templates"
                };
            }
        }

        public MethodResult<PokemonSettings> GetPokemonSetting(PokemonId pokemon)
        {
            if(PokeSettings == null)
            {
                return new MethodResult<PokemonSettings>
                {
                    Message = "Settings haven't been grabbed yet"
                };
            }

            if (pokemon == PokemonId.Missingno)
            {
                return new MethodResult<PokemonSettings>
                {
                    Message = "No settings on Missingno"
                };
            }

            PokemonSettings pokemonSettings = null;

            //Shouldn't happen
            if (!PokeSettings.TryGetValue(pokemon, out pokemonSettings))
            {
                return new MethodResult<PokemonSettings>()
                {
                    Message = "Pokemon Id does not exist"
                };
            }

            return new MethodResult<PokemonSettings>()
            {
                Data = pokemonSettings,
                Message = "Success",
                Success = true
            };
        }

        public async Task<MethodResult> ExportConfig(string filename)
        {
            try
            {
                string usernameTemp = UserSettings.PtcUsername;
                string passwordTemp = UserSettings.PtcPassword;
                string nameTemp = UserSettings.AccountName;

                UserSettings.AccountName = String.Empty;
                UserSettings.PtcPassword = String.Empty;
                UserSettings.PtcUsername = String.Empty;

                string data = Serializer.ToJson<Settings>(UserSettings);

                UserSettings.AccountName = nameTemp;
                UserSettings.PtcPassword = passwordTemp;
                UserSettings.PtcUsername = usernameTemp;

                await Task.Run(() => File.WriteAllText(filename, data));

                LogCaller(new LoggerEventArgs("Successfully exported config file", LoggerTypes.Info));

                return new MethodResult
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                LogCaller(new LoggerEventArgs("Failed to export config file", LoggerTypes.Exception, ex));

                return new MethodResult
                {
                    Message = ex.Message
                };
            }
        }

        public MethodResult ImportConfig(string data)
        {
            try
            {
                Settings settings = Serializer.FromJson<Settings>(data);
                settings.AccountName = UserSettings.AccountName;
                settings.PtcPassword = UserSettings.PtcPassword;
                settings.PtcUsername = UserSettings.PtcUsername;

                UserSettings = settings;

                LogCaller(new LoggerEventArgs("Successfully imported config file", LoggerTypes.Info));

                return new MethodResult
                {
                    Message = "Success",
                    Success = true
                };
            }
            catch(Exception ex)
            {
                string message = String.Format("Failed to import config. Ex: {0}", ex.Message);

                LogCaller(new LoggerEventArgs(message, LoggerTypes.Exception, ex));

                return new MethodResult
                {
                    Message = message
                };
            }
        }

        public async Task<MethodResult> ImportConfigFromFile(string fileName)
        {
            try
            {
                if(!File.Exists(fileName))
                {
                    return new MethodResult
                    {
                        Message = "File does not exist"
                    };
                }

                string data = await Task.Run(() => File.ReadAllText(fileName));

                return ImportConfig(data);
            }
            catch(Exception ex)
            {
                string message = String.Format("Failed to load config file.");

                LogCaller(new LoggerEventArgs(message, LoggerTypes.Exception, ex));

                return new MethodResult
                {
                    Message = message
                };
            }
        }

        public void RestoreCatchDefaults()
        {
            UserSettings.LoadCatchSettings();
        }

        public void RestoreInventoryDefaults()
        {
            UserSettings.LoadInventorySettings();
        }

        public void RestoreEvolveDefaults()
        {
            UserSettings.LoadEvolveSettings();
        }

        public void RestoreTransferDefaults()
        {
            UserSettings.LoadTransferSettings();
        }

        /*
        public void RestoreSniperDefaults()
        {
            UserSettings.LoadSniperSettings();
        }*/

        public override bool Equals(object obj)
        {
            Manager tempManager = obj as Manager;

            if(tempManager == null)
            {
                return base.Equals(obj);
            }

            return tempManager.UserSettings.PtcUsername == this.UserSettings.PtcUsername;
        }

        public override int GetHashCode()
        {
            return this.UserSettings.PtcUsername.GetHashCode();
        }
    }
}
