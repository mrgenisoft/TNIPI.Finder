using System;
using System.Net.NetworkInformation;

using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.UI.Tools;

namespace TNIPI.Finder
{
    /// <summary>
    /// This class will control the lifecycle of the Module.
    /// The order of the methods are the same as the calling order.
    /// </summary>
    [ModuleAppearance(typeof(FinderModuleAppearance))]
    public class FinderModule : IModule
    {
        private FinderProxy finderProxy = null;

        public FinderModule()
        {
            //
            // TODO: Add constructor logic here
            //
            finderProxy = new FinderProxy();
        }

        #region IModule Members

        /// <summary>
        /// This method runs once in the Module life; when it loaded into the petrel.
        /// This method called first.
        /// </summary>
        public void Initialize()
        {
            // TODO:  Add FinderModule.Initialize implementation
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the not UI related components.
        /// (eg: datasource, plugin)
        /// </summary>
        public void Integrate()
        {
            // Registrations:
            // TODO:  Add FinderModule.Integrate implementation

            //IPGlobalProperties props = IPGlobalProperties.GetIPGlobalProperties();
            //if (!props.DomainName.ToUpper().Contains("STRJ") && props.HostName.ToUpper() != "GS-STATION" && props.HostName.ToUpper() != "PC-1937")
            //    return;

            try
            {
                LoadWells loadwellsInstance = new LoadWells();
                loadwellsInstance.Finder = finderProxy.GetFinderAccess();

                UpdateWells updatewellsInstance = new UpdateWells();
                updatewellsInstance.Finder = finderProxy.GetFinderAccess();

                //PetrelSystem.WorkflowEditor.Add(loadwellsInstance);
                PetrelSystem.ProcessDiagram.Add(new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(loadwellsInstance), "Plug-ins");

                //PetrelSystem.WorkflowEditor.Add(updatewellsInstance);
                PetrelSystem.ProcessDiagram.Add(new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(updatewellsInstance), "Plug-ins");
            }
            catch (Exception exc)
            {
                PetrelLogger.InfoOutputWindow("Error while loading plug-in");
                PetrelLogger.InfoOutputWindow("Message: " + exc.Message);
                PetrelLogger.InfoOutputWindow("Source: " + exc.Source);
                PetrelLogger.InfoOutputWindow("Data: " + exc.Data);
            }
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the UI related components.
        /// (eg: settingspages, treeextensions)
        /// </summary>
        public void IntegratePresentation()
        {
            // Registrations:
            // TODO:  Add FinderModule.IntegratePresentation implementation

            //PetrelButtonTool loadTool = new PetrelButtonTool("Load wells from Oracle", loadToolCallback);
            //PetrelButtonTool updTool = new PetrelButtonTool("Update wells from Oracle", updToolCallback);

            //WellKnownMenus.ToolsExtensions.AddTool(loadTool);
            //WellKnownMenus.ToolsExtensions.AddTool(updTool);
        }

        private void loadToolCallback(object sender, EventArgs e)
        {

        }

        private void updToolCallback(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// This method called once in the life of the module; 
        /// right before the module is unloaded. 
        /// It is usually when the application is closing.
        /// </summary>
        public void Disintegrate()
        {
            // TODO:  Add FinderModule.Disintegrate implementation
            try { finderProxy.KillHost(); }
            catch { }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // TODO:  Add FinderModule.Dispose implementation
        }

        #endregion
    }

    #region ModuleAppearance Class

    /// <summary>
    /// Appearance (or branding) for a Slb.Ocean.Core.IModule.
    /// This is associated with a module using Slb.Ocean.Core.ModuleAppearanceAttribute.
    /// </summary>
    internal class FinderModuleAppearance : IModuleAppearance
    {
        /// <summary>
        /// Description of the module.
        /// </summary>
        public string Description
        {
            get { return "Load and update well data from Finder database"; }
        }

        /// <summary>
        /// Display name for the module.
        /// </summary>
        public string DisplayName
        {
            get { return "Finder data connector plug-in"; }
        }

        /// <summary>
        /// Returns the name of a image resource.
        /// </summary>
        public string ImageResourceName
        {
            get { return "TNIPI.Finder.Module.ico"; }
        }

        /// <summary>
        /// A link to the publisher or null.
        /// </summary>
        public Uri ModuleUri
        {
            get { return null; }
        }
    }

    #endregion
}