using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using SharpPcap;
using PacketDotNet;
using System.Reflection;
using System.Timers;
using System.Collections.Generic;


namespace snmplogger
{
    public class MainClass
    {

        protected static List<string> LoadedAssemblies = new List<string>();

        public static void Main(string[] args)
        {
            Console.CancelKeyPress += CleanUpAssemblies;

            SetUpAssemblyResolution();

            BuildOids();

            SetUpLogRotate();

            SnmpSniffer();

            CleanUpAssemblies();
            //Oid.DumpOids();
        }

        //public static void SnmpMonitor()
        //{
        //	bool done = false;
        //	var listen_port = 161;
        //	UdpClient listener = new UdpClient(listen_port);
        //	IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listen_port);

        //	byte[] received_bytes;
        //	try
        //	{
        //		Console.WriteLine("Listening for SNMP traps on port {0}", listen_port);

        //		while (!done)
        //		{
        //			received_bytes = listener.Receive(ref groupEP);
        //			Console.WriteLine("Received broadcast from {0}", groupEP.Address);
        //			try
        //			{
        //				var message = new SnmpTrapMessage(received_bytes);

        //				if (message != null)
        //				{

        //					Console.WriteLine("Log: {0}", message.LogMessage());

        //					if (message.IsValidSnmpMessage())
        //					{
        //						LogMessage(message, groupEP.Address);
        //					}
        //					else
        //					{
        //						LogInvalidMessage(message, ErrorFilename(groupEP.Address));
        //					}
        //				}
        //			}
        //			catch (BerException e)
        //			{
        //				LogMessage("Not a valid message: " + e.ToString() + Environment.NewLine + "Message: " + Utils.GetBytes(received_bytes),
        //						   ErrorFilename(groupEP.Address));
        //			}
        //			catch (Exception e)
        //			{
        //				LogMessage("Not a valid message: " + e.ToString() + Environment.NewLine + " Message: " + Utils.GetBytes(received_bytes),
        //						   ErrorFilename(groupEP.Address));
        //			}
        //		}
        //	}
        //	catch (Exception e)
        //	{
        //		Console.WriteLine(e);
        //	}

        //	listener.Close();
        //}


        protected static void SnmpSniffer()
        {
            // Retrieve all capture devices
            var devices = CaptureDeviceList.Instance;
            string port = "161";
            string filter = "udp dst port " + port;

            foreach (ICaptureDevice dev in devices)
            {
                dev.OnPacketArrival += new PacketArrivalEventHandler(HandlePacket);
                // Open the device for capturing
                dev.Open(DeviceMode.Promiscuous);

                Console.WriteLine("-- Listening for SNMP (port {0}) on {1}...", port, dev.Name);
                dev.Filter = filter;
                // Start the capturing process
                dev.StartCapture();
            }

            // Wait for 'Enter' from the user.
            Console.WriteLine("Hit Enter to stop" + Environment.NewLine);
            Console.ReadLine();

            foreach (ICaptureDevice dev in devices)
            {
                dev.StopCapture();
                dev.Close();
            }
        }


        protected static void HandlePacket(object sender, CaptureEventArgs ev)
        {
            try
            {
                Packet packet = ParseEvent(ev);

                if (packet != null)
                {
                    IpPacket ip_packet = ExtractIpPacket(packet);
                    if (ip_packet != null)
                    {
                        IPAddress source_address = ip_packet.SourceAddress;
                        UdpPacket udp_packet = ExtractUdpPacket(ip_packet);

                        if (udp_packet != null)
                        {
                            try
                            {
                                var message = new SnmpMessage(udp_packet.PayloadData);

                                lock ("log_message")
                                {
                                    if (message.IsValidSnmpMessage())
                                    {
                                        Log.LogMessage(message, source_address);
                                    }
                                    else
                                    {
                                        Log.LogInvalidMessage(message, source_address);
                                    }
                                }

                            }
                            catch (BerException ex)
                            {
                                Debug.WriteLine(ex.ToString());
                                Log.LogInvalidMessage("Error: " + ex.ToString() + Environment.NewLine + "  Message raw data: " + Utils.GetBytes(udp_packet.PayloadData),
                                                      source_address);
                            }
                        }

                        Console.WriteLine("{0} Message received from {1}", DateTime.Now.ToString("s"), source_address);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Log.LogMessage("Expection: " + ex.ToString());
            }
        }


        protected static Packet ParseEvent(CaptureEventArgs ev)
        {
            Packet packet = Packet.ParsePacket(ev.Packet.LinkLayerType, ev.Packet.Data);
            if (packet is EthernetPacket)
            {
                var eth = (EthernetPacket)packet;
                Debug.WriteLine("Ethernet packet: " + eth.ToString());
            }
            else if (packet is NullPacket)
            {
                var nul = (NullPacket)packet;
                Debug.WriteLine("Null/Loopback packet: " + nul.ToString());
            }
            return packet;
        }


        protected static IpPacket ExtractIpPacket(Packet packet)
        {
            IpPacket ip_packet = (IpPacket)packet.Extract(typeof(IpPacket));
            Debug.WriteLineIf((ip_packet != null), "IP packet: " + ip_packet.ToString());

            return ip_packet;
        }



        protected static UdpPacket ExtractUdpPacket(IpPacket ip_packet)
        {
            UdpPacket udp_packet = (UdpPacket)ip_packet.Extract(typeof(UdpPacket));
            Debug.WriteLineIf(udp_packet != null, "UDP packet: " + udp_packet.ToString());

            return udp_packet;
        }


        protected static void BuildOids()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("snmptrapper.snmp-oids.txt"))
            {
                TextReader tr = new StreamReader(stream);
                Oid.Mib2SchemaFileToOidTree(tr);
            }
        }


        protected static void SetUpLogRotate()
        {
            Log.Rotate();
            Timer timer = new Timer();
            //timer.Interval = (1000 * 60 * 60 * 24);
            timer.Interval = (1000 * 6);
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

            Stream dll_config = Assembly.GetEntryAssembly().GetManifestResourceStream("snmptrapper." + dll_name + ".dll.config");
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
            using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream("snmptrapper." + dll_name + ".dll"))
            {
                byte[] data = new byte[s.Length];
                s.Read(data, 0, data.Length);
                File.WriteAllBytes(dll_path, data);
            }
            LoadedAssemblies.Add(dll_path);
            Assembly assembly = Assembly.LoadFrom(dll_path);
            File.Delete(dll_path);
            if (dll_config != null)
            {
                File.Delete(dll_config_path);
            }
            return assembly;
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
    }
}







