﻿using ParallelTestsExamples.Site.Pages;
using VIQA.HAttributes;
using VIQA.SiteClasses;

namespace ParallelTestsExamples.Site
{
    [Site(Domain = "http://market.yandex.ru/")]
    public class YandexMarketSite : VISite
    {
        [Page(Title = "Яндекс.Маркет", Url = "http://market.yandex.ru/", IsHomePage = true)]
        public HomePage HomePage;

        [Page(Title = "выбор по параметрам", Url = "http://market.yandex.ru/guru.xml")]
        public ProductPage ProductPage;
        
    }
}
