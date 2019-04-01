using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSExtension
{
    public class SolutionManager : IVsSolutionLoadManager
    {
        public int OnDisconnect()
        {
            return 0;
        }

        public int OnBeforeOpenProject(ref Guid guidProjectID, ref Guid guidProjectType, string pszFileName, IVsSolutionLoadManagerSupport pSLMgrSupport)
        {
            return 0;   
           
        }
    }
}
