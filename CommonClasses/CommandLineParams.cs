using CommandLine;


namespace CustomizationEditor
{
    /// <summary>
    /// This is a class which us used by the CommandLine library to hold
    /// the command line parameters passed to the application.
    /// </summary>
    public class CommandLineParams
    {
        private string _key3;
        private string _cSGCode;
        private string _company;
        private string _dLLLocation;
        private string _dNSpy;
        private string _username;
        private string _password;

        //Epicor Configuration File
        [Option('c', "config", Required = false, HelpText = "Epicor config file")]
        public string ConfigFile { get; set; }

        //Epicor Username
        [Option('u', "username", Required = false, HelpText = "Epicor username")]
        public string Username { get => _username; set => _username = value=="~"?"":value; }

        //Epicor Password (encrypted)
        [Option('p', "password", Required = false, HelpText = "Epicor password")]
        public string Password { get => _password; set => _password = value; }

        //Customization Product Type Key Typical :EP
        [Option('t', "producttype", Required = false, HelpText = "Epicor Product Type Default EP")]
        public string ProductType { get; set; }

        //Customization Layer Type Typically:Customization
        [Option('l', "layertype", Required = false, HelpText = "Epicor Layer Type Default Customization")]
        public string LayerType { get; set; }

        //Customization Key1 Typically: Customization ID
        [Option('k', "key1", Required = false, HelpText = "Epicor Customization Key1")]
        public string Key1 { get; set; }

        //Customization Key2 Typically: Application ID
        [Option('m', "key2", Required = false, HelpText = "Epicor Customization Key2")]
        public string Key2 { get; set; }

        //Customization Key3
        [Option('n', "key3", Required = false, HelpText = "Epicor Customization Key3")]
        public string Key3 { get => _key3; set => _key3 = value == "~" ? "" : value; }

        //Customization CGCCode
        [Option('g', "cgccode", Required = false, HelpText = "Epicor CGCCode")]
        public string CSGCode { get => _cSGCode; set => _cSGCode = value == "~" ? "" : value; }

        //Epicor Client Folder
        [Option('f', "clientfolder", Required = true, HelpText = "Epicor Client Folder")]
        public string EpicorClientFolder { get; set; }

        //Company which the customization applies to, blank if it's a global customization
        [Option('o', "company", Required = false, HelpText = "Epicor Company for Customization")]
        public string Company { get => _company; set => _company = value == "~" ? "" : value; }

        //Folder where customizations are downloaded to
        [Option('r', "outputfolder", Required = true, HelpText = "Code output Folder")]
        public string Folder { get; set; }

        //Action to take Add , Update, Sync, Download, ToolBox etc..
        [Option('a', "action", Required = true, HelpText = "Action")]
        public string Action { get; set; }

        //Project folder where changes are downloaded to
        [Option('j', "outputproject", Required = false, HelpText = "Project Folder")]
        public string ProjectFolder { get; set; }

        //DLL location of the compiled customization
        [Option('d', "dll", Required = false, HelpText = "DLL Location")]
        public string DLLLocation { get => _dLLLocation; set => _dLLLocation = value == "~" ? "" : value; }

        //Location of the DNSpy Library
        [Option('y', "dn", Required = false, HelpText = "DN Spy Location")]
        public string DNSpy { get => _dNSpy; set => _dNSpy = value == "~" ? "" : value; }

        //Customization Version XXXDef.SysRevID
        [Option('v', "customizationversion", Required = false, HelpText = "Customization Version", Default = 0)]
        public long Version { get; set; }

        //Encrypted Flag Typically:Yes
        [Option('e', "encrypted", Required = false, HelpText = "Password Encrypted", Default = "false")]
        public string Encrypted { get; set; }

        //Launch with Tracing (not used) TODO: Remove
        [Option('s', "tracing", Required = false, HelpText = "Launch with Tracing", Default = "false")]
        public string Tracing { get; set; }


        public string NewConfig { get; set; }


        public string Temp { get; set; }
    }
}
