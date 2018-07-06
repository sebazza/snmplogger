using System;
using System.IO;
using System.Collections.Generic;

namespace snmplogger
{
	public class Oid
	{


		protected static TreeNode<int, string> root;

		public Oid()
		{

		}



		public static void Mib2SchemaFileToOidTree(TextReader stream)
		{
			root = new TreeNode<int, string>();

			string line;
			//StreamReader file = new StreamReader(filename);
			while ((line = stream.ReadLine()) != null)
			{
				string[] parts = SplitOid(line);
				string name = parts[0].Trim('"');
				string oid = parts[1].Trim('"');

				InsertOidAndName(oid, name);
			}
		}


		protected static void InsertOidAndName(string oid, string name)
		{
			//Console.WriteLine(oid + " " + name);
			string[] values = oid.Split('.');
			int key = 0;
			TreeNode<int, string> node = root;
			foreach (string v in values)
			{
				if (Int32.TryParse(v, out key))
				{
					if (node.Contains(key))
					{
						node = node[key];
						continue;
					}
					else
					{
						node = node.AddChild(key);
					}
				}
			}
			node.Item = name;
		}

		protected static string[] SplitOid(string oid)
		{
			return oid.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
		}


		public static string OidToString(string oid)
		{
			string oid_string = oid;

			if (root != null)
			{
				int value;
				TreeNode<int, string> node = root;

				while (oid.Length > 0)
				{
					string[] parts = oid.Split(new Char[]{'.'}, 2);

					if (Int32.TryParse(parts[0], out value))
					{
						if (node.Contains(value))
						{
							node = node[value];
							if (parts.Length > 1)
							{
								oid = parts[1];
							}
							else
							{
								oid = "";
							}
						}
						else
						{
							break;
						}
					}
				}
				if (oid.Length > 0)
				{
					oid_string = node.Item + "." + oid;
				}
				else
				{
					oid_string = node.Item;
				}

			}
			return oid_string;
		}


		public static void DumpOids()
		{
			PrintOid(root, "");
		}


		protected static void PrintOid(TreeNode<int, string> node, string oid)
		{
			Console.WriteLine(oid + "=" + node.Item);
			foreach (KeyValuePair<int, TreeNode<int, string>> n in node)
			{
				PrintOid(n.Value, oid + "." + n.Key.ToString());
			}
		}
	}



	public class TreeNode<K, T> : IEnumerable<KeyValuePair<K, TreeNode<K, T>>>
	{
		Dictionary<K, TreeNode<K, T>> Children;

		[System.Runtime.CompilerServices.IndexerName("__item__")]
		public TreeNode<K, T> this[K key]
		{
			get { return Children[key]; }

			set { }
		}

		public IEnumerator<KeyValuePair<K, TreeNode<K, T>>> GetEnumerator()
		{
			foreach (KeyValuePair<K, TreeNode<K, T>> entry in Children)
			{
				yield return entry;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public T Item { get; set; }

		public TreeNode()
		{
			Children = new Dictionary<K, TreeNode<K, T>>();
		}

		public TreeNode<K, T> AddChild(K key)
		{
			TreeNode<K, T> nodeItem = new TreeNode<K, T>();
			Children.Add(key, nodeItem);
			return nodeItem;
		}

		public bool Contains(K key)
		{
			return Children.ContainsKey(key);
		}

	}

}