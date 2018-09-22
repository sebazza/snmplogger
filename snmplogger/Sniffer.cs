using System;
using System.Net;
using System.Diagnostics;
using SharpPcap;
using PacketDotNet;


namespace snmplogger
{
    public sealed class Sniffer
    {

        private int default_port = 161; 

        private static readonly Sniffer instance = new Sniffer();

        private Sniffer() {}

        public static Sniffer Instance
        {
            get
            {
                return instance;
            }
        }


        public void Start()
        {
            // Retrieve all capture devices
            var devices = CaptureDeviceList.Instance;
            string port = default_port.ToString();
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


        private static void HandlePacket(object sender, CaptureEventArgs ev)
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


        private static Packet ParseEvent(CaptureEventArgs ev)
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


        private static IpPacket ExtractIpPacket(Packet packet)
        {
            IpPacket ip_packet = (IpPacket)packet.Extract(typeof(IpPacket));
            Debug.WriteLineIf((ip_packet != null), "IP packet: " + ip_packet.ToString());

            return ip_packet;
        }



        private static UdpPacket ExtractUdpPacket(IpPacket ip_packet)
        {
            UdpPacket udp_packet = (UdpPacket)ip_packet.Extract(typeof(UdpPacket));
            Debug.WriteLineIf(udp_packet != null, "UDP packet: " + udp_packet.ToString());

            return udp_packet;
        }



    }
}
