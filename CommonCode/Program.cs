extern alias epi;
using CommandLine;
using Ice.Core;
using Ice.Lib.Framework;
using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using Ice.Lib.Searches;
using Ice.BO;
using System.IO;
using Ice.Proxy.BO;
using System.Data;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Ice.Lib;
using Ice.Lib.Deployment;
using CommonForms.Properties;
using System.Security.Cryptography;
using System.Xml.Linq;
using Ice.Lib.SecRights;
using CommonCode;

namespace CustomizationEditor
{
    class Program
    {
        private static Thread progBarThread;
        private static string currAction;
        private static EpicorLauncher launcher;
        [STAThread]

        //Main Program Launched from VS Code
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Session epiSession = null;
            bool reSync = false;
            
            Parser.Default.ParseArguments<CommandLineParams>(args)
                   .WithParsed(o =>
                   {
                       currAction = o.Action;
                       ShowProgressBar();

                       switch (o.Action)
                       {
                           case "Launch":
                               {
                                   epiSession = GetEpiSession(o);
                                   if (epiSession != null)
                                       launcher.LaunchInEpicor(o, epiSession, false, true);
                                
                               }
                               break;
                           case "Add":
                               {
                                   ShowProgressBar(false);
                                   LoginForm frm = new LoginForm(o.EpicorClientFolder);
                                   if(frm.ShowDialog() == DialogResult.OK)
                                   {
                                       ShowProgressBar();
                                       o.Username = Settings.Default.Username;
                                       o.Password = Settings.Default.Password; 
                                       o.ConfigFile = Settings.Default.Environment;
                                       o.Encrypted = "true";
                                       
                                       epiSession = GetEpiSession(o);
                                       if (epiSession != null)
                                       {
                                           DownloadCustomization(o, epiSession);
                                           reSync = true;
                                       }
                                       else
                                        reSync = false;
                                   }
                                   else
                                    reSync = false;
                                   
                               }
                               break;
                           case "Update":
                               {
                                   epiSession = GetEpiSession(o);
                                   if (epiSession != null)
                                   {
                                       reSync = true;
                                       if (!UpdateCustomization(o, epiSession))
                                       {
                                           if (MessageBox.Show("You've canceled the sync operation, would you like to download the latest copy of the customization from Epicor? This will over-write any changes you have made to the custom script since the last sync.", "Re-Download?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                                               reSync = false;
                                       }
                                   }
                                   else
                                       reSync = false;
                               }
                               break;
                           case "Download":
                               {
                                   epiSession = GetEpiSession(o);
                                   if (epiSession != null)
                                   {
                                       reSync = true;
                                   }
                                   else
                                       reSync = false;
                               }
                               break;
                           case "Edit":
                               {
                                   epiSession = GetEpiSession(o);
                                   if (epiSession != null)
                                   {
                                       launcher.LaunchInEpicor(o, epiSession, true);
                                       reSync = true;
                                   }
                                   else
                                       reSync = false;
                               }
                               break;
                           case "Debug":
                               {
                                   epiSession = GetEpiSession(o);
                                   if (epiSession != null)
                                   {
                                       if (!string.IsNullOrEmpty(o.DNSpy))
                                       {
                                           RunDnSpy(o);
                                       }

                                       launcher.LaunchInEpicor(o, epiSession, false, true);
                                   }
                               }
                               break;
                           case "Toolbox":
                               {
                                   epiSession = GetEpiSession(o);

                                   if (epiSession != null)
                                   {
                                       ShowProgressBar(false);
                                       NonModalWokIt win = new NonModalWokIt(epiSession, o);
                                       win.ShowDialog();
                                       reSync = win.Sync;
                                       ShowProgressBar(true);
                                   }
                                   
                               }
                               break;
                           
                       }
                       if(reSync)
                       {
                           if(o.Key2.Contains("MainController"))//Dashboard
                            launcher.DownloadAndSyncDashboard(epiSession, o);
                           else
                            launcher.DownloadAndSync(epiSession, o);
                       }
                       ShowProgressBar(false);
                       
                       if (epiSession != null)
                       {
                           epiSession.OnSessionClosing();

                           epiSession = null;

                       }
                       if(!string.IsNullOrEmpty(o.NewConfig))
                       {
                           try
                           {
                               File.Delete(o.NewConfig);
                           }
                           catch{ }
                       }
                       if (!string.IsNullOrEmpty(o.Temp))
                       {
                           try
                           {
                               Directory.Delete(o.Temp, true);
                           }
                           catch { }
                       }

                   });
        }

        /// <summary>
        /// Encrypts Epicor Password if it wasn't already, this uses a the DataProtectionScope of CurrentUser
        /// </summary>
        /// <param name="o">Command Line Parameter</param>
        private static void ConvertToEncrypted(CommandLineParams o)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(o.Password);
            byte[] protectedPassword = ProtectedData.Protect(bytes, Encoding.Unicode.GetBytes("70A47403717EC0F50E0755B2C4CF8488C8A061F3A694E0D1AB336D672C21781A"), DataProtectionScope.CurrentUser);
            string encryptedString = Convert.ToBase64String(protectedPassword);

            Settings.Default.Password = encryptedString;
            Settings.Default.Encrypted = true;
            Settings.Default.Save();
        }
        /// <summary>
        /// Self explinatory
        /// </summary>
        /// <param name="iFlag"></param>
        private static void ShowProgressBar(bool iFlag = true)
        {
            if (iFlag)
            {
                progBarThread = new Thread(() => new ProgressForm($"{currAction}ing Project... Please Wait").ShowDialog());
                progBarThread.Start();
            }
            else
            {
                if (progBarThread != null)
                {
                    try
                    {
                        progBarThread.Abort();
                        progBarThread = null;
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Launches DNSpy
        /// Deprecated will be removed / replaced shortly.
        /// </summary>
        /// <param name="o"></param>
        private static void RunDnSpy(CommandLineParams o)
        {
            new Thread(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = "dnSpy-x86.exe";
                process.StartInfo.WorkingDirectory = o.DNSpy;
                StringBuilder arguments = new StringBuilder();
                arguments.Append($"--process-name CustomizationEditor.exe --files {o.DLLLocation} --search-for class --search-in selected --search Script --select T:Script");
                process.StartInfo.Arguments = arguments.ToString();
                process.Start();
                process.WaitForExit();
            }).Start();
        }
        
        /// <summary>
        /// Updates Epicor Customization
        /// Deprecated will be replaced shortly.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="epiSession"></param>
        /// <returns></returns>
        private static bool UpdateCustomization(CommandLineParams o, Session epiSession)
        {
      
            using (StreamReader sr = new StreamReader($@"{o.ProjectFolder}\Script.cs"))
            {
                var oTrans = new ILauncher(epiSession);
                Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(oTrans);
                ad.BOConnect();

                GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
                string script = (sr.ReadToEnd().Replace("public partial class Script", "public class Script").Replace("public static partial class Script", "public static class Script").EscapeXml());
                var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                var chunk = ds.XXXDef[0];
                if(chunk.SysRevID != o.Version && o.Version>0)
                {
                    if (MessageBox.Show("The customization appears to have been updated internally within Epicor, this means that if you continue you may be over-writing some changes made. Would you like to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        return false;
                    }
                     
                }
                string content = ad.GetDechunkedStringByIDWithCompany(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);

                string newC = Regex.Replace(content, @"(?=\/\/ \*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*)[\s\S]*?(?=<\/PropertyValue>)", script,
                              RegexOptions.IgnoreCase);
                ad.ChunkNSaveUncompressedStringByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3, chunk.Description, chunk.Version, false, newC);
            }
            return true;
        }
        

        /// <summary>
        /// Downloads Epicor Customization
        /// Deprecated will be replaced shortly.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="epiSession"></param>
        private static void DownloadCustomization(CommandLineParams o, Session epiSession)
        {
            EpiTransaction epiTransaction = new EpiTransaction(new ILauncher(epiSession));
            Ice.UI.App.CustomizationMaintEntry.Transaction oTrans = new Ice.UI.App.CustomizationMaintEntry.Transaction(epiTransaction);
            Ice.UI.App.CustomizationMaintEntry.CustomizationMaintForm custData = new Ice.UI.App.CustomizationMaintEntry.CustomizationMaintForm(oTrans);

            oTrans = (Ice.UI.App.CustomizationMaintEntry.Transaction)custData.GetType().GetField("trans", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(custData);

            SearchOptions opts = new SearchOptions(SearchMode.ShowDialog)
            {
                DataSetMode = DataSetMode.RowsDataSet,
                SelectMode = SelectMode.SingleSelect,
                PreLoadSearchFilter= "TypeCode = 'Customization'"

            };
            oTrans.InvokeSearch(opts);
            EpiDataView edvxxDef = (EpiDataView)oTrans.EpiDataViews["xxxDef"];
            if(edvxxDef.Row>=0)
            {
                GenXDataDataSet.XXXDefRow r = (GenXDataDataSet.XXXDefRow) edvxxDef.CurrentDataRow;
                o.Company = r.Company;
                o.CSGCode = r.CGCCode;
                o.Key1 = r.Key1;
                o.Key2 = r.Key2;
                o.Key3 = r.Key3;
                o.LayerType = r.TypeCode;
                o.ProductType = r.ProductID;
            }

        }
        
        /// <summary>
        /// Creates an Epicor Session
        /// This is a bit of a heavy hitter, it creates a copy of the current system configuration and copies it to a temporary location
        /// then sets an alternate cache folder in order for us to have our own unique undisturbed cache
        /// Then it creates an epicor session and instanciates all the apropriate static libraries to allow
        /// epicor to operate as if it was running a full client, there's quite a bit in here that you probably won't understand without
        /// digging through the Epicor.exe and other libraries. Just go with it
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private static Session GetEpiSession(CommandLineParams o)
        {
            string password = o.Password;
            Session ses = null;

            //Create a copy of the config file so that we can set a temporary cache folder for our instance
            var newConfig = Path.GetTempFileName().Replace(".tmp", ".sysconfig");
            File.Copy(o.ConfigFile, newConfig, true);
            o.NewConfig = newConfig;

            //Create a temp directory to store our epicor cache
            DirectoryInfo di = Directory.CreateDirectory(Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString()));
            o.Temp = di.FullName;
            var x =XElement.Load(newConfig);

            //Set our new Temp Cache location in our new config
            x.Descendants("appSettings").Elements().Where(r => r.Name == "AlternateCacheFolder").FirstOrDefault().FirstAttribute.Value = di.FullName;
            x.Save(newConfig);

            try
            {
                password = NeedtoEncrypt(o);
                ses = new Session(o.Username, password, Session.LicenseType.Default, newConfig);
            }
            catch (Exception ex)
            {
                ShowProgressBar(false);
                MessageBox.Show("Failed to Authenticate","Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                
                LoginForm frm = new LoginForm(o.EpicorClientFolder);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    ShowProgressBar();
                    o.Username = Settings.Default.Username;
                    o.Password = Settings.Default.Password;
                    o.ConfigFile = Settings.Default.Environment;
                    o.Encrypted = "true";
                    password = NeedtoEncrypt(o);
                    
                    try
                    {
                        ses = new Session(o.Username, password, Session.LicenseType.Default, newConfig);
                    }
                    catch(Exception)
                    {
                        MessageBox.Show("Failed to Authenticate", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ses = null;
                    }
                    ShowProgressBar(true);
                }
                
            }

            if (ses != null)
            {
                SecRightsHandler.CacheBOSecSettings(ses);

                dynamic curMRUList= typeof(SecRightsHandler).GetField("_currMRUList", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

                Startup.SetupPlugins(ses);

                if (!string.IsNullOrEmpty(o.DLLLocation))
                {
                    String fineName = Path.GetFileName(o.DLLLocation);
                    string newPath = Path.GetDirectoryName((string)curMRUList.GetType().GetProperty("SavePath", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic| BindingFlags.Public).GetValue(curMRUList)).Replace("BOSecMRUList", "CustomDLLs");
                    o.DLLLocation = Path.Combine(newPath, fineName);
                }

                epi.Ice.Lib.Configuration c = new epi.Ice.Lib.Configuration(newConfig);
                
                c.AlternateCacheFolder = di.FullName;
                Assembly assy = Assembly.LoadFile(Path.Combine(o.EpicorClientFolder, "Epicor.exe"));
                TypeInfo ty = assy.DefinedTypes.Where(r => r.Name == "ConfigureForAutoDeployment").FirstOrDefault();
                dynamic thing = Activator.CreateInstance(ty);

                object[] args = { c };
                thing.GetType().GetMethod("SetUpAssemblyRetrieversAndPossiblyGetNewConfiguration", BindingFlags.Instance | BindingFlags.Public).Invoke(thing, args);
                WellKnownAssemblyRetrievers.AutoDeployAssemblyRetriever = (IAssemblyRetriever)thing.GetType().GetProperty("AutoDeployAssemblyRetriever", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(thing);
                WellKnownAssemblyRetrievers.SessionlessAssemblyRetriever = (IAssemblyRetriever)thing.GetType().GetProperty("SessionlessAssemblyRetriever", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(thing);
                
                Startup.PreStart(ses, true);
                launcher = new EpicorLauncher();
                
            }
            return ses;
        }

        private static string NeedtoEncrypt(CommandLineParams o)
        {
            string password;
            if (bool.Parse(o.Encrypted))
            {
                password = Encoding.Unicode.GetString(ProtectedData.Unprotect(Convert.FromBase64String(o.Password), Encoding.Unicode.GetBytes("70A47403717EC0F50E0755B2C4CF8488C8A061F3A694E0D1AB336D672C21781A"), DataProtectionScope.CurrentUser));
            }
            else
            {
                password = o.Password;
                EncryptPassword(o);
            }

            return password;
        }

        private static string EncryptPassword(CommandLineParams o)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(o.Password);
            byte[] protectedPassword = ProtectedData.Protect(bytes, Encoding.Unicode.GetBytes("70A47403717EC0F50E0755B2C4CF8488C8A061F3A694E0D1AB336D672C21781A"), DataProtectionScope.CurrentUser);
            string encryptedString = Convert.ToBase64String(protectedPassword);
            o.Password = encryptedString;
            o.Encrypted = "true";
            return encryptedString;
        }
    }
}
