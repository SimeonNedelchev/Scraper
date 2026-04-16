using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Scraper
{
    class Program
    {
        /*  Rating normalization - ratings are currently on a 0-10 scale,
            Values <= 5 are kept as is, values > 5 are halved. */
        static double NormalizeRating(double raw) =>
            raw > 5.0 ? Math.Round(raw / 2.0, 2) : raw;

        //Output model
        record Product(
            [property: JsonPropertyName("productName")] string Name,
            [property: JsonPropertyName("price")] string Price,
            [property: JsonPropertyName("rating")] string Rating
         );

        static double ParsePrice(string price)
        {
            string cleaned = Regex.Replace(
                HtmlEntity.DeEntitize(price).Trim(), @"[^\d.]", "");

            return double.TryParse(cleaned,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double result) ? result : 0.0;
        }
        static void Main(string[] args)
        {
            string html = @"
                <div class=""item"" rating=""3"" data-pdid=""5426"">
                    <figure><a href=""https://www.100percent.co.nz/Product/WCM7000WD/Electrolux-700L-Chest-Freezer""><img
                                alt=""Electrolux 700L Chest Freezer &amp; Filter"" src=""/productimages/thumb/1/5426_5731_4009.jpg""
                                data-alternate-image=""/productimages/thumb/2/5426_5731_4010.jpg"" class=""mouseover-set""><span
                                class=""overlay top-horizontal""><span class=""sold-out""><img alt=""Sold Out""
                                        Src=""/Images/Overlay/overlay_1_2_1.png""></span></span></a></figure>
                    <div class=""item-detail"">
                        <h4><a href=""https://www.100percent.co.nz/Product/WCM7000WD/Electrolux-700L-Chest-Freezer"">Electrolux 700L Chest Freezer</a></h4>
                        <div class=""pricing"" itemprop=""offers"" itemscope=""itemscope"" itemtype=""http://schema.org/Offer"">
                            <meta itemprop=""priceCurrency"" content=""NZD"">
                            <p class=""price""><span class=""price-display formatted"" itemprop=""price""><span
                                        style=""display: none"">$2,099.00</span>$<span class=""dollars over500"">2,099</span><span
                                        class=""cents zero"">.00</span></span></p>
                        </div>
                        <p class=""style-number"">WCM7000WD</p>
                        <p class=""offer""><a href=""https://www.100percent.co.nz/Product/WCM7000WD/Electrolux-700L-Chest-Freezer""><span
                                    style=""color:#CC0000;"">WCM7000WD</span></a></p>
                        <div class=""item-asset""><!--.--></div>
                    </div>
                </div>
                <div class=""item"" rating=""3.6"" data-pdid=""5862"">
                    <figure><a href=""https://www.100percent.co.nz/Product/E203S/Electrolux-Anti-Odour-Vacuum-Bags""><img
                                alt=""Electrolux Anti-Odour Vacuum Bags"" src=""/productimages/thumb/1/5862_6182_4541.jpg""></a></figure>
                    <div class=""item-detail"">
                        <h4><a href=""https://www.100percent.co.nz/Product/E203S/Electrolux-Anti-Odour-Vacuum-Bags"">Electrolux Anti-Odour Vacuum Bags</a></h4>
                        <div class=""pricing"" itemprop=""offers"" itemscope=""itemscope"" itemtype=""http://schema.org/Offer"">
                            <meta itemprop=""priceCurrency"" content=""NZD"">
                            <p class=""price""><span class=""price-display formatted"" itemprop=""price""><span
                                        style=""display: none"">$22.99</span>$<span class=""dollars"">22</span><span
                                        class=""cents"">.99</span></span></p>
                        </div>
                        <p class=""style-number"">E203S</p>
                        <p class=""offer""><a href=""https://www.100percent.co.nz/Product/E203S/Electrolux-Anti-Odour-
                Vacuum-Bags""><span style=""color:#CC0000;"">E203S</span></a></p>
                        <div class=""item-asset""><!--.--></div>
                    </div>
                </div>
                <div class=""item"" rating=""8.4"" data-pdid=""4599"">
                    <figure><a href=""https://www.100percent.co.nz/Product/USK11ANZ/Electrolux-UltraFlex-Starter-Kit""><img
                                alt=""Electrolux UltraFlex Starter &#91; Kit &#93; "" src=""/productimages/thumb/1/4599_4843_2928.jpg""></a>
                    </figure>
                    <div class=""item-detail"">
                        <h4><a href=""https://www.100percent.co.nz/Product/USK11ANZ/Electrolux-UltraFlex-Starter-Kit"">Electrolux UltraFlex &#64; Starter Kit</a></h4>
                        <div class=""pricing"" itemprop=""offers"" itemscope=""itemscope"" itemtype=""http://schema.org/Offer"">
                            <meta itemprop=""priceCurrency"" content=""NZD"">
                            <p class=""price""><span class=""price-display formatted"" itemprop=""price""><span
                                        style=""display: none"">$44.99</span>$<span class=""dollars"">44</span><span
                                        class=""cents"">.99</span></span></p>
                        </div>
                        <p class=""style-number"">USK11ANZ</p>
                        <p class=""offer""><a href=""https://www.100percent.co.nz/Product/USK11ANZ/Electrolux-UltraFlex-Starter-Kit""><span
                                    style=""color:#CC0000;"">USK11ANZ</span></a></p>
                        <div class=""item-asset""><!--.--></div>
                    </div>
                </div>";

            List<Product> products = new List<Product>();
            
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item') and @rating]");

            //Check if any items are found
            if (items is null || items.Count == 0)
            {
                Console.Error.WriteLine("No products found.");
                return;
            }

            foreach (var item in items)
            {
                //Name - img alt containst the full name
                var imgNode = item.SelectSingleNode(".//figure//img");
                string name = HtmlEntity.DeEntitize(
                    imgNode?.GetAttributeValue("alt", "") ?? "").Trim();
 
                //Price - .price-display holds the full price string
                var hiddenPrice = item.SelectSingleNode(
                    ".//span[contains(@class,'price-display')]//span[@style and contains(@style,'display: none')]");

                string price = ParsePrice(hiddenPrice?.InnerText ?? "0").ToString("F2");

                //Rating
                string ratingRaw = item.GetAttributeValue("rating", "0").Trim();
                double.TryParse(ratingRaw,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double rawRating);
                string rating = NormalizeRating(rawRating).ToString();

                products.Add(new Product(name, price, rating));
            }

            //Output
            var options = new JsonSerializerOptions { WriteIndented = true,
                                                        Encoder = System.Text
                                                        .Encodings.Web
                                                        .JavaScriptEncoder
                                                        .UnsafeRelaxedJsonEscaping}; //prevents &amp from being escaped

            Console.WriteLine(JsonSerializer.Serialize(products, options));
        }
    }
}
