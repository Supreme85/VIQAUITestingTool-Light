﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.PageObjects;
using VIQA.Common;
using VIQA.HAttributes;
using VIQA.HtmlElements.Interfaces;
using VIQA.SiteClasses;

namespace VIQA.HtmlElements
{
    public class VIElementsSet : Named
    {
        protected By _locator;
        public By Context;
        private VISite _site;
        protected bool IsSiteSet { get { return _site != null; } }
        public VISite Site { get { return _site = _site ?? (DefaultSite ?? new VISite()); } set { _site = value; } }
        public static VISite DefaultSite { set; get; }
        
        private void SetViElement(FieldInfo viElement)
        {
            var instance = (VIElement) viElement.GetValue(this) ??
                (VIElement)Activator.CreateInstance(GetFieldType(viElement.FieldType));
            instance.Site = Site;
            var name = NameAttribute.GetName(viElement);
            instance.Name = (!string.IsNullOrEmpty(name))
                ? name
                : viElement.Name;
            //var frameName = FrameAttribute.GetFrameName(viElement);
            //if (!string.IsNullOrEmpty(frameName))
            //    instance.FrameName = frameName;
            var locator = LocatorAttribute.GetLocator(viElement) 
                ?? LocatorAttribute.GetLocatorFomFindsBy(viElement);
            if (locator != null)
                instance.Locator = locator;
            if (_locator != null)
                instance.Context = (Context != null)
                    ? new ByChained(Context, _locator)
                    : _locator;
            else if (Context != null)
                instance.Context = Context;
            var textElement = instance as IHaveValue;
            if (textElement != null)
            {
                var fillFromName = FillFromFieldAttribute.GetFieldName(viElement);
                if (!string.IsNullOrEmpty(fillFromName))
                    textElement.FillRule = data => data.GetFieldByName(fillFromName);
            }
            var clickableElement = instance as IClickable;
            if (clickableElement != null)
            {
                var clickLoadsPage = ClickLoadsPageAttribute.Handler(viElement);
                clickableElement.ClickLoadsPage = clickLoadsPage;
            }
            viElement.SetValue(this, instance);
            instance.InitSubElements();
        }

        private static Type GetFieldType(Type fieldType)
        {
            if (!fieldType.IsInterface) return fieldType;
            var listOfTypes = VIElement.InterfaceTypeMap.Where(el => fieldType == el.Key).ToList();
            if (listOfTypes.Count() == 1)
                return listOfTypes.First().Value;
            VISite.Alerting.ThrowError("Unknown interface: " + fieldType + "Add relation interface -> class in VIElement.InterfaceTypeMap");
            return fieldType;
        }

        private List<FieldInfo> GetElements() { return GetFields<IVIElement>(); }

        private List<FieldInfo> GetFields<T>()
        {
            return
                GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(_ => typeof (T).IsAssignableFrom(_.FieldType)).ToList();
        }
        public List<T> GetElements<T>()
        {
            return
                GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(_ => typeof(T).IsAssignableFrom(_.FieldType)).Select(_ => (T)_.GetValue(this)).ToList();
        }
        
        public Func<Object, Object> FillRule { set; get; }

        protected static Func<Object, Object> ToFillRule<T>(Func<T, Object> typeFillRule)
        {
            return o => new Func<T, object>(typeFillRule)((T)o);
        }

        private Dictionary<string, IHaveValue> _getValueElements;
        private Dictionary<string, IHaveValue> GethValueElements
        {
            get { return _getValueElements ?? (_getValueElements = 
                GetElements<IHaveValue>().ToDictionary(_ => _.Name, _ => _)); }
        }

        public void FillSubElements(Dictionary<IHaveValue, Object> values)
        {
            FillSubElements(values.ToDictionary(_ => _.Key.Name, _ => _.Value));
        }

        private string ObjToString(Object obj)
        {
            var objects = obj as IEnumerable<object>;
            return objects != null 
                ? string.Join(", ", objects.Select(el => el.ToString())) 
                : obj.ToString();
        }

        public void FillSubElements(Dictionary<string, Object> values)
        {
            var valuesAsString = values.ToDictionary(_ => _.Key, _ => ObjToString(_.Value)).Print();
            VISite.Logger.Event("Fill elements: '" + Name + "'".LineBreak() + "With data: " + valuesAsString);
            if (values.Keys.All(GethValueElements.ContainsKey))
                try
                { values.Where(_ => _.Value != null).ForEach(pair => GethValueElements[pair.Key].SetValue(pair.Value)); }
                catch (Exception ex) { VISite.Alerting.ThrowError("Error in FillSubElements. Exception: " + ex); }
            else
                throw VISite.Alerting.ThrowError("Unknown Keys for Data form.".LineBreak() +
                    "Possible:" + GethValueElements.Keys.Print().LineBreak() +
                    "Requested:" + values.Keys.Print());
        }
        
        public void FillSubElements(Object data)
        {
            VISite.Logger.Event("Fill elements: '" + Name + "'".LineBreak() + "With data: " + data);
            GethValueElements.Select(_ => _.Value).Where(_ => _.FillRule != null)
                .ForEach(element =>
                {
                    try { element.SetValue(element.FillRule(data)); }
                    catch (Exception ex) { VISite.Alerting.ThrowError("Error in FillSubElements. Element '" + element.Name + "'. Data '" + data + "' Exception: " + ex); }
                });
        }

        public bool CompareValuesWith(Object data, Func<string, string, bool> compareFunc = null)
        {
            VISite.Logger.Event("Check Form values: '" + Name + "'".LineBreak() + "With data: " + data);
            var result = true;
            compareFunc = compareFunc ?? VIElement.DefaultCompareFunc;
            var elements = GethValueElements.Select(_ => _.Value).Where(_ => _.FillRule != null);
            foreach (var element in elements) {
                try
                {
                    var expected = element.FillRule(data);
                    var expectedEnum = expected as IEnumerable<Object>;
                    if (expectedEnum == null)
                    {
                        if (compareFunc(element.Value, expected.ToString()))
                            continue;
                    }
                    else
                    {
                        var expecctedList = expectedEnum.ToList();
                        if (expecctedList.Count(el => element.Value.Contains(el.ToString())) == expecctedList.Count())
                        continue;
                    }
                    result = false;
                    break;
                }
                catch (Exception ex) { VISite.Alerting.ThrowError("Error in CompareValuesWith. Element '" + element.Name + "'. Exception: " + ex); }
            }
            return result;
        }

        public void FillSubElement(string name, string value)
        {
            GethValueElements[name].SetValue(value);
        }

        protected void InitSubElements()
        {
            GetElements().ForEach(SetViElement);
        }
    }
}
