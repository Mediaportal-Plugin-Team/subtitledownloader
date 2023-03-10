using SubtitleDownloader.Implementations.OpenSubtitles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubtitleDownloader.Core;
using SubtitleDownloader.Util;
using System.Collections.Generic;
using System.IO;

namespace SubtitleDownloaderTests
{
    
    
    /// <summary>
    ///This is a test class for OpenSubtitlesDownloaderTest and is intended
    ///to contain all OpenSubtitlesDownloaderTest Unit Tests
    ///</summary>
    [TestClass()]
    public class OpenSubtitlesDownloaderTest
    {
        private string configuration = FileUtils.AssemblyDirectory + @"\..\..\..\SubtitleDownloader\Implementations\OpenSubtitles\OpenSubtitlesConfiguration.xml";

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for SaveSubtitle
        ///</summary>
        [TestMethod()]
        public void SaveSubtitleTest()
        {
            OpenSubtitlesDownloader target = new OpenSubtitlesDownloader(configuration);

            EpisodeSearchQuery query = new EpisodeSearchQuery("Prison Break", 4, 21);
            query.LanguageCodes = new string[] { "swe", "dut"};
            List<Subtitle> subtitles = target.SearchSubtitles(query);

            Assert.IsTrue(subtitles.Count > 0);

            List<FileInfo> fileInfos = target.SaveSubtitle(subtitles[0]);

            Assert.IsTrue(fileInfos[0].Exists);
            Assert.IsTrue(fileInfos[0].FullName.EndsWith(".srt", System.StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        ///A test for SearchSubtitles
        ///</summary>
        [TestMethod()]
        public void SearchQuerySearchSubtitlesTest()
        {
            OpenSubtitlesDownloader target = new OpenSubtitlesDownloader(configuration);
            
            SearchQuery query = new SearchQuery("heroes");
            query.LanguageCodes = new string[] { "dut" };
            List<Subtitle> actual = target.SearchSubtitles(query);

            Assert.IsTrue(actual.Count > 0);
        }

        /// <summary>
        ///A test for SearchSubtitles
        ///</summary>
        [TestMethod()]
        public void SearchQuerySearchSubtitlesTest2()
        {
            OpenSubtitlesDownloader target = new OpenSubtitlesDownloader(configuration);

            SearchQuery query = new SearchQuery("batman");
            query.Year = 2005;

            List<Subtitle> actual = target.SearchSubtitles(query);

            Assert.IsTrue(actual.Count > 0);
        }

        /// <summary>
        ///A test for SearchSubtitles
        ///</summary>
        [TestMethod()]
        public void EpisodeSearchQuerySearchSubtitlesTest()
        {
            OpenSubtitlesDownloader target = new OpenSubtitlesDownloader(configuration);

            EpisodeSearchQuery query = new EpisodeSearchQuery("heroes", 1, 1);
            query.LanguageCodes = new string[] { "eng", "dut" };
            
            List<Subtitle> actual = target.SearchSubtitles(query);
            
            Assert.IsTrue(actual.Count > 0);
        }

        /// <summary>
        ///A test for SearchSubtitles
        ///</summary>
        [TestMethod()]
        public void EmptyEpisodeSearchQuerySearchSubtitlesTest()
        {
            OpenSubtitlesDownloader target = new OpenSubtitlesDownloader(configuration);

            EpisodeSearchQuery query = new EpisodeSearchQuery("foobarnotfound", 1, 1);
            query.LanguageCodes = new string[] { "eng", "dut" };

            List<Subtitle> actual = target.SearchSubtitles(query);

            Assert.IsTrue(actual.Count == 0);
        }

        /// <summary>
        ///A test for SearchSubtitles
        ///</summary>
        [TestMethod()]
        public void ImdbSearchQuerySearchSubtitlesTest()
        {
            OpenSubtitlesDownloader target = new OpenSubtitlesDownloader(configuration);

            ImdbSearchQuery query = new ImdbSearchQuery("0375679");
            query.LanguageCodes = new string[] { "dut" };
            
            List<Subtitle> actual = target.SearchSubtitles(query);
            
            Assert.IsTrue(actual.Count > 0);
        }
    }
}
