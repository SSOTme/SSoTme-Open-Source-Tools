using Plossum.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSoTme.OST.Lib.CLIOptions
{

    [CommandLineManager(ApplicationName = "SSOT.me Command Line Request",
                        Copyright = "Copyright (c) EJ Alexandra, CODEiverse.com",
                        Description = @"
SYNTAX: ssotme {command} [...{additional_args}] [options]

Options
")]
    public partial class SSoTmeCLIHandler
    {
        
        [CommandLineOption(Description = "Show help about how to use the ssotme cli", MinOccurs = 0, Aliases = "h")]
        public bool help { get; set; }
        
        [CommandLineOption(Description = "Initialize the current folder as the root of an SSOT.me project", MinOccurs = 0, Aliases = "")]
        public bool init { get; set; }
        
        [CommandLineOption(Description = "Saves the current command into the SSoTmeProject file", MinOccurs = 0, Aliases = "")]
        public bool install { get; set; }
        
        [CommandLineOption(Description = "Removes the current command from the SSoTme project file", MinOccurs = 0, Aliases = "")]
        public bool uninstall { get; set; }
        
        [CommandLineOption(Description = "Build any transpilers in the current folder (or children).", MinOccurs = 0, Aliases = "b")]
        public bool build { get; set; }
        
        [CommandLineOption(Description = "Builds all transpilers in the project", MinOccurs = 0, Aliases = "ba")]
        public bool buildAll { get; set; }
        
        [CommandLineOption(Description = "Describes the current SSoTme project (and all transpilers)", MinOccurs = 0, Aliases = "d")]
        public bool describe { get; set; }
        
        [CommandLineOption(Description = "Input filename or comma separated list of file names", MinOccurs = 0, Aliases = "i")]
        public string input { get; set; }
        
        [CommandLineOption(Description = "Output filename", MinOccurs = 0, Aliases = "o")]
        public string output { get; set; }
        
        [CommandLineOption(Description = "The account which the transpiler belongs to", MinOccurs = 0, Aliases = "a")]
        public string account { get; set; }
        
        [CommandLineOption(Description = "A list of parameters", MinOccurs = 0, Aliases = "p")]
        public List<string> parameters { get; set; }
        
        [CommandLineOption(Description = "List of project settings", MinOccurs = 0, Aliases = "ls")]
        public bool listSettings { get; set; }
        
        [CommandLineOption(Description = "Adds a setting to the SSoTmeProject", MinOccurs = 0, Aliases = "as")]
        public List<string> addSetting { get; set; }
        
        [CommandLineOption(Description = "REmoves a setting from the ssotme project", MinOccurs = 0, Aliases = "rs")]
        public List<string> removeSetting { get; set; }
        
        [CommandLineOption(Description = "The keyfile to use.  By default it looks for ~/SSOT.me/ssotme.key.", MinOccurs = 0, Aliases = "f")]
        public string keyFile { get; set; }
        
        [CommandLineOption(Description = "The email address for the account authenticating", MinOccurs = 0, Aliases = "e")]
        public string emailAddress { get; set; }
        
        [CommandLineOption(Description = "The secret associated with that email address", MinOccurs = 0, Aliases = "k")]
        public string secret { get; set; }
        
        [CommandLineOption(Description = "The amount of time to wait for the command to continue", MinOccurs = 0, Aliases = "w")]
        public int waitTimeout { get; set; }
        
        [CommandLineOption(Description = "Don't output the final results - instead, clean", MinOccurs = 0, Aliases = "c")]
        public bool clean { get; set; }
        
        [CommandLineOption(Description = "Don't output the final results - instead, clean", MinOccurs = 0, Aliases = "ca")]
        public bool cleanAll { get; set; }
        
        [CommandLineOption(Description = "Don't clean the output before cooking", MinOccurs = 0, Aliases = "sc")]
        public bool skipClean { get; set; }
        
        [CommandLineOption(Description = "Run as this user (look for this user's key file)", MinOccurs = 0, Aliases = "ra")]
        public string runAs { get; set; }
        
        [CommandLineOption(Description = "Determines if the input should be preserved.", MinOccurs = 0, Aliases = "rz")]
        public bool preserveZFS { get; set; }
        
    }
}


                    