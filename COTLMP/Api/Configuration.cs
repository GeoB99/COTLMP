/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Save/Load user configuration settings and data support
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using BepInEx;
using BepInEx.Configuration;
using System;
using HarmonyLib;
using UnityEngine.Assertions;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Api
{
    internal static class Configuration
    {
        public enum CONFIGURATION_SECTION
        {
            ServerSettings = 0,
            ModSettings
        }

        internal enum TYPE_DATA
        {
            StringType = 0,
            IntType,
            BoolType
        }

        internal struct ConfigurationData
        {
            internal readonly CONFIGURATION_SECTION Section;
            internal string ValueName;
            internal string ValueDescription;
            internal readonly TYPE_DATA TypeData;
            internal string ValueStringData;
            internal bool ValueBoolData;
            internal int ValueIntData;

            internal ConfigurationData(CONFIGURATION_SECTION Sect,
                                       string ValName,
                                       string ValDesc,
                                       TYPE_DATA Type,
                                       string StringData,
                                       bool BoolData,
                                       int IntData)
            {
                Section = Sect;
                ValueName = ValName;
                ValueDescription = ValDesc;
                TypeData = Type;
                ValueStringData = StringData;
                ValueBoolData = BoolData;
                ValueIntData = IntData;
            }
        }

        private static string ConfigSectionToLabel(CONFIGURATION_SECTION Section)
        {
            string LabelName;

            switch (Section)
            {
                case CONFIGURATION_SECTION.ServerSettings:
                {
                    LabelName = "Server Settings";
                    break;
                }

                case CONFIGURATION_SECTION.ModSettings:
                {
                    LabelName = "Mod Settings";
                    break;
                }

                default:
                {
                    LabelName = null;
                    break;
                }
            }

            return LabelName;
        }

        private static ConfigEntry<bool> LoadSettingAsBool(ConfigurationData SettingData)
        {
            string LabelName;
            ConfigEntry<bool> BoolSettingEntry;

            /* We only support boolean settings here */
            if (SettingData.TypeData != TYPE_DATA.BoolType)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.CONFIGURATION_COMPONENT,
                                               $"{SettingData.TypeData} cannot be used when calling this function!");
                return null;
            }

            /* Retrieve the category label of the config setting */
            LabelName = ConfigSectionToLabel(SettingData.Section);
            Assert.IsNotNull(LabelName);

            /* Create the setting if it doesn't exist or load it */
            BoolSettingEntry = Plugin.Config.Bind(LabelName,
                                                  SettingData.ValueName,
                                                  SettingData.ValueBoolData,
                                                  SettingData.ValueDescription);
            if (BoolSettingEntry == null)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.CONFIGURATION_COMPONENT,
                                               $"Failed to initialize or load the {SettingData.ValueName} setting!");
                return null;
            }

            return BoolSettingEntry;
        }

        private static ConfigEntry<string> LoadSettingAsString(ConfigurationData SettingData)
        {
            string LabelName;
            ConfigEntry<string> StringSettingEntry;

            /* We only support string settings here */
            if (SettingData.TypeData != TYPE_DATA.StringType)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.CONFIGURATION_COMPONENT,
                                               $"{SettingData.TypeData} cannot be used when calling this function!");
                return null;
            }

            /* Retrieve the category label of the config setting */
            LabelName = ConfigSectionToLabel(SettingData.Section);
            Assert.IsNotNull(LabelName);

            /* Create the setting if it doesn't exist or load it */
            StringSettingEntry = Plugin.Config.Bind(LabelName,
                                                    SettingData.ValueName,
                                                    SettingData.ValueStringData,
                                                    SettingData.ValueDescription);
            if (StringSettingEntry == null)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.CONFIGURATION_COMPONENT,
                                               $"Failed to initialize or load the {SettingData.ValueName} setting!");
                return null;
            }

            return StringSettingEntry;
        }

        private static ConfigEntry<int> LoadSettingAsInt(ConfigurationData SettingData)
        {
            string LabelName;
            ConfigEntry<int> IntSettingEntry;

            /* We only support string settings here */
            if (SettingData.TypeData != TYPE_DATA.IntType)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.CONFIGURATION_COMPONENT,
                                               $"{SettingData.TypeData} cannot be used when calling this function!");
                return null;
            }

            /* Retrieve the category label of the config setting */
            LabelName = ConfigSectionToLabel(SettingData.Section);
            Assert.IsNotNull(LabelName);

            /* Create the setting if it doesn't exist or load it */
            IntSettingEntry = Plugin.Config.Bind(LabelName,
                                                 SettingData.ValueName,
                                                 SettingData.ValueIntData,
                                                 SettingData.ValueDescription);
            if (IntSettingEntry == null)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.CONFIGURATION_COMPONENT,
                                               $"Failed to initialize or load the {SettingData.ValueName} setting!");
                return null;
            }

            return IntSettingEntry;
        }

        public static Object LoadSetting<T>(CONFIGURATION_SECTION ConfigSection, string ValueName, string ValueDescription, T ValueData)
        {
            Object ReturnedData;
            ConfigEntry<bool> BoolSetting;
            ConfigEntry<string> StringSetting;
            ConfigEntry<int> IntSetting;
            ConfigurationData SettingData;

            /* Determine the value type provided and call the specific method accordingly */
            if (ValueData is bool)
            {
                /* This is a boolean value, build the internal data for this setting */
                SettingData = new ConfigurationData(ConfigSection,
                                                    ValueName,
                                                    ValueDescription,
                                                    TYPE_DATA.BoolType,
                                                    null,
                                                    Convert.ToBoolean(ValueData),
                                                    0);

                /* And invoke the API helper to do the deed */
                BoolSetting = LoadSettingAsBool(SettingData);
                if (BoolSetting == null)
                {
                    return null;
                }

                /* And retrieve the value returned by the method */
                ReturnedData = BoolSetting.Value;
            }
            else if (ValueData is string)
            {
                /* This is a string value, build the internal data for this setting */
                SettingData = new ConfigurationData(ConfigSection,
                                                    ValueName,
                                                    ValueDescription,
                                                    TYPE_DATA.StringType,
                                                    Convert.ToString(ValueData),
                                                    false,
                                                    0);

                /* And invoke the API helper to do the deed */
                StringSetting = LoadSettingAsString(SettingData);
                if (StringSetting == null)
                {
                    return null;
                }

                /* And retrieve the value returned by the method */
                ReturnedData = StringSetting.Value;
            }
            else if (ValueData is int)
            {
                /* This is an integer value, build the internal data for this setting */
                SettingData = new ConfigurationData(ConfigSection,
                                                    ValueName,
                                                    ValueDescription,
                                                    TYPE_DATA.IntType,
                                                    null,
                                                    false,
                                                    Convert.ToInt32(ValueData));

                /* And invoke the API helper to do the deed */
                IntSetting = LoadSettingAsInt(SettingData);
                if (IntSetting == null)
                {
                    return null;
                }

                /* And retrieve the value returned by the method */
                ReturnedData = IntSetting.Value;
            }
            else
            {
                /* Other configuration settings are unsupported, bail out with no data */
                COTLMP.Debug.PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.CONFIGURATION_COMPONENT,
                                               $"Unknown value data type has been passed (Type -> {ValueData.GetType()})");
                ReturnedData = null;
            }

            return ReturnedData;
        }

        public static void FlushSettings()
        {
            /* Execute the Config API call to flush all the settings to disk */
            Plugin.Config.Save();
        }

        public static T GetSettingData<T>(Object ConfigObject)
        {
            /* NULL objects is illegal here */
            Assert.IsNotNull(ConfigObject);

            /* Cast the object immediately to the generic type if possible */
            if (ConfigObject is T)
            {
                return (T)ConfigObject;
            }

            /* Otherwise change the type of the object and return it */
            return (T)Convert.ChangeType(ConfigObject, typeof(T));
        }
    }
}

/* EOF */
