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


namespace CustomizationEditor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Session epiSession=null;
            bool reSync = false;
            
            Parser.Default.ParseArguments<CommandLineParams>(args)
                   .WithParsed(o =>
                   {
                       Thread t = new Thread(() => new Progress().ShowDialog());
                       t.Start();
                       switch (o.Action)
                       {
                           case "Launch":
                               {
                                   epiSession = GetEpiSession(o);
                                   LaunchInEpicor(o, epiSession, false);
                                   
                               }
                               break;
                           case "Add":
                               {
                                   LoginFrm frm = new LoginFrm(o.EpicorClientFolder);
                                   if(frm.ShowDialog()==DialogResult.OK)
                                   {
                                       o.Username = Settings.Default.Username;
                                       o.Password = Settings.Default.Password;
                                       o.ConfigFile = Settings.Default.Environment;
                                       epiSession = GetEpiSession(o);
                                       DownloadCustomization(o, epiSession);
                                   }
                                   reSync = true;
                               }
                               break;
                           case "Update":
                               {
                                   epiSession = GetEpiSession(o);
                                   reSync = true;
                                   UpdateCustomization(o, epiSession);
                               }
                               break;
                           case "Edit":
                               {
                                   epiSession = GetEpiSession(o);
                                   LaunchInEpicor(o, epiSession, true);
                                   reSync = true;
                               }
                               break;
                           case "Debug":
                               {
                                   epiSession = GetEpiSession(o);
                                   if (!string.IsNullOrEmpty(o.DNSpy))
                                   {
                                       RunDnSpy(o);
                                   }
                                   
                                   LaunchInEpicor(o, epiSession, false, true);
                               }
                               break;
                           
                       }
                       if(reSync)
                       {
                           DownloadAndSync(epiSession, o);
                       }
                       t.Abort();
                   });

            if(epiSession!=null)
            {
                epiSession.OnSessionClosing();
                epiSession.Dispose();
                epiSession = null;
                
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


        private static void UpdateCustomization(CommandLineParams o, Session epiSession)
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
                string content = ad.GetDechunkedStringByIDWithCompany(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);

                string newC = Regex.Replace(content, @"(?=\/\/ \*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*)[\s\S]*?(?=<\/PropertyValue>)", script,
                              RegexOptions.IgnoreCase);
                ad.ChunkNSaveUncompressedStringByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3, chunk.Description, chunk.Version, false, newC);
            }
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

                    swLog.WriteLine("Finding File");
                    foreach (string s in Directory.GetFiles(o.EpicorClientFolder, dll))
                    {
                        Assembly assy = Assembly.LoadFile(s);
                        if (assy.DefinedTypes.Where(r => r.FullName.ToUpper().Contains(o.Key2.ToUpper())).Any())
                        {
                            
                            var typeE = assy.DefinedTypes.Where(r => r.FullName.ToUpper().Contains(o.Key2.ToUpper())).FirstOrDefault();
                            
                            var typeT = assy.DefinedTypes.Where(r => r.Name.Contains("Transaction")).FirstOrDefault();
                            
                            
                            if (typeT != null)
                                epiTransaction = Activator.CreateInstance(typeT, new object[] { oTrans });
                            else
                                epiTransaction = new EpiTransaction(oTrans);

                            epiBaseForm = Activator.CreateInstance(typeE, new object[] { epiTransaction });
                            epiBaseForm.IsVerificationMode = true;
                            epiBaseForm.CustomizationName = o.Key1;
                            refds.AppendLine($"<Reference Include=\"{typeE.Assembly.FullName}\">");
                            refds.AppendLine($"<HintPath>{s}</HintPath>");
                            refds.AppendLine($"</Reference>");
                            break;
                        }
                    }
                    swLog.WriteLine("Initialize EpiUI Utils");
                    EpiUIUtils eu = new EpiUIUtils(epiBaseForm, epiTransaction, epiBaseForm.MainToolManager, null);
                    eu.GetType().GetField("currentSession", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, epiTransaction.Session);
                    eu.GetType().GetField("customizeName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key1);

                    swLog.WriteLine("Get composite Customize Data Set");
                    var mi = eu.GetType().GetMethod("getCompositeCustomizeDataSet", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool customize = false;
                    mi.Invoke(eu, new object[] { o.Key2, customize, customize, customize });
                    Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(epiTransaction);
                    ad.BOConnect();
                    GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
                    swLog.WriteLine("Customization Get By ID");
                    var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                    PersonalizeCustomizeManager csm = new PersonalizeCustomizeManager(epiBaseForm, epiTransaction, o.ProductType, o.Company, "", "", o.Key1, eu.CustLayerMan, DeveloperLicenseType.Customer, LayerType.Customization);
                    swLog.WriteLine("Init Custom Controls");
                    csm.InitCustomControlsAndProperties(ds, LayerName.CompositeBase, true);
                    CustomScriptManager csmR = csm.CurrentCustomScriptManager;
                    swLog.WriteLine("Generate Refs");
                    GenerateRefs(refds, csmR, o);
                    ExportCustmization(ds);
                    int start = csmR.CustomCodeAll.IndexOf("public void InitializeCustomCode()");
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
                    ds.WriteXml($@"{o.ProjectFolder}\{o.Key2}_Customization_{o.Key1}_CustomExport.xml", XmlWriteMode.WriteSchema);
                }
                catch(Exception ee)
                {
                    swLog.WriteLine(ee.ToString());
                }
            
            }
            
            Console.WriteLine(o.ProjectFolder);
            //MessageBox.Show(file);
        }

        public static void ExportCustmization(GenXDataDataSet set1)
        {

            GenXDataDataSet.XXXDefRow row = set1.XXXDef[0];

            set1.ExtendedProperties["ExportFormat"] = "Gen3";
            set1.AcceptChanges();
        }

        private static void GenerateRefs(StringBuilder refds, CustomScriptManager csmR, CommandLineParams o)
        {
            foreach (var r in csmR.SystemRefAssemblies)
            {
                if (r.Value.FullName.Contains(o.Key1))
                    o.DLLLocation = r.Key;
                refds.AppendLine($"<Reference Include=\"{r.Value.FullName}\">");
                refds.AppendLine($"<HintPath>{r.Key}.dll</HintPath>");
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
                if (csmR.CustomAssembly.FullName.Contains(o.Key1))
                    o.DLLLocation = csmR.CustomAssembly.Location;
                refds.AppendLine($"<Reference Include=\"{r.Value.FullName}\">");
                refds.AppendLine($"<HintPath>{r.Key}.dll</HintPath>");
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
                oTrans.GetType().GetMethod("addFormnameToArguments", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(oTrans, new object[] { menuRow });
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
            return new Session(o.Username, o.Password, Session.LicenseType.GlobalUser, o.ConfigFile);
        }

        
    }
}
