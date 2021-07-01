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
    /// This class contains all the methods and subclasses of the LoadWells.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    public class LoadWells : Workstep<LoadWells.Arguments>, IPresentation, IDescriptionSource
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

            Invoke_LoadWells(argumentPackage);
        }

        private void Invoke_LoadWells(Arguments argumentPackage)
        {
            // TODO: finish the Invoke method implementation
            try
            {
                DateTime start = DateTime.Now;

                finder.SetConnectionParameters(argumentPackage.FinderSID, 
                                                argumentPackage.Username, 
                                                argumentPackage.Password, 
                                                argumentPackage.FinderProject,
                                                argumentPackage.WellLogResultTable,
                                                argumentPackage.UwiMask);

                finder.OpenConnection();
                PetrelLogger.InfoOutputWindow("Connected to Oracle " + finder.ServerVersion);

                ICollection<WellHdrRecord> wellHdrList = finder.WellList;
                if (wellHdrList.Count == 0)
                {
                    PetrelLogger.InfoOutputWindow("Wells not found");
                    return;
                }

                using (ITransaction trans = DataManager.NewTransaction(Thread.CurrentThread))
                {
                    try
                    {
                        if (argumentPackage.WellCollection == null)
                        {
                            WellRoot wr = WellRoot.Get(PetrelProject.PrimaryProject);
                            if (!wr.HasBoreholeCollection)
                            {
                                trans.Lock(PetrelProject.PrimaryProject);
                                argumentPackage.WellCollection = wr.CreateBoreholeCollection();
                            }
                            else
                            {
                                trans.Lock(wr.BoreholeCollection);
                                argumentPackage.WellCollection = wr.BoreholeCollection;
                            }
                        }
                        else
                            trans.Lock(argumentPackage.WellCollection);

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
                        if (argumentPackage.LoadWellLogs)
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

                        BoreholeCollection rootColl = argumentPackage.WellCollection;
                        Dictionary<string, BoreholeCollection> clusterDict = new Dictionary<string, BoreholeCollection>();

                        if (argumentPackage.CreateClusters)
                        {
                            clusterDict.Add(string.Empty, rootColl);
                            clusterDict.Add(Common.ExploratoryCollectionName, rootColl.Create(Common.ExploratoryCollectionName));
                        }

                        Dictionary<string, Borehole> bhDict = new Dictionary<string, Borehole>();
                        foreach (WellHdrRecord wellRec in wellHdrList)
                        {
                            BoreholeCollection insColl = rootColl;
                            if (argumentPackage.CreateClusters)
                            {
                                if (Common.IsExploratory(wellRec.Class))
                                    insColl = clusterDict[Common.ExploratoryCollectionName];
                                else if (clusterDict.ContainsKey(wellRec.Cluster))
                                    insColl = clusterDict[wellRec.Cluster];
                                else
                                {
                                    insColl = rootColl.Create(wellRec.Cluster);
                                    clusterDict.Add(wellRec.Cluster, insColl);
                                }
                            }

                            string name = Common.GetWellName(argumentPackage.CreateFromUwi, wellRec.Uwi, wellRec.Name, argumentPackage.AddSuffix);

                            Borehole bh = insColl.CreateBorehole(name);
                            bhDict.Add(wellRec.Uwi, bh);

                            bh.UWI = wellRec.Uwi;
                            bh.KellyBushing = wellRec.Elevation;
                            bh.WellHead = new Point2(wellRec.Nx, wellRec.Ny);
                            bh.SpudDate = wellRec.SpudDate;
                            bh.Operator = wellRec.Operator;

                            Common.SetBoreholeDictionaryProperty(bh, Common.ClassPropertyName, wellRec.Class);
                            Common.SetBoreholeDictionaryProperty(bh, Common.ClusterPropertyName, wellRec.Cluster + argumentPackage.AddSuffix);
                            Common.SetBoreholeProperty(bh, Common.FinDrillPropertyName, wellRec.FinishDate);

                            bool NoError = true;
                            if (argumentPackage.LoadDirSrvy)
                            {
                                try
                                {
                                    Common.LoadDirectionalSurvey(finder, bh, wellRec);
                                    Common.SetBoreholeProperty(bh, Common.SrvyDatePropertyName, wellRec.SurveyDate);
                                    Common.SetBoreholeDictionaryProperty(bh, Common.SrvyToolPropertyName, wellRec.SurveyTool);
                                }
                                catch (Exception exc)
                                {
                                    NoError = false;
                                    bh.Trajectory.Records = Common.CreateDummySurvey();
                                    PetrelLogger.InfoOutputWindow(wellRec.Uwi + ": Error while loading directional survey");
                                    PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                                    PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                                    PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
                                }
                            }
                            else
                                bh.Trajectory.Records = Common.CreateDummySurvey();

                            if (argumentPackage.LoadWellLogs)
                            {
                                try
                                {
                                    Common.LoadWellLogs(finder, bh, pvNameDict);
                                }
                                catch (Exception exc)
                                {
                                    NoError = false;
                                    PetrelLogger.InfoOutputWindow(wellRec.Uwi + ": Error while loading well logs");
                                    PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                                    PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                                    PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
                                }
                            }

                            if (NoError)
                                PetrelLogger.InfoOutputWindow(wellRec.Uwi + " loaded successfully");
                            else
                                PetrelLogger.InfoOutputWindow(wellRec.Uwi + " loaded with errors");
                        }

                        try
                        {
                            Common.LoadWellSymbols(finder, bhDict);
                            PetrelLogger.InfoOutputWindow("Well symbols loaded successfully");
                        }
                        catch (Exception exc)
                        {
                            PetrelLogger.InfoOutputWindow("Error while loading well symbols");
                            PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                            PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                            PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
                        }

                        if (argumentPackage.LoadDirSrvy)
                        {
                            try
                            {
                                Common.LoadWellTops(finder, argumentPackage.MarkerCollection, bhDict);
                                PetrelLogger.InfoOutputWindow("Well tops loaded successfully");
                            }
                            catch (Exception exc)
                            {
                                PetrelLogger.InfoOutputWindow("Error while loading well tops");
                                PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                                PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                                PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
                            }
                        }
                        else
                            PetrelLogger.InfoOutputWindow("Well tops not loaded");

                        PetrelLogger.InfoOutputWindow("Load complete");
                        trans.Commit();

                        DateTime end = DateTime.Now;
                        PetrelLogger.InfoOutputWindow("Execution time: " + (end - start).ToString());
                    }
                    catch (Exception exc)
                    {
                        trans.Commit();
                        PetrelLogger.InfoOutputWindow("Error while loading wells");
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

        #region CopyArgPack implementation

        protected override void CopyArgumentPackageCore(Arguments fromArgumentPackage, Arguments toArgumentPackage)
        {
            DescribedArgumentsHelper.Copy(fromArgumentPackage, toArgumentPackage);
        }

        #endregion

        /// <summary>
        /// ArgumentPackage class for LoadWells.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
        public class Arguments : DescribedArgumentsByReflection
        {
            private Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection wellCollection;
            private Slb.Ocean.Petrel.DomainObject.Well.MarkerCollection markerCollection;
            private bool createFromUwi = true;
            private bool createClusters = true;
            private bool loadDirSrvy = true;
            private bool loadWellLogs = true;
            private string addSuffix = string.Empty;
            private string finderSID = "FINDER";
            private string username = "";
            private string password = "";
            private string finderProject = "";
            string wellLogResultTable = "WELL_LOG_RESULT_LAYER";
            private string uwiMask = "%";

            [Description("WellCollection", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Well.BoreholeCollection WellCollection
            {
                internal get { return this.wellCollection; }
                set { this.wellCollection = value; }
            }

            [Description("MarkerCollection", "description for new argument")]
            public Slb.Ocean.Petrel.DomainObject.Well.MarkerCollection MarkerCollection
            {
                internal get { return this.markerCollection; }
                set { this.markerCollection = value; }
            }

            [Description("CreateFromUwi", "description for new argument")]
            public bool CreateFromUwi
            {
                internal get { return this.createFromUwi; }
                set { this.createFromUwi = value; }
            }

            [Description("CreateClusters", "description for new argument")]
            public bool CreateClusters
            {
                internal get { return this.createClusters; }
                set { this.createClusters = value; }
            }

            [Description("LoadDirSrvy", "description for new argument")]
            public bool LoadDirSrvy
            {
                internal get { return this.loadDirSrvy; }
                set { this.loadDirSrvy = value; }
            }

            [Description("LoadWellLogs", "description for new argument")]
            public bool LoadWellLogs
            {
                internal get { return this.loadWellLogs; }
                set { this.loadWellLogs = value; }
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

            [Description("UwiMask", "description for new argument")]
            public string UwiMask
            {
                internal get { return this.uwiMask; }
                set { this.uwiMask = value; }
            }
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
        /// Gets the description of the LoadWells
        /// </summary>
        public IDescription Description
        {
            get { return LoadWellsDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the LoadWells.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class LoadWellsDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private static LoadWellsDescription instance = new LoadWellsDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static LoadWellsDescription Instance
            {
                get { return instance; }
            }

            #region IDescription Members

            /// <summary>
            /// Gets the name of LoadWells
            /// </summary>
            public string Name
            {
                get { return "Load wells (remoting)"; }
            }
            /// <summary>
            /// Gets the short description of LoadWells
            /// </summary>
            public string ShortDescription
            {
                get { return "Load wells from an external Oracle database"; }
            }
            /// <summary>
            /// Gets the detailed description of LoadWells
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