using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace snmplogger
{
    public class BerNode
    {
        public enum UniversalType
        {
            INTEGER = 2,
            BIT_STRING,
            OCTET_STRING,
            NULL,
            OBJ_ID,
            SEQUENCE = 16,
            SET,
            PRINTABLE_STRING = 19,
            T61_STRING,
            IA5_STRING = 22,
            UTC_TIME
        };



        private Identifier identifier;
        private int length;
        // private int value_offset;
        private byte[] raw_data;
        private List<BerNode> sub_node;


        private BerNode(byte[] identifier, int length, byte[] content)
        {
            this.identifier = new Identifier(identifier);
            this.length = length;
            this.raw_data = content;
            this.sub_node = new List<BerNode>();
        }

        // Returns the tag value (as extracted from the encoded indentier) as integer.
        public int Tag
        {
            get
            {
                return this.identifier.Tag;
            }
        }


        public byte[] Content
        {
            get
            {
                byte[] result = null;
                result = raw_data;
                return result;
            }
        }


        public List<BerNode> SubNode
        {
            get
            {
                return sub_node;
            }
        }


        // Returns the number of subobjects a node has.
        public int Degree
        {
            get
            {
                return sub_node.Count;
            }
        }


        // Provides the index operator to get/set a node.
        public BerNode this[int i]
        {
            get { return sub_node[i]; }
            set { sub_node[i] = value; }
        }


        override public string ToString()
        {
            return String.Format("{0} {1} {2}", Tag, length, Utils.GetByType(Content, Tag));
        }

        public static List<BerNode> ParseBer(byte[] bytes)
        {

            List<BerNode> result = new List<BerNode>();
            ParseBer(bytes, result);
            return result;
        }


        protected static void ParseBer(byte[] bytes, List<BerNode> result)
        {
            byte[] identifier;
            int length = 0;

            for (int i = 0, start = 0; i < bytes.Length; start = i)
            {
                i += ParseIdentifier(bytes, i, out identifier);
                i += ParseLength(bytes, i, out length);

                i += length;
                if (i <= bytes.Length)
                {
                    byte[] content = new byte[length];
                    Array.Copy(bytes, i - length, content, 0, length);
                    BerNode ber = new BerNode(identifier, length, content);
                    result.Add(ber);

                    if (ber.IsConstructed())
                    {
                        ParseBer(content, ber.SubNode);
                    }
                }
                else
                {
                    throw new BerException(String.Format("Invalid message: tried to copy {0} bytes out of byte array of size {1}", i, bytes.Length));
                }
            }
        }


        protected static int ParseIdentifier(byte[] bytes, int i, out byte[] identifier)
        {
            int start = i;
            bool more_bytes = (bytes[i] & 0x1F) == 0x1F;
            while (more_bytes && (bytes[++i] & 0x80) != 0) { }
            i++;
            identifier = new byte[i - start];
            Array.Copy(bytes, start, identifier, 0, i - start);

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

                        length = Utils.GetInt(bytes, start + 1, length_length);
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
            return identifier.IsConstructed;
        }


        protected static bool IsMultiByteLength(byte first_byte)
        {
            return (first_byte & 0x80) != 0;
        }



        public string Dump(int depth = 0)
        {
            string dump;
            if (Enum.IsDefined(typeof(UniversalType), Tag))
            {
                dump = String.Format("{0," + depth * 2 + "}{1}({2}) {3}", " ", Tag.ToString(), ((UniversalType)Tag).ToString(), Utils.GetByType(Content, Tag)) + System.Environment.NewLine;
            }
            else
            {
                dump = String.Format("{0," + depth * 2 + "}{1} {2}", " ", Tag.ToString(), Utils.GetByType(Content, Tag)) + System.Environment.NewLine;
            }

            if (this.sub_node != null)
            {
                foreach (BerNode n in this.sub_node)
                {
                    dump += n.Dump(depth + 1);
                }
            }

            return dump;
        }

    }



    public class BerException : Exception
    {

        public BerException(string message) : base(message) { }

    }
}
