using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml.Serialization;
using SubtitleDownloader.Core;
using SubtitleDownloader.Util;

namespace SubtitleDownloader.Implementations.OpenSubtitles
{
#pragma warning disable 649
    [DataContract]
    internal class FeatureDetails
    {
        [DataMember]
        internal string movie_name;
        [DataMember]
        internal int? year;
    }
    [DataContract]
    internal class OneFile
    {
        [DataMember]
        internal string file_id;

        [DataMember]
        internal string file_name;
    }

    [DataContract]
    internal class Attributes
    {
        [DataMember]
        internal string subtitle_id;
        [DataMember]
        internal string language;
        [DataMember]
        internal string url;
        [DataMember]
        internal FeatureDetails feature_details;
        [DataMember]
        internal OneFile[] files;
    }

    [DataContract]
    internal class OneSub
    {
        [DataMember]
        internal int id;
        [DataMember]
        internal string type;
        [DataMember]
        internal Attributes attributes;
    }
    [DataContract]
    internal class SubtitleSearchResponse
    {
        [DataMember]
        internal int page;
        [DataMember]
        internal int per_page;
        [DataMember]
        internal int total_count;
        [DataMember]
        internal int total_pages;
        [DataMember(Name = "data")]
        internal OneSub[] subtitles;
    }

    [DataContract]
    internal class LoginResponse
    {
        [DataMember]
        internal string token;
    }

    [DataContract]
    internal class DownloadResponse
    {
        [DataMember]
        internal string link;
        [DataMember]
        internal string file_name;
        [DataMember]
        internal int requests;
        [DataMember]
        internal int remaining;
        [DataMember]
        internal string message;
    }
#pragma warning restore 649

    /// <summary>
    /// Implementation uses OpenSubtitles REST API
    /// https://opensubtitles.stoplight.io/docs/opensubtitles-api/e3750fd63a100-getting-started
    /// 
    /// Supports:
    /// - SearchSubtitles(SearchQuery query)
    /// - SearchSubtitles(EpisodeSearchQuery query)
    /// - SearchSubtitles(ImdbSearchQuery query)
    /// </summary>
    public class OpenSubtitlesDownloader : ISubtitleDownloader
    {
        private const string ApiUrl = "https://api.opensubtitles.com/api/v1/";

        private string token = null;

        private int searchTimeout;

        private OpenSubtitlesConfiguration configuration = new OpenSubtitlesConfiguration();

        public OpenSubtitlesDownloader() : this(FileUtils.AssemblyDirectory + "\\SubtitleDownloaders\\OpenSubtitlesConfiguration.xml")
        {

        }

        public OpenSubtitlesDownloader(string configurationFile)
        {
            if (File.Exists(configurationFile))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(OpenSubtitlesConfiguration));

                using (FileStream fileStream = new FileStream(configurationFile, FileMode.Open, FileAccess.Read))
                {
                    this.configuration = (OpenSubtitlesConfiguration)serializer.Deserialize(fileStream);
                }
            }
        }

        public string GetName()
        {
            return "OpenSubtitles";
        }

        public List<Subtitle> SearchSubtitles(SearchQuery query)
        {
            CreateConnectionAndLogin();

            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("languages", GetLanguageCodes(query));
            queryString.Add("query", query.Query.ToLowerInvariant());
            if (query.Year.HasValue)
                queryString.Add("year", query.Year.Value.ToString());

            var response = GetWebData(ApiUrl + "subtitles?" + queryString.ToString());
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(SubtitleSearchResponse));
            SubtitleSearchResponse sr = (SubtitleSearchResponse)ser.ReadObject(response);

            return CreateSubtitleResults(sr, query.Year, query.Query.ToLowerInvariant());
        }

        public List<Subtitle> SearchSubtitles(EpisodeSearchQuery query)
        {
            CreateConnectionAndLogin();

            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("episode_number", query.Episode.ToString());
            queryString.Add("languages", GetLanguageCodes(query));
            queryString.Add("query", query.SerieTitle.ToLowerInvariant());
            queryString.Add("season_number", query.Season.ToString());
            if (!String.IsNullOrEmpty(query.ImdbId))
            {
                if (query.ImdbId.StartsWith("tt"))
                    queryString.Add("imdb_id", query.ImdbId.Substring(2));
                else
                    queryString.Add("imdb_id", query.ImdbId);
            }

            var response = GetWebData(ApiUrl + "subtitles?" + queryString.ToString());

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(SubtitleSearchResponse));
            SubtitleSearchResponse sr = (SubtitleSearchResponse)ser.ReadObject(response);

            return CreateSubtitleResults(sr, null, query.SerieTitle.ToLowerInvariant());
        }

        public List<Subtitle> SearchSubtitles(ImdbSearchQuery query)
        {
            CreateConnectionAndLogin();

            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("imdb_id", query.ImdbId.ToLowerInvariant());
            queryString.Add("languages", GetLanguageCodes(query));

            var response = GetWebData(ApiUrl + "subtitles?" + queryString.ToString());
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(SubtitleSearchResponse));
            SubtitleSearchResponse sr = (SubtitleSearchResponse)ser.ReadObject(response);

            return CreateSubtitleResults(sr, null, null);
        }

        public List<FileInfo> SaveSubtitle(Subtitle subtitle)
        {
            CreateConnectionAndLogin();

            OneFile oneFile = new OneFile()
            {
                file_id = subtitle.Id,
                file_name = subtitle.FileName
            };
            string url;
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(OneFile));
            using (Stream stream = new MemoryStream())
            {
                ser.WriteObject(stream, oneFile);
                var response = GetWebData(ApiUrl + "download", stream);
                DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(DownloadResponse));
                DownloadResponse downloadResponse = (DownloadResponse)ser2.ReadObject(response);
                url = downloadResponse.link;
            }

            if (!String.IsNullOrEmpty(url))
            {
                string tempFile = Path.GetTempPath() + subtitle.FileName;
                if (!tempFile.EndsWith(".srt", StringComparison.InvariantCultureIgnoreCase))
                    tempFile += ".srt";
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                if (SearchTimeout > 0)
                    request.Timeout = SearchTimeout * 1000;
                var responseStream = request.GetResponse().GetResponseStream();
                using (var fStream = new FileStream(tempFile, FileMode.CreateNew))
                {
                    responseStream.CopyTo(fStream);
                }

                return new List<FileInfo> { new FileInfo(tempFile) };
            }
            throw new Exception("Subtitle not found with ID '" + subtitle.Id + "'");
        }

        public int SearchTimeout
        {
            get { return searchTimeout; }
            set { searchTimeout = value; }
        }

        private Stream GetWebData(string url, Stream postData = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/json";
            request.UserAgent = Configuration.OpenSubtitlesUserAgent;
            request.Headers.Add("Api-Key", Configuration.OpenSubtitlesApiKey);
            request.Accept = "*/*";
            if (token != null)
                request.Headers.Add("Authorization", "Bearer " + token);
            if (SearchTimeout > 0)
                request.Timeout = SearchTimeout * 1000;

            if (postData != null)
            {
                request.Method = "POST";
                request.ContentLength = postData.Length;

                var stream = request.GetRequestStream();
                postData.Position = 0;
                postData.CopyTo(stream);
            }
            return request.GetResponse().GetResponseStream();
        }

        private List<Subtitle> CreateSubtitleResults(SubtitleSearchResponse subResults, int? queryYear, string title)
        {
            List<Subtitle> searchResultsExact = new List<Subtitle>();
            List<Subtitle> searchResultsFuzzy = new List<Subtitle>();
            List<Subtitle> searchResults;

            if (subResults != null && subResults.subtitles != null && subResults.subtitles.Length > 0)
            {
                foreach (OneSub result in subResults.subtitles)
                    if (result.attributes.files.Length == 1 && result.attributes.files[0].file_name != null)
                    {
                        Subtitle subtitle = new Subtitle(result.attributes.files[0].file_id, result.attributes.feature_details.movie_name,
                                                         result.attributes.files[0].file_name, Languages.Convert2CharTo3Char(result.attributes.language));
                        if (!String.IsNullOrEmpty(title) && result.attributes.feature_details.movie_name.StartsWith(title, StringComparison.OrdinalIgnoreCase))
                            searchResults = searchResultsExact;
                        else
                            searchResults = searchResultsFuzzy;

                        if (queryYear != null)
                        {
                            // Check if the query year matches
                            if (result.attributes.feature_details.year.HasValue && result.attributes.feature_details.year.Value != 0)
                            {
                                if (queryYear.Equals(result.attributes.feature_details.year.Value))
                                {
                                    searchResults.Add(subtitle);
                                }
                            }
                            else
                            {
                                // No year found in results set
                                searchResults.Add(subtitle);
                            }
                        }
                        else
                        {
                            searchResults.Add(subtitle);
                        }
                    }
            }
            searchResultsExact.AddRange(searchResultsFuzzy);
            return searchResultsExact;
        }

        private void CreateConnectionAndLogin()
        {
            if (token == null)
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(OpenSubtitlesConfiguration));
                using (Stream stream = new MemoryStream())
                {
                    ser.WriteObject(stream, configuration);
                    var response = GetWebData(ApiUrl + "login", stream);
                    DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(LoginResponse));
                    LoginResponse loginResult = (LoginResponse)ser2.ReadObject(response);
                    token = loginResult.token;
                }
            }
        }

        private string GetLanguageCodes(SubtitleSearchQuery query)
        {
            string[] twoCharLanguages = new string[query.LanguageCodes.Length];
            for (int i = 0; i < query.LanguageCodes.Length; i++)
                twoCharLanguages[i] = Languages.Convert3CharTo2Char(query.LanguageCodes[i]);
            return string.Join(",", twoCharLanguages);
        }
    }

}