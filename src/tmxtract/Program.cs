using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace tmxtract
{
    public class Options
    {
        public Options()
        {
            MaxTranslations = int.MaxValue;
            dgt = true;
            GroupDocumentId = 9;
            dgtFolder = "./dgt";
        }
        [Option('m', "dgt", Default = true, Required = false, HelpText = "DGT-Translation Memory")]
        public bool dgt { get; set; }

        [Option('f', "dgtfolder", Default = "./dgt", Required = false, HelpText = "DGT-Translation Memory")]
        public string dgtFolder { get; set; }
        [Option('s', "source-language", Required = true, HelpText = "Lanugage to transalate from ex: EN-UK")]
        public string sourceLanguage { get; set; }
        [Option('d', "destination-language", Required = true, HelpText = "Lamnguage that you want to translate to ex: ES-ES")]
        public string destinationLanguage { get; set; }
        [Option('n', "max-translations", Default = 500000, Required = false, HelpText = "map of lanaguages to be replace in the resulting doc")]
        public int MaxTranslations { get; set; }
        [Option('l', "language-map", Required = false, HelpText = "map of lanaguages to be replace in the resulting doc")]
        public string LanguageMaps { get; set; }
        [Option('i', "max-doc-id-size", Required = false, HelpText = "Maximun size for the id of a document")]
        public int GroupDocumentId { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Runner runner = new Runner(new Options()
            {
                sourceLanguage = "ES-ES",
                destinationLanguage = "EN-GB",
                MaxTranslations = 500000,
                GroupDocumentId = 5,
                LanguageMaps = "ES-ES:es;EN-GB:en"
            });
            await runner.Run();
            //Use DGT-Translation Memory

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(async o =>
                {
                    Runner runner2 = new Runner(o);
                    await runner2.Run();
                });


            Console.WriteLine("Hello World!");
        }
    }
    public class Tu
    {
        public IEnumerable<Tuv> Tuvs { get; set; }
        public string Doc { get; set; }
    }

    public class Tuv
    {
        public string Lang { get; set; }
        public string Text { get; set; }
    }
    class Runner
    {
        public List<Tu> Tus { get; set; }
        static readonly string[] DGT_URI = {
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2007_Version1/Volume_1.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2007_Version1/Volume_2.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2007_Version1/Volume_3.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2007_Version1/Volume_4.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2007_Version1/Volume_5.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2007_Version1/Volume_6.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2019/Vol_2018_1.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2019/Vol_2018_2.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2019/Vol_2018_3.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2018/Vol_2017_2.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2018/Vol_2017_1.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2017/Vol_2016_9.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2017/Vol_2016_8.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2017/Vol_2016_7.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2017/Vol_2016_6.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2017/Vol_2016_5.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2017/Vol_2016_4.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2017/Vol_2016_3.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2017/Vol_2016_2.zip",
          "https://wt-public.emm4u.eu/Resources/DGT-TM-2017/Vol_2016_1.zip",
        };
        private readonly Options options;
        private readonly HttpClient httpClient;
        private readonly Dictionary<string, string> LanguageMap;

        public Runner(Options options)
        {
            this.options = options;
            this.httpClient = new HttpClient();
            Tus = new List<Tu>();
            LanguageMap = new Dictionary<string, string>();
            if (options != null)
            {
                if (!string.IsNullOrWhiteSpace(options.LanguageMaps))
                {
                    foreach (var mapOption in options.LanguageMaps.Split(';'))
                    {
                        var map = mapOption.Split(':');
                        if (map.Length == 2)
                            LanguageMap.Add(map[0], map[1]);
                    }
                }
            }
        }

        public async Task Run()
        {
            if (options.dgt)
            {
                await CheckDgtFiles(options.dgtFolder);
                var files = Directory.EnumerateFiles(options.dgtFolder, "*.zip", new EnumerationOptions() { RecurseSubdirectories = true }).Reverse();
                await ExtractTmx(files, options.sourceLanguage, options.destinationLanguage);
                Console.WriteLine(Tus.Count);
            }
            List<string> fileNames = new List<string>();
            var destinationDir = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), "tmxtract" + DateTime.Now.ToString("yyyyMMDDHHmmss")));

            foreach (var tusGroup in Tus.GroupBy(x => x.Doc.Length > options.GroupDocumentId ? x.Doc.Substring(0, options.GroupDocumentId) : x.Doc))
            {
                XElement root = new XElement("tmx");
                root.SetAttributeValue("version", "version 1.4");
                //<header o-tmf="Euramis" creationtool="tm3" creationtoolversion="Retrieval v8.71 from 11-03-2019 10:05" segtype="sentence" datatype="PlainText" adminlang="EN-US" srclang="EN-GB">
                XElement header = new XElement("header");
                header.SetAttributeValue("creationtoolversion", "tmxtreact-net v0.0.1-alpha");
                header.SetAttributeValue("datatype", "PlainText");
                header.SetAttributeValue("adminlang", "EN-US");
                header.SetAttributeValue("segtype", "sentence");
                header.SetAttributeValue("srclang",
                    LanguageMap.ContainsKey(options.sourceLanguage) ? LanguageMap[options.sourceLanguage] : options.sourceLanguage);
                header.SetAttributeValue("creationtool", "tmxtreact-net");
                XElement body = new XElement("body");
                body.Add(tusGroup.Select(tu =>
                {
                    var tuElement = new XElement("tu");
                    var prop = new XElement("prop", tu.Doc);
                    prop.SetAttributeValue("type", "Txt::Doc. No.");
                    tuElement.Add(prop);
                    tuElement.Add(tu.Tuvs.Select(tuv =>
                    {
                        var tuvElement = new XElement("tuv");
                        tuvElement.SetAttributeValue("lang", LanguageMap.ContainsKey(tuv.Lang) ? LanguageMap[tuv.Lang] : tuv.Lang);
                        tuvElement.Add(new XElement("seg", tuv.Text));
                        return tuvElement;
                    }));
                    return tuElement;
                }));
                root.Add(header);
                root.Add(body);
                using (XmlWriter xml = XmlWriter.Create(Path.Join(destinationDir.FullName, tusGroup.Key + "tmx.tmx")))
                {
                    root.WriteTo(xml);
                }
            }
            string filename = Path.GetTempFileName();
            ZipFile.CreateFromDirectory(destinationDir.FullName, Path.Join(Path.GetTempPath(), "tmxtract" + DateTime.Now.ToString("yyyyMMDDHHmmss") + ".zip"));
        }

        private async Task ExtractTmx(IEnumerable<string> files, string sourceLang, string targetLang)
        {
            foreach (var file in files)
            {
                using (ZipArchive archive = ZipFile.OpenRead(file))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase))
                        {
                            using (Stream fileStream = entry.Open())
                            {
                                try
                                {
                                    XElement element = await XElement.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);
                                    var tus = element.Elements("body").Elements("tu");
                                    bool finishedExtraction = ExtractTranslation(sourceLang, targetLang, tus);
                                    if (finishedExtraction)
                                        return;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }

                            }
                        }
                    }
                }
            }
        }

        private bool ExtractTranslation(string sourceLang, string targetLang, IEnumerable<XElement> tus)
        {
            foreach (var tu in tus)
            {
                if (Tus.Count >= options.MaxTranslations)
                    return true;

                var tuElement = new Tu();
                if (tu.Descendants("tuv").Any(tuv => tuv.Attribute("lang").Value == sourceLang)
                    && tu.Descendants("tuv").Any(tuv => tuv.Attribute("lang").Value == targetLang))
                {
                    //< prop type = "Txt::Doc. No." > 22004A0520(01)R(01) </ prop >
                    var doc = tu.Descendants("prop").FirstOrDefault(p => p.Attribute("type").Value == "Txt::Doc. No.");
                    if (doc != null)
                    {
                        tuElement.Doc = doc.Value;
                    }
                    //<tuv lang = "EN-GB">
                    //<seg> Procès - verbal of rectification to the Agreement </seg>
                    //</tuv>

                    tuElement.Tuvs = tu
                        .Descendants("tuv")
                        .Where(tuv => tuv.Attribute("lang").Value == sourceLang || tuv.Attribute("lang").Value == targetLang)
                        .Select(tuv => new Tuv()
                        {
                            Lang = tuv.Attribute("lang").Value,
                            Text = tuv.Descendants("seg").Single().Value
                        });
                    Tus.Add(tuElement);
                }
            }
            return false;
        }

        private async Task CheckDgtFiles(string dgtFolder)
        {

            foreach (var url in DGT_URI)
            {
                var directory = Directory.CreateDirectory(dgtFolder);
                var uri = new Uri(url);
                int i = 0;
                for (; i < uri.Segments.Length - 1; i++)
                {
                    directory = Directory.CreateDirectory(Path.Join(directory.FullName, uri.Segments[i]));
                }
                string filename = uri.Segments[i];
                string filepath = Path.Join(directory.FullName, filename);
                if (!File.Exists(filepath))
                {
                    try
                    {
                        Stream urlStream = await httpClient.GetStreamAsync(uri.AbsoluteUri);
                        using (Stream fileStream = new FileStream(filepath, FileMode.Create))
                        {
                            await urlStream.CopyToAsync(fileStream);
                        }
                        await urlStream.FlushAsync();
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err);
                    }

                }

            }

        }

    }


}
