using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using CookComputing.XmlRpc;
using SubtitleDownloader.Core;
using SubtitleDownloader.Util;

namespace SubtitleDownloader.Implementations.OpenSubtitles
{
    /// <summary>
    /// Implementation uses OpenSubtitles XML-RPC API
    /// 
    /// Supports:
    /// - SearchSubtitles(SearchQuery query)
    /// - SearchSubtitles(EpisodeSearchQuery query)
    /// - SearchSubtitles(ImdbSearchQuery query)
    /// </summary>
    public class OpenSubtitlesDownloader : ISubtitleDownloader
    {
        private const string ApiUrl = "https://api.opensubtitles.org/xml-rpc";

        private readonly string UserAgent = Configuration.OpenSubtitlesUserAgent;

        private IOpenSubtitlesProxy openSubtitlesProxy;

        private string token;

        private int searchTimeout;

        private OpenSubtitlesConfiguration configuration = new OpenSubtitlesConfiguration();

        public OpenSubtitlesDownloader() : this(FileUtils.AssemblyDirectory + "\\SubtitleDownloaders\\OpenSubtitlesConfiguration.xml")
        {

        }

        public OpenSubtitlesDownloader(string configurationFile)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;

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

            if (searchTimeout > 0)
                openSubtitlesProxy.Timeout = searchTimeout * 1000;

            subInfo[] searchQuery = new[] { new subInfo(GetLanguageCodes(query), "", null, null, query.Query) };

            return PerformSearch(searchQuery, query.Year);
        }

        public List<Subtitle> SearchSubtitles(EpisodeSearchQuery query)
        {
            CreateConnectionAndLogin();

            if (searchTimeout > 0)
                openSubtitlesProxy.Timeout = searchTimeout * 1000;

            string episode = "e" + String.Format("{0:00}", query.Episode);
            string season = "s" + String.Format("{0:00}", query.Season);

            // e.g. "heroes s04e04"
            string q = query.SerieTitle + " " + season + episode;

            subInfo[] searchQuery = new[] { new subInfo(GetLanguageCodes(query), "", null, null, q) };

            return PerformSearch(searchQuery, null);
        }

        public List<Subtitle> SearchSubtitles(ImdbSearchQuery query)
        {
            CreateConnectionAndLogin();

            if (searchTimeout > 0)
                openSubtitlesProxy.Timeout = searchTimeout * 1000;

            subInfo[] searchQuery = new[] { new subInfo(GetLanguageCodes(query), "", null, query.ImdbIdNullable, "") };

            return PerformSearch(searchQuery, null);
        }

        public List<FileInfo> SaveSubtitle(Subtitle subtitle)
        {
            CreateConnectionAndLogin();

            subdata files = openSubtitlesProxy.DownloadSubtitles(token, new[] { subtitle.Id } );

            if (files != null && files.data != null && files.data.Count() > 0)
            {
                string originalFileName = subtitle.FileName;

                string tempFile = Path.GetTempPath() + originalFileName;

                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                FileUtils.WriteNewFile(tempFile,
                                       Decoder.DecodeAndDecompress(files.data[0].data));
                
                return new List<FileInfo> { new FileInfo(tempFile) };
            }
            throw new Exception("Subtitle not found with ID '" + subtitle.Id + "'");
        }

        public int SearchTimeout
        {
            get { return searchTimeout; }
            set { searchTimeout = value; }
        }

        private List<Subtitle> PerformSearch(subInfo[] searchQuery, int? queryYear)
        {
            subrt subResults;

            try
            {
                subResults = openSubtitlesProxy.SearchSubtitles(token, searchQuery);
            }
            catch (XmlRpcTypeMismatchException)
            {
                // For some reason if no results are found, data is type boolean
                return new List<Subtitle>(0);
            }

            return CreateSubtitleResults(subResults, queryYear);
        }

        private List<Subtitle> CreateSubtitleResults(subrt subResults, int? queryYear)
        {
            List<Subtitle> searchResults = new List<Subtitle>();

            if (subResults != null && subResults.data != null && subResults.data.Count() > 0)
            {
                foreach (subRes result in subResults.data)
                {
                    Subtitle subtitle = new Subtitle(result.IDSubtitleFile, result.MovieNameEng, 
                                                     result.SubFileName, result.SubLanguageID);

                    if (queryYear != null)
                    {
                        // Check if the query year matches
                        if (result.MovieYear != null)
                        {
                            int yearAsInt = Convert.ToInt16(result.MovieYear);

                            if (queryYear.Equals(yearAsInt))
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
            return searchResults;
        }

        private void CreateConnectionAndLogin()
        {
            openSubtitlesProxy = XmlRpcProxyGen.Create<IOpenSubtitlesProxy>();
            openSubtitlesProxy.Url = ApiUrl;
            openSubtitlesProxy.KeepAlive = false;

            XmlRpcStruct login = openSubtitlesProxy.LogIn(configuration.Username, configuration.Password, configuration.Language, UserAgent);
            token = login["token"].ToString();
        }

        private string GetLanguageCodes(SubtitleSearchQuery query)
        {
            string languageCodes = "";

            foreach (string languageCode in query.LanguageCodes)
            {
                languageCodes += languageCode;
                languageCodes += ",";
            }

            if (query.LanguageCodes.Length > 0)
                languageCodes = languageCodes.Remove(languageCodes.Length - 1, 1);

            return languageCodes;
        }
    }

    //
    // Classes for XML-RPC communication
    //

    public interface IOpenSubtitlesProxy : IXmlRpcProxy
    {
        [XmlRpcMethod("ServerInfo")]
        XmlRpcStruct ServerInfo();

        [XmlRpcMethod("LogIn")]
        XmlRpcStruct LogIn(string username, string password, string language, string useragent);

        [XmlRpcMethod("LogOut")]
        XmlRpcStruct LogOut(string token);

        [XmlRpcMethod("SearchSubtitles")]
        subrt SearchSubtitles(string token, subInfo[] subs);

        [XmlRpcMethod("DownloadSubtitles")]
        subdata DownloadSubtitles(string token, string[] subs);
    }

    public class subtitle
    {
        public string idsubtitlefile { get; set; }
        public string data { get; set; }
    }

    public class subrt
    {
        public subRes[] data { get; set; }
        public double seconds { get; set; }
    }

    public class subInfo
    {
        public subInfo(string sublanguageid, string moviehash, int? moviebytesize, int? imdbid, string query)
        {
            this.sublanguageid = sublanguageid;
            this.moviehash = moviehash;
            this.moviebytesize = moviebytesize;
            this.imdbid = imdbid;
            this.query = query;
        }

        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string sublanguageid { get; set; }
        public string moviehash { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public int? moviebytesize { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public int? imdbid { get; set; }
        public string query { get; set; }
    }

    public class subdata
    {
        public string status { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public subtitle[] data;
        public double seconds { get; set; }
    }

    public class subRes
    {
        public string IDSubtitle { get; set; }
        public string IDSubtitleFile { get; set; }
        public string SubAuthorComment { get; set; }
        public string LanguageName { get; set; }
        public string UserID { get; set; }
        public string MovieNameEng { get; set; }
        public string MovieByteSize { get; set; }
        public string IDMovie { get; set; }
        public string MovieYear { get; set; }
        public string SubHash { get; set; }
        public string MovieHash { get; set; }
        public string SubFormat { get; set; }
        public string SubBad { get; set; }
        public string SubDownloadsCnt { get; set; }
        public string SubLanguageID { get; set; }
        public string IDMovieImdb { get; set; }
        public string MovieTimeMS { get; set; }
        public string SubActualCD { get; set; }
        public string MovieReleaseName { get; set; }
        public string SubRating { get; set; }
        public string SubDownloadLink { get; set; }
        public string ZipDownloadLink { get; set; }
        public string IDSubMovieFile { get; set; }
        public string ISO639 { get; set; }
        public string SubSumCD { get; set; }
        public string SubSize { get; set; }
        public string UserNickName { get; set; }
        public string SubAddDate { get; set; }
        public string MovieName { get; set; }
        public string MovieImdbRating { get; set; }
        public string SubFileName { get; set; }
    }
}