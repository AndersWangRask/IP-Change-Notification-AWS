using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using IPChange.Core;
using IPChange.Core.Model;
using static IPChange.Cmd.CommandLineUtilFunctions;
using IPChange.Core.Model.Config;

namespace IPChange.Cmd
{
    class Program
    {
        private static void Output(string _out)
        {
            if (string.IsNullOrWhiteSpace(_out))
            {
                return;
            }

            string msg = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + ": " + _out.Trim();

            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }

        static void Main(string[] args)
        {
            Dictionary<string, object> dicArgs = GetCommandLineArgs(args);

            //Set Relevant Base Path
            string basePath = Assembly.GetCallingAssembly()?.CodeBase;

            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Assembly.GetExecutingAssembly()?.CodeBase;
            }

            if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException($"A base path could not be resolved or did not exist.");
            }

            //Get the Work File
            string workFile;

            if (dicArgs.ContainsKey("workfile"))
            {
                workFile = (string)dicArgs["workfile"];
            }
            else
            {
                workFile = "default-workfile.xml";
            }

            //Find the Work file
            if (!Path.IsPathRooted(workFile))
            {

                workFile = Path.Combine(basePath, workFile);
            }

            if (!File.Exists(workFile))
            {
                throw new FileNotFoundException($"Workfile \"{workFile}\" could not be found. A workfile is required.", workFile);
            }

            Output($"Work File set: \"{workFile}\"");

            //Get the Log File
            string logFile;

            if (dicArgs.ContainsKey("logfile"))
            {
                logFile = (string)dicArgs["logfile"];
            }
            else
            {
                logFile = "default-iplog.xml";
            }

            //Find the Log File
            if (!Path.IsPathRooted(logFile))
            {
                logFile = Path.Combine(basePath, logFile);
            }

            if (!File.Exists(logFile))
            {
                throw new FileNotFoundException($"Workfile \"{logFile}\" could not be found. A workfile is required.", logFile);
            }

            Output($"Log File set: \"{logFile}\"");

            //Build the Config
            Config config = ConfigLoader.LoadConfigFromXml(workFile);

            Output($"Config loaded from Work File: \"{workFile}\"");

            //Run the IP Change Worker
            IpChangeWorker ipChangeWorker =
                new IpChangeWorker(
                    config,
                    logFile,
                    Output,
                    null);

            ipChangeWorker.Run();
        }
    }
}
