using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizationEditor
{
   public class CommandLineParams
    {

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
        public string Key3 { get; set; }
        [Option('g', "cgccode", Required = false, HelpText = "Epicor CGCCode")]
        public string CSGCode { get; set; }
        [Option('f', "clientfolder", Required = true, HelpText = "Epicor Client Folder")]
        public string EpicorClientFolder { get; set; }
        [Option('o', "company", Required = false, HelpText = "Epicor Company for Customization")]
        public string Company { get; set; }
        [Option('r', "outputfolder", Required = true, HelpText = "Code output Folder")]
        public string Folder { get; set; }
        [Option('a', "action", Required = true, HelpText = "Action")]
        public string Action { get; set; }
        [Option('j', "outputproject", Required = false, HelpText = "Project Folder")]
        public string ProjectFolder { get; set; }
        [Option('d', "dll", Required = false, HelpText = "DLL Location")]
        public string DLLLocation { get; set; }
        [Option('y', "dll", Required = false, HelpText = "DN Spy Location")]
        public string DNSpy { get; set; }
    }
}
