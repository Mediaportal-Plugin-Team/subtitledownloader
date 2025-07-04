
using System;
using System.Linq;
using SubtitleDownloader.Util;

namespace SubtitleDownloader.Core
{
    /// <summary>
    /// Search subtitles by IMDB ID
    /// </summary>
    public class ImdbSearchQuery : SubtitleSearchQuery
    {
        /// <summary>
        /// Creates a query for searching a subtitle by IMDB ID, e.g. "0813715"
        /// </summary>
        /// <param name="imdbId">Imdb ID for the program, e.g. "0813715"</param>
        public ImdbSearchQuery(string imdbId)
        {
            if (!imdbId.IsNumeric())
                throw new ArgumentException("IMDB ID value must be numeric, like \"0813715\"!");

            ImdbId = imdbId;
        }

        /// <summary>
        /// Imdb ID for the program, e.g. "0813715"
        /// </summary>
        public string ImdbId { get; private set; }

        public int? ImdbIdNullable
        {
            get
            {
                int? imdbId = null;

                if (ImdbId != null)
                {
                    imdbId = Int32.Parse(ImdbId);
                }
                return imdbId;
            }
        }
    }

    /// <summary>
    /// Search subtitles for TV series by using serie name, episode and season info
    /// </summary>
    public class EpisodeSearchQuery : SubtitleSearchQuery
    {
        /// <summary>
        /// Creates a query for searching a subtitle for TV serie episode
        /// </summary>
        /// <param name="serieTitle">Title of the serie</param>
        /// <param name="season">Season number</param>
        /// <param name="episode">Episode number</param>
        [Obsolete("This method is preserved for backwards compatibility")]
        public EpisodeSearchQuery(String serieTitle, int season, int episode)
        {
            SerieTitle = serieTitle;
            Season = season;
            Episode = episode;
        }

        /// <summary>
        /// Creates a query for searching a subtitle for TV serie episode
        /// </summary>
        /// <param name="serieTitle">Title of the serie</param>
        /// <param name="season">Season number</param>
        /// <param name="episode">Episode number</param>
        /// <param name="tvdbId">TVDB (http://thetvdb.com) ID for the serie (optional)</param>
        public EpisodeSearchQuery(String serieTitle, int season, int episode, int? tvdbId = null)
        {
            SerieTitle = serieTitle;
            Season = season;
            Episode = episode;
            TvdbId = tvdbId;
        }

        /// <summary>
        /// Creates a query for searching a subtitle for TV serie episode
        /// </summary>
        /// <param name="serieTitle">Title of the serie</param>
        /// <param name="season">Season number</param>
        /// <param name="episode">Episode number</param>
        /// <param name="imdbId">IMDB (http://imdb.com) ID for the serie (may include "tt")</param>
        public EpisodeSearchQuery(String serieTitle, int season, int episode, string imdbId = null)
        {
            SerieTitle = serieTitle;
            Season = season;
            Episode = episode;
            ImdbId = imdbId;
        }

        /// <summary>
        /// Title for the serie, e.g. "Heroes"
        /// </summary>
        public string SerieTitle { get; private set; }

        /// <summary>
        /// Episode number
        /// </summary>
        public int Episode { get; private set; }

        /// <summary>
        /// Season number
        /// </summary>
        public int Season { get; private set; }

        /// <summary>
        /// TVDB (http://thetvdb.com) ID for the serie
        /// </summary>
        public int? TvdbId { get; private set; }

        /// <summary>
        /// IMDB (http://imdb.com) ID for the serie
        /// </summary>
        public string ImdbId { get; private set; }

    }

    /// <summary>
    /// Search subtitles by using a query string and optional year of making.
    /// </summary>
    public class SearchQuery : SubtitleSearchQuery
    {
        /// <summary>
        /// Creates a search query with query string to search for
        /// </summary>
        /// <param name="query"></param>
        public SearchQuery(string query)
        {
            Query = query;
        }

        private int? year;

        /// <summary>
        /// Year of the program
        /// </summary>
        public int? Year
        {
            get
            {
                return year;
            }

            set
            {
                if (value < 1900 || value > 2050)
                {
                    throw new ArgumentException("Invalid year, must be between 1900-2050");
                }
                year = value;
            }
        }

        /// <summary>
        /// Query string for text search
        /// </summary>
        public string Query { get; private set; }
    }

    /// <summary>
    /// Base class for all subtitle queries.
    /// Default language for subtitles to search for is English.
    /// </summary>
    public abstract class SubtitleSearchQuery
    {
        private string[] languageCodes;

        /// <summary>
        /// Desired languages for subtitles (ISO 639-2 Code). Default is English.
        /// </summary>
        public string[] LanguageCodes
        {
            get
            {
                return languageCodes;
            }
            set
            {
                if (value.Any(lang => lang.Length != 3))
                {
                    throw new ArgumentException("Language codes must be ISO 639-2 Code!");
                }
                languageCodes = value;
            }
        }

        protected SubtitleSearchQuery()
        {
            LanguageCodes = new[] { "eng" };
        }

        public bool HasLanguageCode(string languageCode)
        {
            if (languageCode == null)
                return false;

            if (languageCode.Length != 3)
                throw new ArgumentException("Language code must be ISO 639-2 Code!");

            return languageCodes.Any(code => code.Equals(languageCode.ToLower()));
        }

        public string Get2CharLanguageCodes()
        {
            string[] twoCharLanguages = new string[LanguageCodes.Length];
            for (int i = 0; i < LanguageCodes.Length; i++)
                twoCharLanguages[i] = Languages.Convert3CharTo2Char(LanguageCodes[i]);
            return string.Join(",", twoCharLanguages);
        }

    }
}