using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.VisualStudio.CommandBars;
using System.Drawing;
using stdole;

namespace InternalsViewer.SSMSAddIn
{
    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        private QueryEditorExtender queryEditorExtender;
        private ObjectExplorerExtender objectExplorerExtender;
        private bool monitorTransactionLog;
        private WindowManager windowManager;
        private DTE2 applicationObject;
        private EnvDTE.AddIn addInInstance;

        public Connect()
        {
        }

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            applicationObject = (DTE2)application;
            addInInstance = (EnvDTE.AddIn)addInInst;

            switch (connectMode)
            {
                case ext_ConnectMode.ext_cm_Startup:

                    Commands2 commands = (Commands2)applicationObject.Commands;

                    CommandBar menuBarCommandBar = ((CommandBars)applicationObject.CommandBars)["MenuBar"];

                    CommandBarPopup c = (CommandBarPopup)menuBarCommandBar.Controls.Add(MsoControlType.msoControlPopup, System.Type.Missing, System.Type.Missing, 8, Properties.Resources.AppWindow);
                    c.Caption = "Internals Viewer";

                    AddCommand(commands, c, "AllocationMap", "Allocation Map", "Show the Allocation Map", Properties.Resources.allocationMapIcon, Properties.Resources.allocationMapIconMask);
                    AddCommand(commands, c, "TransactionLog", "Display Transaction Log", "Include the Transaction Log with query results", null, null);

                    IObjectExplorerEventProvider provider = ServiceCache.GetObjectExplorer().GetService(typeof(IObjectExplorerEventProvider)) as IObjectExplorerEventProvider;

                    provider.NodesRefreshed += new NodesChangedEventHandler(Provider_NodesRefreshed);
                    provider.NodesAdded += new NodesChangedEventHandler(Provider_NodesRefreshed);
                    provider.BufferedNodesAdded += new NodesChangedEventHandler(Provider_NodesRefreshed);

                    queryEditorExtender = new QueryEditorExtender(applicationObject);

                    this.windowManager = new WindowManager(applicationObject, addInInstance);

                    break;
            }
        }

        private void AddCommand(Commands2 commands, CommandBarPopup commandBar, string commandName, string caption, string description, Bitmap picture, Bitmap mask)
        {
            Command command = null;
            object[] contextGUIDS = null;

            try
            {
                command = commands.Item(addInInstance.ProgID + "." + commandName, 0);
            }
            catch
            {
            }

            if (command == null)
            {
                vsCommandStyle commandStyle = (picture == null) ? vsCommandStyle.vsCommandStyleText : vsCommandStyle.vsCommandStylePictAndText;

                command = commands.AddNamedCommand2(addInInstance,
                                                commandName,
                                                caption,
                                                description,
                                                true,
                                                null,
                                                ref contextGUIDS,
                                                (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled,
                                                (int)commandStyle,
                                                vsCommandControlType.vsCommandControlTypeButton);
            }

            CommandBarButton control = (CommandBarButton)command.AddControl(commandBar.CommandBar, 1);
            control.Caption = caption;

            if (picture != null)
            {
                control.Picture = (StdPicture)ImageConverter.GetIPictureDispFromImage(picture);
                control.Mask = (StdPicture)ImageConverter.GetIPictureDispFromImage(mask);
            }
        }

        void Provider_NodesRefreshed(object sender, NodesChangedEventArgs args)
        {
            Control objectExplorer = (sender as Control);

            if (objectExplorer.InvokeRequired)
            {
                objectExplorer.Invoke(new NodesChangedEventHandler(Provider_NodesRefreshed), new object[] { sender, args });

                return;
            }

            if (objectExplorerExtender == null)
            {
                objectExplorerExtender = new ObjectExplorerExtender();
            }
        }

        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
        }

        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnStartupComplete(ref Array custom)
        {
        }

        public void OnBeginShutdown(ref Array custom)
        {
        }

        /// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
        /// <param term='commandName'>The name of the command to determine state for.</param>
        /// <param term='neededText'>Text that is needed for the command.</param>
        /// <param term='status'>The state of the command in the user interface.</param>
        /// <param term='commandText'>Text requested by the neededText parameter.</param>
        /// <seealso class='Exec' />
        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
        {
            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
            {
                System.Diagnostics.Debug.Print(commandName);

                switch (commandName)
                {
                    case "InternalsViewer.SSMSAddIn.Connect.TransactionLog":

                        if (monitorTransactionLog)
                        {
                            status = (vsCommandStatus)vsCommandStatus.vsCommandStatusEnabled | vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusLatched;
                        }
                        else
                        {
                            status = (vsCommandStatus)vsCommandStatus.vsCommandStatusEnabled | vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                        }

                        break;

                    case "InternalsViewer.SSMSAddIn.Connect.AllocationMap":

                        status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                        break;
                }
            }
        }

        /// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
        /// <param term='commandName'>The name of the command to execute.</param>
        /// <param term='executeOption'>Describes how the command should be run.</param>
        /// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
        /// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
        /// <param term='handled'>Informs the caller if the command was handled or not.</param>
        /// <seealso class='Exec' />
        public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
        {
            handled = false;
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
            {
                switch (commandName)
                {
                    case "InternalsViewer.SSMSAddIn.Connect.TransactionLog":

                        monitorTransactionLog = !monitorTransactionLog;
                        handled = true;
                        return;

                    case "InternalsViewer.SSMSAddIn.Connect.AllocationMap":

                        AllocationWindow allocations = windowManager.CreateAllocationWindow();
                        
                        allocations.WindowManager = this.windowManager;

                        handled = true;
                        return;

                }
            }
        }

    }

    class ImageConverter : AxHost
    {

        // Methods

        internal ImageConverter()

            : base("52D64AAC-29C1-CAC8-BB3A-115F0D3D77CB")
        {
        }

        public static IPictureDisp GetIPictureDispFromImage(Image image)
        {
            return (IPictureDisp)AxHost.GetIPictureDispFromPicture(image);
        }

    }



}