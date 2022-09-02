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
using System.Xml;
using Newtonsoft.Json;

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
            pictureBox1.Location = new Point(this.Width / 2 - 64, this.Height / 2 - 64);
            button2.Enabled = false;
            comboBox1.SelectedIndex = 0;
        }

        private string GetRing(string selection)
        {
            switch (selection)
            {
                case "Retail": return "retail";
                case "Release Preview": return "RP";
                case "Insider Flow": return "WIS";
                case "Insider Fast": return "WIF";
                default: return "retail";
            }
        }

        private async Task<string> GetCookie(string appID, string ring)
        {
            HttpClient httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            FormUrlEncodedContent ucont = new FormUrlEncodedContent(new Dictionary<string, string> { { "type", "ProductId" }, { "url", appID }, { "ring", ring }, { "lang", System.Globalization.CultureInfo.InstalledUICulture.Name } });

            HttpContent httpContent = new StringContent(Properties.Resources.cookie);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml");

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response = await httpClient.PostAsync("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx", httpContent, cancel).ConfigureAwait(false);
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            string cookie = "";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseString);
            //htmlDoc2.LoadHtml(responseString2);
            foreach (XmlNode node in xmlDoc.GetElementsByTagName("EncryptedData"))
            {
                return node.InnerText;
            }
            httpClient.Dispose();
            return cookie;
        }

        private async Task<string> GetCategoryID(string appID)
        {
            HttpClient htpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            htpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response = await htpClient.GetAsync("https://storeedgefd.dsx.mp.microsoft.com/v9.0/products/" + appID + "?market=" + System.Globalization.CultureInfo.InstalledUICulture.Name.Remove(0, 3).ToUpper() + "&locale=" + System.Globalization.CultureInfo.InstalledUICulture.Name.ToLower() + "&deviceFamily=Windows.Desktop");
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            string data = "";

            htpClient.Dispose();

            JsonTextReader json = new JsonTextReader(new StringReader(responseString));
            while (json.Read())
            {
                if (json.Value != null)
                {
                    if (json.TokenType == JsonToken.PropertyName && (string)json.Value == "FulfillmentData")
                    {
                        json.Read();
                        data = json.Value.ToString().Replace("\\", "");
                        break;
                    }
                }
            }
            json = new JsonTextReader(new StringReader(data));
            while (json.Read())
            {
                if (json.Value != null)
                {
                    if (json.TokenType == JsonToken.PropertyName && (string)json.Value == "WuCategoryId")
                    {
                        json.Read();
                        return json.Value.ToString().Replace("\\", "");
                    }
                }
            }
            return null;
        }

        private async Task<string> GetFileListXML(string categoryID, string cookie, string ring)
        {
            string cookie2 = Properties.Resources.wu.Replace("{1}", cookie).Replace("{2}", categoryID).Replace("{3}", ring);

            HttpClient httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });

            HttpContent httpContent = new StringContent(cookie2);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response = await httpClient.PostAsync("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx", httpContent, cancel).ConfigureAwait(false);
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            httpClient.Dispose();
            return responseString.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        private async Task<List<PackageInfo>> GetPackages(string xmlList, string ring)
        {
            List<PackageInfo> packages = new List<PackageInfo>();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlList);
            Dictionary<string, string> packagesExt = new Dictionary<string, string>();

            foreach (XmlNode node in xmlDoc.DocumentElement.GetElementsByTagName("File"))
            {
                if (node.Attributes.GetNamedItem("InstallerSpecificIdentifier") != null)
                {
                    string name = node.Attributes.GetNamedItem("InstallerSpecificIdentifier").Value;
                    if (!packagesExt.ContainsKey(name))
                    {
                        packagesExt.Add(name, node.Attributes.GetNamedItem("FileName").Value.Remove(0, node.Attributes.GetNamedItem("FileName").Value.LastIndexOf('.')) + "|" + node.Attributes.GetNamedItem("Size").Value);
                    }
                }
            }

            foreach (XmlNode node in xmlDoc.DocumentElement.GetElementsByTagName("SecuredFragment"))
            {
                string name = "";
                if (node.ParentNode.ParentNode["ApplicabilityRules"]["Metadata"]["AppxPackageMetadata"]["AppxMetadata"].Attributes.GetNamedItem("PackageMoniker") != null)
                {
                    name = node.ParentNode.ParentNode["ApplicabilityRules"]["Metadata"]["AppxPackageMetadata"]["AppxMetadata"].Attributes.GetNamedItem("PackageMoniker").Value;
                    packages.Add(new PackageInfo(name,
                        packagesExt[name].Split('|')[0],
                        await GetUri(node.ParentNode.ParentNode["UpdateIdentity"].Attributes.GetNamedItem("UpdateID").Value,
                            node.ParentNode.ParentNode["UpdateIdentity"].Attributes.GetNamedItem("RevisionNumber").Value,
                            ring).ConfigureAwait(false),
                        node.ParentNode.ParentNode["UpdateIdentity"].Attributes.GetNamedItem("RevisionNumber").Value,
                        node.ParentNode.ParentNode["UpdateIdentity"].Attributes.GetNamedItem("UpdateID").Value,
                        node.ParentNode.ParentNode.ParentNode["ID"].InnerText,
                        long.Parse(packagesExt[name].Split('|')[1])));
                }
            }

            return packages;
        }

        public async Task<string> GetUri(string updateID, string revision, string ring)
        {
            HttpClient httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            HttpContent httpContent = new StringContent(Properties.Resources.url.Replace("{1}", updateID).Replace("{2}", revision).Replace("{3}", ring));
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response = await httpClient.PostAsync("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured", httpContent, cancel).ConfigureAwait(false);
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            XmlDocument xmlUri = new XmlDocument();
            xmlUri.LoadXml(responseString);

            httpClient.Dispose();

            return xmlUri.DocumentElement.GetElementsByTagName("Url")[0].InnerText;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string selectedRing = GetRing(comboBox1.SelectedItem.ToString());
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
            if (appID.Contains("/"))
                appID = appID.Remove(0, appID.LastIndexOf("/") + 1);
            if (appID.Contains("?"))
                appID = appID.Remove(appID.IndexOf("?"));
            string cookie = await GetCookie(appID, selectedRing).ConfigureAwait(false);
            string categoryID = await GetCategoryID(appID).ConfigureAwait(false);
            List<PackageInfo> packages = await GetPackages(await GetFileListXML(categoryID, cookie, selectedRing).ConfigureAwait(false), selectedRing).ConfigureAwait(false);

            //udělat comparer pro sortování podle jména
            packages.Sort();

            foreach (PackageInfo package in packages)
            {
                if (package.Extension.ToLower() == ".appx" || package.Extension.ToLower() == ".appxbundle" || package.Extension.ToLower() == ".msix" || package.Extension.ToLower() == ".msixbundle")
                {
                    ListViewItem lvi = new ListViewItem(new string[] { package.Name + package.Extension, ConvertSize(package.Size) });
                    lvi.Tag = package.Uri;
                    if (!InvokeRequired)
                    {
                        listView1.BeginUpdate();
                        listView1.Items.Add(lvi);
                        listView1.EndUpdate();
                    }
                    else
                    {
                        this.BeginInvoke(new Action(() => listView1.BeginUpdate()));
                        this.BeginInvoke(new Action(() => listView1.Items.Add(lvi)));
                        this.BeginInvoke(new Action(() => listView1.EndUpdate()));
                    }
                }
            }

            if (InvokeRequired)
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

        private string ConvertSize(double size)
        {
            List<string> jednotky = new List<string> { "B", "kB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            int nasobek = 0;
            while(size >= 1024)
            {
                size /= 1024;
                nasobek++;
            }
            return Math.Round(size, 2, MidpointRounding.AwayFromZero).ToString() + " " + jednotky[nasobek];
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
