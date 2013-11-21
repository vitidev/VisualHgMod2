using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HgLib;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualHg
{
    [InstalledProductRegistration("#100", "#101", "2.0.0.0", IconResourceID = 400)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource(1000, 1)]
    [ProvideAutoLoad(Guids.Provider)]
    [ProvideService(typeof(VisualHgService))]
    [ProvideToolWindow(typeof(PendingChangesToolWindow))]
    [ProvideToolWindowVisibility(typeof(PendingChangesToolWindow), Guids.Provider)]
    [ProvideSolutionPersistence("VisualHg", Guids.Package)]
    [ProvideSourceControlProvider("VisualHg", Guids.Provider, Guids.Service, Guids.Package)]
    [ProvideOptionPage(typeof(VisualHgOptionsPage), "Source Control", "VisualHg", 106, 107, false)]
    [ProvideOptionsPageVisibility("Source Control", "VisualHg", Guids.Provider)]
    [Guid(Guids.Package)]
    public sealed partial class VisualHgPackage : Package, IOleCommandTarget
    {
        private const int OLECMDERR_E_NOTSUPPORTED = (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;

        private VisualHgService visualHgService;
        private PendingChangesToolWindow _pendingChangesToolWindow;


        public void UpdatePendingChangesToolWindow()
        {
            if (_pendingChangesToolWindow == null)
            {
                _pendingChangesToolWindow = FindToolWindow(typeof(PendingChangesToolWindow), 0, true) as PendingChangesToolWindow;
            }

            _pendingChangesToolWindow.SetFiles(visualHgService.PendingFiles);
        }

        protected override void Dispose(bool disposing)
        {
            visualHgService.Dispose();
            base.Dispose(disposing);
        }


        private void NotifySolutionIsNotUnderVersionControl()
        {
            MessageBox.Show("Solution is not under Mercurial version contol\n\n" + VisualHgSolution.SolutionFileName, "VisualHg", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        protected override void Initialize()
        {
            base.Initialize();

            ((IServiceContainer)this).AddService(typeof(System.IServiceProvider), this, true);

            visualHgService = new VisualHgService();
            ((IServiceContainer)this).AddService(typeof(VisualHgService), visualHgService, true);

            InitializeMenuCommands();

            var rscp = GetService(typeof(IVsRegisterScciProvider)) as IVsRegisterScciProvider;
            rscp.RegisterSourceControlProvider(Guids.ProviderGuid);
        }

        private void InitializeMenuCommands()
        {
            var menuCommandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (menuCommandService != null)
            {
                AddMenuCommands(menuCommandService);
            }
        }

        private void AddMenuCommands(OleMenuCommandService menuCommandService)
        {
            var commandId = new CommandID(Guids.CommandSetGuid, CommandId.PendingChanges);
            var command = new MenuCommand(ShowPendingChangesToolWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.Commit);
            command = new MenuCommand(ShowCommitWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.Workbench);
            command = new MenuCommand(ShowWorkbenchWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.Status);
            command = new MenuCommand(ShowStatusWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.Synchronize);
            command = new MenuCommand(ShowSynchronizeWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.Update);
            command = new MenuCommand(ShowUpdateWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.Add);
            command = new MenuCommand(ShowAddSelectedWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.CommitSelected);
            command = new MenuCommand(ShowCommitSelectedWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.Diff);
            command = new MenuCommand(ShowDiffWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.Revert);
            command = new MenuCommand(ShowRevertWindow, commandId);
            menuCommandService.AddCommand(command);

            commandId = new CommandID(Guids.CommandSetGuid, CommandId.History);
            command = new MenuCommand(ShowHistoryWindow, commandId);
            menuCommandService.AddCommand(command);
        }


        public int QueryStatus(ref Guid commandSetGuid, uint commandCount, OLECMD[] commands, IntPtr text)
        {
            if (commandCount != 1)
            {
                return VSConstants.E_INVALIDARG;
            }

            if (commandSetGuid != Guids.CommandSetGuid)
            {
                return OLECMDERR_E_NOTSUPPORTED;
            }

            var visible = visualHgService.Active ? IsCommandVisible(commands[0].cmdID) : false;

            commands[0].cmdf = (uint)VisibleToOleCmdf(visible);

            return VSConstants.S_OK;
        }

        private OLECMDF VisibleToOleCmdf(bool visible)
        {
            return OLECMDF.OLECMDF_SUPPORTED | (visible ? OLECMDF.OLECMDF_ENABLED : OLECMDF.OLECMDF_INVISIBLE);
        }

        private bool IsCommandVisible(uint commandId)
        {
            switch (commandId)
            {
                case CommandId.Add:
                    return IsAddMenuItemVisible();

                case CommandId.CommitSelected:
                    return IsCommitSelectedMenuItemVisible();

                case CommandId.Diff:
                    return IsDiffMenuItemVisible();

                case CommandId.Revert:
                    return IsRevertMenuItemVisible();

                case CommandId.History:
                    return IsHistoryMenuItemVisible();

                default:
                    return true;
            }
        }

        private bool IsAddMenuItemVisible()
        {
            return VisualHgSolution.SearchAnySelectedFileStatusMatches(HgFileStatus.NotAdded);
        }

        private bool IsCommitSelectedMenuItemVisible()
        {
            return VisualHgSolution.SearchAnySelectedFileStatusMatches(HgFileStatus.Pending);
        }

        private bool IsDiffMenuItemVisible()
        {
            return VisualHgSolution.SelectedFileStatusMatches(HgFileStatus.Comparable);
        }

        private bool IsRevertMenuItemVisible()
        {
            return VisualHgSolution.AnySelectedFileStatusMatches(HgFileStatus.Pending, false);
        }

        private bool IsHistoryMenuItemVisible()
        {
            return VisualHgSolution.SelectedFileStatusMatches(HgFileStatus.Tracked);
        }


        private void ShowPendingChangesToolWindow(object sender, EventArgs e)
        {
            if (_pendingChangesToolWindow == null)
            {
                UpdatePendingChangesToolWindow();
            }

            var windowFrame = _pendingChangesToolWindow.Frame as IVsWindowFrame;

            if (windowFrame != null)
            {
                ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
        }


        private void ShowCommitWindow(object sender, EventArgs e)
        {
            GetRootAnd(TortoiseHg.ShowCommitWindow);
        }

        private void ShowWorkbenchWindow(object sender, EventArgs e)
        {
            GetRootAnd(TortoiseHg.ShowWorkbenchWindow);
        }

        private void ShowStatusWindow(object sender, EventArgs e)
        {
            GetRootAnd(TortoiseHg.ShowStatusWindow);
        }

        private void ShowSynchronizeWindow(object sender, EventArgs e)
        {
            GetRootAnd(TortoiseHg.ShowSynchronizeWindow);
        }

        private void ShowUpdateWindow(object sender, EventArgs e)
        {
            GetRootAnd(TortoiseHg.ShowUpdateWindow);
        }

        private void GetRootAnd(Action<string> showWindow)
        {
            var root = VisualHgSolution.CurrentRootDirectory;

            if (!String.IsNullOrEmpty(root))
            {
                showWindow(root);
            }
            else
            {
                NotifySolutionIsNotUnderVersionControl();
            }
        }


        private void ShowAddSelectedWindow(object sender, EventArgs e)
        {
            var filesToAdd = VisualHgSolution.GetSelectedFiles(true).Where(VisualHgFileStatus.IsNotAdded).ToArray();

            if (filesToAdd.Length > 0)
            {
                TortoiseHg.ShowAddWindow(filesToAdd);
            }
        }

        private void ShowCommitSelectedWindow(object sender, EventArgs e)
        {
            VisualHgDialogs.ShowCommitWindow(VisualHgSolution.GetSelectedFiles(true));
        }

        private void ShowDiffWindow(object sender, EventArgs e)
        {
            VisualHgDialogs.ShowDiffWindow(VisualHgSolution.SelectedFile);
        }

        private void ShowRevertWindow(object sender, EventArgs e)
        {
            VisualHgDialogs.ShowRevertWindow(VisualHgSolution.GetSelectedFiles(false));
        }

        private void ShowHistoryWindow(object sender, EventArgs e)
        {
            VisualHgDialogs.ShowHistoryWindow(VisualHgSolution.SelectedFile);
        }
    }
}