﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HgLib;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualHg
{
    public partial class SccProviderService
    {
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            if (!Active && Configuration.Global.AutoActivatePlugin)
            {
                var root = _sccProvider.SolutionRootDirectory;
                
                if (!String.IsNullOrEmpty(root))
                {
                    var rscp = _sccProvider.GetService(typeof(IVsRegisterScciProvider)) as IVsRegisterScciProvider;
                    
                    rscp.RegisterSourceControlProvider(Guids.ProviderGuid);
                }
            }

            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            Repository.Clear();
            _sccProvider.LastSeenProjectDirectory = "";
            
            UpdatePendingChangesToolWindow();

            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            _sccProvider.LastSeenProjectDirectory = SccProvider.GetDirectoryName(pRealHierarchy);

            var project = pRealHierarchy as IVsSccProject2;

            if (project != null)
            {
                Repository.UpdateProject(project);
            }

            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            var project = pHierarchy as IVsSccProject2;
                        
            var files = Repository.AddSolutionFiles(pHierarchy);

            foreach (var root in files.Select(x => HgPath.FindRepositoryRoot(x)).Distinct())
            {
                Repository.UpdateRootStatus(root);
            }

            if (Configuration.Global.AddFilesOnLoad)
            {
                Repository.AddFiles(files);
            }
            
            _sccProvider.LastSeenProjectDirectory = SccProvider.GetDirectoryName(pHierarchy);

            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            Repository.RemoveSolutionFiles(pHierarchy);
                        
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            Repository.ClearSolutionFiles();

            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            Repository.RemoveSolutionFiles(pRealHierarchy);

            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterMergeSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }


        public int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, [Out] VSQUERYADDFILERESULTS[] pSummaryResult, [Out] VSQUERYADDFILERESULTS[] rgResults)
        {
            Repository.FileSystemWatch = false;
            
            return VSConstants.S_OK;
        }

        public int OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
        {
            Repository.FileSystemWatch = true;

            if (Configuration.Global.AutoAddNewFiles)
            {
                Repository.AddFiles(rgpszMkDocuments);
            }

            return VSConstants.S_OK;
        }

        public int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, [Out] VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, [Out] VSQUERYADDDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, [Out] VSQUERYREMOVEFILERESULTS[] pSummaryResult, [Out] VSQUERYREMOVEFILERESULTS[] rgResults)
        {
            Repository.FileSystemWatch = false;

            return VSConstants.S_OK;
        }

        public int OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
        {
            Repository.FileSystemWatch = true;

            if (rgpProjects == null || rgpszMkDocuments == null)
            {
                return VSConstants.E_POINTER;
            }

            if (!File.Exists(rgpszMkDocuments[0]))
            {
                Repository.RemoveFiles(rgpszMkDocuments);
            }

            return VSConstants.S_OK;
        }

        public int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, [Out] VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, [Out] VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, [Out] VSQUERYRENAMEFILERESULTS[] pSummaryResult, [Out] VSQUERYRENAMEFILERESULTS[] rgResults)
        {
            Repository.FileSystemWatch = false;
            
            return VSConstants.S_OK;
        }

        public int OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
        {
            Repository.FileSystemWatch = true;
            Repository.RenameFiles(rgszMkOldNames, rgszMkNewNames);
            
            return VSConstants.S_OK;
        }

        public int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, [Out] VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, [Out] VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
}