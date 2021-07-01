using System;
using System.Reflection;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using Slb.Ocean.Core;
using Slb.Ocean.Geometry;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Petrel.DomainObject.Well;

namespace TNIPI.Finder
{
    /// <summary>
    /// This class contains all the methods and subclasses of the UpdateWells.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    public class UpdateWells : Workstep<UpdateWells.Arguments>, IPresentation, IDescriptionSource
    {
        private IFinder finder = null;

        public IFinder Finder
        {
            get { return finder; }
            set { finder = value; }
        }

        /// <summary>
        /// This method does the work of the process.
        /// </summary>
        /// <param name="argumentPackage">the arguments to use during the process</param>
        protected override void InvokeSimpleCore(Arguments argumentPackage)
        {
            // TODO: finish the Invoke method implementation
            if (!(argumentPackage.WellCollection is Borehole) && !(argumentPackage.WellCollection is BoreholeCollection))
            {
                PetrelLogger.InfoBox("Accept only well or well collection");
                return;
            }

            Invoke_UpdateWells(argumentPackage);
        }

        enum WellHdrColumns : int { UWI, NAME, NUMBER, OPERATOR, SPUD_DATE, FIN_DRILL, DRILLERS_TD, TVD, ELEVATION, NODE_X, NODE_Y, SURVEY_DATE, SURVEY_TOOL, NORTH, CORRECTION };
        private void Invoke_UpdateWells(Arguments argumentPackage)
        {
            try
            {
                DateTime start = DateTime.Now;

                WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
                Dictionary<string, Borehole> bhDict = new Dictionary<string, Borehole>();
                if (argumentPackage.WellCollection is BoreholeCollection)
                {
                    BoreholeCollection updColl = argumentPackage.WellCollection as BoreholeCollection;
                    if (updColl == null)
                    {
                        if (!wr.HasBoreholeCollection)
                        {
                            PetrelLogger.InfoOutputWindow("Wells not found in the project");
                            return;
                        }

                        updColl = wr.BoreholeCollection;
                    }

                    try
                    {
                        Common.FillBoreholeDictionary(updColl, bhDict);
                    }
                    catch (ArgumentException exc)
                    {
                        PetrelLogger.InfoOutputWindow(exc.Message);
                        return;
                    }
                }
                else
                {
                    Borehole bh = argumentPackage.WellCollection as Borehole;
                    if (!string.IsNullOrEmpty(bh.UWI))
                        bhDict.Add(bh.UWI, bh);
                    else
                    {
                        PetrelLogger.InfoOutputWindow("UWI not specified");
                        return;
                    }
                }

                if (bhDict.Count == 0)
                {
                    PetrelLogger.InfoOutputWindow("Wells not found in the project");
                    return;
                }

                finder.SetConnectionParameters(argumentPackage.FinderSID,
                                                argumentPackage.Username,
                                                argumentPackage.Password,
                                                argumentPackage.FinderProject,
                                                argumentPackage.WellLogResultTable,
                                                null);

                finder.OpenConnection();
                PetrelLogger.InfoOutputWindow("Connected to Oracle " + finder.ServerVersion);

                ICollection<WellHdrRecord> wellHdrList = finder.WellList;
                if (wellHdrList.Count == 0)
                {
                    PetrelLogger.InfoOutputWindow("Wells not found in the database");
                    return;
                }

                using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
                {
                    try
                    {
                        Dictionary<string, TypeCode> propNameDict = new Dictionary<string, TypeCode>();
                        propNameDict.Add(Common.ClassPropertyName, TypeCode.String);
                        propNameDict.Add(Common.ClusterPropertyName, TypeCode.String);
                        propNameDict.Add(Common.FinDrillPropertyName, TypeCode.DateTime);
                        propNameDict.Add(Common.SrvyDatePropertyName, TypeCode.DateTime);
                        propNameDict.Add(Common.SrvyToolPropertyName, TypeCode.String);
                        propNameDict.Add(Common.StateDatePropertyName, TypeCode.DateTime);
                        propNameDict.Add(Common.TypePropertyName, TypeCode.String);
                        propNameDict.Add(Common.StatePropertyName, TypeCode.String);
                        propNameDict.Add(Common.MethodPropertyName, TypeCode.String);
                        Common.CheckBoreholePropertyCollection(propNameDict);

                        Dictionary<string, PropertyVersionBase> pvNameDict = new Dictionary<string, PropertyVersionBase>();
                        if (argumentPackage.UpdateLogs)
                        {
                            Dictionary<string, ITemplate> tNameDict = new Dictionary<string, ITemplate>();
                            tNameDict.Add(Common.SpiWellLogVersionName, PetrelUnitSystem.TemplateGroupMiscellaneous.Fraction);
                            tNameDict.Add(Common.RtWellLogVersionName, PetrelUnitSystem.TemplateGroupLogTypes.ResistivityDeep);
                            tNameDict.Add(Common.PhieWellLogVersionName, PetrelUnitSystem.TemplateGroupPetrophysical.PorosityEffective);
                            tNameDict.Add(Common.KintWellLogVersionName, PetrelUnitSystem.TemplateGroupPetrophysical.Permeability);
                            tNameDict.Add(Common.SwWellLogVersionName, PetrelUnitSystem.TemplateGroupPetrophysical.SaturationWater);
                            tNameDict.Add(Common.VclWellLogVersionName, PetrelUnitSystem.TemplateGroupLogTypes.Vshale);
                            Common.CheckWellLogVesionCollection(tNameDict, pvNameDict);

                            Dictionary<string, IDictionaryTemplate> dtNameDict = new Dictionary<string, IDictionaryTemplate>();
                            dtNameDict.Add(Common.SatWellLogVersionName, PetrelUnitSystem.TemplateGroupFacies.DictionaryGeneral);
                            dtNameDict.Add(Common.TipWellLogVersionName, PetrelUnitSystem.TemplateGroupFacies.DictionaryGeneral);
                            Common.CheckDictionaryWellLogVesionCollection(dtNameDict, pvNameDict);
                        }

                        int i = 0;
                        Borehole[] bhArr = new Borehole[bhDict.Count];
                        foreach (Borehole bh in bhDict.Values)
                            bhArr[i++] = bh;
                        trans.Lock(bhArr);

                        bool found = false;
                        foreach (WellHdrRecord wellRec in wellHdrList)
                        {
                            if (!bhDict.ContainsKey(wellRec.Uwi))
                                continue;

                            found = true;

                            string name = Common.GetWellName(argumentPackage.UpdateFromUwi, wellRec.Uwi, wellRec.Name, argumentPackage.AddSuffix);

                            Borehole bh = bhDict[wellRec.Uwi];
                            if (argumentPackage.UpdateFromUwi)
                                bh.Name = name;

                            if (argumentPackage.UpdateKB)
                            {
                                bh.KellyBushing = wellRec.Elevation;
                                //Common.SetBoreholeProperty(bh, "KBreal", (float)elev);
                                //if (Math.Abs(bh.KellyBushing - elev) < 1.0d)
                                //    bh.KellyBushing = elev;
                            }

                            if (argumentPackage.UpdateWellHeads)
                            {
                                bh.WellHead = new Point2(wellRec.Nx, wellRec.Ny);
                            }

                            bh.SpudDate = wellRec.SpudDate;
                            bh.Operator = wellRec.Operator;
                            Common.SetBoreholeDictionaryProperty(bh, Common.ClusterPropertyName, wellRec.Cluster + argumentPackage.AddSuffix);
                            Common.SetBoreholeProperty(bh, Common.FinDrillPropertyName, wellRec.FinishDate);

                            bool NoError = true;
                            if (argumentPackage.UpdateDirSrvy)
                            {
                                try
                                {
                                    Common.LoadDirectionalSurvey(finder, bh, wellRec);
                                    Common.UpdateBoreholeWellTops(bh);

                                    Common.SetBoreholeProperty(bh, Common.SrvyDatePropertyName, wellRec.SurveyDate);
                                    Common.SetBoreholeDictionaryProperty(bh, Common.SrvyToolPropertyName, wellRec.SurveyTool);
                                }
                                catch (Exception exc)
                                {
                                    NoError = false;
                                    PetrelLogger.InfoOutputWindow(wellRec.Uwi + ": Error while updating directional survey");
                                    PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                                    PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                                    PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
                                }
                            }

                            if (argumentPackage.UpdateLogs)
                            {
                                try
                                {
                                    Common.LoadWellLogs(finder, bh, pvNameDict);
                                }
                                catch (Exception exc)
                                {
                                    NoError = false;
                                    PetrelLogger.InfoOutputWindow(wellRec.Uwi + ": Error while updating well logs");
                                    PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                                    PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                                    PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
                                }
                            }

                            if (NoError)
                                PetrelLogger.InfoOutputWindow(wellRec.Uwi + " updated successfully");
                            else
                                PetrelLogger.InfoOutputWindow(wellRec.Uwi + " updated with errors");
                        }

                        if (!found)
                            PetrelLogger.InfoOutputWindow("Wells not found in the database");
                        else
                        {
                            if (argumentPackage.UpdateSymbols)
                            {
                                try
                                {
                                    Common.LoadWellSymbols(finder, bhDict);
                                    PetrelLogger.InfoOutputWindow("Well symbols updated successfully");
                                }
                                catch (Exception exc)
                                {
                                    PetrelLogger.InfoOutputWindow("Error while updating well symbols");
                                    PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                                    PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                                    PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
                                }
                            }
                        }

                        PetrelLogger.InfoOutputWindow("Update complete");
                        trans.Commit();

                        DateTime end = DateTime.Now;
                        PetrelLogger.InfoOutputWindow("Execution time: " + (end - start).ToString());
                    }
                    catch (Exception exc)
                    {
                        trans.Commit();
                        PetrelLogger.InfoOutputWindow("Error while updating wells");
                        PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                        PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                        PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
                    }
                }

                finder.CloseConnection();
            }
            catch (Exception exc)
            {
                finder.CloseConnection();
                PetrelLogger.InfoOutputWindow("Error while loading wells");
                PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
            }
        }
        /*
        private void Clone(Borehole bh, Borehole copy, bool allowCopyWellHead, bool allowCopyTrajectory, bool allowCopyLogs)
        {
            copy.UWI = bh.UWI;
            copy.Comments = bh.Comments;
            if (allowCopyWellHead)
            {
                copy.KellyBushing = bh.KellyBushing;
                copy.WellHead = bh.WellHead;
            }
            //copy.Cost = bh.Cost;
            //copy.SpudDate = bh.SpudDate;
            //copy.SimulationName = bh.SimulationName;
            //copy.Operator = bh.Operator;

            foreach (DictionaryBoreholeProperty bhProp in WellRoot.Get(PetrelProject.PrimaryProject).BoreholeCollection.BoreholePropertyCollection.DictionaryProperties)
            {
                if (bhProp.IsWritable)
                {
                    Type type = typeof(Common);
                    MethodInfo mi = type.GetMethod("GetBoreholeDictionaryProperty");
                    MethodInfo mic = mi.MakeGenericMethod(bhProp.DataType);
                    object retval = mic.Invoke(null, new object[] { bh, bhProp.Name });
                    Common.SetBoreholeDictionaryProperty(copy, bhProp.Name, retval);
                    //Common.SetBoreholeDictionaryProperty(copy, bhProp.Name, Common.GetBoreholeDictionaryProperty<bhProp.DataType>(bh, bhProp.Name));
                }
            }

            foreach (BoreholeProperty bhProp in WellRoot.Get(PetrelProject.PrimaryProject).BoreholeCollection.BoreholePropertyCollection.Properties)
            {
                if (bhProp.IsWritable)
                {
                    if (bhProp.PropertyType.Equals(WellKnownBoreholePropertyTypes.WellHeadX))
                        continue;
                    if (bhProp.PropertyType.Equals(WellKnownBoreholePropertyTypes.WellHeadY))
                        continue;
                    if (bhProp.PropertyType.Equals(WellKnownBoreholePropertyTypes.KellyBushing))
                        continue;

                    Type type = typeof(Common);
                    MethodInfo mi = type.GetMethod("GetBoreholeProperty");
                    MethodInfo mic = mi.MakeGenericMethod(bhProp.DataType);
                    object retval = mic.Invoke(null, new object[] { bh, bhProp.Name });
                    Common.SetBoreholeProperty(copy, bhProp.Name, retval);
                    //Common.SetBoreholeProperty(copy, bhProp.Name, Common.GetBoreholeProperty<bhProp.DataType>(bh, bhProp.Name));
                }
            }
            
            //Common.SetBoreholeDictionaryProperty(copy, Common.ClusterPropertyName, Common.GetBoreholeDictionaryProperty<string>(bh, Common.ClusterPropertyName));
            //Common.SetBoreholeProperty(copy, Common.FinDrillPropertyName, Common.GetBoreholeProperty<DateTime>(bh, Common.FinDrillPropertyName));

            IBoreholePresentationFactory bpf = CoreSystem.GetService<IBoreholePresentationFactory>(bh); 
            IBoreholePresentation bpr = bpf.GetBoreholePresentation(bh);
            IBoreholePresentationFactory cpf = CoreSystem.GetService<IBoreholePresentationFactory>(copy);
            IBoreholePresentation cpr = cpf.GetBoreholePresentation(copy);
            cpr.WellSymbol = bpr.WellSymbol;

            ISettingsInfoFactory bsif = CoreSystem.GetService<ISettingsInfoFactory>(bh);
            ISettingsInfo binfo = bsif.GetSettingsInfo(bh);
            ISettingsInfoFactory csif = CoreSystem.GetService<ISettingsInfoFactory>(copy);
            ISettingsInfo cinfo = csif.GetSettingsInfo(copy);
            cinfo.Color = binfo.Color;

            if (allowCopyTrajectory)
                copy.Trajectory.Records = bh.Trajectory.Records;
            else
                copy.Trajectory.Records = Common.CreateDummySurvey();

            #region Clone Logs

            if (allowCopyLogs)
            {
                // BoreholeSeismogram
                foreach (BoreholeSeismogram item in bh.Logs.BoreholeSeismograms)
                {
                    if (!item.IsWritable)
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": " + item.Name + " is not writable");
                        continue;
                    }

                    BoreholeSeismogram newitem = copy.Logs.CreateBoreholeSeismogram(item.WellLogVersion);
                    try
                    {
                        newitem.Comments = item.Comments;
                        newitem.Domain = item.Domain;
                        //newitem.SampleCount = item.SampleCount;
                        newitem.Amplitudes.SampleCount = item.Amplitudes.SampleCount;
                        newitem.Amplitudes.SamplingStart = item.Amplitudes.SamplingStart;
                        newitem.Amplitudes.SamplingInterval = item.Amplitudes.SamplingInterval;
                        for (int t = 0; t < item.Amplitudes.TraceCount; t++)
                            for (int s = 0; s < item.Amplitudes.SampleCount; s++)
                                newitem.Amplitudes[t, s] = item.Amplitudes[t, s];
                    }
                    catch 
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": Error while copying " + newitem.Name);
                    }
                }

                // CheckShot
                foreach (CheckShot item in bh.Logs.CheckShots)
                {
                    CheckShot newitem = copy.Logs.CreateCheckShot(item.WellLogVersion);
                    try
                    {
                        newitem.Samples = item.Samples;
                    }
                    catch 
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": Error while copying " + newitem.WellLogVersion.Name);
                    }
                }

                // DictionaryWellLog
                foreach (DictionaryWellLog item in bh.Logs.DictionaryWellLogs)
                {
                    if (!item.IsWritable)
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": " + item.Name + " is not writable");
                        continue;
                    }

                    DictionaryWellLog newitem = copy.Logs.CreateDictionaryWellLog(item.DictionaryWellLogVersion);
                    try
                    {
                        newitem.Comments = item.Comments;
                        newitem.Samples = item.Samples;
                    }
                    catch 
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": Error while copying " + newitem.Name);
                    }
                }

                // MultitraceWellLog
                foreach (MultitraceWellLog item in bh.Logs.MultitraceWellLogs)
                {
                    if (!item.IsWritable)
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": " + item.Name + " is not writable");
                        continue;
                    }

                    MultitraceWellLog newitem = copy.Logs.CreateMultitraceWellLog(item.WellLogVersion);
                    try
                    {
                        newitem.Comments = item.Comments;
                        newitem.Samples = item.Samples;
                    }
                    catch 
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": Error while copying " + newitem.Name);
                    }
                }

                // PointWellLog
                foreach (PointWellLog item in bh.Logs.PointWellLogs)
                {
                    PointWellLog newitem = copy.Logs.CreatePointWellLog(item.WellLogVersion);
                    try
                    {
                        newitem.Samples = item.Samples;
                    }
                    catch 
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": Error while copying " + newitem.WellLogVersion.Name);
                    }
                }

                // WellLog
                foreach (WellLog item in bh.Logs.WellLogs)
                {
                    if (!item.IsWritable)
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": " + item.Name + " is not writable");
                        continue;
                    }

                    WellLog newitem = copy.Logs.CreateWellLog(item.WellLogVersion);
                    try
                    {
                        newitem.Comments = item.Comments;
                        newitem.Samples = item.Samples;
                    }
                    catch 
                    {
                        PetrelLogger.InfoOutputWindow(bh.UWI + ": Error while copying " + newitem.Name);
                    }
                }
            }

            #endregion

            #region Clone Completions

            // CasingString
            foreach (CasingString item in bh.Completions.CasingStrings)
            {
                CasingString newitem = copy.Completions.CreateCasingString(item.Name, item.EndMD);
                newitem.Comments = item.Comments;
                newitem.StartDate = item.StartDate;
                newitem.InnerDiameter = item.OuterDiameter;
                newitem.InnerRoughness = item.InnerRoughness;
                newitem.OuterDiameter = item.OuterDiameter;
            }

            // LinerString
            foreach (LinerString item in bh.Completions.LinerStrings)
            {
                LinerString newitem = copy.Completions.CreateLinerString(item.Name, item.StartMD, item.EndMD);
                newitem.Comments = item.Comments;
                newitem.StartDate = item.StartDate;
                newitem.InnerDiameter = item.InnerDiameter;
                newitem.InnerRoughness = item.InnerRoughness;
                newitem.OuterDiameter = item.OuterDiameter;
            }

            // Perforation
            foreach (Perforation item in bh.Completions.Perforations)
            {
                Perforation newitem = copy.Completions.CreatePerforation(item.Name, item.StartMD, item.EndMD);
                newitem.Comments = item.Comments;
                newitem.StartDate = item.StartDate;
                newitem.Diameter = item.Diameter;
                newitem.Skin = item.Skin;
            }

            // Plugback
            foreach (Plugback item in bh.Completions.Plugbacks)
            {
                Plugback newitem = copy.Completions.CreatePlugback(item.Name, item.StartMD);
                newitem.Comments = item.Comments;
                newitem.StartDate = item.StartDate;
            }

            // Squeeze
            foreach (Squeeze item in bh.Completions.Squeezes)
            {
                Squeeze newitem = copy.Completions.CreateSqueeze(item.Name, item.StartMD, item.EndMD);
                newitem.Comments = item.Comments;
                newitem.StartDate = item.StartDate;
            }

            // Stimulation
            foreach (Stimulation item in bh.Completions.Stimulations)
            {
                Stimulation newitem = copy.Completions.CreateStimulation(item.Name, item.StartMD, item.EndMD);
                newitem.Comments = item.Comments;
                newitem.StartDate = item.StartDate;
                newitem.Diameter = item.Diameter;
                newitem.Skin = item.Skin;
            }

            // TubingString
            foreach (TubingString item in bh.Completions.TubingStrings)
            {
                TubingString newitem = copy.Completions.CreateTubingString(item.Name, item.EndMD);
                newitem.Comments = item.Comments;
                newitem.StartDate = item.StartDate;
                newitem.EndDate = item.EndDate;
                newitem.InnerDiameter = item.InnerDiameter;
                newitem.InnerRoughness = item.InnerRoughness;
                newitem.OuterDiameter = item.OuterDiameter;
                newitem.OuterRoughness = item.OuterRoughness;

                #region Clone Tubing String

                // Choke
                foreach (Choke subitem in item.Chokes)
                {
                    Choke newsubitem = newitem.CreateChoke(subitem.Name, subitem.StartMD);
                    newsubitem.Length = subitem.Length;
                    newsubitem.InnerDiameter = subitem.InnerDiameter;
                    newsubitem.InnerRoughness = subitem.InnerRoughness;
                    newsubitem.OuterDiameter = subitem.OuterDiameter;
                    newsubitem.OuterRoughness = subitem.OuterRoughness;
                }

                // GasLiftValve
                foreach (GasLiftValve subitem in item.GasLiftValves)
                {
                    GasLiftValve newsubitem = newitem.CreateGasLiftValve(subitem.Name, subitem.StartMD);
                    newsubitem.Length = subitem.Length;
                    newsubitem.InnerDiameter = subitem.InnerDiameter;
                    newsubitem.InnerRoughness = subitem.InnerRoughness;
                    newsubitem.OuterDiameter = subitem.OuterDiameter;
                    newsubitem.OuterRoughness = subitem.OuterRoughness;
                }

                // InflowValve
                foreach (InflowValve subitem in item.InflowValves)
                {
                    InflowValve newsubitem = newitem.CreateInflowValve(subitem.Name, subitem.StartMD);
                    newsubitem.Length = subitem.Length;
                    newsubitem.InnerDiameter = subitem.InnerDiameter;
                    newsubitem.InnerRoughness = subitem.InnerRoughness;
                    newsubitem.OuterDiameter = subitem.OuterDiameter;
                    newsubitem.OuterRoughness = subitem.OuterRoughness;
                }

                // Packer
                foreach (Packer subitem in item.Packers)
                {
                    Packer newsubitem = newitem.CreatePacker(subitem.Name, subitem.StartMD);
                    newsubitem.Length = subitem.Length;
                }

                // Pump
                foreach (Pump subitem in item.Pumps)
                {
                    Pump newsubitem = newitem.CreatePump(subitem.Name, subitem.StartMD);
                    newsubitem.Length = subitem.Length;
                    newsubitem.InnerDiameter = subitem.InnerDiameter;
                    newsubitem.InnerRoughness = subitem.InnerRoughness;
                    newsubitem.OuterDiameter = subitem.OuterDiameter;
                    newsubitem.OuterRoughness = subitem.OuterRoughness;
                }

                #endregion
            }

            #endregion
        }
        */
        #region CopyArgPack implementation

        protected override void CopyArgumentPackageCore(Arguments fromArgumentPackage, Arguments toArgumentPackage)
        {
            DescribedArgumentsHelper.Copy(fromArgumentPackage, toArgumentPackage);
        }

        #endregion

        /// <summary>
        /// ArgumentPackage class for UpdateWells.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
        public class Arguments : DescribedArgumentsByReflection
        {
            //private Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection wellCollection;
            private Slb.Ocean.Data.Hosting.IDomainObject wellCollection;
            //private bool addNewWells = false;
            private bool updateFromUwi = false;
            //private bool createClusters = false;
            private bool updateKB = false;
            private bool updateWellHeads = false;
            private bool updateDirSrvy = false;
            private bool updateLogs = false;
            private bool updateSymbols = false;
            private string addSuffix = string.Empty;
            private string finderSID = "FINDER";
            private string username = "";
            private string password = "";
            private string finderProject = "";
            string wellLogResultTable = "WELL_LOG_RESULT_LAYER";
            //private string uwiMask = "%";

            [Description("WellCollection", "description for new argument")]
            //public Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection WellCollection
            public Slb.Ocean.Data.Hosting.IDomainObject WellCollection
            {
                internal get { return this.wellCollection; }
                set { this.wellCollection = value; }
            }
            /*
            [Description("AddNewWells", "description for new argument")]
            public bool AddNewWells
            {
                internal get { return this.addNewWells; }
                set { this.addNewWells = value; }
            }
            */
            [Description("UpdateFromUwi", "description for new argument")]
            public bool UpdateFromUwi
            {
                internal get { return this.updateFromUwi; }
                set { this.updateFromUwi = value; }
            }
            /*
            [Description("CreateClusters", "description for new argument")]
            public bool CreateClusters
            {
                internal get { return this.createClusters; }
                set { this.createClusters = value; }
            }
            */
            [Description("UpdateKB", "description for new argument")]
            public bool UpdateKB
            {
                internal get { return this.updateKB; }
                set { this.updateKB = value; }
            }

            [Description("UpdateWellHeads", "description for new argument")]
            public bool UpdateWellHeads
            {
                internal get { return this.updateWellHeads; }
                set { this.updateWellHeads = value; }
            }

            [Description("UpdateDirSrvy", "description for new argument")]
            public bool UpdateDirSrvy
            {
                internal get { return this.updateDirSrvy; }
                set { this.updateDirSrvy = value; }
            }

            [Description("UpdateLogs", "description for new argument")]
            public bool UpdateLogs
            {
                internal get { return this.updateLogs; }
                set { this.updateLogs = value; }
            }

            [Description("UpdateSymbols", "description for new argument")]
            public bool UpdateSymbols
            {
                internal get { return this.updateSymbols; }
                set { this.updateSymbols = value; }
            }

            [Description("AddSuffix", "description for new argument")]
            public string AddSuffix
            {
                internal get { return this.addSuffix; }
                set { this.addSuffix = value; }
            }

            [Description("FinderSID", "description for new argument")]
            public string FinderSID
            {
                internal get { return this.finderSID; }
                set { this.finderSID = value; }
            }

            [Description("Username", "description for new argument")]
            public string Username
            {
                internal get { return this.username; }
                set { this.username = value; }
            }

            [Description("Password", "description for new argument")]
            public string Password
            {
                internal get { return this.password; }
                set { this.password = value; }
            }

            [Description("FinderProject", "description for new argument")]
            public string FinderProject
            {
                internal get { return this.finderProject; }
                set { this.finderProject = value; }
            }

            [Description("WellLogResultTable", "description for new argument")]
            public string WellLogResultTable
            {
                internal get { return this.wellLogResultTable; }
                set { this.wellLogResultTable = value; }
            }
            /*
            [Description("UwiMask", "description for new argument")]
            public string UwiMask
            {
                internal get { return this.uwiMask; }
                set { this.uwiMask = value; }
            }
            */
        }

        #region IPresentation Members

        public event EventHandler PresentationChanged;

        public string Text
        {
            get { return Description.Name; }
        }

        public System.Drawing.Bitmap Image
        {
            get { return PetrelImages.Modules; }
        }

        #endregion

        #region IDescriptionSource Members

        /// <summary>
        /// Gets the description of the UpdateWells
        /// </summary>
        public IDescription Description
        {
            get { return UpdateWellsDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the UpdateWells.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class UpdateWellsDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private static UpdateWellsDescription instance = new UpdateWellsDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static UpdateWellsDescription Instance
            {
                get { return instance; }
            }

            #region IDescription Members

            /// <summary>
            /// Gets the name of UpdateWells
            /// </summary>
            public string Name
            {
                get { return "Update wells (remoting)"; }
            }
            /// <summary>
            /// Gets the short description of UpdateWells
            /// </summary>
            public string ShortDescription
            {
                get { return "Update wells from an external Oracle database"; }
            }
            /// <summary>
            /// Gets the detailed description of UpdateWells
            /// </summary>
            public string Description
            {
                get { return ""; }
            }

            #endregion
        }

        #endregion
    }
}