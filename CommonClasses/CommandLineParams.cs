using CommandLine;

namespace CustomizationEditor
{
    public class CommandLineParams
    {
        private string _key3;
        private string _cSGCode;
        private string _company;
        private string _dLLLocation;
        private string _dNSpy;

        [Option('c', "config", Required = false, HelpText = "Epicor config file")]
        public string ConfigFile { get; set; }
        [Option('u', "username", Required = false, HelpText = "Epicor username")]
        public string Username { get; set; }
        [Option('p', "password", Required = false, HelpText = "Epicor password")]
        public string Password { get; set; }
        [Option('t', "producttype", Required = false, HelpText = "Epicor Product Type Default EP")]
        public string ProductType { get; set; }
        [Option('l', "layertype", Required = false, HelpText = "Epicor Layer Type Default Customization")]
        public string LayerType { get; set; }
        [Option('k', "key1", Required = false, HelpText = "Epicor Customization Key1")]
        public string Key1 { get; set; }
        [Option('m', "key2", Required = false, HelpText = "Epicor Customization Key2")]
        public string Key2 { get; set; }
        [Option('n', "key3", Required = false, HelpText = "Epicor Customization Key3")]
        public string Key3 { get => _key3; set => _key3 = value == "~" ? "" : value; }
        [Option('g', "cgccode", Required = false, HelpText = "Epicor CGCCode")]
        public string CSGCode { get => _cSGCode; set => _cSGCode = value == "~" ? "" : value; }
        [Option('f', "clientfolder", Required = true, HelpText = "Epicor Client Folder")]
        public string EpicorClientFolder { get; set; }
        [Option('o', "company", Required = false, HelpText = "Epicor Company for Customization")]
        public string Company { get => _company; set => _company = value == "~" ? "" : value; }
        [Option('r', "outputfolder", Required = true, HelpText = "Code output Folder")]
        public string Folder { get; set; }
        [Option('a', "action", Required = true, HelpText = "Action")]
        public string Action { get; set; }
        [Option('j', "outputproject", Required = false, HelpText = "Project Folder")]
        public string ProjectFolder { get; set; }
        [Option('d', "dll", Required = false, HelpText = "DLL Location")]
        public string DLLLocation { get => _dLLLocation; set => _dLLLocation = value == "~" ? "" : value; }
        [Option('y', "dn", Required = false, HelpText = "DN Spy Location")]
        public string DNSpy { get => _dNSpy; set => _dNSpy = value == "~" ? "" : value; }
        [Option('v', "customizationversion", Required = false, HelpText = "Customization Version", Default = 0)]
        public long Version { get; set; }
        [Option('e', "encrypted", Required = false, HelpText = "Password Encrypted", Default = "false")]
        public string Encrypted { get; set; }
    }
}
