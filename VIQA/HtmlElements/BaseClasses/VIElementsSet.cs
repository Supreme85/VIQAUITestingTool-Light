﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium;
using VIQA.Common;
using VIQA.Common.Pairs;
using VIQA.HAttributes;
using VIQA.HtmlElements.BaseClasses;
using VIQA.HtmlElements.Interfaces;
using VIQA.SiteClasses;

namespace VIQA.HtmlElements
{
    public class VIElementsSet : Named
    {
        protected By _locator;
        public Pairs<ContextType, By> Context = new Pairs<ContextType, By>();
        private VISite _site;
        protected bool IsSiteSet { get { return _site != null; } }
        public VISite Site { get { return _site = _site ?? (DefaultSite ?? new VISite()); } set { _site = value; } }
        public static VISite DefaultSite { set; get; }

        public bool HaveLocator() { return _locator != null; }

        private void SetViElement(FieldInfo viElement)
        {
            var instance = CreateInstance(viElement);
            SetName(viElement, ref instance);
            CreateContext(viElement, ref instance);
            SetHaveValueData(viElement, ref instance);
            SetClickableElementData(viElement, ref instance);
            viElement.SetValue(this, instance);
            instance.InitSubElements();
        }

        private VIElement CreateInstance(FieldInfo viElement)
        {
            var instance = (VIElement)viElement.GetValue(this) ??
                (VIElement)Activator.CreateInstance(GetFieldType(viElement.FieldType));
            instance.Site = Site;
            return instance;
        }

        private void SetName(FieldInfo viElement, ref VIElement instance)
        {
            var nameAttr = NameAttribute.GetName(viElement);
            instance.Name = (!string.IsNullOrEmpty(nameAttr))
                ? nameAttr
                : viElement.Name;
        }

        private void CreateContext(FieldInfo viElement, ref VIElement instance)
        {
            instance.Context.Add(Context);

            var frameAttr = FrameAttribute.GetFrame(viElement);
            if (frameAttr != null)
                instance.Context.Add(ContextType.Frame, frameAttr);

            var locatorAttr = LocatorAttribute.GetLocator(viElement)
                ?? LocatorAttribute.GetLocatorFomFindsBy(viElement);
            if (locatorAttr != null)
                instance.Locator = locatorAttr;
            if (_locator != null)
                instance.Context.Add((this is Frame) ? ContextType.Frame : ContextType.Locator, _locator);
        }

        private void SetHaveValueData(FieldInfo viElement, ref VIElement instance)
        {
            var haveValueElement = instance as IHaveValue;
            if (haveValueElement == null) return;
            var fillFromNameAttr = FillFromFieldAttribute.GetFieldName(viElement);
            if (!string.IsNullOrEmpty(fillFromNameAttr))
                haveValueElement.FillRule = data => data.GetFieldByName(fillFromNameAttr);
        }

        private void SetClickableElementData(FieldInfo viElement, ref VIElement instance)
        {
            var clickableElement = instance as IClickable;
            if (clickableElement == null) return;
            var clickLoadsPageAttr = ClickLoadsPageAttribute.Handler(viElement);
            clickableElement.ClickLoadsPage = clickLoadsPageAttr;
        }
        #region Public
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

        public bool CompareValuesWith(Object data, out string resultText, Func<string, string, bool> compareFunc = null)
        {
            VISite.Logger.Event("Check Form values: '" + Name + "'".LineBreak() + "With data: " + data);
            var result = true;
            resultText = "";
            compareFunc = compareFunc ?? VIElement.DefaultCompareFunc;
            var elements = GethValueElements.Select(_ => _.Value).Where(_ => _.FillRule != null);
            foreach (var element in elements) {
                try
                {
                    var expected = element.FillRule(data);
                    var expectedEnum = expected as IEnumerable<Object>;
                    if (expectedEnum == null)
                    {
                        if (!compareFunc(element.Value, expected.ToString()))
                        {
                            GetResult(string.Format("Error in CompareValuesWith for element '{0}'. Actual: {1}; Expected: {2}",
                                    element.Name, element.Value, expected), out result, out resultText);
                            break;
                        }
                    }
                    else
                    {
                        var expecctedList = expectedEnum.Select(el => el.ToString()).ToArray();
                        var actualList = element.Value.Split(',').Select(el => el.Trim()).ToArray();
                        if (actualList.Count() != expecctedList.Count())
                        {
                            GetResult(string.Format("Error in CompareValuesWith for element '{0}'. Different Count of Elements: {1}(Actual), {2}(Expected); Actual List: {3}; Expected List: {4}",
                                element.Name, actualList.Count(), expecctedList.Count(), element.Value, expecctedList.Print()), out result, out resultText);
                            break;
                            
                        }
                        Array.Sort(expecctedList);
                        Array.Sort(actualList);
                        if (actualList.Print() != expecctedList.Print())
                        {
                            GetResult(string.Format("Error in CompareValuesWith for element '{0}'. Actual: {1}; Expected: {2}", element.Name, element.Value, expecctedList.Print()), 
                                out result, out resultText);
                            break;
                        }
                    }
                }
                catch (Exception ex) { VISite.Alerting.ThrowError("Error in CompareValuesWith for element '" + element.Name + "'. Exception: " + ex); }
            }
            return result;
        }

        private void GetResult(string msg, out bool result, out string resultText)
        {
            VISite.Alerting.ThrowError(msg);
            result = false;
            resultText = msg;
        }

        public void FillSubElement(string name, string value)
        {
            GethValueElements[name].SetValue(value);
        }

        public void FillSubElements(Dictionary<IHaveValue, Object> values)
        {
            FillSubElements(values.ToDictionary(_ => _.Key.Name, _ => _.Value));
        }

        public List<T> GetElements<T>()
        {
            return
                GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(_ => typeof(T).IsAssignableFrom(_.FieldType)).Select(_ => (T)_.GetValue(this)).ToList();
        }

        public Func<Object, Object> FillRule { set; get; }
        #endregion


        #region Private
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
        
        private string ObjToString(Object obj)
        {
            var objects = obj as IEnumerable<object>;
            return objects != null 
                ? string.Join(", ", objects.Select(el => el.ToString())) 
                : obj.ToString();
        }

        public void InitSubElements()
        {
            GetElements().ForEach(SetViElement);
        }
        #endregion
    }

    public enum ContextType { Locator, Frame }
}
