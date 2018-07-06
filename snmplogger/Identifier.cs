using System;
namespace snmplogger
{
	public class Identifier
	{
		public enum TagClass
		{
			UNIVERSAL, APPLICATION, CONTEXT_SPECIFIC, PRIVATE
		}


		private byte[] identifier_bytes;
		private TagClass identifier_class;
		private int tag;
		private bool is_constructed;




		public Identifier(byte[] bytes)
		{
			identifier_bytes = bytes;
			ParseIdentifier(bytes);
		}


		public TagClass IdentifierClass {
			get
			{
				return identifier_class;
			}
		}

		public int Tag
		{
			get
			{
				return tag;
			}
		}

		protected void ParseIdentifier(byte[] bytes)
		{
			bool long_form = (bytes[0] & 0x1F) == 0x1F;
			identifier_class = (TagClass)(bytes[0] & 0xC0);
			is_constructed = ((bytes[0] & 0x20) == 0x20);
			if (long_form)
			{
				int i = 1, temp_tag = 0;
				do  
				{
					temp_tag <<= 7;
					tag += bytes[i];
				} while ((bytes[i++] & 0x80) == 0x80); // Highest bit not set in final octet.
				tag = temp_tag;
			}
			else
			{
				tag = bytes[0] & 0x1F;
			}
		}


		public bool IsConstructed
		{
			get
			{
				return is_constructed;
			}
		}

	}
}
