using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
    //ms store ID 9wzdncrfjbmp
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
            panel1.Visible = false;
            button5.Enabled = false;
        }

        private string GetRing(string selection)
        {
            switch (selection)
            {
                case "Retail": return "retail";
                case "Release Preview": return "RP";
                case "Insider Slow": return "WIS";
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

            htpClient.Dispose();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                CategoryIDData categoryData = JsonConvert.DeserializeObject<CategoryIDData>(responseString);
                FulfillmentData fulfillmentData = null;
                if (categoryData.Payload.Skus.Count > 0 && categoryData.Payload.Skus[0].FulfillmentData != null)
                    fulfillmentData = JsonConvert.DeserializeObject<FulfillmentData>(categoryData.Payload.Skus[0].FulfillmentData);
                return fulfillmentData?.WuCategoryId;
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

        private async Task<List<PackageInfo>> GetNonAppxPackage(string appID)
        {
            HttpClient httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response = await httpClient.GetAsync("https://storeedgefd.dsx.mp.microsoft.com/v9.0/packageManifests/" + appID).ConfigureAwait(false);
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            httpClient.Dispose();

            string url = "";
            string type = "";
            string packageName = "";

            List<PackageInfo> packages = new List<PackageInfo>();

            PackageInfo pi = new PackageInfo("", "", "", "", "", appID, -1, "");

            NonUWPPackageDown nonUWPPackage = JsonConvert.DeserializeObject<NonUWPPackageDown>(responseString);

            if (nonUWPPackage != null)
            {
                foreach (NonUWPPackageDownVersions ver in nonUWPPackage.PackageData.Versions)
                {
                    foreach (NonUWPPackageInstaller inst in ver.Installers)
                    {
                        if (inst.InstallerType == "" || inst.InstallerUrl.ToLower().EndsWith(".exe") || inst.InstallerUrl.ToLower().EndsWith(".msi"))
                        {
                            packages.Add(new PackageInfo(inst.InstallerUrl.Remove(inst.InstallerUrl.LastIndexOf('.')).Remove(0, inst.InstallerUrl.LastIndexOf('/') + 1), inst.InstallerUrl.Remove(0, inst.InstallerUrl.LastIndexOf('.')), inst.InstallerUrl, "", "", appID, -1, ""));
                        }
                        else
                        {
                            string name = "";
                            if (inst.AppsAndFeaturesEntries != null)
                            {
                                if (inst.AppsAndFeaturesEntries[0].DisplayName != null)
                                {
                                    name = inst.AppsAndFeaturesEntries[0].DisplayName;
                                }
                            }
                            else if (ver.DefaultLocale != null && ver.DefaultLocale.PackageName != null)
                            {
                                name = ver.DefaultLocale.PackageName;
                            }
                            packages.Add(new PackageInfo(name + " (" + inst.InstallerLocale + ")", "." + inst.InstallerType, inst.InstallerUrl, "", "", appID, -1, ""));
                        }
                    }
                }
                return packages;
            }
            return null;
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
                    string digest = node.Attributes.GetNamedItem("Digest").Value;
                    if (!packagesExt.ContainsKey(name))
                    {
                        packagesExt.Add(name, node.Attributes.GetNamedItem("FileName").Value.Remove(0, node.Attributes.GetNamedItem("FileName").Value.LastIndexOf('.')) + "|" + node.Attributes.GetNamedItem("Size").Value + "|" + digest);
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
                            ring, packagesExt[name].Split('|')[2]).ConfigureAwait(false),
                        node.ParentNode.ParentNode["UpdateIdentity"].Attributes.GetNamedItem("RevisionNumber").Value,
                        node.ParentNode.ParentNode["UpdateIdentity"].Attributes.GetNamedItem("UpdateID").Value,
                        node.ParentNode.ParentNode.ParentNode["ID"].InnerText,
                        long.Parse(packagesExt[name].Split('|')[1]),
                        packagesExt[name].Split('|')[2]));
                }
            }

            return packages;
        }

        public async Task<string> GetUri(string updateID, string revision, string ring, string digets)
        {
            HttpClient httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            HttpContent httpContent = new StringContent(Properties.Resources.url.Replace("{1}", updateID).Replace("{2}", revision).Replace("{3}", ring));
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response = await httpClient.PostAsync("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured", httpContent, cancel).ConfigureAwait(false);
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            XmlDocument xmlUri = new XmlDocument();

            if(response.StatusCode == HttpStatusCode.OK)
            { 
                xmlUri.LoadXml(responseString);

                httpClient.Dispose();

                foreach (XmlNode node in xmlUri.DocumentElement.GetElementsByTagName("FileLocation"))
                {
                    if (node["FileDigest"].InnerText == digets)
                        return node["Url"].InnerText;
                }

                return null;
            }
            else
            {
                httpClient.Dispose();
                return null;
            }
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

            if(packages.Count == 0)
            {
                //pokud není WuCategoryID
                packages = await GetNonAppxPackage(appID);
            }

            if(packages == null)
            {
                MessageBox.Show("Application not found or it is paid appliocation. This program is unable to retreive paid apps and gaimes. It can be used only for free apps.");
                return;
            }

            //udělat comparer pro sortování podle jména
            packages.Sort();
            bool service = false;

            foreach (PackageInfo package in packages)
            {
                if (package.Extension.ToLower() == ".appx" || package.Extension.ToLower() == ".appxbundle" || package.Extension.ToLower() == ".msix" || package.Extension.ToLower() == ".msixbundle" || package.Extension.ToLower() == ".exe" || package.Extension.ToLower() == ".msi"
                    || package.Extension.ToLower() == ".eappx" || package.Extension.ToLower() == ".eappxbundle" || package.Extension.ToLower() == ".emsix" || package.Extension.ToLower() == ".emsixbundle")
                {
                    if (package.Uri != null)
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
                    else
                        service = true;
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

            if (service)
                MessageBox.Show("There was problem with communication with MS servers. Some packages may be missing in result. Try to search again later", "Communication problem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private string ConvertSize(double size)
        {
            if (size >= 0)
            {
                List<string> jednotky = new List<string> { "B", "kB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

                int nasobek = 0;
                while (size >= 1024)
                {
                    size /= 1024;
                    nasobek++;
                }
                return Math.Round(size, 2, MidpointRounding.AwayFromZero).ToString() + " " + jednotky[nasobek];
            }
            else
                return "unknown";
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            /*pictureBox1.Location = new Point(this.Width/2 - 64, this.Height/2 - 64);
            listView1.Columns[0].Width = listView1.Width - 128;
            listView2.Columns[1].Width = ((listView2.Width - 137) / 2) - 28 + 50;
            listView2.Columns[2].Width = ((listView2.Width - 137) / 2) - 28;*/
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

        private void button3_Click(object sender, EventArgs e)
        {
            panel1.Visible = true;
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => pictureBox1.Visible = true));
            else
                pictureBox1.Visible = true; 
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button4.Enabled = false));
            else
                button4.Enabled = false;
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button6.Enabled = false));
            else
                button6.Enabled = false;

            //hledání appek na https://storeedgefd.dsx.mp.microsoft.com/v9.0/manifestSearch
            //https://github.com/ThomasPe/MS-Store-API/blob/master/endpoints/v9.0/manifestSearch.md
            //vytvoření JSON query
            string jsonText = "{\"Query\": {\"KeyWord\": \"" + textBox2.Text + "\",\"MatchType\": \"Substring\"}}";
            HttpClient httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            HttpContent httpContent = new StringContent(jsonText);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response = await httpClient.PostAsync("https://storeedgefd.dsx.mp.microsoft.com/v9.0/manifestSearch", httpContent, cancel).ConfigureAwait(false);
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            httpClient.Dispose();

            List<string[]> results = new List<string[]>();
            NonUWPPackageData nonUWPPackage = JsonConvert.DeserializeObject<NonUWPPackageData>(responseString);
            foreach(NonUWPPackageJson njs in  nonUWPPackage.Data)
            {
                results.Add(new string[] { njs.PackageIdentifier, njs.PackageName, njs.Publisher });
            }

            
            if (!InvokeRequired)
                listView2.Items.Clear();
            else
                BeginInvoke(new Action(() => listView2.Items.Clear()));
            foreach (string[] s in results)
            {
                ListViewItem lvi = new ListViewItem(s);
                if (!InvokeRequired)
                    listView2.Items.Add(lvi);
                else
                    BeginInvoke(new Action(() => listView2.Items.Add(lvi)));
            }
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => pictureBox1.Visible = false));
            else
                pictureBox1.Visible = false;
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button4.Enabled = true));
            else
                button4.Enabled = true;
            if (InvokeRequired)
                this.BeginInvoke(new Action(() => button6.Enabled = true));
            else
                button6.Enabled = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox1.Text = listView2.SelectedItems[0].Text;
            panel1.Visible = false;
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count != 0)
                button5.Enabled = true;
            else
                button5.Enabled=false;
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
