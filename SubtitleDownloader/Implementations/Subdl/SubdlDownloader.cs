using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using SubtitleDownloader.Core;
using SubtitleDownloader.Util;

namespace SubtitleDownloader.Implementations.Subdl
{
    [DataContract]
    internal class OneResult
    {
        [DataMember]
        internal string imdb_id;
        [DataMember]
        internal string name;
        [DataMember]
        internal int sd_id;
        [DataMember]
        internal string slug;
        [DataMember]
        internal string tmdb_id;
        [DataMember]
        internal string type;
        [DataMember]
        internal int? year;
    }

    [DataContract]
    internal class OneSub
    {
        [DataMember]
        internal int? episode;
        [DataMember]
        internal int? season;
        [DataMember]
        internal string language;
        [DataMember]
        internal string name;
        [DataMember]
        internal string subtitlePage;
        [DataMember]
        internal string release_name;
        [DataMember]
        internal string url;
    }

    [DataContract]
    internal class SubtitleSearchResponse
    {
        [DataMember]
        internal int currentPage;
        [DataMember]
        internal bool status;
        [DataMember]
        internal int totalPages;
        [DataMember]
        internal OneResult[] results;
        [DataMember]
        internal OneSub[] subtitles;
    }

    /// <summary>
    /// Implementation uses Subdl REST API
    /// https://subdl.com/api-doc
    /// 
    /// </summary>
    public class SubdlDownloader : ISubtitleDownloader
    {
        private int searchTimeout;

        private const string ApiUrl = "https://api.subdl.com/api/v1/subtitles";

        public string GetName()
        {
            return "Subdl";
        }

        public List<Subtitle> SearchSubtitles(SearchQuery query)
        {
            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("languages", query.Get2CharLanguageCodes());
            queryString.Add("film_name", query.Query);
            if (query.Year.HasValue)
                queryString.Add("year", query.Year.Value.ToString());

            var response = GetSubtitles(queryString);
            return CreateSubtitleResults(response);
        }

        public List<Subtitle> SearchSubtitles(EpisodeSearchQuery query)
        {
            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("episode_number", query.Episode.ToString());
            queryString.Add("languages", query.Get2CharLanguageCodes());
            queryString.Add("film_name", query.SerieTitle.ToLowerInvariant());
            queryString.Add("season_number", query.Season.ToString());
            queryString.Add("type", "tv");
            queryString.Add("full_season", "0");
            if (!String.IsNullOrEmpty(query.ImdbId))
            {
                queryString.Add("imdb_id", query.ImdbId);
            }

            var response = GetSubtitles(queryString);
            return CreateSubtitleResults(response);
        }

        public List<Subtitle> SearchSubtitles(ImdbSearchQuery query)
        {
            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("languages", query.Get2CharLanguageCodes());
            queryString.Add("imdb_id", "tt" + query.ImdbId.ToString());

            var response = GetSubtitles(queryString);
            return CreateSubtitleResults(response);
        }

        public List<FileInfo> SaveSubtitle(Subtitle subtitle)
        {
            string downloadUrl = "https://dl.subdl.com" + subtitle.Id;
            string zipFile = FileUtils.GetTempFileName();

            WebClient client = new WebClient();
            client.DownloadFile(downloadUrl, zipFile);

            return FileUtils.ExtractFilesFromZipOrRarFile(zipFile);
        }

        public int SearchTimeout
        {
            get { return searchTimeout; }
            set { searchTimeout = value; }
        }

        private SubtitleSearchResponse GetSubtitles(NameValueCollection query)
        {
            query.Add("api_key", Configuration.SubdlApiKey);
            var url = ApiUrl + "?" + query.ToString();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (SearchTimeout > 0)
                request.Timeout = SearchTimeout * 1000;

            request.Accept = "application/json";

            var response = request.GetResponse().GetResponseStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(SubtitleSearchResponse));
            return (SubtitleSearchResponse)ser.ReadObject(response);
        }

        private List<Subtitle> CreateSubtitleResults(SubtitleSearchResponse subResults)
        {
            List<Subtitle> searchResults = new List<Subtitle>();

            if (subResults != null && subResults.subtitles != null && subResults.subtitles.Length > 0)
            {
                foreach (OneSub result in subResults.subtitles)
                    if (result.url != null)
                    {
                        Subtitle subtitle = new Subtitle(result.url, result.name,
                                                         result.release_name, Languages.Convert2CharTo3Char(result.language));
                        searchResults.Add(subtitle);
                    }
            }
            return searchResults;
        }
    }

}