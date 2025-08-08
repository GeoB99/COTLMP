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
using HarmonyLib;
using System;
using System.IO;
using UnityEngine.Assertions;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains classes and code for the Configuration API.
 * 
 * @class Configuration
 * Main class of which implements methods for manipulation
 * of user configuration settings, loading and saving them.
 */
namespace COTLMP.Api
{
    /*
     * @brief
     * Configuration category enumeration. This is used to regroup
     * individual settings in separate configuration sections.
     * 
     * @field ServerSettings
     * The server settings category.
     * 
     * @field ModSettings
     * The mod settings category.
     */
    public enum CONFIGURATION_SECTION
    {
        ServerSettings = 0,
        ModSettings
    }

    internal static class Configuration
    {
        /*
         * @brief
         * Data type enumeration. This divides the setting data
         * into several different types.
         * 
         * @field StringType
         * The setting data is a string type.
         * 
         * @field IntType
         * The setting data is an integer type.
         * 
         * @field BoolType
         * The setting data is a boolean type.
         */
        internal enum TYPE_DATA
        {
            StringType = 0,
            IntType,
            BoolType
        }

        /*
         * @brief
         * Configuration data structure. This is internally used by the
         * Configuration API to encapsulate the arguments provided by the caller such
         * as setting name, value and such and pass it down to the private helpers.
         * 
         * @field Section
         * The section category of the setting.
         * 
         * @field ValueName
         * The name of the value setting.
         * 
         * @field ValueDescription
         * The description of the value setting.
         * 
         * @field TypeData
         * The type of the setting.
         * 
         * @field ValueStringData
         * The data of the setting, as a string.
         * 
         * @field ValueBoolData
         * The data of the setting, as a boolean.
         * 
         * @field ValueIntData
         * The data of the setting, as an integer.
         */
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

        /*
         * @brief
         * Converts the configuration section into a string label.
         * 
         * @param[in] Section
         * The section category.
         * 
         * @return
         * Returns the label string that represents the category section,
         * otherwise NULL is returned if the given section is not supported.
         */
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

        /*
         * @brief
         * Creates a setting as a boolean.
         * 
         * @param[in] SettingData
         * Data structure with configuration data of the setting, filled by the caller.
         * 
         * @return
         * Returns an object to a configuration entry that represents the setting,
         * otherwise NULL is returned if the operation fails.
         */
        private static ConfigEntry<bool> CreateSettingAsBool(ConfigurationData SettingData)
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

        /*
         * @brief
         * Creates a setting as a string.
         * 
         * @param[in] SettingData
         * Data structure with configuration data of the setting, filled by the caller.
         * 
         * @return
         * Returns an object to a configuration entry that represents the setting,
         * otherwise NULL is returned if the operation fails.
         */
        private static ConfigEntry<string> CreateSettingAsString(ConfigurationData SettingData)
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

        /*
         * @brief
         * Creates a setting as an integer.
         * 
         * @param[in] SettingData
         * Data structure with configuration data of the setting, filled by the caller.
         * 
         * @return
         * Returns an object to a configuration entry that represents the setting,
         * otherwise NULL is returned if the operation fails.
         */
        private static ConfigEntry<int> CreateSettingAsInt(ConfigurationData SettingData)
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

        /*
         * @brief
         * Creates a user configuration setting.
         * 
         * @param[in] ConfigSection
         * The configuration category section of the setting.
         * 
         * @param[in] ValueName
         * The name of the setting.
         * 
         * @param[in] ValueDescription
         * The description of the setting, which is displayed within the user
         * configuration file of the mod.
         * 
         * @param[in] ValueData
         * An arbitrary type of the value data of the setting, provided by the caller.
         * This can be either a boolean, integer or a string.
         * 
         * @return
         * Returns an object to the setting value that has been created, NULL otherwise.
         * 
         * @remarks
         * If the setting has already been created it will open the already created
         * setting by returning the existent config entry. The caller is expected
         * to retrieve the value data of the setting by using the GetSettingData method.
         */
        public static Object CreateSetting<T>(CONFIGURATION_SECTION ConfigSection, string ValueName, string ValueDescription, T ValueData)
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
                BoolSetting = CreateSettingAsBool(SettingData);
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
                StringSetting = CreateSettingAsString(SettingData);
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
                IntSetting = CreateSettingAsInt(SettingData);
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

        /*
         * @brief
         * Retrieves a config entry of the setting that has already
         * been created.
         * 
         * @param[in] Definition
         * Data structure with configuration definition data that represents
         * the setting of which an entry is to be retrieved, filled by the caller.
         * 
         * @return
         * Returns a config entry of the setting, NULL otherwise if it fails.
         */
        public static ConfigEntry<T> GetSettingEntry<T>(ConfigDefinition Definition)
        {
            ConfigEntry<T> Entry;

            /* Obtain the config entry of the specific setting */
            Entry = Plugin.Config.Bind(Definition, default(T), null);
            return Entry;
        }

        /*
         * @brief
         * For more documentation information, see ConfigSectionToLabel.
         */
        public static string GetSectionName(CONFIGURATION_SECTION Section)
        {
            string Name;

            /* Call the private helper */
            Name = ConfigSectionToLabel(Section);
            return Name;
        }

        /*
         * @brief
         * Flushes the dirty settings data from RAM back to the
         * physical backing storage.
         */
        public static void FlushSettings()
        {
            /* Execute the Config API call to flush all the settings to disk */
            Plugin.Config.Save();
        }

        /*
         * @brief
         * Retrieves the data of the setting.
         * 
         * @param[in] ConfigObject
         * An object to a configuration setting.
         * 
         * @return
         * Returns the type data of the setting.
         */
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
