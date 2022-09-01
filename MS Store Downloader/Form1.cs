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

            //test dotazu na WU
            HttpClient appIdHttp2 = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            FormUrlEncodedContent ucont2 = new FormUrlEncodedContent(new Dictionary<string, string> { { "type", "ProductId" }, { "url", appID }, { "ring", checkBox1.Checked ? "WIF" : "Retail" }, { "lang", System.Globalization.CultureInfo.InstalledUICulture.Name } });
            
            HttpContent httpContent = new StringContent(File.ReadAllText("./cookie.xml"));
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml");

            appIdHttp2.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response2 = await appIdHttp2.PostAsync("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx", httpContent, cancel).ConfigureAwait(false);
            string responseString2 = await response2.Content.ReadAsStringAsync().ConfigureAwait(false);
            string cookie = "";

            HtmlAgilityPack.HtmlDocument htmlDoc2 = new HtmlAgilityPack.HtmlDocument();
            htmlDoc2.LoadHtml(responseString2);
            foreach (HtmlNode node2 in htmlDoc2.DocumentNode.Descendants("EncryptedData"))
            {
                cookie = node2.InnerText;
                break;
            }
            HttpClient appIdHttp4 = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            appIdHttp4.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response4 = await appIdHttp4.GetAsync("https://storeedgefd.dsx.mp.microsoft.com/v9.0/products/" + appID + "?market=CZ&locale=cs-cz&deviceFamily=Windows.Desktop");
            string responseString4 = await response4.Content.ReadAsStringAsync().ConfigureAwait(false);
            string data = "";

            JsonTextReader json = new JsonTextReader(new StringReader(responseString4));
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
                        data = json.Value.ToString().Replace("\\", "");
                        break;
                    }
                }
            }

            string cookie2 = File.ReadAllText("wu.xml");
            cookie2 = cookie2.Replace("{1}", cookie).Replace("{2}", data).Replace("{3}", checkBox1.Checked ? "WIF" : "Retail");
            HttpClient appIdHttp3 = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true });
            
            HttpContent httpContent2 = new StringContent(cookie2);
            httpContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml"); 
            appIdHttp3.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response3 = await appIdHttp3.PostAsync("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx", httpContent2, cancel).ConfigureAwait(false);
            string responseString3 = await response3.Content.ReadAsStringAsync().ConfigureAwait(false);

            //tady číst response3 jako xml, hledat tagy xml, vzít jejich inner text a &gt;&lt; převést na ><.
            //Pak to načíst jako nový xml a hledat tagy Files, InstallerSpecificIdentifier (to je jméno balíku),
            // FileName (ukazuje appx, atd), SecuredFragment, UpdateID, RevisionNumber
            // a pomocí url.xml s tím pracovat dál na vygenerování linku pomocí cookie3 a http5

            string cookie3 = File.ReadAllText("wu.xml");
            cookie2 = cookie3.Replace("{1}", cookie).Replace("{2}", data).Replace("{3}", checkBox1.Checked ? "WIF" : "Retail");
            HttpClient appIdHttp5 = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, UseCookies = true }); 
            HttpContent httpContent3 = new StringContent(File.ReadAllText("./url.xml"));
            httpContent3.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml");
            appIdHttp5.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
            var response5 = await appIdHttp5.PostAsync("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured", httpContent3, cancel).ConfigureAwait(false);
            string responseString5 = await response5.Content.ReadAsStringAsync().ConfigureAwait(false);

            File.WriteAllText("./msapi.txt", responseString3);

            //konec testu dotazu WU
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
