using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xbox_Sales_Scraper.Models;

namespace Xbox_Sales_Scraper
{
    class Program
    {
        public static void Log(string tag, string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"({DateTime.Now.ToShortTimeString()}) [{tag}] {message}");
        }

        static void Main(string[] args) => Start().Wait();

        static string tXS = "Xbox Scraper";
        static bool ToClipboard;

        static async Task Start()
        {
            var start = DateTime.Now;
            Log(tXS, "Initializing Xbox Scraper...");
            XboxScraper scraper = new XboxScraper();
            Log(tXS, "Scraping games listing...");
            var onSale = await scraper.GetGamesOnSale();
            Log(tXS, $"Found games on sale: {onSale.Count}");
            Log(tXS, "Scraping regional prices for every game on sale...");
            await GetAdditionalPrices(onSale);
            var result = CustomizeOutput(onSale);
            Console.Clear();
            Log(tXS, $"Generating table from {result.Count()} entries...");
            var markdown = scraper.GenerateRedditMarkdownTable(result);
            if(ToClipboard)
                Clipboard.SetText(markdown);
            else
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), $"table-{DateTime.Now.ToString(@"yyyy\-mm\-dd")}.txt");
                File.WriteAllText(path, markdown);
            }
            Log(tXS, $"Finished exporting output. The entire operation took: {(DateTime.Now - start).TotalMinutes:0.0}min");
            Console.ReadLine();
        }

        static async Task GetAdditionalPrices(IEnumerable<XOneGame> games)
        {
            var currentTasks = new List<Task>();
            var index = 0;
            foreach (var game in games)
            {
                if (index % 3 == 0 && index != 0)
                {
                    await Task.WhenAll(currentTasks);
                    currentTasks.RemoveAll(o => o.IsCompleted);
                }
                var msg = "Scraping price for region: {}...";
                currentTasks.Add(Task.Run(async () =>
                {
                    Log(game.Title, msg.Replace("{}", "en-CA"));
                    game.SalePriceCA = await game.GetRegionalPrice("en-ca");
                }));
                currentTasks.Add(Task.Run(async () =>
                {
                    Log(game.Title, msg.Replace("{}", "en-AU"));
                    game.SalePriceAU = await game.GetRegionalPrice("en-au");
                }));
                currentTasks.Add(Task.Run(async () =>
                {
                    Log(game.Title, msg.Replace("{}", "en-GB"));
                    game.SalePriceGB = await game.GetRegionalPrice("en-gb");
                }));
                index++;
            }
        }

        static IEnumerable<XOneGame> CustomizeOutput(IEnumerable<XOneGame> games)
        {
            var confirmed = false;
            Console.Clear();
            IEnumerable<XOneGame> sorted = Enumerable.Empty<XOneGame>();
            while (!confirmed)
            {
                Console.WriteLine("Results sorting order:\n\t1 - Game (A-Z) [Default];\n\t2 - % Off Descending;\n\t3 - Regular Pride Descending;\n\t4 - USD Price Descending");
                var sortRes = Console.ReadKey();
                if (sortRes.KeyChar == '2')
                    sorted = games.OrderByDescending(o => o.OffPercentage).ThenBy(o => o.Title);
                else if (sortRes.KeyChar == '3')
                    sorted = games.OrderByDescending(o => o.RegularPrice).ThenBy(o => o.Title);
                else if (sortRes.KeyChar == '4')
                    sorted = games.OrderByDescending(o => o.SalePrice).ThenBy(o => o.Title);
                else sorted = games.OrderBy(o => o.Title);
                var limit = 0;
                Console.WriteLine("Limit number of returned sales (0-1000000); 0 - no limit");
                var _limit = Console.ReadLine();
                int.TryParse(_limit, out limit);
                sorted = sorted.Take(limit == 0 ? 1000000 : limit);
                Console.WriteLine("Output type: \n\t1 - Save to 'table.txt' in the Current Directory [Default];\n\t2 - Copy to Clipboard;");
                var outputRes = Console.ReadKey();
                if (outputRes.KeyChar == '2')
                    ToClipboard = true;
                Console.WriteLine("Are you sure? (y/n): ");
                var sureRes = Console.ReadKey();
                if (sureRes.KeyChar == 'y')
                    confirmed = true;
            }
            return sorted;
        }
    }
}
