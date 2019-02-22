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
        private static Action<string> Output = Output_Null;

        private static void Output_Null(string msg)
        {
            Debug.WriteLine(msg);
        }

        private static void Output_Full(string msg)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                msg = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + ": " + msg.Trim();
            }

            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }

        private static void Output_Simple(string msg)
        {
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }

        private static string BasePath
        {
            get
            {
                if (_basePath == null)
                {
                    //Set Relevant Base Path
                    string basePathCandidate =
                        Assembly
                            .GetCallingAssembly()?
                            .CodeBase?
                            .Replace(@"file:///", "", StringComparison.OrdinalIgnoreCase);

                    if (string.IsNullOrWhiteSpace(basePathCandidate))
                    {
                        basePathCandidate =
                            Assembly
                                .GetExecutingAssembly()?
                                .CodeBase?
                                .Replace(@"file:///", "", StringComparison.OrdinalIgnoreCase);
                    }

                    if (string.IsNullOrWhiteSpace(basePathCandidate))
                    {
                        throw new DirectoryNotFoundException("A basepath could not be resolved");
                    }

                    basePathCandidate = Path.GetDirectoryName(basePathCandidate);

                    if (!Directory.Exists(basePathCandidate))
                    {
                        throw new DirectoryNotFoundException(
                            $"Base path \"{basePathCandidate}\" could not be resolved or did not exist.");
                    }

                    _basePath = basePathCandidate;
                }

                return _basePath;
            }
        }
        private static string _basePath = null;

        private static string getWorkFilePath(Dictionary<string, object> dicArgs)
        {
            string workFile =
                dicArgs.ContainsKey("workfile")
                ?
                (string)dicArgs["workfile"]
                :
                "default-workfile.xml";

            //Find the Work file
            if (!Path.IsPathRooted(workFile))
            {
                workFile = Path.Combine(BasePath, workFile);
            }

            if (!File.Exists(workFile))
            {
                throw new FileNotFoundException(
                    $"Workfile \"{workFile}\" could not be found. A workfile is required.", workFile);
            }

            return workFile;
        }

        private static string getLogFilePath(Dictionary<string, object> dicArgs)
        {
            string logFile =
                dicArgs.ContainsKey("logfile")
                ?
                (string)dicArgs["logfile"]
                :
                "default-iplog.xml";

            //Find the Log File
            if (!Path.IsPathRooted(logFile))
            {
                logFile = Path.Combine(BasePath, logFile);
            }

            if (!File.Exists(logFile))
            {
                throw new FileNotFoundException(
                    $"Workfile \"{logFile}\" could not be found. A workfile is required.", logFile);
            }

            return logFile;
        }

        static Config getConfig(Dictionary<string, object> dicArgs)
        {
            //Get the Work File
            string workFile = getWorkFilePath(dicArgs);

            Output($"Work File set: \"{workFile}\"");

            //Build the Config
            Config config = ConfigLoader.LoadConfigFromXml(workFile);

            Output($"Config loaded from Work File: \"{workFile}\"");

            return config;
        }

        static void Main(string[] args)
        {
            //Parse Command Line into a Dictionary
            Dictionary<string, object> dicArgs =
                GetCommandLineArgs(
                    args,
                    DefaultObjectValue: true,
                    DefaultValues:
                        new
                        {
                            ForceUpdate = false,
                            Help = false,
                            Display = ""
                        });

            //Display Help
            if ((bool)dicArgs["help"])
            {
                DisplayHelp(args, dicArgs);
            }
            //Display Something Command
            else if (!string.IsNullOrWhiteSpace((string)dicArgs["display"]))
            {
                DisplayCommand((string)dicArgs["display"], args, dicArgs);
            }
            //Do Main Work (Look for IP Change)
            else
            {
                MakeIpChange(args, dicArgs);
            }
        }

        static void MakeIpChange(string[] args, Dictionary<string, object> dicArgs)
        {
            //Set Outputter
            Output = Output_Full;

            //Load Config from Work File
            Config config = getConfig(dicArgs);

            //Get the Log File
            string logFile = getLogFilePath(dicArgs);

            Output($"Log File set: \"{logFile}\"");

            //Settings
            bool forceUpdate = (bool)dicArgs["forceupdate"];

            //Use and Run the IP Change Worker
            using (
                IpChangeWorker ipChangeWorker =
                    new IpChangeWorker(
                        config,
                        logFile,
                        Output,
                        null))
            {
                ipChangeWorker.Run(forceUpdate);
            }
        }

        static void DisplayHelp(string[] args, Dictionary<string, object> dicArgs)
        {
            //Set Outputter
            Output = Output_Simple;

            string helpFilePath = Path.Combine(BasePath, "help.txt");

            if (File.Exists(helpFilePath))
            {
                string helpContent = File.ReadAllText(helpFilePath);

                Output(helpContent);
            }
            else
            {
                Output($"Apparently the Help file is missing. Helpfile was expected at path: \"{helpFilePath}\"");
            }
        }

        static void DisplayCommand(string displayCommand, string[] args, Dictionary<string, object> dicArgs)
        {
            //Load Config from Work File
            Config config = getConfig(dicArgs);

            //Set Outputter
            Output = Output_Simple;

            //Select and Execute Display Command
            switch (displayCommand?.ToLowerInvariant())
            {
                case "mcs":
                case "clientstate":
                case "multiclientstate":
                    DisplayMultiClientState(args, dicArgs, config);
                    break;

                default:
                    DisplayUnknown(displayCommand);
                    break;
            }
        }

        static void DisplayMultiClientState(string[] args, Dictionary<string, object> dicArgs, Config config)
        {
            //Values
            List<MultiClientEntry> clients;

            using (MultiClientState multiClientState = new MultiClientState(config, null, null))
            {
                clients = multiClientState.GetClients();
            }

            //Output
            Output("");
            Output($"MCS: Listing {clients?.Count} clients from Multi Client State in workfile");
            Output("-----");

            int i = 0;

            foreach (MultiClientEntry client in clients)
            {
                i++;
                Output($"MCS {i}: {client}");
            }

            Output("-----");
            Output("");
        }

        static void DisplayUnknown(string displayCommand)
        {
            Console.WriteLine($"Unknown Display Command: \"{displayCommand}\"");
        }
    }
}