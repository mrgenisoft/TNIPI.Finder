using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TNIPI.Finder
{
    public class FinderHost
    {
        static void Main(string[] args)
        {
            // Create and register an IPC channel
            //IDictionary props = new Hashtable();
            //props["portName"] = "finder";
            //props["includeVersions"] = true;
            //IpcServerChannel ipch = new IpcServerChannel(props, new BinaryServerFormatterSinkProvider(props, null));

            IpcServerChannel ipch;
            if (args.Length > 0)
                ipch = new IpcServerChannel(args[0]);
            else
                ipch = new IpcServerChannel("finder");

            ChannelServices.RegisterChannel(ipch, false);

            // Expose an object
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(FinderAccess), "finder.rem", WellKnownObjectMode.Singleton);
            
            Application.Run();
        }
    }
}
