﻿using HtmlAgilityPack;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace MangaUpdatesBakaUpdates
{
    [IExtensionDescription(
        Name = "MangaUpdatesBakaUpdates",
        URLFormat = "mangaupdates.com",
        RefererHeader = "http://www.mangaupdates.com/",
        RootUrl = "http://www.mangaupdates.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "English")]
    public sealed class MangaUpdatesBakaUpdates : IDatabaseExtension
    {
        private Int32 PageCount = 30;

        #region IExtesion
        private IExtensionDescriptionAttribute EDA;
        public IExtensionDescriptionAttribute ExtensionDescriptionAttribute
        { get { return EDA ?? (EDA = GetType().GetCustomAttribute<IExtensionDescriptionAttribute>(false)); } }

        private Icon extensionIcon;
        public Icon ExtensionIcon
        {
            get
            {
                if (Equals(extensionIcon, null)) extensionIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                return extensionIcon;
            }
        }

        public CookieCollection Cookies
        { get; private set; }

        public Boolean IsAuthenticated
        { get; private set; }

        public bool Authenticate(NetworkCredential credentials, CancellationToken ct, IProgress<Int32> ProgressReporter)
        {
            if (IsAuthenticated) return true;
            throw new NotImplementedException();
        }

        public void Deauthenticate()
        {
            if (!IsAuthenticated) return;
            Cookies = null;
            IsAuthenticated = false;
        }

        public List<MangaObject> GetUserFavorites()
        {
            throw new NotImplementedException();
        }

        public bool AddUserFavorites(MangaObject MangaObject)
        {
            throw new NotImplementedException();
        }

        public bool RemoveUserFavorites(MangaObject MangaObject)
        {
            throw new NotImplementedException();
        }
        #endregion

        public SearchRequestObject GetSearchRequestObject(string searchTerm)
        {
            return new SearchRequestObject()
            {
                Url = String.Format("{0}/series.html?stype=title&search={1}&perpage={2}", ExtensionDescriptionAttribute.RootUrl, Uri.EscapeUriString(searchTerm), PageCount),
                Method = SearchMethod.GET,
                Referer = ExtensionDescriptionAttribute.RefererHeader,
            };
        }

        public DatabaseObject ParseDatabaseObject(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            DatabaseObjectDocument.LoadHtml(content);

            HtmlNode NameNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//span[contains(@class,'releasestitle')]");
            HtmlNodeCollection sCatNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//div[contains(@class,'sCat')]"),
                sContentNodes = DatabaseObjectDocument.DocumentNode.SelectNodes("//div[contains(@class,'sContent')]");

            Dictionary<String, HtmlNode> ContentNodes = sCatNodes.Zip(sContentNodes, (sCategory, sContent) => new { Category = sCategory.FirstChild.InnerText, Content = sContent }).ToDictionary(item => item.Category, item => item.Content);

            HtmlNode AssociatedNamesNode = ContentNodes.FirstOrDefault(item => item.Key.Equals("Associated Names")).Value,
                CoverNode = ContentNodes.FirstOrDefault(item => item.Key.Equals("Image")).Value,
                YearNode = ContentNodes.FirstOrDefault(item => item.Key.Equals("Year")).Value;

            List<String> AssociatedNames = (from HtmlNode TextNode in AssociatedNamesNode.ChildNodes where TextNode.Name.Equals("#text") && !TextNode.InnerText.Trim().Equals(String.Empty) && !TextNode.InnerText.Trim().Equals("N/A") select HtmlEntity.DeEntitize(TextNode.InnerText.Trim())).ToList<String>();
            
            List<LocationObject> Covers = new List<LocationObject>();
            if (CoverNode != null && CoverNode.SelectSingleNode(".//img") != null)
                Covers.Add(new LocationObject() {
                    Url = CoverNode.SelectSingleNode(".//img").Attributes["src"].Value,
                    ExtensionName = ExtensionDescriptionAttribute.Name,
                    ExtensionLanguage = ExtensionDescriptionAttribute.Language
                });

            Match DatabaseObjectIdMatch = Regex.Match(content, @"id=(?<DatabaseObjectId>\d+)&");
            Int32 DatabaseObjectId = Int32.Parse(DatabaseObjectIdMatch.Groups["DatabaseObjectId"].Value),
                ReleaseYear = 0;
            Int32.TryParse(YearNode.FirstChild.InnerText, out ReleaseYear);
            return new DatabaseObject()
            {
                Name = HtmlEntity.DeEntitize(NameNode.InnerText),
                Covers = Covers,
                AlternateNames = AssociatedNames,
                Description = HtmlEntity.DeEntitize(ContentNodes.FirstOrDefault(item => item.Key.Equals("Description")).Value.InnerText.Trim()),
                Locations = { new LocationObject() { 
                    ExtensionName = ExtensionDescriptionAttribute.Name,
                    ExtensionLanguage = ExtensionDescriptionAttribute.Language,
                    Url = String.Format("{0}/series.html?id={1}", ExtensionDescriptionAttribute.RootUrl, DatabaseObjectId) } },
                ReleaseYear = ReleaseYear
            };
        }

        public List<DatabaseObject> ParseSearch(string content)
        {
            HtmlDocument DatabaseObjectDocument = new HtmlDocument();
            if (!content.Contains("There are no series in the database"))
            {
                DatabaseObjectDocument.LoadHtml(content);
                HtmlWeb HtmlWeb = new HtmlWeb();
                HtmlNode TableSeriesNode = DatabaseObjectDocument.DocumentNode.SelectSingleNode("//table[contains(@class,'series_rows_table')]");
                return (from HtmlNode MangaNode 
                        in TableSeriesNode.SelectNodes(".//tr[not(@valign='top')]").Skip(2).Take(PageCount)
                        where MangaNode.SelectSingleNode(".//td[1]/a") != null
                        select ParseDatabaseObject(HtmlWeb.Load(MangaNode.SelectSingleNode(".//td[1]/a").Attributes["href"].Value).DocumentNode.OuterHtml)).ToList();
            }
            return new List<DatabaseObject>();
        }

        List<SearchResultObject> IExtension.ParseSearch(string Content)
        { throw new NotImplementedException("Database extensions return DatabaseObjects"); }
    }
}
