using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretLetter.Core
{
    public class AppConfig
    {
        public string LetterKey { get; set; } = "";
        public string InputPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
    }
}

