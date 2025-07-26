/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Localization API support
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 *              Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debugging;
using BepInEx;
using HarmonyLib;
using I2.Loc;
using System.Collections.Generic;
using UnityEngine.Assertions;

/* LOCALIZATION NAMESPACES ****************************************************/

using COTLMP.Localization.English;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the classes and code for the localization API
 * infrastructure of the mod.
 * 
 * @class LocaleManager
 * The main localization manager API, containing API methods
 * for manipulation of localized (aka translated) strings of
 * the mod.
 * 
 * @class LocalizationManagerPatches
 * Contains harmony patches of which hook up with the main
 * localization manager of the game.
 */
namespace COTLMP.Localization
{
    /*
     * @brief
     * Localization table data structure. It's used to store
     * a translated string onto the specific language of a specific term.
     * 
     * @field Term
     * The term group of the translation string of which it corresponds to.
     * An example of a term would be "Multiplayer/UI".
     * 
     * @field Translation
     * The actual translated string onto the target language.
     * 
     * @field Overriden
     * If this field is initialized to TRUE, it means the specific translation
     * string overwrites the already existing original string of the Cult of the Lamb game.
     */
    public struct LocalizationTable
    {
        public string Term;
        public string Translation;
        public bool Overriden;

        public LocalizationTable(string TermString, string TranslationString, bool IsOverriden)
        {
            Term = TermString;
            Translation = TranslationString;
            Overriden = IsOverriden;
        }
    }

    internal static class LocaleManager
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>();

        /*
         * @brief
         * Checks if the given language locale is supported by the mod.
         * 
         * @param[in] Language
         * A string that points to the language passed by the caller.
         * 
         * @return
         * Returns TRUE if tthe locale is supported, FALSE otherwise.
         */
        private static bool IsLocaleSupported(string Language)
        {
            int LanguageIndex;
            string[] SupportedLanguages = {"English", "Japanese", "Russian", "French", "German", "Spanish",
                                           "Portuguese (Brazil)", "Chinese (Simplified)", "Chinese (Traditional)", "Korean"};

            /* Check if the passed language locale argument is supported */
            for (LanguageIndex = 0;
                 LanguageIndex < SupportedLanguages.Length;
                 LanguageIndex++)
            {
                /* We found the supported locale, stop looking */
                if (string.Equals(Language, SupportedLanguages[LanguageIndex]))
                {
                    return true;
                }
            }

            return false;
        }

        /*
         * @brief
         * Initializes the translation strings of a given language from
         * the locale table.
         * 
         * @param[in] Table
         * An array to a table of localized strings of the target language,
         * passed by the caller.
         * 
         * @param[in] Language
         * A string that points to the language passed by the caller.
         */
        private static void InitializeTranslationsFromLocaleTable(LocalizationTable[] Table, string Language)
        {
            int TranslationIndex;

            /* Initialize the translation strings from the resource elements of the locale table */
            for (TranslationIndex = 0;
                 TranslationIndex < Table.Length;
                 TranslationIndex++)
            {
                Add(Language, Table[TranslationIndex].Term, Table[TranslationIndex].Translation, Table[TranslationIndex].Overriden);
            }
        }

        /*
         * @brief
         * Retrieves the translated string of the given term.
         * 
         * @param[in] Language
         * A string that points to the language passed by the caller.
         * 
         * @param[in] Term
         * The term group of the translation string of which it corresponds to.
         * 
         * @return
         * Returns a string which points to the translation of the given term,
         * otherwise NULL is returned if the translation doesn't exist.
         */
        private static string TryGetTranslation(string Language, string Term)
        {
            if (!Translations.ContainsKey(Language)) return null;

            return Translations[Language].TryGetValue(Term, out var value) ? value : null;
        }

        /*
         * @brief
         * Adds a translated string into the locale translations dictionary.
         * 
         * @param[in] Language
         * A string that points to the language passed by the caller.
         * 
         * @param[in] Term
         * The term group of the translation string of which it corresponds to.
         * 
         * @param[in] Translation
         * The translated string of the target language.
         * 
         * @param[in] Overriden
         * If set to TRUE the method will overwrite the existing translation string
         * of the given term from the game. If set to FALSE then it indicates the
         * passed translation string is a new string. This is for debugging purposes.
         */
        public static void Add(string Language, string Term, string Translation, bool Overriden)
        {
            /* Setup a new dictionary for the given language if we haven't done it before */
            if (!Translations.ContainsKey(Language)) Translations[Language] = new Dictionary<string, string>();

            /* Log to the debugger the given translation string is overriden */
            if (Overriden == true)
            {
                COTLMP.Debugging.Log.Print(DebugLevel.WARNING_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                           $"Overriding the {Term} term with {Translation}!");
            }

            /* Add the string */
#if DEBUG
            COTLMP.Debugging.Log.Print(DebugLevel.INFO_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                       $"Adding {Translation} translation from {Term} term for {Language} language!");
#endif
            Translations[Language][Term] = Translation;
        }

        /*
         * @brief
         * Removes a translated string from the dictionary.
         * 
         * @param[in] Language
         * A string that points to the language passed by the caller.
         * 
         * @param[in] Term
         * The term group of the translation string of which it corresponds to.
         */
        public static void Remove(string Language, string Term)
        {
            /* Bail out if the following term has no translation */
            if (!Translations.ContainsKey(Language)) return;

            /* Remove the translated string */
#if DEBUG
            COTLMP.Debugging.Log.Print(DebugLevel.INFO_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                       $"Removing {Term} term from {Language} language!");
#endif
            Translations[Language].Remove(Term);
        }

        /*
         * @brief
         * Loads a locale.
         * 
         * @param[in] Language
         * A string that points to the language passed by the caller to be
         * loaded.
         * 
         * @remarks
         * Generally this method is used to load different language locales
         * during startup of the mod. DO NOT USE IT ON ANYWHERE PART OF THE CODE!
         */
        public static void LoadLocale(string Language)
        {
            LocalizationTable[] StringsTable;

            COTLMP.Debugging.Log.Print(DebugLevel.INFO_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                       $"Loading the {Language} language locale");

            /* Check that the given language locale is supported, bail out if not the case */
            if (!IsLocaleSupported(Language))
            {
                COTLMP.Debugging.Log.Print(DebugLevel.FATAL_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                           $"The {Language} language locale is not supported. Expect problems with mod initialization!");
                return;
            }

            /* Grab the apporpriate strings localization table */
            switch (Language)
            {
                case "English":
                {
                    StringsTable = COTLMP.Localization.English.Strings.StringsTable;
                    break;
                }

                default:
                {
                    StringsTable = null;
                    break;
                }
            }

            /* Getting a null table is illegal here, it shouldn't happen */
            Assert.IsNotNull(StringsTable);

            /* Now initialize the translation strings from the locale table */
            InitializeTranslationsFromLocaleTable(StringsTable, Language);
        }

        [HarmonyPatch]
        private static class LocalizationManagerPatches
        {
            /*
             * @brief
             * Patches the GetTranslation method of the localization manager
             * of the game. Its purpose is to add custom localized strings
             * provided by the mod into the game.
             * 
             * @param[in] Term
             * The term group of the translation string of which it corresponds to.
             * 
             * @param[in] overrideLanguage
             * A string that points to language locale being overriden with custom
             * translation strings. This parameter is optional.
             * 
             * @param[in,out] __result
             * The current returned value of the method. Typically this is a translation 
             * string returned by the original method of the game, which is modified on 
             * our end by the returned translation we have provided by the mod.
             * 
             * @remarks
             * Returns TRUE if tthe original method of the game is to be executed.
             * FALSE if our method is to be executed instead.
             */
            [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetTranslation))]
            [HarmonyPrefix]
            private static bool TranslationPatch(string Term, string overrideLanguage, ref string __result)
            {
                string GameLanguage, Translation;

                /* Did the caller provide us a language? */
                if (!string.IsNullOrEmpty(overrideLanguage))
                {
                    /* Try to get the translation string from the given locale */
                    Translation = LocaleManager.TryGetTranslation(overrideLanguage, Term);
                    if (!string.IsNullOrEmpty(Translation))
                    {
                        __result = Translation;
                        return false;
                    }

                    return true;
                }

                /*
                 * We don't know what kind of language is this so we have to
                 * retrieve from the game settings the Unity engine has set it
                 * up for the game. Then retry obtaining the translation string
                 * and modify the resultant of the returned string with ours.
                 */
                GameLanguage = SettingsManager.Settings.Game.Language;
                Translation = LocaleManager.TryGetTranslation(GameLanguage, Term);
                if (!string.IsNullOrEmpty(Translation))
                {
                    __result = Translation;
                    return false;
                }

                return true;
            }
        }
    }
}

/* EOF */
