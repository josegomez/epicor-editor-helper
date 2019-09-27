using CustomizationEditor;
using Ice.BO;
using Ice.Core;
using Ice.Lib;
using Ice.Lib.Customization;
using Ice.Lib.Deployment;
using Ice.Lib.Framework;
using Ice.Lib.Searches;
using Ice.Proxy.BO;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace CommonCode
{
    public class EpicorLauncher
    {
        Form launchedForm;
        CommandLineParams o;

        /// <summary>
        /// Launches an Epicor Menu with Trasing 
        /// This is deprecated and not used will be removed shortly.
        /// </summary>
        /// <param name="mnuRow">Menu Row</param>
        /// <param name="ses">Epicor Session</param>
        /// <param name="lfo">Launch for Options</param>
        /// <param name="o">Command Line Parameters</param>
        /// <param name="me">Calling Form</param>
        public void LauncWithTracing(object mnuRow, object ses, object lfo, CommandLineParams o, Form me)
        {
            this.o = o;
            MenuDataSet.MenuRow menuRow = mnuRow as MenuDataSet.MenuRow;
            Session session = ses as Session;
            LaunchFormOptions opts = lfo as LaunchFormOptions;
            opts.CallBackToken = "EpicorCustomizatoinCBT";
            opts.IsModal = false;
            launchedForm = me;
            CustomizationVerify cm = new CustomizationVerify(session);
            string dll = cm.getDllName(o.Key2);
            ProcessCaller.ProcessCallBack += new ProcessCallBack(standardCallBackHandler);
            ProcessCaller.LaunchForm(new ILauncher(session), dll.Replace(".dll", ""), opts, true);

            TracingOptions.ShowTracingOptions(new Form(), session);
        }

        /// <summary>
        /// This is the standard call back handler for ProcessCaller
        /// this gets called when a standard transaction occurs on the called form
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="args"></param>
        void standardCallBackHandler(object Sender, TransactionCallBackArgs args)
        {
            ILaunch il = Sender as ILaunch;
            string msgFrom = il == null ? "" : " on " + il.WhoAmI;
            if (args.CallBackToken == "EpicorCustomizatoinCBT")
            {
                if (args.TransactionEvent == TransactionEvent.FormClosed)
                {
                    launchedForm.Close();
                    launchedForm.Dispose();
                }
            }
        }

        /// <summary>
        /// Launches Epicor Tracing Screen
        /// </summary>
        /// <param name="ses">Epicor Session</param>
        public void LaunchTracingOptions(object ses)
        {
            Session session = ses as Session;
            TracingOptions.ShowTracingOptions(new Form(), session);
        }


        /// <summary>
        /// Launches Epicor Menu by MenuID
        /// </summary>
        /// <param name="ses">Epicor Session</param>
        /// <param name="menuID">Menu ID</param>
        public void LaunchMenuOptions(object ses, string menuID)
        {
            Session session = ses as Session;
            LaunchFormOptions lfo = new LaunchFormOptions();
            lfo.IsModal = false;
            ProcessCaller.LaunchForm(new ILauncher(session), menuID);
        }

        public void LaunchObjectExplorer(CommandLineParams pm, object session)
        {
            Session epiSession = session as Session;
            CommandLineParams o = pm as CommandLineParams;
            CustomizationVerify cm;
            dynamic epiTransaction;
            EpiUIUtils eu;
            Ice.Adapters.GenXDataAdapter ad;
            PersonalizeCustomizeManager csm;
            CustomScriptManager csmR;
            if (!o.Key2.Contains("MainController"))
                GetCSM(epiSession, o, out cm, out epiTransaction, out eu, out ad, out csm, out csmR);
            else
                GetCSMDB(epiSession, o, out cm, out epiTransaction, out eu, out ad, out csm, out csmR);


            CustomObjectExplorerDialog coD = new CustomObjectExplorerDialog(csm.TopControl, epiTransaction, csmR);
            coD.ShowDialog();

            coD.Dispose();
            ad.Dispose();
            cm = null;

            eu.Dispose();
        }

        private static void GetCSM(Session epiSession, CommandLineParams o, out CustomizationVerify cm, out dynamic epiTransaction, out EpiUIUtils eu, out Ice.Adapters.GenXDataAdapter ad, out PersonalizeCustomizeManager csm, out CustomScriptManager csmR)
        {
            epiSession["Customizing"] = false;
            var oTrans = new ILauncher(epiSession);
            cm = new CustomizationVerify(epiSession);

            string dll = cm.getDllName(o.Key2);
            dynamic epiBaseForm = null;
            epiTransaction = null;
            if (string.IsNullOrEmpty(dll))
                dll = "*.UI.*.dll";

            Assembly assy = ClientAssemblyRetriever.ForILaunch(oTrans).RetrieveAssembly(dll);
            if (assy == null)
                foreach (string x in Directory.GetFiles(o.EpicorClientFolder, dll))
                {
                    assy = Assembly.LoadFile(x);
                    if (assy.DefinedTypes.Where(r => r.FullName.ToUpper().Contains(o.Key2.ToUpper())).Any())
                    {
                        break;
                    }
                }
            var typeE = assy.DefinedTypes.Where(r => r.FullName.ToUpper().Contains(o.Key2.ToUpper())).FirstOrDefault();
            var typeTList = assy.DefinedTypes.Where(r => r.BaseType.Name.Equals("EpiTransaction") || r.BaseType.Name.Equals("EpiMultiViewTransaction") || r.BaseType.Name.Equals("EpiSingleViewTransaction") || r.BaseType.Name.Equals("UDMultiViewTransaction") || r.BaseType.Name.Equals("UDSingleViewTransaction")).ToList();
            if (typeTList != null && typeTList.Count > 0)
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
                    catch (Exception)
                    { }
                }
            else
            {
                epiTransaction = new EpiTransaction(oTrans);
                epiBaseForm = Activator.CreateInstance(typeE, new object[] { epiTransaction });
            }



            epiBaseForm.IsVerificationMode = true;
            epiBaseForm.CustomizationName = o.Key1;
            eu = new EpiUIUtils(epiBaseForm, epiTransaction, epiBaseForm.MainToolManager, null);
            eu.GetType().GetField("currentSession", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, epiTransaction.Session);
            eu.GetType().GetField("customizeName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key1);
            eu.GetType().GetField("baseExtentionName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key3.Replace("BaseExtension^", string.Empty));
            var mi = eu.GetType().GetMethod("getCompositeCustomizeDataSet", BindingFlags.Instance | BindingFlags.NonPublic);
            bool customize = false;
            mi.Invoke(eu, new object[] { o.Key2, customize, customize, customize });

            ad = new Ice.Adapters.GenXDataAdapter(epiTransaction);
            ad.BOConnect();
            GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
            var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
            string beName = o.Key3.Replace("BaseExtension^", string.Empty);
            string exName = (string)eu.GetType().GetField("extensionName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(eu);
            CustomizationDS nds = new CustomizationDS();
            if (string.IsNullOrEmpty(o.Company))
            {
                eu.CustLayerMan.GetType().GetProperty("RetrieveFromCache", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, false);
                eu.CustLayerMan.GetType().GetField("custAllCompanies", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, string.IsNullOrEmpty(o.Company));
                eu.CustLayerMan.GetType().GetField("selectCompCode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, o.Company);
                eu.CustLayerMan.GetType().GetField("companyCode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, o.Company);
                eu.CustLayerMan.GetType().GetField("loadDeveloperMode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, string.IsNullOrEmpty(o.Company));

                bool cancel = false;
                eu.CustLayerMan.GetType().GetMethod("GetCompositeCustomDataSet", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Invoke(eu.CustLayerMan, new object[] { o.Key2, exName, o.Key1, cancel });
            }
            typeof(EpiBaseForm).GetField("utils", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(epiBaseForm, eu);
            csm = new PersonalizeCustomizeManager(epiBaseForm, epiTransaction, o.ProductType, o.Company, beName, exName, o.Key1, eu.CustLayerMan, DeveloperLicenseType.Partner, LayerType.Customization);
            try
            {
                Log.Debug("Creating Handle for Top Control");
                typeof(Control).GetMethod("CreateHandle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(csm.TopControl, null);
            }
            catch (Exception ee)
            {
                Log.Debug(ee, "Failed to Create Handle");
            }
            csm.InitCustomControlsAndProperties(ds, LayerName.CompositeBase, true);
            csmR = csm.CurrentCustomScriptManager;
            eu.CloseCacheRespinSplash();
        }


        private static void GetCSMDB(Session epiSession, CommandLineParams o, out CustomizationVerify cm, out dynamic epiTransaction, out EpiUIUtils eu, out Ice.Adapters.GenXDataAdapter ad, out PersonalizeCustomizeManager csm, out CustomScriptManager csmR)
        {
            epiSession["Customizing"] = false;
            var oTrans = new ILauncher(epiSession);
            cm = new CustomizationVerify(epiSession);
            CompositeAssemblyRetriever caR = (CompositeAssemblyRetriever)typeof(ClientAssemblyRetriever).GetMethod("GetOrCreateAssemblyRetriever", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { epiSession });
            Assembly asy = caR.RetrieveAssembly($"Ice.UI.App.{(o.Key2.Replace("App.", "").Replace(".MainController", ""))}.dll");
            string dll = cm.getDllName(o.Key2);
            dynamic epiBaseForm = null;
            epiTransaction = null;
            if (string.IsNullOrEmpty(dll))
                dll = "*.UI.*.dll";

            Assembly assy = ClientAssemblyRetriever.ForILaunch(oTrans).RetrieveAssembly(dll);

            var typeE = assy.DefinedTypes.Where(r => r.FullName.ToUpper().Contains(o.Key2.ToUpper())).FirstOrDefault();
            var typeTList = assy.DefinedTypes.Where(r => r.BaseType.Name.Equals("EpiTransaction") || r.BaseType.Name.Equals("EpiMultiViewTransaction") || r.BaseType.Name.Equals("EpiSingleViewTransaction") || r.BaseType.Name.Equals("UDMultiViewTransaction") || r.BaseType.Name.Equals("UDSingleViewTransaction")).ToList();
            epiTransaction = new EpiTransaction(oTrans);
            var typ = assy.DefinedTypes.Where(r => r.Name == "Launch").FirstOrDefault();
            dynamic launcher = Activator.CreateInstance(typ);
            launcher.Session = epiSession;
            launcher.GetType().GetMethod("InitializeLaunch", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(launcher, null);

            epiBaseForm = launcher.GetType().GetField("lForm", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(launcher);



            epiBaseForm.IsVerificationMode = true;
            epiBaseForm.CustomizationName = o.Key1;
            eu = epiBaseForm.GetType().GetField("utils", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(epiBaseForm);
            eu.GetType().GetField("currentSession", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, epiTransaction.Session);
            eu.GetType().GetField("customizeName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key1);
            eu.GetType().GetField("baseExtentionName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key3.Replace("BaseExtension^", string.Empty));
            eu.ParentForm = epiBaseForm;
            var mi = eu.GetType().GetMethod("getCompositeCustomizeDataSet", BindingFlags.Instance | BindingFlags.NonPublic);
            bool customize = false;
            mi.Invoke(eu, new object[] { o.Key2, customize, customize, customize });

            ad = new Ice.Adapters.GenXDataAdapter(epiTransaction);
            ad.BOConnect();
            GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
            var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
            string beName = o.Key3.Replace("BaseExtension^", string.Empty);
            string exName = (string)eu.GetType().GetField("extensionName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(eu);
            CustomizationDS nds = new CustomizationDS();
            if (string.IsNullOrEmpty(o.Company))
            {
                eu.CustLayerMan.GetType().GetProperty("RetrieveFromCache", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, false);
                eu.CustLayerMan.GetType().GetField("custAllCompanies", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, string.IsNullOrEmpty(o.Company));
                eu.CustLayerMan.GetType().GetField("selectCompCode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, o.Company);
                eu.CustLayerMan.GetType().GetField("companyCode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, o.Company);
                eu.CustLayerMan.GetType().GetField("loadDeveloperMode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, string.IsNullOrEmpty(o.Company));

                bool cancel = false;
                eu.CustLayerMan.GetType().GetMethod("GetCompositeCustomDataSet", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Invoke(eu.CustLayerMan, new object[] { o.Key2, exName, o.Key1, cancel });
            }
            typeof(EpiBaseForm).GetField("utils", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(epiBaseForm, eu);
            csm = new PersonalizeCustomizeManager(epiBaseForm, epiTransaction, o.ProductType, o.Company, beName, exName, o.Key1, eu.CustLayerMan, DeveloperLicenseType.Partner, LayerType.Customization);
            try
            {
                Log.Debug("Creating Handle for Top Control");
                typeof(Control).GetMethod("CreateHandle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(csm.TopControl, null);
            }
            catch (Exception ee)
            {
                Log.Debug(ee, "Failed to Create Handle");
            }
            csm.InitCustomControlsAndProperties(ds, LayerName.CompositeBase, true);
            csmR = csm.CurrentCustomScriptManager;
            eu.CloseCacheRespinSplash();
        }


        internal void LaunchDataTools(CommandLineParams oo, object session)
        {
            Session epiSession = session as Session;
            CommandLineParams o = oo as CommandLineParams;
            CustomizationVerify cm;
            dynamic epiTransaction;
            EpiUIUtils eu;
            Ice.Adapters.GenXDataAdapter ad;
            PersonalizeCustomizeManager csm;
            CustomScriptManager csmR;
            if (!o.Key2.Contains("MainController"))
                GetCSM(epiSession, o, out cm, out epiTransaction, out eu, out ad, out csm, out csmR);
            else
                GetCSMDB(epiSession, o, out cm, out epiTransaction, out eu, out ad, out csm, out csmR);

            csm.GetType().GetMethod("EnterEditMode", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(csm, null);

            CustomToolsDialog cmd = (CustomToolsDialog)csm.GetType().GetField("uiToolsDialog", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(csm);
            cmd.Hide();
            cmd.GetType().GetMethod("mnuToolsDataTools_Click", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(cmd, new object[] { null, null });
            cmd.GetType().GetMethod("SaveCustomization", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(cmd, null);

            cmd.Close();
            csm.TopControl.Dispose();
            ad.Dispose();
            eu.Dispose();

        }

        internal void LaunchWizard(CommandLineParams oo, object session)
        {
            Session epiSession = session as Session;
            CommandLineParams o = oo as CommandLineParams;
            CustomizationVerify cm;
            dynamic epiTransaction;
            EpiUIUtils eu;
            Ice.Adapters.GenXDataAdapter ad;
            PersonalizeCustomizeManager csm;
            CustomScriptManager csmR;
            if (!o.Key2.Contains("MainController"))
                GetCSM(epiSession, o, out cm, out epiTransaction, out eu, out ad, out csm, out csmR);
            else
                GetCSMDB(epiSession, o, out cm, out epiTransaction, out eu, out ad, out csm, out csmR);

            csm.GetType().GetMethod("EnterEditMode", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(csm, null);
            CustomToolsDialog cmd = (CustomToolsDialog)csm.GetType().GetField("uiToolsDialog", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(csm);
            cmd.Hide();
            cmd.GetType().Assembly.DefinedTypes.Where(t => t.Name == "CustomCodeWizardManager").FirstOrDefault().GetMethod("LaunchWizardDialog", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(null, new object[] { csm, cmd.GetType().GetField("customScriptEditorPanel1", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(cmd) });
            cmd.GetType().GetMethod("SaveCustomization", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(cmd, null);

            cmd.Close();
            csm.TopControl.Dispose();
            ad.Dispose();
            eu.Dispose();





        }

        public void LaunchReferences(CommandLineParams oo, object session)
        {
            Session epiSession = session as Session;
            CommandLineParams o = oo as CommandLineParams;
            CustomizationVerify cm;
            dynamic epiTransaction;
            EpiUIUtils eu;
            Ice.Adapters.GenXDataAdapter ad;
            PersonalizeCustomizeManager csm;
            CustomScriptManager csmR;
            if (!o.Key2.Contains("MainController"))
                GetCSM(epiSession, o, out cm, out epiTransaction, out eu, out ad, out csm, out csmR);
            else
                GetCSMDB(epiSession, o, out cm, out epiTransaction, out eu, out ad, out csm, out csmR);

            csm.GetType().GetMethod("EnterEditMode", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(csm, null);

            CustomToolsDialog cmd = (CustomToolsDialog)csm.GetType().GetField("uiToolsDialog", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(csm);
            cmd.Hide();
            cmd.GetType().GetMethod("mnuToolsAssemblyRefs_Click", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(cmd, new object[] { null, null });
            cmd.GetType().GetMethod("SaveCustomization", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(cmd, null);
            cmd.Close();
            csm.TopControl.Dispose();
            ad.Dispose();
            eu.Dispose();

        }

        /// <summary>
        /// Launches a particular customization in Epicor
        /// </summary>
        /// <param name="o">Command Line Parameters</param>
        /// <param name="epiSession">Epicor Session</param>
        /// <param name="edit">Edit the Customization? True Flase</param>
        /// <param name="modal">Show the screen as modal?</param>
        public void LaunchInEpicor(CommandLineParams o, Session epiSession, bool edit, bool modal = true)
        {
            epiSession["Customizing"] = edit;
            if (o.Key2.Contains("MainController"))
            {
                CompositeAssemblyRetriever caR = (CompositeAssemblyRetriever)typeof(ClientAssemblyRetriever).GetMethod("GetOrCreateAssemblyRetriever", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { epiSession });
                Assembly asy = caR.RetrieveAssembly($"Ice.UI.App.{(o.Key2.Replace("App.", "").Replace(".MainController", ""))}.dll");
            }
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
            if (edit)
                epiSession["Customizing"] = epiSession.CanCustomize;
            else
                epiSession["Customizing"] = false;
            if (edit)
            {
#if EPICOR_10_2_300 || EPICOR_10_2_400
                oTrans.GetType().GetMethod("addFormnameToArguments", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(oTrans, new object[] { menuRow });
#endif
            }

            LaunchFormOptions lfo = new LaunchFormOptions();
            lfo.IsModal = modal;
            lfo.Sender = oTrans;
            MenuDataSet mnuds = new MenuDataSet();
            mnuds.Menu.Rows.Add(menuRow.ItemArray);
            lfo.MenuDataSet = mnuds;
            try
            {
                FormFunctions.Launch(oTrans.currentSession, menuRow, lfo);
            }
            catch { }
        }

        /// <summary>
        /// Runs the DNSpy utility and attaches it to the current process
        /// </summary>
        /// <param name="o">Command Line Parameters</param>
        public void RunDnSpy(CommandLineParams o)
        {

            string path = Path.GetDirectoryName(o.DLLLocation);
            foreach (string file in Directory.GetFiles(path, "*.dll").OrderByDescending(r => File.GetLastWriteTime(r)))
            {
                if (file.Contains(o.Key1) && file.Contains(o.Key2))
                {
                    o.DLLLocation = file;
                    break;
                }
            }

            new Thread(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = "dnSpy-x86.exe";
                process.StartInfo.WorkingDirectory = o.DNSpy;
                StringBuilder arguments = new StringBuilder();
                arguments.Append($"--process-name CustomizationEditor.exe --files {o.DLLLocation} --search-for class --search-in all --search Script --select T:Script");
                process.StartInfo.Arguments = arguments.ToString();
                process.Start();
                process.WaitForExit();
            }).Start();
        }

        /// <summary>
        /// Downloads the customization from Epicor and pushes out to the output project
        /// </summary>
        /// <param name="epiSession">Epicor Session</param>
        /// <param name="o">Command Line Parameters</param>
        public void DownloadAndSync(Session epiSession, CommandLineParams o)
        {

            Log.Debug("Got in the Function");
            try
            {
                epiSession["Customizing"] = false;
                var oTrans = new ILauncher(epiSession);
                CustomizationVerify cm = new CustomizationVerify(epiSession);
                Log.Debug("Customization Verify");
                string dll = cm.getDllName(o.Key2);
                Log.Debug("Got Epicor DLL");
                StringBuilder refds = new StringBuilder();
                dynamic epiBaseForm = null;
                dynamic epiTransaction = null;
                if (string.IsNullOrEmpty(dll))
                    dll = "*.UI.*.dll";


                Assembly assy = ClientAssemblyRetriever.ForILaunch(oTrans).RetrieveAssembly(dll);
                Log.Debug("Finding File");
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
                if (typeTList != null && typeTList.Count > 0)
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
                        catch (Exception)
                        { }
                    }
                else
                {
                    epiTransaction = new EpiTransaction(oTrans);
                    epiBaseForm = Activator.CreateInstance(typeE, new object[] { epiTransaction });
                }



                epiBaseForm.IsVerificationMode = true;
                epiBaseForm.CustomizationName = o.Key1;


                refds.AppendLine($"<Reference Include=\"{typeE.Assembly.FullName}\">");
                refds.AppendLine($"<HintPath>{s}</HintPath>");
                refds.AppendLine($"</Reference>");


                Log.Debug("Initialize EpiUI Utils");


                EpiUIUtils eu = new EpiUIUtils(epiBaseForm, epiTransaction, epiBaseForm.MainToolManager, null);
                eu.GetType().GetField("currentSession", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, epiTransaction.Session);
                eu.GetType().GetField("customizeName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key1);
                eu.GetType().GetField("baseExtentionName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key3.Replace("BaseExtension^", string.Empty));

                Log.Debug("Get composite Customize Data Set");

                var mi = eu.GetType().GetMethod("getCompositeCustomizeDataSet", BindingFlags.Instance | BindingFlags.NonPublic);
                bool customize = false;
                mi.Invoke(eu, new object[] { o.Key2, customize, customize, customize });

                Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(epiTransaction);
                ad.BOConnect();
                GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
                Log.Debug("Customization Get By ID");
                var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                string beName = o.Key3.Replace("BaseExtension^", string.Empty);
                string exName = (string)eu.GetType().GetField("extensionName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(eu);
                CustomizationDS nds = new CustomizationDS();
                if (string.IsNullOrEmpty(o.Company))
                {
                    eu.CustLayerMan.GetType().GetProperty("RetrieveFromCache", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, false);
                    eu.CustLayerMan.GetType().GetField("custAllCompanies", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, string.IsNullOrEmpty(o.Company));
                    eu.CustLayerMan.GetType().GetField("selectCompCode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, o.Company);
                    eu.CustLayerMan.GetType().GetField("companyCode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, o.Company);
                    eu.CustLayerMan.GetType().GetField("loadDeveloperMode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, string.IsNullOrEmpty(o.Company));

                    bool cancel = false;
                    eu.CustLayerMan.GetType().GetMethod("GetCompositeCustomDataSet", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Invoke(eu.CustLayerMan, new object[] { o.Key2, exName, o.Key1, cancel });
                }
                Log.Debug("Get composite Customize Data Set");
                eu.CloseCacheRespinSplash();

                PersonalizeCustomizeManager csm = new PersonalizeCustomizeManager(epiBaseForm, epiTransaction, o.ProductType, o.Company, beName, exName, o.Key1, eu.CustLayerMan, DeveloperLicenseType.Partner, LayerType.Customization);
                Log.Debug("Init Custom Controls");
                try
                {
                    Log.Debug("Creating Handle for Top Control");
                    typeof(Control).GetMethod("CreateHandle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(csm.TopControl, null);
                }
                catch (Exception ee)
                {
                    Log.Debug(ee, "Failed to Create Handle");
                }
                csm.InitCustomControlsAndProperties(ds, LayerName.CompositeBase, true);

                CustomScriptManager csmR = csm.CurrentCustomScriptManager;
                Log.Debug("Generate Refs");

                Dictionary<decimal,string> netver = new Dictionary<decimal,string>();
                GenerateRefs(refds, csmR, o, netver);
                ExportCustmization(nds, ad, o);
                Resync(o, refds, ds, nds, csmR, netver);
                eu.CloseCacheRespinSplash();
                epiBaseForm.Dispose();


                ad.Dispose();
                cm = null;
              
                eu.Dispose();


            }
            catch (Exception ee)
            {
                Log.Debug(ee.ToString());
            }



            Console.WriteLine(o.ProjectFolder);

        }

        /// <summary>
        /// Writes down any changes to the customization
        /// </summary>
        /// <param name="o">Command Line Parameter</param>
        /// <param name="swLog">Stream Writer for Debugging</param>
        /// <param name="refds">Collection of References for the VS Code Project</param>
        /// <param name="ds">XXXDef DatSet</param>
        /// <param name="nds">CustomizationDS object</param>
        /// <param name="csmR">CustomScriptManger contains all the customization code</param>
        private void Resync(CommandLineParams o, StringBuilder refds, GenXDataDataSet ds, CustomizationDS nds, CustomScriptManager csmR, Dictionary<decimal, string> verdic)
        {
            int start = csmR.CustomCodeAll.IndexOf("// ** Wizard Insert Location - Do Not Remove 'Begin/End Wizard Added Module Level Variables' Comments! **");
            if (start < 0)
            {
                start = 0;
                MessageBox.Show("We couldn't find the Wizard Insert Location in your source code, this code may not compile properly. Like Epicor we rely on those comments so if you remove them or move them it may not function correctly", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            int end = csmR.CustomCodeAll.Length - start;
            string allCode;
            string script;
            allCode = csmR.CustomCodeAll.Replace(csmR.CustomCodeAll.Substring(start, end), "}").Replace("public class Script", "public partial class Script").Replace("public static class Script", "public static partial class Script");
            script = csmR.Script.Replace("public class Script", "public partial class Script");
            script = script.Replace("public static class Script", "public static partial class Script");
            Log.Debug("Write Project");
            string projectFile = Resource.BasicProjc;
            projectFile = projectFile.Replace("<CUSTID>", o.Key1);
            projectFile = projectFile.Replace("<!--<ReferencesHere>-->", refds.ToString());
            XDocument xmldoc = XDocument.Load(new StringReader(projectFile));
            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            var xe = xmldoc.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault();
            var valKey = verdic.Max(r => r.Key);
            xe.Value = verdic[valKey];
            projectFile = xmldoc.ToString();
            o.Version = ds.XXXDef[0].SysRevID;

            Log.Debug("Create Folder");
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
            Log.Debug("Write Script");
            using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\Script.cs"))
            {
                sw.Write(script);
                sw.Close();
            }

            Log.Debug("Write ScriptRO");
            using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\ScriptReadOnly.cs"))
            {
                sw.Write(allCode);
                sw.Close();
            }

            Log.Debug("Write Command");
            string command = Newtonsoft.Json.JsonConvert.SerializeObject(o);
            using (StreamWriter sw = new StreamWriter($@"{o.ProjectFolder}\CustomizationInfo.json"))
            {
                sw.Write(command);
                sw.Close();
            }

            File.SetAttributes($@"{o.ProjectFolder}\ScriptReadOnly.cs", File.GetAttributes($@"{o.ProjectFolder}\ScriptReadOnly.cs") & ~FileAttributes.ReadOnly);
            Log.Debug("Write Customization");
            nds.WriteXml($@"{o.ProjectFolder}\{o.Key2}_Customization_{o.Key1}_CustomExport.xml", XmlWriteMode.WriteSchema);
        }

        public static void ExportCustmization(CustomizationDS nds, Ice.Adapters.GenXDataAdapter ad, CommandLineParams o)
        {

            string s = ad.GetDechunkedStringByIDWithCompany(o.Company, o.ProductType, o.LayerType, o.Key1, o.Key2, o.Key3);
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

        public void GenerateRefs(StringBuilder refds, CustomScriptManager csmR, CommandLineParams o, Dictionary<decimal,string> aliases)
        {
#if EPICOR_10_1_500
            aliases.Add(4.6m,"v4.6.1");
            foreach (DictionaryEntry entry in csmR.SystemRefAssemblies)
            {
                AssemblyName r = entry.Value as AssemblyName;
                if (r.FullName.Contains(o.Key1))
                    o.DLLLocation = entry.Key + ".dll";
                if (!refds.ToString().ToUpper().Contains(r.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{r.FullName}\">");
                    refds.AppendLine($"<HintPath>{entry.Key}.dll</HintPath>");
                    refds.AppendLine($"</Reference>");
                }
            }
            foreach (DictionaryEntry entry in csmR.SystemRefAssemblies)
            {
                AssemblyName r = entry.Value as AssemblyName;
                if (r.FullName.Contains(o.Key1))
                    o.DLLLocation = entry.Key + ".dll";
                if (!refds.ToString().ToUpper().Contains(r.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{r.FullName}\">");
                    refds.AppendLine($"<HintPath>{entry.Key}.dll</HintPath>");
                    refds.AppendLine($"</Reference>");
                }
            }

            if (csmR.CustomAssembly != null)
            {
                if (!refds.ToString().ToUpper().Contains(csmR.CustomAssembly.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{csmR.CustomAssembly.FullName}\">");
                    refds.AppendLine($"<HintPath>{csmR.CustomAssembly.Location}.dll</HintPath>");
                    refds.AppendLine($"</Reference>");
                }
                if (csmR.CustomAssembly.FullName.Contains(o.Key1))
                    o.DLLLocation = csmR.CustomAssembly.Location;
            }

            foreach (DictionaryEntry entry in csmR.CustRefAssembliesSL)
            {
                AssemblyName r = entry.Value as AssemblyName;
                if (r.FullName.Contains(o.Key1))
                    o.DLLLocation = Path.Combine(o.EpicorClientFolder, entry.Key + ".dll");
                if (!refds.ToString().ToUpper().Contains(r.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{r.FullName}\">");
                    refds.AppendLine($@"<HintPath>{Path.Combine(o.EpicorClientFolder, entry.Key + ".dll")}</HintPath>");

                    refds.AppendLine($"</Reference>");
                }
            }

            foreach (DictionaryEntry entry in csmR.ReferencedAssembliesHT)
            {
                AssemblyName r = entry.Value as AssemblyName;
                if (r.FullName.Contains(o.Key1))
                    o.DLLLocation = Path.Combine(o.EpicorClientFolder, entry.Key + ".dll");
                if (!refds.ToString().ToUpper().Contains(r.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{r.FullName}\">");
                    refds.AppendLine($@"<HintPath>{Path.Combine(o.EpicorClientFolder, entry.Key + ".dll")}</HintPath>");
                    refds.AppendLine($"</Reference>");
                }
            }

            foreach (AssemblyName entry in csmR.ReferencedAssemblies)
            {

                if (!refds.ToString().ToUpper().Contains(entry.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{entry.FullName}\"/>");
                }
            }
#else
#if EPICOR_10_2_400 || EPICOR_10_2_300 ||  EPICOR_10_2_500
            aliases.Add(4.7m,"v4.7.2");
#endif
#if EPICOR_10_1_500 || EPICOR_10_1_600 || EPICOR_10_2_100 || EPICOR_10_2_200
            aliases.Add(4.6m, "v4.6.1");
#endif
            foreach (var r in csmR.SystemRefAssemblies)
            {
                
                    if (r.Value.FullName.Contains(o.Key1))
                        o.DLLLocation = r.Key;
                if (!refds.ToString().ToUpper().Contains(r.Value.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{r.Value.FullName}\">");
                    refds.AppendLine($"<HintPath>{r.Key}.dll</HintPath>");
                    refds.AppendLine($"</Reference>");
                    try
                    {
                        var runver = System.Reflection.Assembly.LoadFrom($"{r.Key}.dll").GetCustomAttributes(true).OfType<TargetFrameworkAttribute>().FirstOrDefault();
                        if (runver != null)
                        {
                            GenerateVersion(aliases, runver);

                            //!aliases.Contains(runver.FrameworkName))
                            //aliases.Add(runver.FrameworkName);
                        }
                    }
                    catch { }
                }
            }

            if (csmR.CustomAssembly != null)
            {
                if (!refds.ToString().ToUpper().Contains(csmR.CustomAssembly.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{csmR.CustomAssembly.FullName}\">");
                    refds.AppendLine($"<HintPath>{csmR.CustomAssembly.Location}.dll</HintPath>");
                    refds.AppendLine($"</Reference>");
                }
                
                if (csmR.CustomAssembly.FullName.Contains(o.Key1))
                    o.DLLLocation = csmR.CustomAssembly.Location;
                try
                { 
                    var runver = System.Reflection.Assembly.LoadFrom($"{csmR.CustomAssembly.Location}.dll").GetCustomAttributes(true).OfType<TargetFrameworkAttribute>().FirstOrDefault();
                    if (runver != null)
                    {
                        GenerateVersion(aliases, runver);

                        //!aliases.Contains(runver.FrameworkName))
                        //aliases.Add(runver.FrameworkName);
                    }
                }
                catch { }
            }
            foreach (var r in csmR.CustRefAssembliesSL)
            {
                if (r.Value.FullName.Contains(o.Key1))
                    o.DLLLocation = Path.Combine(o.EpicorClientFolder, r.Key + ".dll");
                if (!refds.ToString().ToUpper().Contains(r.Value.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{r.Value.FullName}\">");
                    refds.AppendLine($@"<HintPath>{Path.Combine(o.EpicorClientFolder, r.Key + ".dll")}</HintPath>");

                    refds.AppendLine($"</Reference>");
                }
                try
                {
                    var runver = System.Reflection.Assembly.LoadFrom($"{Path.Combine(o.EpicorClientFolder, r.Key + ".dll")}").GetCustomAttributes(true).OfType<TargetFrameworkAttribute>().FirstOrDefault();
                    if (runver != null)
                    {
                        GenerateVersion(aliases, runver);
                        //!aliases.Contains(runver.FrameworkName))
                        //aliases.Add(runver.FrameworkName);
                    }
                }
                catch { }
            }

            foreach (var r in csmR.ReferencedAssembliesHT)
            {
                if (r.Value.FullName.Contains(o.Key1))
                    o.DLLLocation = Path.Combine(o.EpicorClientFolder, r.Key + ".dll");
                if (!refds.ToString().ToUpper().Contains(r.Value.FullName.ToUpper()))
                {
                    refds.AppendLine($"<Reference Include=\"{r.Value.FullName}\">");
                    refds.AppendLine($@"<HintPath>{Path.Combine(o.EpicorClientFolder, r.Key + ".dll")}</HintPath>");

                    refds.AppendLine($"</Reference>");
                }
                try
                {
                    var runver = System.Reflection.Assembly.LoadFrom($"{Path.Combine(o.EpicorClientFolder, r.Key + ".dll")}").GetCustomAttributes(true).OfType<TargetFrameworkAttribute>().FirstOrDefault();
                    if (runver != null)
                    {
                        GenerateVersion(aliases, runver);

                        //!aliases.Contains(runver.FrameworkName))
                        //aliases.Add(runver.FrameworkName);
                    }
                }
                catch { }
            }
#endif
            
        }

        private static void GenerateVersion(Dictionary<decimal, string> aliases, TargetFrameworkAttribute runver)
        {
            var regex = new Regex(@"([0-9]\.[0-9])");
            var match = regex.Matches(runver.FrameworkName);
            if (match.Count > 0)
            {
                decimal ver = decimal.Parse(match[0].Value);
                if (!aliases.ContainsKey(ver))
                {
                    regex = new Regex(@"(v[0-9]\.[0-9\.]+)");
                    match = regex.Matches(runver.FrameworkName);
                    aliases.Add(ver, match[0].Value);
                }
            }
        }


        /// <summary>
        /// Download and sync any changes to a deployed dashboard
        /// </summary>
        /// <param name="epiSession">Epicor Session</param>
        /// <param name="o">Command Line Parameters</param>
        public void DownloadAndSyncDashboard(Session epiSession, CommandLineParams o)
        {


            try
            {
                epiSession["Customizing"] = false;
                var oTrans = new ILauncher(epiSession);



                CompositeAssemblyRetriever caR = (CompositeAssemblyRetriever)typeof(ClientAssemblyRetriever).GetMethod("GetOrCreateAssemblyRetriever", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { epiSession });
                Assembly asy = caR.RetrieveAssembly($"Ice.UI.App.{(o.Key2.Replace("App.", "").Replace(".MainController", ""))}.dll");

                Log.Debug("Customization Verify");
                CustomizationVerify cm = new CustomizationVerify(epiSession);

                string dll = cm.getDllName(o.Key2);
                Log.Debug($"Got Epicor DLL: {dll}");
                StringBuilder refds = new StringBuilder();
                dynamic epiBaseForm = null;
                dynamic epiTransaction = null;
                if (string.IsNullOrEmpty(dll))
                    dll = "*.UI.*.dll";


                Assembly assy = ClientAssemblyRetriever.ForILaunch(oTrans).RetrieveAssembly(dll);
                Log.Debug($"Finding Assembyl File");
                string s = "";

                s = assy.Location;
                var typeE = assy.DefinedTypes.Where(r => r.FullName.ToUpper().Contains(o.Key2.ToUpper())).FirstOrDefault();
                var typeTList = assy.DefinedTypes.Where(r => r.BaseType.Name.Equals("EpiTransaction")).ToList();

                epiTransaction = new EpiTransaction(oTrans);





                refds.AppendLine($"<Reference Include=\"{typeE.Assembly.FullName}\">");
                refds.AppendLine($"<HintPath>{s}</HintPath>");
                refds.AppendLine($"</Reference>");


                var typ = assy.DefinedTypes.Where(r => r.Name == "Launch").FirstOrDefault();
                dynamic launcher = Activator.CreateInstance(typ);
#if EPICOR_10_1_600 || EPICOR_10_1_500
                    //launcher.Session = epiSession;
                    //launcher.GetType().GetProperty("Session", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(launcher, epiSession);
#else
                launcher.Session = epiSession;
#endif
                launcher.GetType().GetMethod("InitializeLaunch", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(launcher, null);

                epiBaseForm = launcher.GetType().GetField("lForm", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(launcher);
                Log.Debug("Initialize EpiUI Utils");
                EpiUIUtils eu = epiBaseForm.GetType().GetField("utils", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(epiBaseForm);
                eu.GetType().GetField("currentSession", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, epiTransaction.Session);
                eu.GetType().GetField("customizeName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key1);
                eu.GetType().GetField("baseExtentionName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(eu, o.Key3.Replace("BaseExtension^", string.Empty));
                eu.ParentForm = epiBaseForm;
                Log.Debug("Get composite Customize Data Set");
                var mi = eu.GetType().GetMethod("getCompositeCustomizeDataSet", BindingFlags.Instance | BindingFlags.NonPublic);
                bool customize = false;
                mi.Invoke(eu, new object[] { o.Key2, customize, customize, customize });
                Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(epiTransaction);
                ad.BOConnect();
                GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
                Log.Debug("Customization Get By ID");
                var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                string beName = o.Key3.Replace("BaseExtension^", string.Empty);
                string exName = (string)eu.GetType().GetField("extensionName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(eu);
                CustomizationDS nds = new CustomizationDS();


                if (string.IsNullOrEmpty(o.Company))
                {
                    eu.CustLayerMan.GetType().GetProperty("RetrieveFromCache", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, false);
                    eu.CustLayerMan.GetType().GetField("custAllCompanies", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, string.IsNullOrEmpty(o.Company));
                    eu.CustLayerMan.GetType().GetField("selectCompCode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, o.Company);
                    eu.CustLayerMan.GetType().GetField("companyCode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, o.Company);
                    eu.CustLayerMan.GetType().GetField("loadDeveloperMode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).SetValue(eu.CustLayerMan, string.IsNullOrEmpty(o.Company));

                    bool cancel = false;
                    eu.CustLayerMan.GetType().GetMethod("GetCompositeCustomDataSet", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).Invoke(eu.CustLayerMan, new object[] { o.Key2, exName, o.Key1, cancel });
                }


                PersonalizeCustomizeManager csm = new PersonalizeCustomizeManager(epiBaseForm, epiTransaction, o.ProductType, o.Company, beName, exName, o.Key1, eu.CustLayerMan, DeveloperLicenseType.Partner, LayerType.Customization);

                Log.Debug("Init Custom Controls");
                try
                {
                    Log.Debug("Creating Handle for Top Control");
                    typeof(Control).GetMethod("CreateHandle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(csm.TopControl, null);
                }
                catch (Exception ee)
                {
                    Log.Debug(ee, "Failed to Create Handle");
                }
                csm.InitCustomControlsAndProperties(ds, LayerName.CompositeBase, true);
                CustomScriptManager csmR = csm.CurrentCustomScriptManager;
                Log.Debug("Generate Refs");
                List<string> aliases = new List<string>();
                Match match = Regex.Match(csmR.CustomCodeAll, "((?<=extern alias )(.*)*(?=;))");
                while (match.Success)
                {
                    aliases.Add(match.Value.Replace("_", ".").ToUpper());
                    match = match.NextMatch();
                }
                o.Version = ds.XXXDef[0].SysRevID;
                Dictionary<decimal,string> netver = new Dictionary<decimal, string>();
                GenerateRefs(refds, csmR, o, netver);
                ExportCustmization(nds, ad, o);
                Resync(o, refds, ds, nds, csmR, netver);



                epiBaseForm.Dispose();


                ad.Dispose();
                cm = null;
                eu.CloseCacheRespinSplash();
                eu.Dispose();

                Console.WriteLine(o.ProjectFolder);
            }
            catch (Exception ee)
            {
                Log.Error(ee, "Error Downloading");
            }




            //MessageBox.Show(file);
        }

        /// <summary>
        /// Updates changes in Epicor from our local project
        /// </summary>
        /// <param name="o">Command Line Paramter</param>
        /// <param name="epiSession">Epicor Session</param>
        /// <returns></returns>
        public bool UpdateCustomization(CommandLineParams o, Session epiSession)
        {

            using (StreamReader sr = new StreamReader($@"{o.ProjectFolder}\Script.cs"))
            {
                var oTrans = new ILauncher(epiSession);
                Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(oTrans);
                ad.BOConnect();

                GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
                string script = (sr.ReadToEnd().Replace("public partial class Script", "public class Script").Replace("public static partial class Script", "public static class Script"));
                var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                var chunk = ds.XXXDef[0];
                if (chunk.SysRevID != o.Version && o.Version > 0)
                {
                    if (MessageBox.Show("The customization appears to have been updated internally within Epicor, this means that if you continue you may be over-writing some changes made. Would you like to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        return false;
                    }

                }
                string content = ad.GetDechunkedStringByIDWithCompany(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                XDocument doc = XDocument.Load(new StringReader(content));
                XNamespace ns = "http://tempuri.org/XMLSchema.xsd";
                var elList = doc.Descendants(ns + "PropertyName").ToList().Where(xm => xm.Value == "Script").FirstOrDefault();
                (elList.NextNode as XElement).Value = script;
                
                ad.ChunkNSaveUncompressedStringByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3, chunk.Description, chunk.Version, false, doc.ToString());
            }
            return true;
        }

        /// <summary>
        /// Save the working customization into a new temporary copy
        /// </summary>
        /// <param name="o">Command Line Paramter</param>
        /// <param name="epiSession">Epicor Session</param>
        /// <param name="customizationSuffix">Suffix to append to the end of the customization name to make it unique</param>
        /// <returns></returns>
        public bool CreateTemporaryCustomization(CommandLineParams o, Session epiSession, string customizationSuffix)
        {

            using (StreamReader sr = new StreamReader($@"{o.ProjectFolder}\Script.cs"))
            {
                var oTrans = new ILauncher(epiSession);
                Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(oTrans);
                ad.BOConnect();

                GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
                string script = (sr.ReadToEnd().Replace("public partial class Script", "public class Script").Replace("public static partial class Script", "public static class Script"));
                var ds = i.GetByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                i.GetNewXXXDef(ds, o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1 + customizationSuffix, o.Key2);
                var chunk = ds.XXXDef[0];
                if (chunk.SysRevID != o.Version && o.Version > 0)
                {
                    if (MessageBox.Show("The customization appears to have been updated internally within Epicor, this means that if you continue you may be over-writing some changes made. Would you like to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        return false;
                    }

                }
                string content = ad.GetDechunkedStringByIDWithCompany(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);
                XDocument doc = XDocument.Load(new StringReader(content));
                XNamespace ns = "http://tempuri.org/XMLSchema.xsd";
                var elList = doc.Descendants(ns + "PropertyName").ToList().Where(xm => xm.Value == "Script").FirstOrDefault();
                (elList.NextNode as XElement).Value = script;
                
                ad.ChunkNSaveUncompressedStringByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1 + customizationSuffix, o.Key2, o.Key3, chunk.Description, chunk.Version, false, doc.ToString());
            }
            return true;
        }

        /// <summary>
        /// Delete the temporary customization
        /// </summary>
        /// <param name="o">Command Line Paramter</param>
        /// <param name="epiSession">Epicor Session</param>
        /// <param name="customizationSuffix">Suffix to append to the end of the customization name to make it unique</param>
        /// <returns></returns>
        public bool DeleteTemporaryCustomization(CommandLineParams o, Session epiSession, string customizationSuffix)
        {
            var oTrans = new ILauncher(epiSession);
            Ice.Adapters.GenXDataAdapter ad = new Ice.Adapters.GenXDataAdapter(oTrans);
            ad.BOConnect();

            GenXDataImpl i = (GenXDataImpl)ad.BusinessObject;
            i.DeleteByID(o.Company, o.ProductType, o.LayerType, o.CSGCode, o.Key1, o.Key2, o.Key3);

            return true;
        }

    }
}
