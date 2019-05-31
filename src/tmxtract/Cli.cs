using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
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
        public string dgtFolder { get; set; }

        [Option("-s|--source-language", Description = "Lanugage to transalate from ex: EN-UK")]
        public string sourceLanguage { get; set; }

        [Option("-d|--destination-language", Description = "Language that you want to translate to ex: ES-ES")]
        public string destinationLanguage { get; set; }

        [Option("-f|--output-file", Description = "output-file")]
        public string outputFile { get; set; }

        [Option("-n|--max-translations", Description = "map of lanaguages to be replace in the resulting doc")]
        public int MaxTranslations { get; set; }

        [Option("-l|--language-map", Description = "map of lanaguages to be replace in the resulting doc can be used multople times Ex: -l ES-ES:es -l EN-UK:en")]
        public IEnumerable<string> LanguageMaps { get; set; }

        internal readonly Dictionary<string, string> LanguageMap = new Dictionary<string, string>();

        [Option("-i|--max-doc-id-size",Description = "Maximun size for the id of a document")]
        public int GroupDocumentId { get; set; }

        private async Task OnExecute()
        {
            foreach (var mapOption in LanguageMaps)
            {
                var map = mapOption.Split(':');
                if (map.Length == 2)
                    LanguageMap.Add(map[0], map[1]);
            }

            if (string.IsNullOrWhiteSpace(outputFile))
                outputFile = Path.GetFullPath($"./tmxtract-{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip");
            var extractor = new Extractor(this);
            await extractor.Run();
        }
    }
}
