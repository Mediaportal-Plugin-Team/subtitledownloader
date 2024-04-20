using System;
using System.Diagnostics;
using System.Linq;

namespace SubtitleDownloader.Core
{
    [DebuggerDisplay("ProgramName = {ProgramName}, FileName = {FileName}")]
    public class Subtitle
    {
        /// <summary>
        /// Identifier of the subtitle in source system
        /// </summary>
        public string Id { get; private set; }
        
        /// <summary>
        /// Name of the program
        /// </summary>
        public string ProgramName { get; private set; }
        
        /// <summary>
        /// Original subtitle filename from source system
        /// or generated if original filename is not available.
        /// 
        /// Should be used on display purposes only, because generated
        /// file name may not contain the actual file suffix
        /// </summary>
        public string FileName { get; private set; }
        
        /// <summary>
        /// Language code of this subtitle (ISO 639-2 Code)
        /// </summary>
        public string LanguageCode { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Mandatory</param>
        /// <param name="programName"></param>
        /// <param name="fileName">Mandatory</param>
        /// <param name="languageCode">Mandatory</param>
        public Subtitle(string id, string programName, string fileName, string languageCode)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("ID cannot be null or empty!");
            }
            
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty!");
            }

            if (string.IsNullOrEmpty(languageCode))
            {
                throw new ArgumentException("Language code cannot be null or empty!");
            }

            if (languageCode != null && languageCode.Count() != 3)
            {
                throw new ArgumentException("Language code must be ISO 639-2 Code!");
            }
            if (!Languages.IsSupportedLanguageCode(languageCode))
            {
                throw new ArgumentException("Language code '" + languageCode + "' is not supported!");
            }

            Id = id;
            ProgramName = programName;
            FileName = fileName;
            LanguageCode = languageCode;
        }
    }
}