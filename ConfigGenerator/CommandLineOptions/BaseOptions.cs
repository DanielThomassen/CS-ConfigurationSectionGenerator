using Fclp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigGenerator.CommandLineOptions
{
    public class BaseOptions
    {
        public string Input { get; set; }
        public string Output { get; set; }
        public bool MultipleFiles { get; set; }        
    }
}
