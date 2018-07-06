using System;
using System.Text;
using System.Net;


namespace snmplogger
{
	public class Utils
	{
		public Utils()
		{
		}


		/*public static string getHexString(byte[] arr)
		{
			StringBuilder sb = new StringBuilder(arr.Length * 2);
			foreach (byte b in arr)
			{
				sb.AppendFormat("{0:X2}", b);
			}
			return sb.ToString();
		}
		public static byte[] getBytes(String str)
		{
			byte[] result = new byte[str.Length >> 1];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = (byte)Convert.ToInt32(str.Substring(i * 2, 2), 16);
			}
			return result;
		}*/

		public static int GetInt(byte[] data)
		{
			return Utils.GetInt(data, 0, data.Length);
		}

		public static int GetInt(byte[] data, int offset, int length)
		{
			int result = 0;
			for (int i = 0; i < length; i++)
			{
				result = (result << 8) | data[offset + i];
			}
			return result;
		}

		public static string GetString(byte[] bytes)
		{
			return Encoding.UTF8.GetString(bytes);
		}

		public static string GetBytes(byte[] bytes)
		{
			return GetBytes(bytes, " ");
		}

		public static string GetBytes(byte[] bytes, string replacement)
		{
			return BitConverter.ToString(bytes).Replace("-", replacement);
		}

		public static string GetOid(byte[] bytes)
		{
			StringBuilder oid = new StringBuilder();

			// First byte
			if (bytes.Length > 0)
			{
				oid.Append(String.Format("{0}.{1}", bytes[0] / 40, bytes[0] % 40));
			}
			// Subsequent bytes
			int current_arc = 0;
			for (int i = 1; i < bytes.Length; i++)
			{
				current_arc = (current_arc <<= 7) | bytes[i] & 0x7F;

				// Check if last byte of arc value
				if ((bytes[i] & 0x80) == 0)
				{
					oid.Append('.');
					oid.Append(Convert.ToString(current_arc));
					current_arc = 0;
				}
			}

			return Oid.OidToString(oid.ToString());
		}

		public static string GetIpAddress(byte[] bytes)
		{
			return new IPAddress(bytes).ToString();
		}

		public static string GetGenericTrap(byte[] bytes)
		{
			string generic_trap = null;
			string[] generic_trap_names = { "coldStart", "warmStart", "linkDown", "linkUp", "authenticationFailure", "egpNeighborLoss", "enterpriseSpecific" };
			int i = Utils.GetInt(bytes, 0, bytes.Length);
			if ((i >= 0) && (i < generic_trap_names.Length))
			{
				generic_trap = generic_trap_names[i];
			}
			return generic_trap;
		}

		public static string GetSpecificTrap(byte[] bytes)
		{
			string specific_trap = null;
			int i = Utils.GetInt(bytes, 0, bytes.Length);
			if (i >= 0)
			{
				specific_trap = i.ToString();
			}
			return specific_trap;
		}



		public static string GetByType(byte[] bytes, int type)
		{
			string value = null;
			switch ((BerNode.UniversalType) type)
			{
				case BerNode.UniversalType.INTEGER:
					value = Utils.GetInt(bytes).ToString(); break;

				case BerNode.UniversalType.OCTET_STRING:
				case BerNode.UniversalType.PRINTABLE_STRING:
				case BerNode.UniversalType.IA5_STRING:
				case BerNode.UniversalType.T61_STRING:
					value = Utils.GetString(bytes); break;

				case BerNode.UniversalType.OBJ_ID:
					value = Utils.GetOid(bytes); break;

				case BerNode.UniversalType.BIT_STRING:
				default:
					value = BitConverter.ToString(bytes).Replace(" ", string.Empty); break;

			}
			return value;
		}
	}



}
