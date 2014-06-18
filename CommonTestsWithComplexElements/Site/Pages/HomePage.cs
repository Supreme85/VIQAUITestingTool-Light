﻿using CommonTestsWithComplexElements.Site.Sections;
using VIQA.HAttributes;
using VIQA.SiteClasses;

namespace CommonTestsWithComplexElements.Site.Pages
{
    [Page(Title = "Яндекс.Маркет", Url = "http://market.yandex.ru/")]
    public class HomePage : VIPage
    {
        [Name("Search section")]
        [Locate(ByClassName = "search__table")]
        public SearchSection SearchSection;

    }
}
