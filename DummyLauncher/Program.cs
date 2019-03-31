using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommandLine;

/*
 * This program is designed to simply launch a program and exits itself
 * The purpose of this program is the launch another EXE without windows
 * recognising it as part of the process tree 
 * */

namespace DummyLauncher
{
    class Options
    {
        [Option('p', "process", Required = true, HelpText = "Process to start")]
        public string Process { get; set; }

        [Option('d', "dir", Required = false, HelpText = "Directory to start process")]
        public string Directory { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string processPath = String.Empty;
            string cwdir = String.Empty;
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       Console.WriteLine($"Current Arguments: -p {o.Process}");
                       processPath = o.Process;
                       cwdir = o.Directory;
                   });

            Process process = new Process();
            // Configure the process using the StartInfo properties.
            process.StartInfo.FileName = processPath;
            process.StartInfo.Arguments = "";
            if (cwdir != String.Empty && cwdir != null)
                process.StartInfo.WorkingDirectory = cwdir;
            process.Start();
            System.Environment.Exit(1);
        }
    }
}
