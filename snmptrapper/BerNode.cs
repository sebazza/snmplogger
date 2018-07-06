using System;
using System.Collections.Generic;


namespace snmptrapper
{
	public class Ber
	{
		public enum UniversalType
		{
			INTEGER = 2,
			BIT_STRING,
			OCTET_STRING,
			NULL_,
			OBJ_ID,
			SEQUENCE = 16,
			SET,
			PRINTABLE_STRING = 19,
			T61_STRING,
			IA5_STRING = 22,
			UTC_TIME
		};

		private int identifier;
		private int length;
		private int value_offset;
		private byte[] raw_data;
		private List<Ber> sub_ber;


		private Ber(int identifier, int length, int value_offset, byte[] raw_data)
		{
			this.identifier = identifier;
			this.length = length;
			this.value_offset = value_offset;
			this.raw_data = raw_data;
			this.sub_ber = new List<Ber>();
		}

		public UniversalType Identifier
		{
			get
			{
				return (UniversalType)this.identifier;
			}
		}


		public byte[] Content
		{
			get
			{
				byte[] result = new byte[length];
				Array.Copy(raw_data, value_offset, result, 0, length);
				return result;
			}
		}

		public List<Ber> SubBer
		{
			get
			{
				return sub_ber;
			}
		}


		public static List<Ber> ParseBer(byte[] bytes)
		{

			List<Ber> result = new List<Ber>();
			Console.WriteLine(BitConverter.ToString(bytes));
			ParseBer(bytes, result);
			return result;
		}


		protected static void ParseBer(byte[] bytes, List<Ber> result)
		{
			int identifier = 0;
			int length = 0;

			for (int i = 0, start = 0; i < bytes.Length; start = i)
			{
				i += ParseIdentifier(bytes, i, out identifier);
				Console.WriteLine("Identified identifer {0:X} ({0}) at {1}", identifier, i);

				i += ParseLength(bytes, i, out length);
				Console.WriteLine("Identified length {0} at {1}", length, i);

				i += length;

				byte[] raw_data = new byte[i - start];
				Array.Copy(bytes, start, raw_data, 0, i - start);
				Console.WriteLine("Content data: {0}", BitConverter.ToString(raw_data));
				Ber ber = new Ber(identifier, length, raw_data.Length - length, raw_data);

				result.Add(ber);


				if (ber.IsConstructed())
				{
					Console.WriteLine("Add sub ber");
					ParseBer(ber.Content, ber.SubBer);
				}
			}
		}


		protected static int ParseIdentifier(byte[] bytes, int i, out int identifier)
		{
			int start = i;
			bool more_bytes = (bytes[i] & 0x1F) == 0x1F;
			while (more_bytes && (bytes[++i] & 0x80) != 0) { }
			i++;
			identifier = Utils.getInt(bytes, start, i - start);

			return i - start;
		}


		protected static int ParseLength(byte[] bytes, int start, out int length)
		{
			if (bytes.Length > start)
			{
				// If, multi-byte length, bits 7-1 give the number of additional length bytes.
				// Second and following octets give the length, base 256, most significant digit first
				if (IsMultiByteLength(bytes[start]))
				{
					int length_length = bytes[start] & 0x1F;
					if (length_length < (bytes.Length - (start + 1)))
					{

						length = Utils.getInt(bytes, start + 1, length_length);
						return length_length + 1;
					}
				}
				else
				{
					length = bytes[start];
					return 1;
				}
			}
			throw new BerException("Invalid length");
		}


		protected bool IsConstructed()
		{
			return (identifier & 0x20) != 0;
		}


		protected static bool IsMultiByteLength(byte first_byte)
		{
			return (first_byte & 0x80) != 0;
		}

	}



	public class BerException : Exception
	{

		public BerException(string message) : base(message) {}

	}
}
