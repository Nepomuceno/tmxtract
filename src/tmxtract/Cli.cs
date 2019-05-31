using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace tmxtract
{
    [HelpOption("-?|-h|--help")]
    public class Cli
    {
        public Cli()
        {
            MaxTranslations = 100000;
            dgt = true;
            GroupDocumentId = 9;
            dgtFolder = "./dgt";
        }
        public bool dgt { get; set; }

        [Option("--download-folder", Description = "Where to download the DGT Files")]
        public string dgtFolder { get; set; }

        [Required]
        [Option("-s|--source-language", Description = "Language to translate from ex: EN-GB")]
        public string sourceLanguage { get; set; }

        [Required]
        [Option("-d|--destination-language", Description = "Language that you want to translate to ex: ES-ES")]
        public string destinationLanguage { get; set; }

        [Option("-f|--output-file", Description = "Output file")]
        public string outputFile { get; set; }

        [Option("-n|--max-translations", Description = "Number of translations to generate default:100000")]
        public int MaxTranslations { get; set; }

        [Option("-l|--language-map", Description = "Map of languages to be replaced in the resulting doc can be used multiple times Ex: -l ES-ES:es -l EN-UK:en")]
        public IEnumerable<string> LanguageMaps { get; set; }

        internal readonly Dictionary<string, string> LanguageMap = new Dictionary<string, string>();

        [Option("-i|--max-doc-id-size",Description = "Maximum size for the id of a document")]
        public int GroupDocumentId { get; set; }

        private async Task OnExecute()
        {
            if (LanguageMaps != null)
            {
                foreach (var mapOption in LanguageMaps)
                {
                    var map = mapOption.Split(':');
                    if (map.Length == 2)
                        LanguageMap.Add(map[0], map[1]);
                }
            }

            if (string.IsNullOrWhiteSpace(outputFile))
                outputFile = Path.GetFullPath($"./tmxtract-{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip");

            var extractor = new Extractor(this);
            await extractor.Run();
        }
    }
}
