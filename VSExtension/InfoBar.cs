using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace VSExtension
{
    class InfoBarService : IVsInfoBarUIEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private uint _cookie;

        private InfoBarService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static InfoBarService Instance { get; private set; }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            Instance = new InfoBarService(serviceProvider);
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            infoBarUIElement.Unadvise(_cookie);
            
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            string context = (string)actionItem.ActionContext;

            if (context == "yes")
            {
                MessageBox.Show("You clicked Yes!");
            }
            else
            {
                MessageBox.Show("You clicked No!");
            }
        }
        IVsInfoBarUIElement element;
        public void ShowInfoBar(string message)
        {

            var shell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            if (shell != null)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                var host = (IVsInfoBarHost)obj;

                if (host == null)
                {
                    return;
                }
                InfoBarTextSpan text = new InfoBarTextSpan(message);
                //InfoBarHyperlink yes = new InfoBarHyperlink("Yes", "yes");
                //InfoBarHyperlink no = new InfoBarHyperlink("No", "no");

                IVsInfoBarTextSpan[] spans = new IVsInfoBarTextSpan[] { text };
                //InfoBarActionItem[] actions = new InfoBarActionItem[] { yes, no };
                InfoBarModel infoBarModel = new InfoBarModel(spans, KnownMonikers.StatusInformation, isCloseButtonVisible: true);

                var factory = _serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                element = factory.CreateInfoBar(infoBarModel);
                element.Advise(this, out _cookie);
                host.AddInfoBar(element);

            }
        }

        public void CloseInfoBar()
        {
            if (element != null)
            {
                var shell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
                if (shell != null)
                {
                    shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                    var host = (IVsInfoBarHost)obj;

                    if (host == null)
                    {
                        return;
                    }
                    element.Close();
                    host.RemoveInfoBar(element);
                    element = null;

                }
            }
        }
    }
}
