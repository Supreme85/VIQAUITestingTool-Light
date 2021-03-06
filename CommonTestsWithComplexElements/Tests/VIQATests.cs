﻿using System.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using VIQA.HAttributes;
using VIQA.HtmlElements;
using VIQA.HtmlElements.BaseClasses;
using VIQA.HtmlElements.Interfaces;
using VIQA.SiteClasses;
using CommonTestsWithComplexElements.Site;

namespace CommonTestsWithComplexElements.Tests
{
    [TestFixture]
    public class VIQATests
    {
        [SetUp]
        public void Init() { }

        [TearDown]
        public void TestCleanup() { }

        [Test]
        public void VITestExampleLarge()
        {
            var site = new VISite(BrowserType.Chrome) {Domain = "http://market.yandex.ru/"};
            site.HomePage.Open();

            new TextField("Поле Поиска", By.XPath("//*[@class='search__table']//*[@id='market_search']"))
                .Input("IPhone");
            new Button("Кнопка 'Найти'", By.XPath("//*[@class='search__table']//*[contains(text(),'Найти')]//..//..//button"))
                .Click();
            new TextField("Цена От", By.XPath("//*[@class='b-gurufilters__filter-inputs']/input[contains(@id,'-0')]"))
                .Input("1000");
            new TextField("Цена До", By.XPath("//*[@class='b-gurufilters__filter-inputs']/input[contains(@id,'-1')]"))
                .Input("20000");

            new Checkbox("Wi-fi", By.XPath("//*[@class='b-gurufilters']//*[contains(text(),'Wi-Fi')]//..//input"))
                .Check();

            new ClickableElement("Сенсорный экран",
                By.XPath("//*[@class='b-gurufilters']//*[contains(text(),'Сенсорный экран')]//..//i"))
                .Click();
            new RadioButtons("Выбор Сенсорного Экрана",
                By.XPath(
                    "//*[@class='b-gurufilters']//*[contains(text(),'Сенсорный экран')]//..//..//*[text()='{0}']//..//input[@type='radio']"))
                .Select("да");

            new ClickableElement("Процессор",
                By.XPath("//*[@class='b-gurufilters']//*[contains(text(),'Процессор')]//..//i"))
                .Click();
            var a = site.WebDriver.FindElements(
                By.XPath(
                    "//*[@class='b-gurufilters']//*[contains(text(),'Процессор')]//..//..//li//span[contains(text(),'8500')]"));

            var processorsCheckBox = new CheckList("Выбор Процессора",
                By.XPath(
                    "//*[@class='b-gurufilters']//*[contains(text(),'Процессор')]//..//..//*[text()='{0} ']//..//input[@type='checkbox']"));
            processorsCheckBox.CheckGroup("Apple A4", "Apple A5", "Apple A6", "Apple A7", "MediaTek MT6572W",
                "MediaTek MT6515");
            processorsCheckBox.GetAllElementsFunc = driver => driver.FindElements(
                By.XPath("//*[@class='b-gurufilters']//*[contains(text(),'Процессор')]//..//..//li//label"))
                .ToDictionary(
                    webEl => webEl.FindElement(By.TagName("span")).Text,
                    webEl => new Checkbox(webEl.FindElement(By.CssSelector("input[type=checkbox]"))));
            processorsCheckBox.CheckGroup("Apple A5");
            new Button("Показать", "input[value='Показать']").Click();
        }

        public ISelector MyDropDown = new DropDown("MyDropDown", 
            By.XPath("rootlocator"),
            () => new SelectItem("", By.XPath("element locator")))
            {
                SelectAction = (selector, name) =>
                {
                    selector.GetWebElement().Click();
                    selector.GetVIElementByTemplate(name).Click();
                }
            };
    

    [Test]
        public void VITestExamplePageObjects()
        {
            YandexMarket.HomePage.Open();
            {
                var _ = YandexMarket.HomePage.SearchSection;

                _.SearchTextField.Input("IPhone");
                _.SearchButton.Click();
            }
            {
                var _ = YandexMarket.ProductPage.FilterSection;

                _.TextFieldFrom.Input("1000");
                _.TextFieldTo.Input("2000");
                _.WiFiCheckbox.Check();
                _.SensorScreenRadioButtons.Select("да");
                _.PlatformTypesChecklist.CheckGroup(new[] { "Android", "iOS", "BlackBerry OS", "Symbian", "Series 60" });
                _.PlatformTypesChecklist.CheckGroup("iOS");
                _.ShowResultsButton.Click();
            }

            YandexMarket.ProductPage.CheckUrl(PageCheckType.Contains);
        }

        #region Common tests data
        private static readonly YandexMarketSite YandexMarket = new YandexMarketSite { UseBrowser = BrowserType.Chrome };

        #endregion
    }
}
