using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizationEditor
{
    public class Environment
    {
        string path;

        public string Path { get => path; set => path = value; }
        public string Name { get => name; set => name = value; }

        string name;

        public override string ToString()
        {
            return Name;
        }
    }
}
