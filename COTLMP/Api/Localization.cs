/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Localization API support
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 *              Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using COTLMP.Language;
using BepInEx;
using HarmonyLib;
using I2.Loc;
using System.Collections.Generic;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Api
{
    /// <summary>
    /// Localization table data structure. It's used to store
    /// a translated string onto the specific language of a specific term.
    /// </summary>
    public struct LocalizationTable
    {
        /// <summary>
        /// The term group of the translation string of which it corresponds to.
        /// An example of a term would be "Multiplayer/UI".
        /// </summary>
        public string Term;

        /// <summary>
        /// The actual translated string onto the target language.
        /// </summary>
        public string Translation;

        /// <summary>
        /// If this field is initialized to TRUE, it means the specific translation
        /// string overwrites the already existing original string of the Cult of the Lamb game.
        /// </summary>
        public bool Overriden;

        public LocalizationTable(string TermString, string TranslationString, bool IsOverriden)
        {
            Term = TermString;
            Translation = TranslationString;
            Overriden = IsOverriden;
        }
    }

    internal static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Checks if the given language locale is supported by the mod.
        /// </summary>
        /// <param name = "Language">A string that points to the language passed by the caller.</param>
        /// <returns>Returns TRUE if the locale is supported, FALSE otherwise.</returns>
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

        /// <summary>
        /// Initializes the translation strings of a given language from the local table.
        /// </summary>
        /// <param name = "Table">An array to a table of localized strings of the target language, passed by the caller.</param>
        /// <param name = "Language">A string that points to the language passed by the caller.</param>
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

        /// <summary>
        /// Retrieves the translated string of the given term.
        /// </summary>
        /// <param name = "Language">A string that points to the language passed by the caller.</param>
        /// <param name = "Term">The term group of the translation string of which it corresponds to.</param>
        /// <returns>Returns a string which points to the translation of the given term, otherwise NULL is returned if the translation doesn't exist.</returns>
        private static string TryGetTranslation(string Language, string Term)
        {
            if (!Translations.ContainsKey(Language)) return null;

            return Translations[Language].TryGetValue(Term, out var value) ? value : null;
        }

        /// <summary>
        /// Adds a translated string into the locale translations dictionary.
        /// </summary>
        /// <param name = "Language">A string that points to the language passed by the caller.</param>
        /// <param name = "Term">The term group of the translation string of which it corresponds to.</param>
        /// <param name = "Translation">The translated string of the target language.</param>
        /// <param name = "Overriden">If set to TRUE the method will overwrite the existing translation string
        /// of the given term from the game. If set to FALSE then it indicates the passed translation string is a new string.
        /// This is for debugging purposes.</param>
        public static void Add(string Language, string Term, string Translation, bool Overriden)
        {
            /* Setup a new dictionary for the given language if we haven't done it before */
            if (!Translations.ContainsKey(Language)) Translations[Language] = new Dictionary<string, string>();

            /* Log to the debugger the given translation string is overriden */
            if (Overriden == true)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                               $"Overriding the {Term} term with {Translation}!");
            }

            /* Add the string */
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.INFO_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                                  $"Adding {Translation} translation from {Term} term for {Language} language!");
            Translations[Language][Term] = Translation;
        }

        /// <summary>
        /// Removes a translated string from the dictionary.
        /// </summary>
        /// <param name = "Language">A string that points to the language passed by the caller.</param>
        /// <param name = "Term">The term group of the translation string of which it corresponds to.</param>
        public static void Remove(string Language, string Term)
        {
            /* Bail out if the following term has no translation */
            if (!Translations.ContainsKey(Language)) return;

            /* Remove the translated string */
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.INFO_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                                  $"Removing {Term} term from {Language} language!");
            Translations[Language].Remove(Term);
        }

        /// <summary>
        /// Loads a locale.
        /// </summary>
        /// <param name = "Language">A string that points to the language passed by the caller to be loaded.</param>
        /// <remarks>Generally this method is used to load different language locales during startup of the mod. DO NOT USE IT ON ANYWHERE PART OF THE CODE!</remarks>
        public static void LoadLocale(string Language)
        {
            LocalizationTable[] StringsTable;

            COTLMP.Debug.PrintLogger.Print(DebugLevel.INFO_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                           $"Loading the {Language} language locale");

            /* Check that the given language locale is supported, bail out if not the case */
            if (!IsLocaleSupported(Language))
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.FATAL_LEVEL, DebugComponent.LOCALIZATION_COMPONENT,
                                               $"The {Language} language locale is not supported. Expect problems with mod initialization!");
                return;
            }

            /* Grab the apporpriate strings localization table */
            switch (Language)
            {
                case "English":
                {
                    StringsTable = COTLMP.Language.English.StringsTable;
                    break;
                }

                default:
                {
                    StringsTable = null;
                    break;
                }
            }

            /* Getting a null table is illegal here, it shouldn't happen */
            COTLMP.Debug.Assertions.Assert(StringsTable != null, false, null, null);

            /* Now initialize the translation strings from the locale table */
            InitializeTranslationsFromLocaleTable(StringsTable, Language);
        }

        [HarmonyPatch]
        private static class LocalizationManagerPatches
        {
            /// <summary>
            /// Patches the GetTranslation method of the localization manager of the game.
            /// Its purpose is to add custom localized strings provided by the mod into the game.
            /// </summary>
            /// <param name = "Term">The term group of the translation string of which it corresponds to.</param>
            /// <param name = "overrideLanguage">A string that points to language locale being overriden with custom translation strings. This parameter is optional.</param>
            /// <param name = "__result">The current returned value of the method. Typically this is a translation string returned by the original method
            /// of the game, which is modified on our end by the returned translation we have provided by the mod.</param>
            /// <remarks>Returns TRUE if tthe original method of the game is to be executed. FALSE if our method is to be executed instead.</remarks>
            [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetTranslation))]
            [HarmonyPrefix]
            private static bool TranslationPatch(string Term, string overrideLanguage, ref string __result)
            {
                string GameLanguage, Translation;

                /* Did the caller provide us a language? */
                if (!string.IsNullOrEmpty(overrideLanguage))
                {
                    /* Try to get the translation string from the given locale */
                    Translation = Localization.TryGetTranslation(overrideLanguage, Term);
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
                Translation = Localization.TryGetTranslation(GameLanguage, Term);
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
