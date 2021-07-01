using System;
using System.Reflection;
using System.Drawing;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Slb.Ocean.Core;
using Slb.Ocean.Geometry;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Petrel.DomainObject.Basics;
using Slb.Ocean.Petrel.DomainObject.Well;

namespace TNIPI.Finder
{
    class Common
    {
        public static Random Random = new Random();

        public static string ExploratoryCollectionName = "EXPLORATORY";

        public static string ExploratoryClassName = "EXPLORATORY";

        public static string HorizonTopNamePrefix = "";
        public static string HorizonTopNameSuffix = "_top";
        public static string HorizonBaseNamePrefix = "";
        public static string HorizonBaseNameSuffix = "_base";

        public static string ClassPropertyName = "Class";
        public static string ClusterPropertyName = "Cluster";
        public static string FinDrillPropertyName = "Fin drill";
        public static string SrvyDatePropertyName = "Survey Date";
        public static string SrvyToolPropertyName = "Survey Tool";
        public static string StateDatePropertyName = "State Date";
        public static string TypePropertyName = "Type";
        public static string StatePropertyName = "State";
        public static string MethodPropertyName = "Method";

        public static string SpiWellLogVersionName = "SPI";
        public static string RtWellLogVersionName = "RT";
        public static string PhieWellLogVersionName = "PHIE";
        public static string KintWellLogVersionName = "KINT";
        public static string SwWellLogVersionName = "SW";
        public static string VclWellLogVersionName = "VCL";
        public static string SatWellLogVersionName = "SAT";
        public static string TipWellLogVersionName = "TIP";

        public static string ProductionWellSymbol = "R_AFTER_TEST_PRODUCTION_WELL";         // 269
        public static string InjectionWellSymbol = "R_INJECTION_WELL_FUNCTIONING";          // 288
        public static string ExplorationWellSymbol = "R_AFTER_TEST_EXPLORATORY_WELL";       // 270
        public static string DefaultWellSymbol = "R_PRODUCTION_IN_DEVELOPMENT";             // 299

        private static Dictionary<string, string> LogColumnDict = null;
        private static string SpiDbColumnName = "SPI";
        private static string RtDbColumnName = "RT";
        private static string PhieDbColumnName = "PHIE";
        private static string KintDbColumnName = "KINT";
        private static string SwDbColumnName = "SW";
        private static string VclDbColumnName = "VCL";
        private static string SatDbColumnName = "SD.SAT_CODE";
        private static string TipDbColumnName = "TIP";

        private static Dictionary<string, bool> LogNeedConvertDict = null;
        private static bool SpiNeedConvert = false;
        private static bool RtNeedConvert = false;
        private static bool PhieNeedConvert = false;
        private static bool KintNeedConvert = true;
        private static bool SwNeedConvert = false;
        private static bool VclNeedConvert = false;
        private static bool SatNeedConvert = false;
        private static bool TipNeedConvert = false;

        static Common()
        {
            LogColumnDict = new Dictionary<string, string>();
            LogColumnDict.Add(SpiWellLogVersionName, SpiDbColumnName);
            LogColumnDict.Add(RtWellLogVersionName, RtDbColumnName);
            LogColumnDict.Add(PhieWellLogVersionName, PhieDbColumnName);
            LogColumnDict.Add(KintWellLogVersionName, KintDbColumnName);
            LogColumnDict.Add(SwWellLogVersionName, SwDbColumnName);
            LogColumnDict.Add(VclWellLogVersionName, VclDbColumnName);
            LogColumnDict.Add(SatWellLogVersionName, SatDbColumnName);
            LogColumnDict.Add(TipWellLogVersionName, TipDbColumnName);

            LogNeedConvertDict = new Dictionary<string, bool>();
            LogNeedConvertDict.Add(SpiWellLogVersionName, SpiNeedConvert);
            LogNeedConvertDict.Add(RtWellLogVersionName, RtNeedConvert);
            LogNeedConvertDict.Add(PhieWellLogVersionName, PhieNeedConvert);
            LogNeedConvertDict.Add(KintWellLogVersionName, KintNeedConvert);
            LogNeedConvertDict.Add(SwWellLogVersionName, SwNeedConvert);
            LogNeedConvertDict.Add(VclWellLogVersionName, VclNeedConvert);
            LogNeedConvertDict.Add(SatWellLogVersionName, SatNeedConvert);
            LogNeedConvertDict.Add(TipWellLogVersionName, TipNeedConvert);
        }

        public static bool Is64bit()
        {
            return (IntPtr.Size == 8);
            //return true;
        }

        public static bool IsSqlLikeMatch(string input, string pattern)
        {
            /* Turn "off" all regular expression related syntax in
            * the pattern string. */
            pattern = Regex.Escape(pattern);

            /* Replace the SQL LIKE wildcard metacharacters with the
            * equivalent regular expression metacharacters. */
            pattern = pattern.Replace("%", ".*?").Replace("_", ".");

            /* The previous call to Regex.Escape actually turned off
            * too many metacharacters, i.e. those which are recognized by
            * both the regular expression engine and the SQL LIKE
            * statement ([...] and [^...]). Those metacharacters have
            * to be manually unescaped here. */
            pattern = pattern.Replace(@"\[", "[").Replace(@"\]",
            "]").Replace(@"\^", "^");

            return Regex.IsMatch(input, pattern);
        }

        public static bool IsExploratory(string _class)
        {
            return _class.Equals(ExploratoryClassName);
        }

        public static string CreateNameFromUwi(string uwi)
        {
            string uwiname = uwi.Substring(5, 4).TrimStart('0'); ;
            char b1 = uwi[9], b2 = uwi[10];
            if (Char.IsDigit(b1))
            {
                if (Char.IsDigit(b2))
                {
                    if (b1 == '0')
                    {
                        if (b2 != '0')
                            uwiname += "_" + b2.ToString();
                    }
                    else
                    {
                        uwiname += "_" + b1.ToString() + b2.ToString();
                    }
                }
                else
                {
                    uwiname += "_" + b1.ToString() + b2.ToString();
                }
            }
            else
            {
                uwiname += b1.ToString();
                if (Char.IsDigit(b2))
                {
                    if (b2 != '0')
                        uwiname += "_" + b2.ToString();
                }
                else
                {
                    uwiname += b2.ToString();
                }
            }

            return uwiname;
        }

        public static string GetWellName(bool createFromUwi, string uwi, string name, string suffix)
        {
            if (createFromUwi)
            {
                try
                {
                    name = CreateNameFromUwi(uwi);
                }
                catch
                {
                    PetrelLogger.InfoOutputWindow(uwi + ": UWI has wrong format");
                    name = uwi;
                }
            }

            if (string.IsNullOrEmpty(name))
                name = uwi;

            name += suffix;

            return name;
        }

        public static T GetBoreholeDictionaryProperty<T>(Borehole bh, string propName)
        {
            T retval = bh.PropertyAccess.GetPropertyValue<T>(Common.GetBoreholeDictionaryPropertyByName(propName));
            return retval;
        }

        public static T GetBoreholeProperty<T>(Borehole bh, string propName)
        {
            T retval = bh.PropertyAccess.GetPropertyValue<T>(Common.GetBoreholePropertyByName(propName));
            return retval;
        }

        public static void SetBoreholeDictionaryProperty(Borehole bh, string propName, object obj)
        {
            DictionaryBoreholeProperty bhProp = Common.GetBoreholeDictionaryPropertyByName(propName);
            if (obj != null)
                bh.PropertyAccess.SetPropertyValue(bhProp, obj);
            else
                bh.PropertyAccess.SetPropertyValue(bhProp, Slb.Ocean.Data.NullObject.NullValueDefaults.GetDefaultByType(obj.GetType()));
        }

        public static void SetBoreholeProperty(Borehole bh, string propName, object obj)
        {
            BoreholeProperty bhProp = Common.GetBoreholePropertyByName(propName);
            if (obj != null)
                bh.PropertyAccess.SetPropertyValue(bhProp, obj);
            else
                bh.PropertyAccess.SetPropertyValue(bhProp, Slb.Ocean.Data.NullObject.NullValueDefaults.GetDefaultByType(obj.GetType()));
        }

        public static void FillBoreholeDictionary(BoreholeCollection bhColl, IDictionary<string, Borehole> bhDict)
        {
            foreach (Borehole bh in bhColl)
            {
                if(bhDict.ContainsKey(bh.UWI))
                    throw new ArgumentException(bhDict[bh.UWI].Name + " and " + bh.Name + " have the same UWI");
                if (!string.IsNullOrEmpty(bh.UWI))
                    bhDict.Add(bh.UWI, bh);
            }

            foreach (BoreholeCollection bhc in bhColl.BoreholeCollections)
                FillBoreholeDictionary(bhc, bhDict);
        }

        public static bool HasBoreholeDictionaryProperty(string name)
        {
            bool found = false;
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            BoreholePropertyCollection bhPropColl = wr.BoreholeCollection.BoreholePropertyCollection;
            foreach (DictionaryBoreholeProperty bhProp in bhPropColl.DictionaryProperties)
                if(bhProp.Name == name)
                {
                    found = true;
                    break;
                }

            return found;
        }

        public static bool HasBoreholeProperty(string name)
        {
            bool found = false;
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            BoreholePropertyCollection bhPropColl = wr.BoreholeCollection.BoreholePropertyCollection;
            foreach (BoreholeProperty bhProp in bhPropColl.Properties)
                if (bhProp.Name == name)
                {
                    found = true;
                    break;
                }

            return found;
        }

        public static DictionaryBoreholeProperty GetBoreholeDictionaryPropertyByName(string name)
        {
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            BoreholePropertyCollection bhPropColl = wr.BoreholeCollection.BoreholePropertyCollection;
            foreach (DictionaryBoreholeProperty bhProp in bhPropColl.DictionaryProperties)
                if (bhProp.Name == name)
                    return bhProp;

            return null;
        }

        public static BoreholeProperty GetBoreholePropertyByName(string name)
        {
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            BoreholePropertyCollection bhPropColl = wr.BoreholeCollection.BoreholePropertyCollection;
            foreach (BoreholeProperty bhProp in bhPropColl.Properties)
                if (bhProp.Name == name)
                    return bhProp;

            return null;
        }

        public static void CheckBoreholePropertyCollection(IDictionary<string,TypeCode> typeDict)
        {
            using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
            {
                WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
                BoreholePropertyCollection bhPropColl = wr.BoreholeCollection.BoreholePropertyCollection;
                trans.Lock(bhPropColl);

                foreach (string name in typeDict.Keys)
                {
                    switch (typeDict[name])
                    {
                        case TypeCode.Boolean:
                            if (!HasBoreholeDictionaryProperty(name))
                                bhPropColl.CreateDictionaryProperty(typeof(Boolean), name);
                            break;
                        case TypeCode.String:
                            if (!HasBoreholeDictionaryProperty(name))
                                bhPropColl.CreateDictionaryProperty(typeof(String), name);
                            break;
                        case TypeCode.DateTime:
                            if (!HasBoreholeProperty(name))
                                bhPropColl.CreateProperty(typeof(DateTime), name);
                            break;
                        default:
                            throw new ArgumentException(typeDict[name].ToString() + " is not supported");
                    }
                }

                trans.Commit();
            }
        }

        public static DictionaryWellLog GetDictionaryWellLogByName(Borehole bh, string name)
        {
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            foreach (DictionaryWellLog log in bh.Logs.DictionaryWellLogs)
                if (log.Name == name)
                    return log;

            return null;
        }

        public static WellLog GetWellLogByName(Borehole bh, string name)
        {
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            foreach (WellLog log in bh.Logs.WellLogs)
                if (log.Name == name)
                    return log;

            return null;
        }

        public static bool HasDictionaryWellLogVersion(string name)
        {
            bool found = false;
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            foreach (DictionaryPropertyVersion wlVers in wr.DictionaryWellLogVersions)
                if (wlVers.Name == name)
                {
                    found = true;
                    break;
                }

            return found;
        }

        public static bool HasWellLogVersion(string name)
        {
            bool found = false;
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            foreach (PropertyVersion wlVers in wr.WellLogVersions)
                if (wlVers.Name == name)
                {
                    found = true;
                    break;
                }

            return found;
        }

        public static DictionaryPropertyVersion GetDictionaryWellLogVersionByName(string name)
        {
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            foreach (DictionaryPropertyVersion wlVers in wr.DictionaryWellLogVersions)
                if (wlVers.Name == name)
                    return wlVers;

            return null;
        }

        public static PropertyVersion GetWellLogVersionByName(string name)
        {
            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
            foreach (PropertyVersion wlVers in wr.WellLogVersions)
                if (wlVers.Name == name)
                    return wlVers;

            return null;
        }

        public static void CheckDictionaryWellLogVesionCollection(IDictionary<string, IDictionaryTemplate> dtNameDict, IDictionary<string, PropertyVersionBase> pvNameDict)
        {
            using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
            {
                trans.Lock(PetrelProject.PrimaryProject);
                WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);

                IDictionaryPropertyVersionService idpvs = PetrelSystem.DictionaryPropertyVersionService;
                foreach (string name in dtNameDict.Keys)
                {
                    DictionaryPropertyVersion dpv;
                    if (!HasDictionaryWellLogVersion(name))
                        dpv = idpvs.FindOrCreate(name, dtNameDict[name]);
                    else
                        dpv = Common.GetDictionaryWellLogVersionByName(name);
                    pvNameDict.Add(name, dpv);
                }

                trans.Commit();
            }
        }

        public static void CheckWellLogVesionCollection(IDictionary<string, ITemplate> tNameDict, IDictionary<string, PropertyVersionBase> pvNameDict)
        {
            using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
            {
                trans.Lock(PetrelProject.PrimaryProject);
                WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);

                IPropertyVersionService ipvs = PetrelSystem.PropertyVersionService;
                foreach (string name in tNameDict.Keys)
                {
                    PropertyVersion pv;
                    if (!HasWellLogVersion(name))
                        pv = ipvs.FindOrCreate(name, tNameDict[name]);
                    else
                        pv = Common.GetWellLogVersionByName(name);
                    pvNameDict.Add(name, pv);
                }

                trans.Commit();
            }
        }

        public static void UpdateBoreholeWellTops(Borehole bh)
        {
            using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
            {
                WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
                foreach (MarkerCollection markerColl in wr.MarkerCollections)
                {
                    foreach (Marker marker in markerColl.GetMarkers(bh))
                    {
                        trans.Lock(marker);
                        double md = marker.MD;
                        marker.MD = md;
                    }
                }

                trans.Commit();
            }
        }

        public static List<TrajectoryRecord> CreateDummySurvey()
        {
            List<TrajectoryRecord> traj = new List<TrajectoryRecord>();
            traj.Add(new TrajectoryRecord(0.0d, 0.0d, 0.0d));
            traj.Add(new TrajectoryRecord(10.0d, 0.0d, 0.0d));
            return traj;
        }

        public static void LoadWellSymbols(IFinder finder, IDictionary<string, Borehole> bhDict)
        {
            List<string> uwiList = new List<string>(bhDict.Keys);
            ICollection<WellStateRecord> wsrList = finder.GetWellState(uwiList);
            if (wsrList.Count == 0)
                throw new Exception("Well state not found");

            foreach (WellStateRecord wsr in wsrList)
            {
                Borehole bh = bhDict[wsr.Uwi];
                Common.SetBoreholeProperty(bh, Common.StateDatePropertyName, wsr.TimeStamp);
                Common.SetBoreholeDictionaryProperty(bh, Common.TypePropertyName, wsr.TypeDescription);
                Common.SetBoreholeDictionaryProperty(bh, Common.StatePropertyName, wsr.StateDescription);
                Common.SetBoreholeDictionaryProperty(bh, Common.MethodPropertyName, wsr.MethodDescription);

                string symbol;
                Color color;
                switch (wsr.Type)
                {
                    case 11: symbol = ProductionWellSymbol; color = Color.Black; break;
                    case 20: symbol = InjectionWellSymbol; color = Color.Blue; break;
                    case 42: symbol = ExplorationWellSymbol; color = Color.Black; break;
                    default: symbol = DefaultWellSymbol; color = Color.Black; break;
                }

                IBoreholePresentationFactory bpf = CoreSystem.GetService<IBoreholePresentationFactory>(bh); // IBoreholePresentationFactory inside generic angle brackets
                IBoreholePresentation pres = bpf.GetBoreholePresentation(bh);
                string _class = GetBoreholeDictionaryProperty<string>(bh, ClassPropertyName);
                if (IsExploratory(_class))
                {
                    WellSymbolDescription wsDesc = WellSymbolRegistry.GetWellSymbol(ExplorationWellSymbol);
                    pres.WellSymbol = wsDesc;
                    pres.Color = color;
                }
                else
                {
                    WellSymbolDescription wsDesc = WellSymbolRegistry.GetWellSymbol(symbol);
                    pres.WellSymbol = wsDesc;
                    pres.Color = color;
                }
            }

            foreach (Borehole bh in bhDict.Values)
            {
                IBoreholePresentationFactory bpf = CoreSystem.GetService<IBoreholePresentationFactory>(bh);
                IBoreholePresentation pres = bpf.GetBoreholePresentation(bh);
                if (pres.WellSymbol == WellSymbolDescription.UNDEFINED)
                {
                    string _class = GetBoreholeDictionaryProperty<string>(bh, ClassPropertyName);
                    if (IsExploratory(_class))
                    {
                        pres.WellSymbol = WellSymbolRegistry.GetWellSymbol(ExplorationWellSymbol);
                        pres.Color = Color.Black;
                    }
                    else
                    {
                        pres.WellSymbol = WellSymbolRegistry.GetWellSymbol(DefaultWellSymbol);
                        pres.Color = Color.Black;
                    }
                }
            }
        }

        public static void LoadWellTops(IFinder finder, MarkerCollection markerColl, IDictionary<string, Borehole> bhDict)
        {
            List<string> uwiList = new List<string>(bhDict.Keys);
            ICollection<WellTopRecord> wtrList = finder.GetWellTops(uwiList);
            if (wtrList.Count == 0)
                throw new Exception("Well tops not found");

            using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
            {
                if (markerColl == null)
                {
                    trans.Lock(PetrelProject.PrimaryProject);
                    WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
                    markerColl = wr.CreateMarkerCollection("Well Tops " + finder.Project);
                }
                else
                    trans.Lock(markerColl);

                Dictionary<string, Surface> ifcDict = new Dictionary<string, Surface>();
                foreach (Surface ifc in markerColl.Horizons)
                    ifcDict.Add(ifc.Name, ifc);
                foreach (Surface ifc in markerColl.Interfaces)
                    ifcDict.Add(ifc.Name, ifc);

                foreach (WellTopRecord wtr in wtrList)
                {
                    Borehole bh = bhDict[wtr.Uwi];

                    Surface topifc, baseifc;
                    string topname = HorizonTopNamePrefix + wtr.Layer + HorizonTopNameSuffix;
                    string basename = HorizonBaseNamePrefix + wtr.Layer + HorizonBaseNameSuffix;

                    if (!ifcDict.ContainsKey(topname))
                    {
                        topifc = markerColl.CreateInterface();
                        topifc.Name = topname;
                        ifcDict.Add(topname, topifc);
                    }
                    else
                        topifc = ifcDict[topname];

                    if (!ifcDict.ContainsKey(basename))
                    {
                        baseifc = markerColl.CreateInterface();
                        baseifc.Name = basename;
                        ifcDict.Add(basename, baseifc);
                    }
                    else
                        baseifc = ifcDict[basename];

                    markerColl.CreateMarker(bh, topifc, wtr.Top);
                    markerColl.CreateMarker(bh, baseifc, wtr.Base);
                }

                trans.Commit();
            }
        }

        public static void LoadDirectionalSurvey(IFinder finder, Borehole bh, WellHdrRecord wellRec)
        {
            ICollection<WellTrajRecord> wtrList = finder.GetDirectionalSurvey(wellRec.Uwi);
            if (wtrList.Count == 0)
                throw new Exception("Directional survey not found");

            List<TrajectoryRecord> traj = new List<TrajectoryRecord>();
            foreach (WellTrajRecord trajRec in wtrList)
            {
                if (wellRec.NorthReference.Equals("M"))
                    trajRec.Azimuth += wellRec.MagneticCorrection;

                if (trajRec.Azimuth > 2 * Math.PI)
                    trajRec.Azimuth -= 2 * Math.PI;

                traj.Add(new TrajectoryRecord(trajRec.MD, trajRec.Inclination, trajRec.Azimuth));
            }

            bh.Trajectory.Records = traj;
        }

        public static void LoadWellLogs(IFinder finder, Borehole bh, IDictionary<string, PropertyVersionBase> pvNameDict)
        {
            Dictionary<string, WellLogType> logColumnDict = new Dictionary<string, WellLogType>();
            foreach (string pvName in pvNameDict.Keys)
                if (pvNameDict[pvName] is PropertyVersion)
                    logColumnDict.Add(LogColumnDict[pvName], WellLogType.Continuous);
                else
                    logColumnDict.Add(LogColumnDict[pvName], WellLogType.Discrete);

            IDictionary<string, WellLogRecordList> wlrDict = finder.GetWellLogs(bh.UWI, logColumnDict);
            foreach (string colName in LogColumnDict.Values)
                if (wlrDict[colName].Count == 0)
                    throw new Exception("Well logs not found");

            using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
            {
                foreach (string pvName in pvNameDict.Keys)
                {
                    string colName = LogColumnDict[pvName];
                    if (pvNameDict[pvName] is PropertyVersion)
                    {
                        WellLog log = GetWellLogByName(bh, pvName);
                        if (log == null)
                            log = bh.Logs.CreateWellLog((PropertyVersion)pvNameDict[pvName]);

                        List<WellLogSample> wlsList = new List<WellLogSample>();
                        foreach (WellLogRecord wlr in wlrDict[colName])
                        {
                            WellLogSample wls;
                            if (wlr.Value != null)
                            {
                                if (LogNeedConvertDict[pvName])
                                    wlr.Value = PetrelUnitSystem.ConvertFromUI(log.WellLogVersion, (float)wlr.Value);
                                wls = new WellLogSample(wlr.MD, (float)wlr.Value);
                            }
                            else
                                wls = new WellLogSample(wlr.MD, float.NaN);

                            wlsList.Add(wls);
                        }

                        trans.Lock(log);
                        log.Samples = wlsList;
                    }
                    else
                    {
                        DictionaryWellLog log = GetDictionaryWellLogByName(bh, pvName);
                        if (log == null)
                            log = bh.Logs.CreateDictionaryWellLog((DictionaryPropertyVersion)pvNameDict[pvName]);

                        List<DictionaryWellLogSample> wlsList = new List<DictionaryWellLogSample>();
                        foreach (WellLogRecord wlr in wlrDict[colName])
                        {
                            DictionaryWellLogSample wls;
                            if (wlr.Value != null)
                            {
                                if ((int)wlr.Value == int.MinValue)
                                    wlr.Value = DictionaryWellLogSample.UndefinedValue;
                                wls = new DictionaryWellLogSample(wlr.MD, (int)wlr.Value);
                            }
                            else
                                wls = new DictionaryWellLogSample(wlr.MD, DictionaryWellLogSample.UndefinedValue);

                            wlsList.Add(wls);
                        }

                        trans.Lock(log);
                        log.Samples = wlsList;
                    }
                }

                trans.Commit();
            }
        }
    }
}
