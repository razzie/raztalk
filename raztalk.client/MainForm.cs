using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace raztalk.client
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            web.ScriptErrorsSuppressed = true;
            SHDocVw.WebBrowser wbCOMmain = (SHDocVw.WebBrowser)web.ActiveXInstance;
            wbCOMmain.NewWindow3 += wbCOMmain_NewWindow3;
        }

        void wbCOMmain_NewWindow3(ref object ppDisp,
                          ref bool Cancel,
                          uint dwFlags,
                          string bstrUrlContext,
                          string bstrUrl)
        {
            // bstrUrl is the url being navigated to
            Cancel = true; // stop the navigation

            // Do whatever else you want to do with that URL
            // open in the same browser or new browser, etc.
            OpenUrl(bstrUrl);
        }

        static private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
