using System;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Collections.Generic;
using System.IO.Compression;
using Mono.Options;



namespace snmplogger
{
    public class MainClass
    {

        protected static List<string> LoadedAssemblies = new List<string>();
        protected static int port;

        public static void Main(string[] args)
        {
            //Console.CancelKeyPress += CleanUpAssemblies;

            ProcessCommandLineOptions();

            SetUpAssemblyResolution();
            BuildOids();

            SetUpLogRotate();

            Sniffer sniffer = Sniffer.Instance;
            sniffer.Start();


            //CleanUpAssemblies();
            //Oid.DumpOids();
        }


        protected static void BuildOids()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("snmplogger.snmp-oids.txt.gz"))
            {
                GZipStream gunzip_stream = new GZipStream(stream, CompressionMode.Decompress);
                TextReader tr = new StreamReader(gunzip_stream);
                Oid.Mib2SchemaFileToOidTree(tr);
            }
        }


        protected static void SetUpLogRotate()
        {
            Log.Rotate();
            Timer timer = new Timer();
            timer.Interval = (1000 * 60 * 60 * 24);
            timer.Elapsed += new ElapsedEventHandler(LogRotate);
            timer.AutoReset = true;
            timer.Start();
        }


        protected static void LogRotate(object sender, ElapsedEventArgs e)
        {
            Log.Rotate();
        }


        protected static void SetUpAssemblyResolution()
        {

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

        }


        protected static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dll_name = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");

            dll_name = dll_name.Replace(".", "_");

            if (dll_name.EndsWith("_resources")) return null;

            string dll_config_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dll_name + ".dll.config");

            Stream dll_config = Assembly.GetEntryAssembly().GetManifestResourceStream("snmplogger." + dll_name + ".dll.config");
            if (dll_config != null)
            {
                using (Stream s = dll_config)
                {
                    byte[] data = new byte[s.Length];
                    s.Read(data, 0, data.Length);
                    File.WriteAllBytes(dll_config_path, data);
                }
                LoadedAssemblies.Add(dll_config_path);
            }

            string dll_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dll_name + ".dll");
            if (!LoadedAssemblies.Contains(dll_path))
            {
                using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream("snmplogger." + dll_name + ".dll"))
                {
                    byte[] data = new byte[s.Length];
                    s.Read(data, 0, data.Length);
                    File.WriteAllBytes(dll_path, data);
                }
                LoadedAssemblies.Add(dll_path);
            }
            return Assembly.LoadFrom(dll_path);

        }


        protected static void CleanUpAssemblies(object sender, ConsoleCancelEventArgs args)
        {
            CleanUpAssemblies();
        }


        protected static void CleanUpAssemblies()
        {
            foreach (string file in LoadedAssemblies)
            {
                File.Delete(file);
            }

        }


        protected static void ProcessCommandLineOptions()
        {
            bool show_help = false;

            OptionSet options = new OptionSet()
            {
                {"p|port", "port(s) to sniff for SNMP messages", (int p) => port = p},
                {"h|?|help", "print this message", h => show_help = true}
            };

            if (show_help)
            {
                DisplayHelp(options);
            }
        }


        protected static void DisplayHelp(OptionSet options)
        {
            Console.WriteLine();

            options.WriteOptionDescriptions(Console.Out);
        }

    }
}







