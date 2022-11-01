using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SubtitleDownloader.Core;
using SubtitleDownloader.Util;

namespace SubtitleDownloader.Implementations.TVSubtitles
{
    /// <summary>
    /// Implementation based on XPath parsing.
    /// 
    /// Supports:
    /// - SearchSubtitles(EpisodeSearchQuery query)
    /// </summary>
    public class TvSubtitlesDownloader : ISubtitleDownloader
    {
        private int searchTimeout;

        public string GetName()
        {
            return "TvSubtitles";
        }

        private const string TvSubtitlesUrl = "http://www.tvsubtitles.net/";

        private const string SeriesListingUrl = "http://www.tvsubtitles.net/tvshows.html";

        [Obsolete("Not supported by current implementation")]
        public List<Subtitle> SearchSubtitles(SearchQuery query)
        {
            throw new NotSupportedException();
        }

        public List<Subtitle> SearchSubtitles(EpisodeSearchQuery query)
        {
            if (query.SerieTitle == null)
                throw new ArgumentException("Serie title is required in query");

            string link = ParseSeriesLinkFromSeriesListingPage(query);

            if (link == null)
                return new List<Subtitle>();

            string episodeLink = ParseEpisodeLinkFromSeasonPage(link, query.Episode);

            if (episodeLink == null)
                return new List<Subtitle>();

            return ParseSubtitlesForEpisode(episodeLink, query);
        }

        [Obsolete("Not supported by current implementation")]
        public List<Subtitle> SearchSubtitles(ImdbSearchQuery query)
        {
            throw new NotSupportedException();
        }

        public List<FileInfo> SaveSubtitle(Subtitle subtitle)
        {
            string downloadUrl = TvSubtitlesUrl + "download-" + subtitle.Id + ".html";
            string zipFile = FileUtils.GetTempFileName();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downloadUrl);
            if (SearchTimeout > 0)
                request.Timeout = SearchTimeout * 1000;


            var response = (HttpWebResponse)request.GetResponse();
            var encoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
            string result = null;
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), encoding, true))
            {
                result = reader.ReadToEnd().Trim();
            }
            var parts = Regex.Match(result, @"var\ss(?<nr>[^=]*)=\s'(?<value>[^']*)'");
            string total = String.Empty;
            while (parts.Success)
            {
                total = total + parts.Groups["value"].Value;
                parts = parts.NextMatch();
            }
            using (WebClient client = new WebClient())
                client.DownloadFile(TvSubtitlesUrl + total, zipFile);

            return FileUtils.ExtractFilesFromZipOrRarFile(zipFile);
        }

        public int SearchTimeout
        {
            get { return searchTimeout; }
            set { searchTimeout = value; }
        }

        private bool OnPreRequest(HttpWebRequest request)
        {
            if (SearchTimeout > 0)
                request.Timeout = SearchTimeout * 1000;

            return true;
        }

        //
        // Parses http://www.tvsubtitles.net/tvshows.html which is a listing of all TV series.
        // We shall try to find the serie specified in the query.SerieTitle 
        //
        private string ParseSeriesLinkFromSeriesListingPage(EpisodeSearchQuery query)
        {
            HtmlWeb web = new HtmlWeb();
            web.PreRequest = new HtmlWeb.PreRequestHandler(OnPreRequest);
            HtmlDocument seriesListingPage = web.Load(SeriesListingUrl);

            // <td align=left style="padding: 0 4px;"><a href="tvshow-8-4.html"><b>Heroes</b></a></td> 
            // -> Match <b>Heroes</b>
            // -> Get href
            HtmlNodeCollection serieTitles = seriesListingPage.DocumentNode.SelectNodes("//b");

            if (serieTitles == null)
                //throw new Exception("No link found for serie '" + query.SerieTitle + "' in " + SeriesListingUrl);
                return null;

            string link = "";

            foreach (var title in serieTitles)
            {
                if (title.InnerText.ToLower().Equals(query.SerieTitle.ToLower()))
                {
                    // Get link to serie page, parent is the link tag
                    var parent = title.ParentNode;
                    link = parent.GetAttributeValue("href", string.Empty);
                }
            }

            if (String.IsNullOrEmpty(link))
                //throw new Exception("No link found for serie '" + query.SerieTitle + "' in " + SeriesListingUrl);
                return null;

            // Link to the serie page e.g. http://www.tvsubtitles.net/tvshow-8-4.html
            string[] splitted = link.Split('-');

            // http://www.tvsubtitles.net/tvshow-8-[seasonnumberfromquery].html
            return splitted[0] + "-" + splitted[1] + "-" + query.Season + ".html";
        }

        private string ParseEpisodeLinkFromSeasonPage(string link, int episode)
        {
            HtmlWeb web = new HtmlWeb();
            web.PreRequest = new HtmlWeb.PreRequestHandler(OnPreRequest);
            HtmlDocument episodesListingPage = web.Load(TvSubtitlesUrl + link);

            // <td>1x11</td> 
            // <td align=left style="padding: 0 4px;"><a href="episode-469.html"><b>Fallout</b></a></td> 
            HtmlNodeCollection episodeTds = episodesListingPage.DocumentNode.SelectNodes("//td");

            foreach (var episodeTd in episodeTds)
            {
                if (episodeTd.InnerText.Contains("x"))
                {
                    string[] splitted = episodeTd.InnerText.Split('x');
                    int currentEpisode = -1;

                    if (splitted.Length == 2)
                    {

                        try
                        {
                            currentEpisode = Convert.ToInt32(splitted[1]);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (currentEpisode > 0 && episode == currentEpisode)
                    {
                        HtmlNode linkTd = episodeTd.NextSibling.NextSibling;
                        HtmlNode episodeLink = linkTd.FirstChild;

                        return episodeLink.GetAttributeValue("href", string.Empty);
                    }
                }
            }
            return null;
        }

        //
        // Parses all subtitles for found episodes
        //
        private List<Subtitle> ParseSubtitlesForEpisode(string episodePage, EpisodeSearchQuery query)
        {
            List<Subtitle> results = new List<Subtitle>();

            HtmlWeb web = new HtmlWeb();
            web.PreRequest = new HtmlWeb.PreRequestHandler(OnPreRequest);
            HtmlDocument subtitlesListingPage = web.Load(TvSubtitlesUrl + episodePage);

            // <a href="/subtitle-84813.html" title="Download spanish subtitles" class="subtitlen"> 
            // -> Get href for parsing subtitle id
            // -> Get language from title for subtitle language
            HtmlNodeCollection subtitleLinks = subtitlesListingPage.DocumentNode.SelectNodes("//a");

            foreach (var subtitleLink in subtitleLinks)
            {
                string href = subtitleLink.GetAttributeValue("href", string.Empty);

                if (href.StartsWith("/subtitle"))
                {
                    string id = ParseSubtitleId(href);

                    var childDiv = subtitleLink.FirstChild;

                    string language = ParseSubtitleLanguageFromTitleAttribute(
                        childDiv.GetAttributeValue("title", string.Empty));

                    if (SubtitleLanguageMatchesQueried(language.ToLower(), query.LanguageCodes))
                    {
                        string filename = ParseSubtitleFilenameFromSubtitlePage(href);
                        Subtitle sub = new Subtitle(id, query.SerieTitle, filename, Languages.GetLanguageCode(language));
                        results.Add(sub);
                    }
                }
            }
            return results;
        }

        private bool SubtitleLanguageMatchesQueried(string subtitleLang, string[] queriedLangs)
        {
            foreach (var lang in queriedLangs)
            {
                string queriedLang = Languages.GetLanguageName(lang).ToLower();

                if (subtitleLang.Equals(queriedLang))
                {
                    return true;
                }
            }
            return false;
        }

        private string ParseSubtitleId(string href)
        {
            // /subtitle-84813.html -> 84813
            return href.Split('-')[1].Split('.')[0];
        }

        private string ParseSubtitleFilenameFromSubtitlePage(string link)
        {
            HtmlWeb web = new HtmlWeb();
            web.PreRequest = new HtmlWeb.PreRequestHandler(OnPreRequest);
            HtmlDocument subtitleInfoPage = web.Load(TvSubtitlesUrl + link);

            // <td><b>filename:</b></td><td width=70%>Heroes - 4x01-02 - Orientation.HDTV.FQM.en.srt</td>
            HtmlNodeCollection bTags = subtitleInfoPage.DocumentNode.SelectNodes("//b");

            foreach (var tag in bTags)
            {
                if (tag.InnerText.Equals("filename:"))
                {
                    // Get parent <td>
                    HtmlNode parentTd = tag.ParentNode;

                    // Get parent <td>'s sibling <td width=70%>Heroes - 4x01-02 - Orientation.HDTV.FQM.en.srt</td>
                    HtmlNode filenameTd = parentTd.NextSibling;

                    return filenameTd.InnerText;
                }
            }
            return null;
        }

        private string ParseSubtitleLanguageFromTitleAttribute(string title)
        {
            // "Download spanish subtitles" -> spanish
            string[] splitted = title.Split(' ');
            return splitted[1];
        }
    }
}
