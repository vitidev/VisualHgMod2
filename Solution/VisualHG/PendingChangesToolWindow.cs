using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HgLib;
using Microsoft.VisualStudio.Shell;
using VisualHg.Controls;

namespace VisualHg
{
    [Guid(Guids.ToolWindow)]
    public class PendingChangesToolWindow : ToolWindowPane
    {
        private PendingChangesView pendingChangesControl;

        public PendingChangesToolWindow()
        {
            pendingChangesControl = new PendingChangesView();

            Content = pendingChangesControl;
            Caption = Resources.ResourceManager.GetString("100");
        }

        public void SetFiles(HgFileInfo[] files)
        {
            pendingChangesControl.SetFiles(files);
        }
    }
}