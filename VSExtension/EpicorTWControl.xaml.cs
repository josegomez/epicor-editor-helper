namespace VSExtension
{
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Linq;
    using System.Windows.Threading;
    using System.Threading;
    using System;

    /// <summary>
    /// Interaction logic for EpicorTWControl.
    /// </summary>
    public partial class EpicorTWControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpicorTWControl"/> class.
        /// </summary>
        /// 

        EpicorTW parent;
        public EpicorTWControl(EpicorTW parent)
        {
            this.InitializeComponent();
            this.parent = parent;
            InfoBarService.Initialize(parent);
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void openCust_click(object sender, RoutedEventArgs e)
        {
           
        }
        bool editMode = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            frmSettings set = new frmSettings();
            set.ShowDialog();
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSettings())
            {
                StringBuilder args = new StringBuilder();
                args.Append($"-f \"{Settings.Default.EpicorFolder}\"");
                args.Append(" ");
                args.Append($"-r \"{Settings.Default.CustomiationPath}\"");
                args.Append(" -a Add");
                editMode = false;
                runCommand(args, true);
            }
        }

        private static bool CheckSettings()
        {
            bool rturn = !string.IsNullOrEmpty(Settings.Default.EpicorFolder) && (!string.IsNullOrEmpty(Settings.Default.CustomiationPath)); ;
            if(!rturn)
            {
                MessageBox.Show("Settings are missing!");
            }
            return rturn;
        }
        System.Diagnostics.Process process;
        public void runCommand(StringBuilder args, bool wait=false)
        {
            //* Create your Process
            if (process != null && !process.HasExited)
            {
                BringProcessToFront(process);
            }
            else
            {
                process = new System.Diagnostics.Process();
                process.StartInfo.FileName = Path.Combine(Settings.Default.EpicorFolder, "CustomizationEditor.exe");
                process.StartInfo.Arguments = args.ToString();
                process.StartInfo.WorkingDirectory = Settings.Default.EpicorFolder;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                //* Set your output and error (asynchronous) handlers
                process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
                //* Start process and handlers
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.EnableRaisingEvents = true;
                process.Exited += Process_Exited;
            }
            
        }

        public static void BringProcessToFront(System.Diagnostics.Process process)
        {
            IntPtr handle = process.MainWindowHandle;
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            SetForegroundWindow(handle);
        }

        const int SW_RESTORE = 9;

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        private void Process_Exited(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
                dte.Properties["Environment", "Documents"].Item("DetectFileChangesOutsideIDE").Value = 1;
                if(dte.Solution.IsOpen)
                {
                    dte.Solution.Open(dte.Solution.FileName);
                }
            }));
        }

        public void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {


            //* Do your stuff with the output (write to console/log/StringBuilder)
            //Console.WriteLine(outLine.Data);
            if (outLine.Data != null)
            {
                if (outLine.Data.Trim() == "EDITMODE")
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
                        dte.ExecuteCommand("File.SaveAll");
                        dte.Properties["Environment", "Documents"].Item("DetectFileChangesOutsideIDE").Value = 0;
                        InfoBarService.Instance.ShowInfoBar("You are editing the customization in Epicor, do not make changes in Visual Studio until you are done and this message has been dismissed");
                    }));
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {

                        //Update UI here
                        InfoBarService.Instance.CloseInfoBar();
                        DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
                        
                        var sol = Directory.GetFiles(outLine.Data, "*.sln").FirstOrDefault();
                        if (sol == null)
                        {
                            sol = Directory.GetFiles(outLine.Data, "*.csproj").FirstOrDefault();
                            
                            dte.Solution.Create(Path.GetDirectoryName(sol), $"{Path.GetFileNameWithoutExtension(sol)}.sln");
                            dte.ExecuteCommand("File.AddExistingProject", $"\"{sol}\"");
                            dte.ExecuteCommand("File.SaveAll");
                        }
                        else
                            dte.Solution.Open(sol);

                        dte.Properties["Environment", "Documents"].Item("DetectFileChangesOutsideIDE").Value = 1;

                    }));
                    

                }
            }
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSettings())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
                    Project p;
                    var projs = dte.ActiveSolutionProjects;
                    if (projs != null && ((projs as Object[]).Length>0))
                    {
                        p = (projs as Object[])[0] as Project;
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(p.FileName), "CustomizationInfo.json")))
                        {
                            var dyn = Newtonsoft.Json.JsonConvert.DeserializeObject<CustomizationInfo>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(p.FileName), "CustomizationInfo.json")));
                            StringBuilder args = new StringBuilder();
                            args.Append($"-c \"{dyn.ConfigFile}\"");
                            args.Append(" ");
                            args.Append($"-u \"{(string.IsNullOrEmpty(dyn.Username) ? "~" : dyn.Username)}\"");
                            args.Append(" ");
                            args.Append($"-p \"{dyn.Password}\"");
                            args.Append(" ");
                            args.Append($"-t \"{dyn.ProductType}\"");
                            args.Append(" ");
                            args.Append($"-l \"{dyn.LayerType}\"");
                            args.Append(" ");
                            args.Append($"-k \"{dyn.Key1}\"");
                            args.Append(" ");
                            args.Append($"-m \"{dyn.Key2}\"");
                            args.Append(" ");
                            args.Append($"-n \"{(string.IsNullOrEmpty(dyn.Key3) ? "~" : dyn.Key3)}\"");
                            args.Append(" ");
                            args.Append($"-g \"{(string.IsNullOrEmpty(dyn.CSGCode) ? "~" : dyn.CSGCode)}\"");
                            args.Append(" ");
                            args.Append($"-f \"{Settings.Default.EpicorFolder}\"");
                            args.Append(" ");
                            args.Append($"-o \"{(string.IsNullOrEmpty(dyn.Company) ? "~" : dyn.Company)}\"");
                            args.Append(" ");
                            args.Append($"-r \"{dyn.Folder}\"");
                            args.Append(" ");
                            args.Append($"-j \"{dyn.ProjectFolder}\"");
                            args.Append(" ");
                            args.Append($"-e \"{dyn.Encrypted}\"");
                            args.Append(" ");
                            args.Append($"-v \"{dyn.Version}\"");
                            args.Append(" -a Download");
                            editMode = true;
                            runCommand(args, true);
                        }
                    }
                }));
     


            }
        }

        private void BtnTools_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSettings())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
                    Project p;
                    var projs = dte.ActiveSolutionProjects;
                    if (projs != null && ((projs as Object[]).Length > 0))
                    {
                        p = (projs as Object[])[0] as Project;
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(p.FileName), "CustomizationInfo.json")))
                        {
                            var dyn = Newtonsoft.Json.JsonConvert.DeserializeObject<CustomizationInfo>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(p.FileName), "CustomizationInfo.json")));
                            StringBuilder args = new StringBuilder();
                            args.Append($"-c \"{dyn.ConfigFile}\"");
                            args.Append(" ");
                            args.Append($"-u \"{(string.IsNullOrEmpty(dyn.Username) ? "~" : dyn.Username)}\"");
                            args.Append(" ");
                            args.Append($"-p \"{dyn.Password}\"");
                            args.Append(" ");
                            args.Append($"-t \"{dyn.ProductType}\"");
                            args.Append(" ");
                            args.Append($"-l \"{dyn.LayerType}\"");
                            args.Append(" ");
                            args.Append($"-k \"{dyn.Key1}\"");
                            args.Append(" ");
                            args.Append($"-m \"{dyn.Key2}\"");
                            args.Append(" ");
                            args.Append($"-n \"{(string.IsNullOrEmpty(dyn.Key3) ? "~" : dyn.Key3)}\"");
                            args.Append(" ");
                            args.Append($"-g \"{(string.IsNullOrEmpty(dyn.CSGCode) ? "~" : dyn.CSGCode)}\"");
                            args.Append(" ");
                            args.Append($"-f \"{Settings.Default.EpicorFolder}\"");
                            args.Append(" ");
                            args.Append($"-o \"{(string.IsNullOrEmpty(dyn.Company) ? "~" : dyn.Company)}\"");
                            args.Append(" ");
                            args.Append($"-r \"{dyn.Folder}\"");
                            args.Append(" ");
                            args.Append($"-j \"{dyn.ProjectFolder}\"");
                            args.Append(" ");
                            args.Append($"-e \"{dyn.Encrypted}\"");
                            args.Append(" ");
                            args.Append($"-v \"{dyn.Version}\"");
                            args.Append(" -a Toolbox");
                            editMode = true;
                            runCommand(args);
                        }
                    }
                }));
                /*dte.act

                StringBuilder args = new StringBuilder();
                args.Append($"-f \"{Settings.Default.EpicorFolder}\"");
                args.Append(" ");
                args.Append($"-r \"{Settings.Default.CustomiationPath}\"");
                args.Append(" -a Add");

                runCommand(args);*/




            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSettings())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
                    Project p;
                    var projs = dte.ActiveSolutionProjects;
                    if (projs != null && ((projs as Object[]).Length > 0))
                    {
                        p = (projs as Object[])[0] as Project;
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(p.FileName), "CustomizationInfo.json")))
                        {
                            
                            var dyn = Newtonsoft.Json.JsonConvert.DeserializeObject<CustomizationInfo>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(p.FileName), "CustomizationInfo.json")));
                            StringBuilder args = new StringBuilder();
                            args.Append($"-c \"{dyn.ConfigFile}\"");
                            args.Append(" ");
                            args.Append($"-u \"{(string.IsNullOrEmpty(dyn.Username) ? "~" : dyn.Username)}\"");
                            args.Append(" ");
                            args.Append($"-p \"{dyn.Password}\"");
                            args.Append(" ");
                            args.Append($"-t \"{dyn.ProductType}\"");
                            args.Append(" ");
                            args.Append($"-l \"{dyn.LayerType}\"");
                            args.Append(" ");
                            args.Append($"-k \"{dyn.Key1}\"");
                            args.Append(" ");
                            args.Append($"-m \"{dyn.Key2}\"");
                            args.Append(" ");
                            args.Append($"-n \"{(string.IsNullOrEmpty(dyn.Key3) ? "~" : dyn.Key3)}\"");
                            args.Append(" ");
                            args.Append($"-g \"{(string.IsNullOrEmpty(dyn.CSGCode) ? "~" : dyn.CSGCode)}\"");
                            args.Append(" ");
                            args.Append($"-f \"{Settings.Default.EpicorFolder}\"");
                            args.Append(" ");
                            args.Append($"-o \"{(string.IsNullOrEmpty(dyn.Company) ? "~" : dyn.Company)}\"");
                            args.Append(" ");
                            args.Append($"-r \"{dyn.Folder}\"");
                            args.Append(" ");
                            args.Append($"-j \"{dyn.ProjectFolder}\"");
                            args.Append(" ");
                            args.Append($"-e \"{dyn.Encrypted}\"");
                            args.Append(" ");
                            args.Append($"-v \"{dyn.Version}\"");
                            args.Append(" -a Update");
                            editMode = true;
                            runCommand(args, true);
                        }
                    }
                }));
   

            }
        }
    }
}