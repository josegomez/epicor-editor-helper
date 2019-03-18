extern alias epi;
using CommandLine;
using Ice.Core;
using Ice.Lib.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using Ice.Lib.Searches;
using Ice.BO;
using Ice.Lib.Customization;
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

namespace CustomizationEditor
{
    class Program
    {
        private static Thread progBarThread;
        private static string currAction;

        [STAThread]
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
                                   if(epiSession!=null)
                                    LaunchInEpicor(o, epiSession, false);                                   
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
                           case "Edit":
                               {
                                   epiSession = GetEpiSession(o);
                                   if (epiSession != null)
                                   {
                                       LaunchInEpicor(o, epiSession, true);
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

                                       LaunchInEpicor(o, epiSession, false, true);
                                   }
                               }
                               break;
                           
                       }
                       if(reSync)
                       {
                           if(o.Key2.Contains("MainController"))//Dashboard
                            DownloadAndSyncDashboard(epiSession, o);
                           else
                            DownloadAndSync(epiSession, o);
                       }
                       ShowProgressBar(false);
                   });

            if(epiSession!=null)
            {
                epiSession.OnSessionClosing();
               
                epiSession = null;
                
            }
            
        }

        private static void ConvertToEncrypted(CommandLineParams o)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(o.Password);
            byte[] protectedPassword = ProtectedData.Protect(bytes, Encoding.Unicode.GetBytes("70A47403717EC0F50E0755B2C4CF8488C8A061F3A694E0D1AB336D672C21781A"), DataProtectionScope.CurrentUser);
            string encryptedString = Convert.ToBase64String(protectedPassword);

            Settings.Default.Password = encryptedString;
            Settings.Default.Encrypted = true;
            Settings.Default.Save();
        }

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
                    progBarThread.Abort();
                    progBarThread = null;
                }
            }
        }

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
        

        private static bool UpdateCustomization(CommandLineParams o, Session epiSession)
        {
      
            using (StreamReader sr = new StreamReader($@"{o.ProjectFolder}\Script.cs"))
            {
                var oTrans = new ILauncher(epiSession);
                Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(oTrans);
                ad.BOConnect();

                GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
                string script = (sr.ReadToEnd().Replace("public partial class Script", "public class Script").EscapeXml());
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

        private static void DownloadAndSyncDashboard(Session epiSession, CommandLineParams o)
        {
            string file = Path.GetTempFileName();
            using (StreamWriter swLog = new StreamWriter(file))
            {
                swLog.WriteLine("Got in the Function");
                try
                {
                    epiSession["Customizing"] = false;
                    var oTrans = new ILauncher(epiSession);
                    CustomizationVerify cm = new CustomizationVerify(epiSession);
                    swLog.WriteLine("Customization Verify");
                    string dll = cm.getDllName(o.Key2);
                    swLog.WriteLine("Got Epicor DLL");
                    StringBuilder refds = new StringBuilder();
                    dynamic epiBaseForm = null;
                    dynamic epiTransaction = null;
                    if (string.IsNullOrEmpty(dll))
                        dll = "*.UI.*.dll";


                    Assembly assy = ClientAssemblyRetriever.ForILaunch(oTrans).RetrieveAssembly(dll);
                    swLog.WriteLine("Finding File");
                    string s = "";
                    
                    s = assy.Location;
                    var typeE = assy.DefinedTypes.Where(r => r.FullName.ToUpper().Contains(o.Key2.ToUpper())).FirstOrDefault();
                    var typeTList = assy.DefinedTypes.Where(r => r.BaseType.Name.Equals("EpiTransaction")).ToList();
                    
                    epiTransaction = new EpiTransaction(oTrans);
                    //epiBaseForm = Activator.CreateInstance(typeE, new object[] { epiTransaction });
                    

                    

                    refds.AppendLine($"<Reference Include=\"{typeE.Assembly.FullName}\">");
                    refds.AppendLine($"<HintPath>{s}</HintPath>");
                    refds.AppendLine($"</Reference>");
                    
                        //epiBaseForm.GetType().GetMethod("InitializeLaunch", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(epiBaseForm);
                    var typ = assy.DefinedTypes.Where(r => r.Name == "Launch").FirstOrDefault();
                    dynamic launcher = Activator.CreateInstance(typ);
                    launcher.Session = epiSession;
                    launcher.GetType().GetMethod("InitializeLaunch", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(launcher, null);

                    epiBaseForm = launcher.GetType().GetField("lForm", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(launcher);
                    swLog.WriteLine("Initialize EpiUI Utils");
                    EpiUIUtils eu = epiBaseForm.GetType().GetField("utils",BindingFlags.Instance | BindingFlags.NonPublic).GetValue(epiBaseForm);
                    eu.GetType().GetField("currentSession", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, epiTransaction.Session);
                    eu.GetType().GetField("customizeName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key1);
                    eu.GetType().GetField("baseExtentionName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key3.Replace("BaseExtension^", string.Empty));
                    eu.ParentForm = epiBaseForm;
                    swLog.WriteLine("Get composite Customize Data Set");
                    var mi = eu.GetType().GetMethod("getCompositeCustomizeDataSet", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool customize = false;
                    mi.Invoke(eu, new object[] { o.Key2, customize, customize, customize });
                    Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(epiTransaction);
                    ad.BOConnect();
                    GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
                    swLog.WriteLine("Customization Get By ID");
                    var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                    string beName = o.Key3.Replace("BaseExtension^", string.Empty);
                    string exName = (string)eu.GetType().GetField("extensionName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(eu);
                    CustomizationDS nds = new CustomizationDS();
                    PersonalizeCustomizeManager csm = new PersonalizeCustomizeManager(epiBaseForm, epiTransaction, o.ProductType, o.Company, beName, exName, o.Key1, eu.CustLayerMan, DeveloperLicenseType.Partner, LayerType.Customization);

                    swLog.WriteLine("Init Custom Controls");
                    csm.InitCustomControlsAndProperties(ds, LayerName.CompositeBase, true);
                    CustomScriptManager csmR = csm.CurrentCustomScriptManager;
                    swLog.WriteLine("Generate Refs");
                    List<string> aliases = new List<string>();
                    Match match = Regex.Match(csmR.CustomCodeAll, "((?<=extern alias )(.*)*(?=;))");
                    while (match.Success)
                    {
                        aliases.Add(match.Value.Replace("_", ".").ToUpper());
                        match = match.NextMatch();
                    }
                    o.Version = ds.XXXDef[0].SysRevID;
                    GenerateRefs(refds, csmR, o, aliases);
                    ExportCustmization(nds, ad, o);
                    int start = csmR.CustomCodeAll.IndexOf("// ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **");
                    int end = csmR.CustomCodeAll.Length - start;
                    string allCode;
                    string script;
                    allCode = csmR.CustomCodeAll.Replace(csmR.CustomCodeAll.Substring(start, end), "}").Replace("public class Script", "public partial class Script");
                    script = csmR.Script.Replace("public class Script", "public partial class Script");
                    swLog.WriteLine("Write Project");
                    string projectFile = Resource.BasicProjc;
                    projectFile = projectFile.Replace("<CUSTID>", o.Key1);
                    projectFile = projectFile.Replace("<!--<ReferencesHere>-->", refds.ToString());


                    swLog.WriteLine("Create Folder");
                    if (string.IsNullOrEmpty(o.ProjectFolder))
                    {
                        string origFolderName = ($@"{o.Folder}\{o.Key2}_{ o.Key1}").Replace('.', '_');
                        string newFolderName = origFolderName;
                        int ct = 0;
                        while (Directory.Exists(newFolderName))
                        {
                            newFolderName = ($"{origFolderName}_{++ct}").Replace('.', '_');
                        }
                        o.ProjectFolder = newFolderName;
                        Directory.CreateDirectory(o.ProjectFolder);
                    }


                    using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\{o.Key1}.csproj"))
                    {
                        sw.Write(projectFile);
                        sw.Close();
                    }
                    swLog.WriteLine("Write Script");
                    using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\Script.cs"))
                    {
                        sw.Write(script);
                        sw.Close();
                    }

                    swLog.WriteLine("Write ScriptRO");
                    using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\ScriptReadOnly.cs"))
                    {
                        sw.Write(allCode);
                        sw.Close();
                    }

                    swLog.WriteLine("Write Command");
                    string command = Newtonsoft.Json.JsonConvert.SerializeObject(o);
                    using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\CustomizationInfo.json"))
                    {
                        sw.Write(command);
                        sw.Close();
                    }

                    File.SetAttributes($@"{o.ProjectFolder}\ScriptReadOnly.cs", File.GetAttributes($@"{o.ProjectFolder}\ScriptReadOnly.cs") & ~FileAttributes.ReadOnly);
                    swLog.WriteLine("Write Customization");
                    nds.WriteXml($@"{o.ProjectFolder}\{o.Key2}_Customization_{o.Key1}_CustomExport.xml", XmlWriteMode.WriteSchema);



                    epiBaseForm.Dispose();


                    ad.Dispose();
                    cm = null;

                    eu.Dispose();


                }
                catch (Exception ee)
                {
                    swLog.WriteLine(ee.ToString());
                }

            }

            Console.WriteLine(o.ProjectFolder);
            //MessageBox.Show(file);
        }



        private static void DownloadAndSync(Session epiSession, CommandLineParams o)
        {
            string file = Path.GetTempFileName();
            using (StreamWriter swLog = new StreamWriter(file))
            {
                swLog.WriteLine("Got in the Function");
                try
                {
                    epiSession["Customizing"] = false;
                    var oTrans = new ILauncher(epiSession);
                    CustomizationVerify cm = new CustomizationVerify(epiSession);
                    swLog.WriteLine("Customization Verify");
                    string dll = cm.getDllName(o.Key2);
                    swLog.WriteLine("Got Epicor DLL");
                    StringBuilder refds = new StringBuilder();
                    dynamic epiBaseForm = null;
                    dynamic epiTransaction = null;
                    if (string.IsNullOrEmpty(dll))
                        dll = "*.UI.*.dll";


                    Assembly assy = ClientAssemblyRetriever.ForILaunch(oTrans).RetrieveAssembly(dll);
                    swLog.WriteLine("Finding File");
                    string s = "";
                    if (assy == null)
                        foreach (string x in Directory.GetFiles(o.EpicorClientFolder, dll))
                        {
                                assy = Assembly.LoadFile(x);
                            if (assy.DefinedTypes.Where(r => r.FullName.ToUpper().Contains(o.Key2.ToUpper())).Any())
                            {
                                
                                    s = x;
                                    break;
                                }
                        }
                    s = assy.Location;
                    var typeE = assy.DefinedTypes.Where(r => r.FullName.ToUpper().Contains(o.Key2.ToUpper())).FirstOrDefault();

                    var typeTList = assy.DefinedTypes.Where(r => r.BaseType.Name.Equals("EpiTransaction") || r.BaseType.Name.Equals("EpiMultiViewTransaction") || r.BaseType.Name.Equals("EpiSingleViewTransaction") || r.BaseType.Name.Equals("UDMultiViewTransaction") || r.BaseType.Name.Equals("UDSingleViewTransaction")).ToList();
                    if(typeTList!=null && typeTList.Count>0)
                        foreach (var typeT in typeTList)
                        {
                            try
                            {
                                if (typeT != null)
                                    epiTransaction = Activator.CreateInstance(typeT, new object[] { oTrans });
                                else
                                    epiTransaction = new EpiTransaction(oTrans);

                                epiBaseForm = Activator.CreateInstance(typeE, new object[] { epiTransaction });
                                break;
                            }
                            catch (Exception e)
                            { }
                        }
                    else
                    {
                        epiTransaction = new EpiTransaction(oTrans);
                        epiBaseForm = Activator.CreateInstance(typeE, new object[] { epiTransaction });
                    }

                    bool dashboard = false;
                    try
                    {
                        epiBaseForm.IsVerificationMode = true;
                        epiBaseForm.CustomizationName = o.Key1;
                    }
                    catch (Exception) {
                        //Dashboard
                        dashboard = true;
                    }
                    
                    refds.AppendLine($"<Reference Include=\"{typeE.Assembly.FullName}\">");
                    refds.AppendLine($"<HintPath>{s}</HintPath>");
                    refds.AppendLine($"</Reference>");
                    if (dashboard)
                    {
                        //epiBaseForm.GetType().GetMethod("InitializeLaunch", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(epiBaseForm);
                        var typ = assy.DefinedTypes.Where(r => r.Name == "Launch").FirstOrDefault();
                        dynamic launcher = Activator.CreateInstance(typ);
                        launcher.Session = epiSession;
                        launcher.GetType().GetMethod("InitializeLaunch", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(launcher, null);
                    }
                    
                    swLog.WriteLine("Initialize EpiUI Utils");
                    EpiUIUtils eu = new EpiUIUtils(epiBaseForm, epiTransaction, epiBaseForm.MainToolManager, null);
                    eu.GetType().GetField("currentSession", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, epiTransaction.Session);
                    eu.GetType().GetField("customizeName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key1);
                    eu.GetType().GetField("baseExtentionName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key3.Replace("BaseExtension^", string.Empty));

                    swLog.WriteLine("Get composite Customize Data Set");
                    var mi = eu.GetType().GetMethod("getCompositeCustomizeDataSet", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool customize = false;
                    mi.Invoke(eu, new object[] { o.Key2, customize, customize, customize });
                    Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(epiTransaction);
                    ad.BOConnect();
                    GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
                    swLog.WriteLine("Customization Get By ID");
                    var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                    string beName = o.Key3.Replace("BaseExtension^", string.Empty);
                    string exName = (string)eu.GetType().GetField("extensionName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(eu);
                    CustomizationDS nds = new CustomizationDS();
                    PersonalizeCustomizeManager csm = new PersonalizeCustomizeManager(epiBaseForm, epiTransaction, o.ProductType, o.Company, beName, exName, o.Key1, eu.CustLayerMan, DeveloperLicenseType.Partner, LayerType.Customization);
                    swLog.WriteLine("Init Custom Controls");
                    csm.InitCustomControlsAndProperties(ds, LayerName.CompositeBase, true);
                    CustomScriptManager csmR = csm.CurrentCustomScriptManager;
                    swLog.WriteLine("Generate Refs");
                    List<string> aliases = new List<string>();
                    Match match =Regex.Match(csmR.CustomCodeAll, "((?<=extern alias )(.*)*(?=;))");
                    while(match.Success)
                    {
                        aliases.Add(match.Value.Replace("_",".").ToUpper());
                        match =match.NextMatch();
                    }

                    GenerateRefs(refds, csmR, o, aliases);
                    ExportCustmization(nds,ad,o);
                    int start = csmR.CustomCodeAll.IndexOf("// ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **");
                    int end = csmR.CustomCodeAll.Length - start;
                    string allCode;
                    string script;
                    allCode = csmR.CustomCodeAll.Replace(csmR.CustomCodeAll.Substring(start, end), "}").Replace("public class Script", "public partial class Script");
                    script = csmR.Script.Replace("public class Script", "public partial class Script");
                    swLog.WriteLine("Write Project");
                    string projectFile = Resource.BasicProjc;
                    projectFile = projectFile.Replace("<CUSTID>", o.Key1);
                    projectFile = projectFile.Replace("<!--<ReferencesHere>-->", refds.ToString());
                    o.Version = ds.XXXDef[0].SysRevID;

                    swLog.WriteLine("Create Folder");
                    if (string.IsNullOrEmpty(o.ProjectFolder))
                    {
                        string origFolderName = ($@"{o.Folder}\{o.Key2}_{ o.Key1}").Replace('.', '_');
                        string newFolderName = origFolderName;
                        int ct = 0;
                        while (Directory.Exists(newFolderName))
                        {
                            newFolderName = ($"{origFolderName}_{++ct}").Replace('.', '_');
                        }
                        o.ProjectFolder = newFolderName;
                        Directory.CreateDirectory(o.ProjectFolder);
                    }


                    using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\{o.Key1}.csproj"))
                    {
                        sw.Write(projectFile);
                        sw.Close();
                    }
                    swLog.WriteLine("Write Script");
                    using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\Script.cs"))
                    {
                        sw.Write(script);
                        sw.Close();
                    }

                    swLog.WriteLine("Write ScriptRO");
                    using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\ScriptReadOnly.cs"))
                    {
                        sw.Write(allCode);
                        sw.Close();
                    }

                    swLog.WriteLine("Write Command");
                    string command = Newtonsoft.Json.JsonConvert.SerializeObject(o);
                    using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\CustomizationInfo.json"))
                    {
                        sw.Write(command);
                        sw.Close();
                    }

                    File.SetAttributes($@"{o.ProjectFolder}\ScriptReadOnly.cs", File.GetAttributes($@"{o.ProjectFolder}\ScriptReadOnly.cs") & ~FileAttributes.ReadOnly);
                    swLog.WriteLine("Write Customization");
                    nds.WriteXml($@"{o.ProjectFolder}\{o.Key2}_Customization_{o.Key1}_CustomExport.xml", XmlWriteMode.WriteSchema);


                    
                    epiBaseForm.Dispose();
                    
                    
                    ad.Dispose();
                    cm = null;
                    
                    eu.Dispose();

                
                }
                catch(Exception ee)
                {
                    swLog.WriteLine(ee.ToString());
                }
            
            }
            
            Console.WriteLine(o.ProjectFolder);
            //MessageBox.Show(file);
        }

        public static void ExportCustmization(CustomizationDS nds, Ice.Adapters.GenXDataAdapter ad, CommandLineParams o)
        {

            string s= ad.GetDechunkedStringByIDWithCompany(o.Company, o.ProductType, o.LayerType, o.Key1, o.Key2, o.Key3);
            StringReader sr = new StringReader(s);
            nds.ReadXml(sr, XmlReadMode.IgnoreSchema);
            sr.Close();
            if (!nds.ExtendedProperties.ContainsKey("Company"))
            {
                nds.ExtendedProperties.Add("Company", o.Company);
            }
            else
            {
                nds.ExtendedProperties["Company"] = o.Company;
            }
            if (!nds.ExtendedProperties.ContainsKey("ProductID"))
            {
                nds.ExtendedProperties.Add("ProductID", o.ProductType);
            }
            else
            {
                nds.ExtendedProperties["ProductID"] = o.ProductType;
            }
            if (!nds.ExtendedProperties.ContainsKey("TypeCode"))
            {
                nds.ExtendedProperties.Add("TypeCode", o.LayerType);
            }
            else
            {
                nds.ExtendedProperties["TypeCode"] = o.LayerType;
            }
            if (!nds.ExtendedProperties.ContainsKey("CGCCode"))
            {
                nds.ExtendedProperties.Add("CGCCode", o.CSGCode);
            }
            else
            {
                nds.ExtendedProperties["CGCCode"] = o.CSGCode;
            }
            
            if (!nds.ExtendedProperties.ContainsKey("Key1"))
            {
                nds.ExtendedProperties.Add("Key1", o.Key1);
            }
            else
            {
                nds.ExtendedProperties["Key1"] = o.Key1;
            }
            if (!nds.ExtendedProperties.ContainsKey("Key2"))
            {
                nds.ExtendedProperties.Add("Key2", o.Key2);
            }
            else
            {
                nds.ExtendedProperties["Key2"] = o.Key2;
            }
            if (!nds.ExtendedProperties.ContainsKey("Key3"))
            {
                nds.ExtendedProperties.Add("Key3", o.Key3);
            }
            else
            {
                nds.ExtendedProperties["Key3"] = o.Key3;
            }
        }

        private static void GenerateRefs(StringBuilder refds, CustomScriptManager csmR, CommandLineParams o, List<string> aliases)
        {
            foreach (var r in csmR.SystemRefAssemblies)
            {
                if (r.Value.FullName.Contains(o.Key1))
                    o.DLLLocation = r.Key;
                refds.AppendLine($"<Reference Include=\"{r.Value.FullName}\">");
                refds.AppendLine($"<HintPath>{r.Key}.dll</HintPath>");
                if(aliases.Contains(Path.GetFileName(r.Key).ToUpper()))
                {
                    refds.AppendLine($"<Aliases>{r.Key.Replace(".", "_")}</Aliases>");
                }
               // < Aliases > asdasda </ Aliases >
                refds.AppendLine($"</Reference>");
            }

            if (csmR.CustomAssembly != null)
            {
                refds.AppendLine($"<Reference Include=\"{csmR.CustomAssembly.FullName}\">");
                refds.AppendLine($"<HintPath>{csmR.CustomAssembly.Location}.dll</HintPath>");
                refds.AppendLine($"</Reference>");
                if (csmR.CustomAssembly.FullName.Contains(o.Key1))
                    o.DLLLocation = csmR.CustomAssembly.Location;
            }
            foreach (var r in csmR.CustRefAssembliesSL)
            {
                if (r.Value.FullName.Contains(o.Key1))
                    o.DLLLocation = Path.Combine(o.EpicorClientFolder, r.Key + ".dll");
                refds.AppendLine($"<Reference Include=\"{r.Value.FullName}\">");
                refds.AppendLine($@"<HintPath>{Path.Combine(o.EpicorClientFolder,r.Key+".dll")}</HintPath>");
                if (aliases.Contains(Path.GetFileName(r.Key).ToUpper()))
                {
                    refds.AppendLine($"<Aliases>{r.Key.Replace(".", "_")}</Aliases >");
                }
                refds.AppendLine($"</Reference>");
            }

            foreach (var r in csmR.ReferencedAssembliesHT)
            {
                if (r.Value.FullName.Contains(o.Key1))
                    o.DLLLocation = Path.Combine(o.EpicorClientFolder, r.Key + ".dll");
                refds.AppendLine($"<Reference Include=\"{r.Value.FullName}\">");
                refds.AppendLine($@"<HintPath>{Path.Combine(o.EpicorClientFolder, r.Key + ".dll")}</HintPath>");
                if (aliases.Contains(Path.GetFileName(r.Key).ToUpper()))
                {
                    refds.AppendLine($"<Aliases>{r.Key.Replace(".", "_")}</Aliases >");
                }
                refds.AppendLine($"</Reference>");
            }
        }

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

        private static void LaunchInEpicor(CommandLineParams o, Session epiSession, bool edit, bool modal = true)
        {
            epiSession["Customizing"] = edit;
            EpiTransaction epiTransaction = new EpiTransaction(new ILauncher(epiSession));
            Ice.UI.App.CustomizationMaintEntry.Transaction oTrans = new Ice.UI.App.CustomizationMaintEntry.Transaction(epiTransaction);
            Ice.UI.App.CustomizationMaintEntry.CustomizationMaintForm custData = new Ice.UI.App.CustomizationMaintEntry.CustomizationMaintForm(oTrans);

            oTrans = (Ice.UI.App.CustomizationMaintEntry.Transaction)custData.GetType().GetField("trans", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(custData);

            SearchOptions opts = new SearchOptions(SearchMode.AutoSearch)
            {
                DataSetMode = DataSetMode.RowsDataSet,
                SelectMode = SelectMode.SingleSelect

            };
            opts.NamedSearch.WhereClauses.Add("XXXDef", $"Key1='{o.Key1}' and Key2 ='{o.Key2}' and Key3='{o.Key3}' and TypeCode='{o.LayerType}' and ProductID='{o.ProductType}'");
            oTrans.InvokeSearch(opts);
            MenuDataSet.MenuRow menuRow = (MenuDataSet.MenuRow)oTrans.GetType().GetMethod("getMenuRow", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(oTrans, new object[] { "Run" });
            menuRow["Company"] = string.Empty;
            
            epiSession["AllowCustomizing"] = edit;
            if(edit)
                epiSession["Customizing"] = epiSession.CanCustomize;
            else
                epiSession["Customizing"] = false;
            if (edit)
            {
#if EPICOR_10_2_300
                oTrans.GetType().GetMethod("addFormnameToArguments", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(oTrans, new object[] { menuRow });
#endif
            }

            LaunchFormOptions lfo = new LaunchFormOptions();
            lfo.IsModal = modal;
            lfo.Sender = oTrans;
            MenuDataSet mnuds = new MenuDataSet();
            mnuds.Menu.Rows.Add(menuRow.ItemArray);
            lfo.MenuDataSet = mnuds;
            FormFunctions.Launch(oTrans.currentSession, menuRow, lfo);
        }

        private static Session GetEpiSession(CommandLineParams o)
        {
            string password = o.Password;
            Session ses = null;
           
            try
            {
                password = NeedtoEncrypt(o);
                ses = new Session(o.Username, password, Session.LicenseType.Default, o.ConfigFile);
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
                        ses = new Session(o.Username, password, Session.LicenseType.Default, o.ConfigFile);
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
                Startup.SetupPlugins(ses);


                epi.Ice.Lib.Configuration c = new epi.Ice.Lib.Configuration(o.ConfigFile);
                Assembly assy = Assembly.LoadFile(Path.Combine(o.EpicorClientFolder, "Epicor.exe"));
                TypeInfo ty = assy.DefinedTypes.Where(r => r.Name == "ConfigureForAutoDeployment").FirstOrDefault();
                dynamic thing = Activator.CreateInstance(ty);

                object[] args = { c };
                thing.GetType().GetMethod("SetUpAssemblyRetrieversAndPossiblyGetNewConfiguration", BindingFlags.Instance | BindingFlags.Public).Invoke(thing, args);
                WellKnownAssemblyRetrievers.AutoDeployAssemblyRetriever = (IAssemblyRetriever)thing.GetType().GetProperty("AutoDeployAssemblyRetriever", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(thing);
                WellKnownAssemblyRetrievers.SessionlessAssemblyRetriever = (IAssemblyRetriever)thing.GetType().GetProperty("SessionlessAssemblyRetriever", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(thing);
                ses.DisableTheming = true;
                Startup.PreStart(ses, true);
                ses.DisableTheming = false;
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
