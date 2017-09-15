using System;
using System.ComponentModel;
using SassyMQ.SSOTME.Lib.RMQActors;
using System.IO;
using System.Diagnostics;
using SSoTme.OST.Lib.CLIOptions;
using SSoTme.OST.Lib.Extensions;

namespace SSoTme.OST.Lib.DataClasses
{
    public partial class ProjectTranspiler
    {
        public ProjectTranspiler()
        {
            this.InitPoco();
        }

        public ProjectTranspiler(string relativePath, SSOTMEPayload result)
            : this()
        {
            this.Name = result.Transpiler.Name;
            this.RelativePath = relativePath;
            if (Environment.CommandLine.Contains(" "))
            {
                this.CommandLine = Environment.CommandLine.Substring(Environment.CommandLine.IndexOf(" "));
                this.CommandLine = this.CommandLine.Replace("-install", "").Trim();
            }
            else this.CommandLine = String.Empty;
        }

        internal void Rebuild(SSoTmeProject project)
        {
            Console.WriteLine("RE-transpiling: " + this.RelativePath + ": " + this.Name);
            Console.WriteLine("CommandLine:> ssotme {0}", this.CommandLine);
            Environment.CurrentDirectory = Path.Combine(project.RootPath, this.RelativePath.Trim("\\/".ToCharArray()));
            var cliResult = SSoTmeCLIHandler.ProcessCommand(this.CommandLine);
            if (cliResult != 0) throw new Exception("Error RE-Transpiling");
        }

        internal void Clean(SSoTmeProject project, bool preserveZFS)
        {
            Console.WriteLine("CLEANING: " + this.RelativePath + ": " + this.Name);
            Console.WriteLine("CommandLine:> ssotme {0}", this.CommandLine);
            Environment.CurrentDirectory = Path.Combine(project.RootPath, this.RelativePath.Trim("\\/".ToCharArray()));
            String zsfFileName = String.Format("{0}.zfs", this.Name.ToTitle().ToLower().Replace(" ", "-"));
            var zfsFI = new FileInfo(zsfFileName);
            if (zfsFI.Exists)
            {
                var zippedFileSet = File.ReadAllBytes(zfsFI.FullName);
                zippedFileSet.CleanZippedFileSet();
                if (!preserveZFS) File.Delete(zfsFI.FullName);
            }
        }

        public void Describe(SSoTmeProject project)
        {
            Console.WriteLine("\n-----------------------------------");
            Console.WriteLine("---- {0}", this.Name);
            Console.WriteLine("---- .{0}/", this.RelativePath.Replace("\\","/"));
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("\nCommand Line:> ssotme {0}\n", this.CommandLine);
        }
    }
}