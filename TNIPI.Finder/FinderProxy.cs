using System;
//using System.Data.Common;
using System.Reflection;
using System.Runtime.Remoting;

namespace TNIPI.Finder
{
    class FinderProxy
    {
        private IFinder finder = null;
        private System.Diagnostics.Process hostProcess = null;

        public FinderProxy()
        {
        }

        public void KillHost()
        {
            if (hostProcess != null)
            {
                finder.CloseConnection();
                hostProcess.Kill();
                hostProcess = null;
                finder = null;
            }
        }

        public IFinder GetFinderAccess()
        {
            if (finder != null)
                return finder;

            string path = Assembly.GetExecutingAssembly().Location;
            path = path.Substring(0, path.LastIndexOf('\\') + 1);

            ObjectHandle objHandle = Activator.CreateInstanceFrom(path + "TNIPI.FinderAccess.dll", "TNIPI.Finder.FinderAccess");
            finder = (IFinder)objHandle.Unwrap();

            string hostName;
            if(Common.Is64bit())
            {
                if (finder.IsClient64bit())
                    return finder;

                hostName = "TNIPI.FinderHost_x32.exe";
            }
            else
            {
                if (!finder.IsClient64bit())
                    return finder;

                hostName = "TNIPI.FinderHost_x64.exe";
            }

            if (hostProcess == null)
            {
                hostProcess = new System.Diagnostics.Process();
                hostProcess.StartInfo = new System.Diagnostics.ProcessStartInfo(hostName);
                hostProcess.StartInfo.Arguments = System.Security.Principal.WindowsIdentity.GetCurrent().Name + Common.Random.Next().ToString();
                hostProcess.StartInfo.UseShellExecute = true;
                hostProcess.StartInfo.WorkingDirectory = path;
            }

            if (!hostProcess.Start())
                throw new Exception("Error while starting host");

            if (!hostProcess.WaitForInputIdle(5000))
                throw new Exception("Host not responding");

            finder = (IFinder)Activator.GetObject(typeof(IFinder), "ipc://" + hostProcess.StartInfo.Arguments + "/finder.rem");

            return finder;
        }
    }
}
