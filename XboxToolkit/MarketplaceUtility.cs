using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Xml;
using XboxToolkit.Models;

namespace XboxToolkit
{
    public static class MarketplaceUtility
    {
        private static bool TryDownloadMarketplaceImage(string imageUrl, out byte[] imageData)
        {
            imageData = Array.Empty<byte>();

            var retries = 10;
            while (retries > 0)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var response = client.GetAsync(imageUrl).GetAwaiter().GetResult())
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                var responseContent = response.Content;
                                imageData = responseContent.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                            }
                        }
                    }
                    return true;
                }
                catch
                {
                    Thread.Sleep(500);
                }
                retries--;
            }
            return false;
        }

        public static string GetMarketPlaceUrl(uint titleId)
        {
            const string locale = "en-US";

            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>( "Locale", locale ),
                new KeyValuePair<string, string>("LegalLocale", locale ),
                new KeyValuePair<string, string>("Store", "1" ),
                new KeyValuePair<string, string>( "PageSize", "100" ),
                new KeyValuePair<string, string>( "PageNum", "1" ),
                new KeyValuePair<string, string>("DetailView", "3" ), // 5
                new KeyValuePair<string, string>("OfferFilterLevel", "1" ),
                new KeyValuePair<string, string>("MediaIds", "66acd000-77fe-1000-9115-d802" + titleId.ToString("X8") ),
                new KeyValuePair<string, string>("UserTypes", "2" ),
                new KeyValuePair<string, string>("MediaTypes", "1" ), // Xbox360
                new KeyValuePair<string, string>("MediaTypes", "21" ),
                new KeyValuePair<string, string>("MediaTypes", "23" ), // XBLA
                new KeyValuePair<string, string>("MediaTypes", "37" ), // Community
                new KeyValuePair<string, string>("MediaTypes", "46" ),
            };

            var url = "http://catalog.xboxlive.com/Catalog/Catalog.asmx/Query?methodName=FindGames";
            foreach (var parameter in parameters)
            {
                url = $"{url}&Names={parameter.Key}&Values={parameter.Value}";
            }

            return url;
        }

        private static bool TryGetMarketPlaceXml(string marketPlaceURL, out XmlDocument marketPlaceXml)
        {
            if (string.IsNullOrEmpty(marketPlaceURL))
            {
                throw new ArgumentException("Argument cannot be Null or Empty", "marketPlaceURL");
            }

            marketPlaceXml = new XmlDocument();

            var retries = 10;
            while (retries > 0)
            {
                try
                {
                    marketPlaceXml.Load(marketPlaceURL);
                    return true;
                }
                catch
                {
                    Thread.Sleep(500);
                }
                retries--;
            }
            return false;
        }

        public static bool TryProcessMarketplaceTitle(uint titleId, bool downloadImages, out MarketplaceMetaData marketplaceMetaData)
        {
            marketplaceMetaData = new MarketplaceMetaData();

            try
            {
                var marketPlaceUrl = GetMarketPlaceUrl(titleId);
                if (TryGetMarketPlaceXml(marketPlaceUrl, out var marketPlaceXML) == false)
                {
                    return false;
                }

                var root = marketPlaceXML.DocumentElement;
                if (root == null)
                {
                    return false;
                }

                var xmlnsm = new XmlNamespaceManager(marketPlaceXML.NameTable);
                xmlnsm.AddNamespace("default", "http://www.w3.org/2005/Atom");
                xmlnsm.AddNamespace("live", "http://www.live.com/marketplace");

                if (root.SelectSingleNode("live:totalItems/text()", xmlnsm)?.Value == "0")
                {
                    return false;
                }

                marketplaceMetaData.Developer = root.SelectSingleNode("default:entry/live:media/live:developer/text()", xmlnsm)?.Value ?? string.Empty;
                marketplaceMetaData.Title = root.SelectSingleNode("default:entry/default:title/text()", xmlnsm)?.Value ?? string.Empty;
                marketplaceMetaData.Publisher = root.SelectSingleNode("default:entry/live:media/live:publisher/text()", xmlnsm)?.Value ?? string.Empty;
                marketplaceMetaData.Description = root.SelectSingleNode("default:entry/live:media/live:reducedDescription/text()", xmlnsm)?.Value ?? string.Empty;

                var xmlNodeList = root.SelectNodes("default:entry/live:categories/live:category", xmlnsm);
                if (xmlNodeList == null)
                {
                    return false;
                }

                var genreMap = new Dictionary<uint, Genre>
                {
                    { 0, new Genre(0, "Unknown") },
                    { 3001, new Genre(3001, "Other") },
                    { 3002, new Genre(3002, "Action & Adventure") },
                    { 3005, new Genre(3005, "Family") },
                    { 3006, new Genre(3006, "Fighting") },
                    { 3007, new Genre(3007, "Music") },
                    { 3008, new Genre(3008, "Platformer") },
                    { 3009, new Genre(3009, "Racing & Flying") },
                    { 3010, new Genre(3010, "Role Playing") },
                    { 3011, new Genre(3011, "Shooter") },
                    { 3012, new Genre(3012, "Strategy & Simulation") },
                    { 3013, new Genre(3013, "Sports & Recreation") },
                    { 3018, new Genre(3018, "Board & Card") },
                    { 3019, new Genre(3019, "Classics") },
                    { 3022, new Genre(3022, "Puzzle & Trivia") }
                };

                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    var node = xmlNode.SelectSingleNode("live:categoryId/text()", xmlnsm);
                    if (node == null)
                    {
                        continue;
                    }
                    if (uint.TryParse(node.Value, out var categoryId) && categoryId > 3000)
                    {
                        if (genreMap.ContainsKey(categoryId) == false)
                        {
                            continue;
                        }
                        marketplaceMetaData.Genre = (GenreType)categoryId;
                        break;
                    }
                }

                if (downloadImages == true)
                {
                    var titleNode = root.SelectSingleNode("default:entry/live:images/live:image[live:imageMediaType=14 and live:size=14 and (live:relationshipType=23 or live:relationshipType=15)]/live:fileUrl/text()", xmlnsm);
                    if (titleNode?.Value != null)
                    {
                        if (TryDownloadMarketplaceImage(titleNode.Value, out var titleImage))
                        {
                            marketplaceMetaData.TitleImage = titleImage;
                        }
                    }

                    var backgroundNode = root.SelectSingleNode("default:entry/live:images/live:image[live:imageMediaType=14 and live:size=22 and live:relationshipType=25]/live:fileUrl/text()", xmlnsm);
                    if (backgroundNode?.Value != null)
                    {
                        if (TryDownloadMarketplaceImage(backgroundNode.Value, out var backgroundImage))
                        {
                            marketplaceMetaData.BackgroundImage = backgroundImage;
                        }
                    }

                    var bannerNode = root.SelectSingleNode("default:entry/live:images/live:image[live:imageMediaType=14 and live:size=15 and live:relationshipType=27]/live:fileUrl/text()", xmlnsm);
                    if (bannerNode?.Value != null)
                    {
                        if (TryDownloadMarketplaceImage(bannerNode.Value, out var bannerImage))
                        {
                            marketplaceMetaData.BannerImage = bannerImage;
                        }
                    }

                    var boxartNode = root.SelectSingleNode("default:entry/live:images/live:image[live:imageMediaType=14 and live:size=23 and live:relationshipType=33]/live:fileUrl/text()", xmlnsm);
                    if (boxartNode?.Value != null)
                    {
                        if (TryDownloadMarketplaceImage(boxartNode.Value, out var boxArtImage))
                        {
                            marketplaceMetaData.BoxArtImage = boxArtImage;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
