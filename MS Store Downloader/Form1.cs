using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Net;
using System.Threading;
using Microsoft.Win32;
using System.Security;
using HtmlAgilityPack;

namespace MS_Store_Downloader
{
    public partial class Form1 : Form
    {
        CancellationToken cancel = new CancellationToken();
        public Form1()
        {
            InitializeComponent(); 
            if (!WBEmulator.IsBrowserEmulationSet(this))
            {
                WBEmulator.SetBrowserEmulationVersion(this);
            }
            pictureBox1.Location = new Point(this.Width/2 - 64, this.Height/2 - 64);
            button2.Enabled = false;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => pictureBox1.Visible = true));
            else
                pictureBox1.Visible = true;
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => listView1.Items.Clear()));
            else
                listView1.Items.Clear();
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button1.Enabled = false));
            else
                button1.Enabled = false;
            string appID = textBox1.Text;
            if(appID.Contains("/"))
                appID = appID.Remove(0, appID.LastIndexOf("/") + 1);
            if (appID.Contains("?"))
                appID = appID.Remove(appID.IndexOf("?"));
            HttpClient appIdHttp = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            FormUrlEncodedContent ucont = new FormUrlEncodedContent(new Dictionary<string, string> { { "type", "ProductId" }, { "url", appID }, { "ring", checkBox1.Checked ? "WIF" : "Retail" }, { "lang", System.Globalization.CultureInfo.InstalledUICulture.Name } });
                        
            appIdHttp.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response = await appIdHttp.PostAsync("https://store.rg-adguard.net/api/GetFiles", ucont , cancel).ConfigureAwait(false);
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            responseString = responseString.Replace("<head/>", "");
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(responseString);

            foreach (HtmlNode node in htmlDoc.DocumentNode.Descendants("a"))
            {
                string text = node.InnerText;
                string link = node.GetAttributeValue("href", "");
                if (text.ToLower().EndsWith(".appx") || text.ToLower().EndsWith(".appxbundle") || text.ToLower().EndsWith(".msixbundle") || text.ToLower().EndsWith(".msix"))
                {
                    ListViewItem lvi = new ListViewItem(text);
                    lvi.Tag = link;
                    if (InvokeRequired)
                        this.BeginInvoke(new Action(() => listView1.Items.Add(lvi)));
                    else
                        listView1.Items.Add(lvi);
                }
            }
            if(InvokeRequired)
                this.BeginInvoke(new Action(() => pictureBox1.Visible = false));
            else
                pictureBox1.Visible = false;

            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button2.Enabled = true));
            else
                button2.Enabled = true;
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button1.Enabled = true));
            else
                button1.Enabled = true;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            pictureBox1.Location = new Point(this.Width/2 - 64, this.Height/2 - 64);
            listView1.Columns[0].Width = listView1.Width - 27;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => pictureBox1.Visible = true));
            else
                pictureBox1.Visible = true;
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button1.Enabled = false));
            else
                button1.Enabled = false;
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button2.Enabled = false));
            else
                button2.Enabled = false;
            Dictionary<string, string> toDownload = new Dictionary<string, string>();
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string path = Path.GetDirectoryName(saveFileDialog1.FileName);
                if (listView1.SelectedItems == null || listView1.SelectedItems.Count == 0)
                {
                    foreach (ListViewItem lvi in listView1.Items)
                    {
                        toDownload.Add(lvi.Text, (string)lvi.Tag);
                    }
                }
                else
                {
                    foreach(ListViewItem lvi in listView1.SelectedItems)
                    {
                        toDownload.Add(lvi.Text, (string)lvi.Tag);
                    }
                }

                foreach (string s in toDownload.Keys)
                {
                    using (HttpClient download = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true }))
                    {
                        using (var result = await download.GetAsync(toDownload[s]).ConfigureAwait(false))
                        {
                            using (FileStream fs = new FileStream(path + "/" + s, FileMode.Create))
                            {
                                await result.Content.CopyToAsync(fs).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => pictureBox1.Visible = false));
            else
                pictureBox1.Visible = false;
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button1.Enabled = true));
            else
                button1.Enabled = true;
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button2.Enabled = true));
            else
                button2.Enabled = true;
        }
        
    }

    public enum BrowserEmulationVersion
    {
        Default = 0,
        Version7 = 7000,
        Version8 = 8000,
        Version8Standards = 8888,
        Version9 = 9000,
        Version9Standards = 9999,
        Version10 = 10000,
        Version10Standards = 10001,
        Version11 = 11000,
        Version11Edge = 11001
    }

    public static class WBEmulator
    {
        private const string InternetExplorerRootKey = @"Software\Microsoft\Internet Explorer";

        public static int GetInternetExplorerMajorVersion(Form1 form)
        {
            int result;

            result = 0;

            try
            {
                RegistryKey key;

                key = Registry.LocalMachine.OpenSubKey(InternetExplorerRootKey);

                if (key != null)
                {
                    object value;

                    value = key.GetValue("svcVersion", null) ?? key.GetValue("Version", null);

                    if (value != null)
                    {
                        string version;
                        int separator;

                        version = value.ToString();
                        separator = version.IndexOf('.');
                        if (separator != -1)
                        {
                            int.TryParse(version.Substring(0, separator), out result);
                        }
                    }
                }
            }
            catch (SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return result;
        }
        private const string BrowserEmulationKey = InternetExplorerRootKey + @"\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

        public static BrowserEmulationVersion GetBrowserEmulationVersion(Form1 form)
        {
            BrowserEmulationVersion result;

            result = BrowserEmulationVersion.Default;

            try
            {
                RegistryKey key;

                key = Registry.CurrentUser.OpenSubKey(BrowserEmulationKey, true);
                if (key != null)
                {
                    string programName;
                    object value;

                    programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                    value = key.GetValue(programName, null);

                    if (value != null)
                    {
                        result = (BrowserEmulationVersion)Convert.ToInt32(value);
                    }
                }
            }
            catch (SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return result;
        }
        public static bool SetBrowserEmulationVersion(BrowserEmulationVersion browserEmulationVersion, Form1 form)
        {
            bool result;

            result = false;

            try
            {
                RegistryKey key;

                key = Registry.CurrentUser.OpenSubKey(BrowserEmulationKey, true);

                if (key != null)
                {
                    string programName;

                    programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);

                    if (browserEmulationVersion != BrowserEmulationVersion.Default)
                    {
                        // if it's a valid value, update or create the value
                        key.SetValue(programName, (int)browserEmulationVersion, RegistryValueKind.DWord);
                    }
                    else
                    {
                        // otherwise, remove the existing value
                        key.DeleteValue(programName, false);
                    }

                    result = true;
                }
            }
            catch (SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return result;
        }

        public static bool SetBrowserEmulationVersion(Form1 form)
        {
            int ieVersion;
            BrowserEmulationVersion emulationCode;

            ieVersion = GetInternetExplorerMajorVersion(form);

            if (ieVersion >= 11)
            {
                emulationCode = BrowserEmulationVersion.Version11Edge;
            }
            else
            {
                switch (ieVersion)
                {
                    case 10:
                        emulationCode = BrowserEmulationVersion.Version10;
                        break;
                    case 9:
                        emulationCode = BrowserEmulationVersion.Version9;
                        break;
                    case 8:
                        emulationCode = BrowserEmulationVersion.Version8;
                        break;
                    default:
                        emulationCode = BrowserEmulationVersion.Version7;
                        break;
                }
            }

            return SetBrowserEmulationVersion(emulationCode, form);
        }
        public static bool IsBrowserEmulationSet(Form1 form)
        {
            return GetBrowserEmulationVersion(form) != BrowserEmulationVersion.Default;
        }
    }
}
