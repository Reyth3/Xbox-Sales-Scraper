using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xbox_Sales_Scraper.Models;

namespace Xbox_Sales_Scraper
{
    public class XboxScraper
    {
        public static HttpClient Http
        {
            get
            {
                HttpClientHandler handler = new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate };
                HttpClient http = new HttpClient(handler);
                http.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36");
                http.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("gzip, deflate");
                return http;
            }
        }

        public async Task<List<XOneGame>> GetGamesOnSale()
        {
            var games = new List<XOneGame>();
            using (var http = Http)
            {
                var urlPattern = "https://www.microsoft.com/pl-pl/store/top-paid/games/xbox?rtc=1&s=store&skipitems={}";
                for (int i = 0; i < 20; i++)
                {
                    using (var res = await http.GetAsync(urlPattern.Replace("{}", (i * 90).ToString())))
                        if(res.IsSuccessStatusCode)
                        {
                            var doc = new HtmlDocument();
                            doc.LoadHtml(await res.Content.ReadAsStringAsync());
                            var sections = doc.DocumentNode.Descendants("section").Where(o => o.GetAttributeValue("class", "").Contains("m-product-placement-item"));
                            var _games = sections.Select(o => new XOneGame(o));
                            games.AddRange(_games);
                        }
                }
            }
            var onSale = games.Where(o => o.IsOnSale).ToList();
            return onSale;
        }

        public string GenerateRedditMarkdownTable(IEnumerable<XOneGame> games)
        {
            StringBuilder table = new StringBuilder();
            table.AppendLine("Game | Original Price | % Off | USD | CAD | AUD | GB");
            table.AppendLine("---|---|---|---|---|---|---|---");
            foreach(var game in games)
                table.AppendLine($"[{game.Title}]({game.StoreUrl}) | ${game.RegularPrice:0.00} | {game.OffPersentageString} | ${game.SalePrice:0.00} | ${game.SalePriceCA:0.00} | ${game.SalePriceAU:0.00} | £{game.SalePriceGB:0.00}");
            return table.ToString();
        }
    }
}
