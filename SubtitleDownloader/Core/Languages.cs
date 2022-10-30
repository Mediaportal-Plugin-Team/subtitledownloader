using System;
using System.Collections.Generic;
using System.Linq;

namespace SubtitleDownloader.Core
{
    /// <summary>
    /// Utility class for subtitle languages
    /// </summary>
    public static class Languages
    {
        private static readonly SubLang DefaultLanguage = new SubLang("en", "eng", "English");

        private static readonly SubLang[] aliases =
        {
            new SubLang("nl", "nld", "Dutch")
        };

        private static readonly SubLang[] languages =
        {
            new SubLang("bs", "bos", "Bosnian"),

            new SubLang("sl", "slv", "Slovenian"),

            new SubLang("hr", "hrv", "Croatian"),

            new SubLang("sr", "srp", "Serbian"),

            new SubLang("en", "eng", "English"),

            new SubLang("es", "spa", "Spanish"),

            new SubLang("fr", "fre", "French"),

            new SubLang("el", "gre", "Greek"),

            new SubLang("de", "ger", "German"),

            new SubLang("ru", "rus", "Russian"),

            new SubLang("zh", "chi", "Chinese"),

            new SubLang("pt", "por", "Portuguese"),

            new SubLang("nl", "dut", "Dutch"),

            new SubLang("it", "ita", "Italian"),

            new SubLang("ro", "rum", "Romanian"),

            new SubLang("cs", "cze", "Czech"),

            new SubLang("ar", "ara", "Arabic"),

            new SubLang("pl", "pol", "Polish"),

            new SubLang("tr", "tur", "Turkish"),

            new SubLang("sv", "swe", "Swedish"),

            new SubLang("fi", "fin", "Finnish"),

            new SubLang("hu", "hun", "Hungarian"),

            new SubLang("da", "dan", "Danish"),

            new SubLang("he", "heb", "Hebrew"),

            new SubLang("et", "est", "Estonian"),

            new SubLang("sk", "slo", "Slovak"),

            new SubLang("id", "ind", "Indonesian"),

            new SubLang("fa", "per", "Persian"),

            new SubLang("bg", "bul", "Bulgarian"),

            new SubLang("ja", "jpn", "Japanese"),

            new SubLang("sq", "alb", "Albanian"),

            new SubLang("be", "bel", "Belarusian"),

            new SubLang("hi", "hin", "Hindi"),

            new SubLang("ga", "gle", "Irish"),

            new SubLang("is", "ice", "Icelandic"),

            new SubLang("ca", "cat", "Catalan"),

            new SubLang("ko", "kor", "Korean"),

            new SubLang("la", "lav", "Latvian"),

            new SubLang("lt", "lit", "Lithuanian"),

            new SubLang("mk", "mac", "Macedonian"),

            new SubLang("no", "nor", "Norwegian"),

            new SubLang("th", "tha", "Thai"),

            new SubLang("uk", "ukr", "Ukrainian"),

            new SubLang("vi", "vie", "Vietnamese")
        };

        /// <summary>
        /// Gets the ISO 639-2 language code for given language
        /// </summary>
        /// <param name="languageName">Name of the language in english, e.g. "finnish", "Finnish"</param>
        /// <returns>ISO 639-2 language code for the language, e.g. "eng"
        /// Returns "eng" if language code cannot be found with given language name</returns>
        public static string GetLanguageCode(string languageName)
        {
            var code = FindLanguageCode(languageName);

            return code ?? DefaultLanguage.ThreeCharCode;
        }

        /// <summary>
        /// Gets the ISO 639-2 language code for given language
        /// </summary>
        /// <param name="languageName">Name of the language in english, e.g. "finnish", "Finnish"</param>
        /// <returns>ISO 639-2 language code for the language, e.g. "eng" or null if not found
        /// Returns null if language code cannot be found with given language name</returns>
        public static string FindLanguageCode(string languageName)
        {
            if (String.IsNullOrEmpty(languageName))
                throw new ArgumentException("Language name cannot be null or empty!");

            var lang = languages.Where(l => l.Name.Equals(languageName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            return lang == null ? null : lang.ThreeCharCode;
        }

        /// <summary>
        /// Gets the language name
        /// </summary>
        /// <param name="languageCode">Language code for language, e.g. "eng"</param>
        /// <returns>Language name in english, e.g. "Finnish". 
        /// Returns English if language cannot be found with given language code</returns>
        public static string GetLanguageName(string languageCode)
        {
            if (String.IsNullOrEmpty(languageCode))
                throw new ArgumentException("Language code cannot be null or empty!");

            if (languageCode.Count() != 3) throw new ArgumentException("Invalid ISO 639-2 language code!");

            var lang = FindLanguageByLanguageCodeInternal(languageCode);

            return lang == null ? DefaultLanguage.Name : lang.Name;
        }

        /// <summary>
        /// Check if the language code given is supported.
        /// </summary>
        /// <param name="languageCode">ISO 639-2 language code, e.g. "eng"</param>
        /// <returns>True if language code is supported, otherwise false</returns>
        public static bool IsSupportedLanguageCode(string languageCode)
        {
            return FindLanguageByLanguageCodeInternal(languageCode) != null;
        }

        /// <summary>
        /// Check if language given is supported
        /// </summary>
        /// <param name="languageName">Name of the language in english, e.g. "Finnish"</param>
        /// <returns>True if language is supported, otherwise false</returns>
        public static bool IsSupportedLanguageName(string languageName)
        {
            return languages.Any(lang => lang.Name.Equals(languageName,
                                                          StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all language names supported
        /// </summary>
        /// <returns>Language names in english</returns>
        public static List<String> GetLanguageNames()
        {
            return languages.Select(lang => lang.Name).ToList();
        }

        public static string Convert2CharTo3Char(string twoChar)
        {
            if (String.IsNullOrEmpty(twoChar))
                throw new ArgumentException("Languagecode cannot be null or empty!");

            var lang = languages.Where(l => l.TwoCharCode.Equals(twoChar, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            return lang == null ? null : lang.ThreeCharCode;
        }

        public static string Convert3CharTo2Char(string threeChar)
        {
            if (String.IsNullOrEmpty(threeChar))
                throw new ArgumentException("Languagecode cannot be null or empty!");

            var lang = languages.Where(l => l.ThreeCharCode.Equals(threeChar, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            return lang == null ? null : lang.TwoCharCode;
        }

        private static SubLang FindLanguageByLanguageCodeInternal(string languageCode)
        {
            return languages.Where(l => l.ThreeCharCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault() ??
                       aliases.Where(l => l.ThreeCharCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }
    }

    internal class SubLang
    {
        public string TwoCharCode { get; private set; }
        public string ThreeCharCode { get; private set; }

        public string Name { get; private set; }

        public SubLang(string twoCharCode, string threeCharCode, string name)
        {
            TwoCharCode = twoCharCode;
            ThreeCharCode = threeCharCode;
            Name = name;
        }
    }
}