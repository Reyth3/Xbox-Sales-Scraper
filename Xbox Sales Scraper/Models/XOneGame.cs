using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Xbox_Sales_Scraper.Models
{
    public class XOneGame
    {
        public XOneGame() { }

        static float ParsePrice(string price)
        {
            if (price == null)
                return -1;
            var text = Regex.Replace(WebUtility.HtmlDecode(price), @"[^0-9\.\,]", "").Replace(",", ".");
            if (text != "" && text.Last() == '.')
                text = text.Substring(0, text.Length - 2);
            return text == "" ? 0 : float.Parse(text);
        }

        public XOneGame(HtmlNode rootNode)
        {
            Title = WebUtility.HtmlDecode(rootNode.Descendants("h3").FirstOrDefault()?.InnerText);
            StoreRelativeUrl = Regex.Replace(rootNode.Element("a")?.GetAttributeValue("href", ""), "/..-..", "");
            StoreUrl = $"https://www.microsoft.com{StoreRelativeUrl}";
            var price = rootNode.Descendants("div").Where(o => o.GetAttributeValue("class", "") == "c-price").FirstOrDefault();
            if(price != null)
            {
                var strike = price.Descendants("s").FirstOrDefault();
                if (strike == null)
                    RegularPrice = ParsePrice(price.Descendants("span").First().InnerText);
                else
                {
                    RegularPrice = ParsePrice(strike.InnerText);
                    SalePrice = ParsePrice(strike.NextSibling.NextSibling.NextSibling.NextSibling.InnerText);
                    SaleType = price.Elements("span").LastOrDefault(o => o.Elements("img").FirstOrDefault()?.GetAttributeValue("alt", null) != null)?.Element("img")?.GetAttributeValue("alt", null);
                    IsOnSale = true;
                }
            }
        }

        public string Title { get; set; }
        public string StoreRelativeUrl { get; set; }
        public string StoreUrl { get; set; }
        public float RegularPrice { get; set; }
        public bool IsOnSale { get; set; }
        public float SalePrice { get; set; }
        public float OffPercentage { get { return (RegularPrice - SalePrice) / RegularPrice; } }
        public string OffPersentageString { get { return $"{OffPercentage*100:0}%"; } }
        public string SaleType { get; set; }
        public float SalePriceAU { get; set; }
        public float SalePriceCA { get; set; }
        public float SalePriceGB { get; set; }

        public async Task<float> GetRegionalPrice(string regionCode)
        {
            var url = StoreUrl.Replace(".com/", $".com/{regionCode}/");
            using (var http = XboxScraper.Http)
            using (var res = await http.GetAsync(url))
                if (res.IsSuccessStatusCode)
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(await res.Content.ReadAsStringAsync());
                    return ParsePrice(doc.DocumentNode.Descendants("div").Where(o => o.GetAttributeValue("class", "") == "price-disclaimer ").FirstOrDefault()?.Element("span")?.InnerText);
                }
            return 0;
        }


        public override string ToString()
        {
            return Title;
        }
    }
}
