using System;
using System.IO;
using System.Net;


namespace snmplogger
{
	public class Log
	{
		protected static string application_log_stem = "snmptrapper";
		protected static string logfile_stem = "traps";
		protected static string log_extension = ".log";
		protected static int depth_of_rotation = 1;


		private Log()
		{
		}


		public static void LogMessage(string message)
		{
			LogMessage(message, ApplicationLogFilename());
		}


		public static void LogMessage(SnmpMessage message, IPAddress ip_address)
		{
			string filename = Log.LogFilename(ip_address);
			Log.LogMessage(message.LogMessage(), filename);
		}


		public static void LogInvalidMessage(SnmpMessage message, IPAddress ip_address)
		{
			string filename = Log.ErrorLogFilename(ip_address);
			Log.LogInvalidMessage(message.MessageData + Environment.NewLine + message.Dump(), filename);
		}

		public static void LogInvalidMessage(string message, IPAddress ip_address)
		{
			string filename = Log.ErrorLogFilename(ip_address);
			Log.LogInvalidMessage(message, filename);
		}

		protected static void LogMessage(string message, string filename)
		{
			using (StreamWriter sw = File.AppendText(filename))
			{
				sw.WriteLine(DateTime.Now.ToString("s") + " " + message);
			}
		}

		protected static void LogInvalidMessage(string message, string filename)
		{
			LogMessage("Not a valid SNMP message: " + message, filename);
		}


		protected static string LogFilename(IPAddress ip_address)
		{
			return Log.logfile_stem + "." + ip_address.ToString() + Log.log_extension;
		}


		protected static string ErrorLogFilename(IPAddress ip_address)
		{
			return Log.logfile_stem + "." + ip_address.ToString() + ".err";
		}

		protected static string ApplicationLogFilename()
		{
			return application_log_stem + Log.log_extension;
		}





		public static void Rotate()
		{
            Console.WriteLine("Rotate");
			string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(),
			                                    logfile_stem + ".*",
			                                    SearchOption.TopDirectoryOnly);
			foreach (string path in files)
			{
				DateTime file_date = File.GetCreationTime(path);
                Console.WriteLine("Check {0}: {1}", path, file_date);
				if (file_date.CompareTo(DateTime.Today) < 0)
				{
					RotateFile(path);
				}
			}
		}


		protected static void RotateFile(string path)
		{
			int index;
			string ext = Path.GetExtension(path).TrimStart(new Char[] { '.' });
			{
				if (Int32.TryParse(ext, out index) &&
					(index >= depth_of_rotation))
				{
					File.Delete(path);
				}
				else
				{
                    File.Move(path, IncrementFileIndex(path));
				}
			}
		}



		protected static string IncrementFileIndex(string path)
		{
			string new_path;
			int index;
			string ext = Path.GetExtension(path).TrimStart(new Char[] { '.' });
			if (Int32.TryParse(ext, out index))
			{
				new_path = Path.ChangeExtension(path, (index + 1).ToString());
			}
			else
			{
				new_path = path + ".0";
			}

			return new_path;
		}
	}
}