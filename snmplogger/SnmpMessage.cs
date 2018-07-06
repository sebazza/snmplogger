using System;
using System.Collections.Generic;
using System.Text;


namespace snmplogger
{
	public class SnmpMessage
	{

		protected byte[] message_data;
		protected List<BerNode> root;



		public SnmpMessage(byte[] message)
		{
			message_data = message;
			this.DecodeSnmpTrap(message);
		}


		public override string ToString()
		{
			return Dump();
		}


		public string Version
		{
			get
			{
				string version = "Not found";
				if ((root != null) && (root.Count == 1) && (root[0].Degree > 0))
				{
					int version_number = Utils.GetInt(root[0].SubNode[0].Content, 0, root[0].SubNode[0].Content.Length);

					// For SNMP version 1, field will contain 0.
					if (version_number == 0)
					{
						version_number = 1;
					}
					else if (version_number == 1)
					{
						version_number = 2;
					}
					version = version_number.ToString();
				}

				return version;
			}
		}


		public string Community
		{
			get
			{
				string community = null;

				if ((root != null) && (root.Count == 1) && (root[0].Degree > 1))
				{
					community = Encoding.UTF8.GetString(root[0].SubNode[1].Content);
				}

				return community;
			}
		}


		public string Enterprise
		{
			get
			{
				string enterprise = null;

				if ((root != null) && (root.Count == 1) && (root[0].Degree > 2) && (root[0].Degree > 0))
				{
					enterprise = Utils.GetOid(root[0].SubNode[2].SubNode[0].Content);
				}

				return enterprise;
			}
		}


		public string AgentAddress
		{
			get
			{
				string agent_address = null;

				if ((root != null) && (root.Count == 1) && (root[0].Degree > 2) && (root[0].SubNode[2].Degree > 1))
				{
					agent_address = Utils.GetIpAddress(root[0].SubNode[2].SubNode[1].Content);
				}

				return agent_address;
			}
		}


		public string GenericTrap
		{
			get
			{
				string generic_trap = null;
				if ((root != null) && (root.Count == 1) && (root[0].Degree > 2) && (root[0].SubNode[2].Degree > 2))
				{
					generic_trap = Utils.GetGenericTrap(root[0].SubNode[2].SubNode[2].Content);
				}
				return generic_trap;
			}
		}

		public string SpecificTrap
		{
			get
			{
				string specific_trap = null;
				if ((root != null) && (root.Count == 1) && (root[0].Degree > 2) && (root[0].SubNode[2].Degree > 2))
				{
					specific_trap = Utils.GetSpecificTrap(root[0].SubNode[2].SubNode[3].Content);
				}
				return specific_trap;
			}

		}

		public string TimeStamp
		{
			get
			{
				string time_stamp = null;
				if ((root != null) && (root.Count == 1) && (root[0].Degree > 2) && (root[0].SubNode[2].Degree > 3))
				{
					time_stamp = ((float)Utils.GetInt(root[0].SubNode[2].SubNode[4].Content) / 100.00).ToString() + " s";
				}
				return time_stamp;
			}
		}

		public string RequestId
		{
			get
			{
				string request_id = "Not found";
				if ((root != null) && (root.Count == 1) && (root[0].Degree > 2) &&
					(root[0][2].Degree > 0) &&
					(root[0][2][0].Degree == 0))
				{
					request_id = Utils.GetInt(root[0][2][0].Content).ToString();
				}
				return request_id;
			}
		}

		public string ContextEngineId
		{
			get
			{
				string context_engine_id = null;
				if ((root != null) && (root.Count == 1) && (root[0].Degree > 3) &&
					(root[0][3].Degree > 0))
				{
					context_engine_id = Utils.GetBytes(root[0][3][0].Content, "");
				}
				return context_engine_id;

			}

		}

		public string ErrorStatus
		{
			get
			{
				string error_status = null;
				if (Version == "3")
				{
					if ((root != null) && (root.Count == 1) && (root[0].Degree > 3) &&
						(root[0][3].Degree > 2) &&
						(root[0][3][2].Degree > 1))
					{
						error_status = Utils.GetInt(root[0][3][2][1].Content).ToString();
					}
				}
				else
				{

					if ((root != null) && (root.Count == 1) && (root[0].Degree > 2) &&
						(root[0][2].Degree > 2))
					{
						error_status = Utils.GetInt(root[0][2][1].Content).ToString();
					}
				}

				return error_status;
			}
		}


		public string ErrorIndex
		{
			get
			{
				string error_index = null;
				if (Version == "3")
				{
					if ((root != null) && (root.Count == 1) && (root[0].Degree > 3) &&
						(root[0][3].Degree > 2) &&
						(root[0][3][2].Degree > 2))
					{
						error_index = Utils.GetInt(root[0][3][2][2].Content).ToString();
					}
				}
				else
				{
					if ((root != null) && (root.Count == 1) && (root[0].Degree > 2) &&
					(root[0][2].Degree > 2))
					{
						error_index = Utils.GetInt(root[0][2][2].Content).ToString();
					}
				}
				return error_index;
			}
		}

		public int PduType
		{
			get
			{
				int pdu_type = 0;
				switch (Version)
				{
					case "1":
					case "2":
						pdu_type = root[0][2].Tag;
						break;
					case "3":
						pdu_type = root[0][3][2].Tag;
						break;
				}

				return pdu_type;
			}
		}

		public string Variables
		{
			get
			{
				List<string> variables = new List<string>();
				BerNode variables_node = null;

				if (Version == "1")
				{
					if ((root != null) &&
						(root.Count == 1) &&
						(root[0].Degree > 2))
					{
						if ((PduType == 4) && (root[0][2].Degree > 5))
						{
							variables_node = root[0][2][5];
						}
						else if (root[0][2].Degree > 3)
						{
							variables_node = root[0][2][3];
						}
					}
				}
				else if (Version == "2")
				{
					if ((root != null) &&
						(root.Count == 1) &&
						(root[0].Degree > 2) &&
						(root[0][2].Degree > 2))
					{
						variables_node = root[0][2][3];
					}
				}
				else
				{
					if ((root != null) &&
						(root.Count == 1) &&
						(root[0].Degree > 3) &&
						(root[0][3].Degree > 2) &&
						(root[0][3][2].Degree > 3))
					{
						variables_node = root[0][3][2][3];
					}
				}

				if (variables_node != null)
				{
					foreach (BerNode v in variables_node.SubNode)
					{
						string oid = Utils.GetOid(v.SubNode[0].Content);
						string value = Utils.GetByType(v.SubNode[1].Content, v.SubNode[1].Tag);
						variables.Add(oid + "=" + value);
					}
				}
				return String.Join(", ", variables.ToArray());
			}
		}

		public string MessageData
		{
			get
			{
				return Utils.GetBytes(message_data);
			}
		}

		// Test validity of SNMP trap by checking for right structure.
		public bool IsValidSnmpMessage()
		{
			return (
				// Common to all versions
				((root != null) &&
				 (root.Count == 1) &&
				 // version field
				 (root[0][0].Degree == 0)) &&

				// Version 1
				(((root[0].Degree == 3) &&
				 // community
				 (root[0][1].Degree == 0) &&
				 // data
				 (root[0][2].Degree > 4) &&
				 // enterprise
				 (root[0][2][0].Degree == 0) &&
				 // agent-address
				 (root[0][2][0].Degree == 0) &&
				 // generic-trap
				 (root[0][2][0].Degree == 0) &&
				 // specific-trap
				 (root[0][2][0].Degree == 0) &&
				 // time-stamp 
				 (root[0][2][0].Degree == 0))

								 ||

				// Version v2c
				(((root[0].Degree == 3) &&
				 // community
				 (root[0][1].Degree == 0) &&
				 // data
				 (root[0][2].Degree > 3) &&
				 // request-id
				 (root[0][2][0].Degree == 0) &&
				 // error-status
				 (root[0][2][1].Degree == 0) &&
		    	  // error-index
				  (root[0][2][2].Degree == 0)))


				 				||
				// Version 3
				((root[0].Degree == 4) &&
				 (root[0][0].Degree == 0) &&
				 // msgGlobalData
				 (root[0][1].Degree == 4) &&
				 // msgSecurityParameters
				 (root[0][2].Degree == 0) &&
				 // msgData
				 (root[0][3].Degree == 3) &&
				 // contextEngineID
				 (root[0][3][0].Degree == 0) &&
				 // contextName
				 (root[0][3][1].Degree == 0) &&
				 // data
				 (root[0][3][2].Degree > 3) &&
				 // request-id
				 (root[0][3][2][0].Degree == 0) &&
				 // error-status
				 (root[0][3][2][1].Degree == 0) &&
				 // error-index
				 (root[0][3][2][2].Degree == 0))));

		}


		public string LogMessage()
		{
			string log_message;
			if (IsValidSnmpMessage())
			{
				if (Version == "1")
				{
					log_message = LogVersion1Message();
				}
				else if (Version == "2")
				{
					log_message = LogVersion2Message();
				}
				else
				{
					log_message = String.Format("SNMP v3 trap received from {0}, error-status: {1}, error-index: {2}, variables: {3}",
												ContextEngineId, ErrorStatus, ErrorIndex, Variables);
				}
			}
			else
			{
				log_message = String.Format("Not a valid SNMP trap: {0}", MessageData);
			}
			return log_message;
		}


		protected string LogVersion1Message()
		{
			string[] message_types = { "get-request", "get-next-request", "get-response", "set-request", "trap" };

			string log_message = "SNMPv1 ";
			switch (PduType)
			{
				case 0:
				case 1:
				case 2:
				case 3:
					log_message += String.Format("{0} received, request id: {1}, error status: {2}, error index: {3}, variables: {4}",
												message_types[PduType], RequestId, ErrorStatus, ErrorIndex, Variables);
					break;
				case 4:
					log_message += String.Format("{0} message received from {1}, specific-trap: {2}, variables: {3}",
												message_types[PduType], AgentAddress, SpecificTrap, Variables);
					break;
			}
			return log_message;
		}

		protected string LogVersion2Message()
		{

			string[] message_types = {"get-request", "get-next-request", "response", "set-request", "",
				"get-bulk-request", "inform-request", "snmpV2-trap", "report"};

			string log_message = "SNMPv2c ";
			switch (PduType)
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 6:
				case 7:
				case 8:
					log_message += String.Format("{0} received, community {1}, request id: {2}, error status: {3}, error index: {4}, variables: {5}",
												message_types[PduType], Community, RequestId, ErrorStatus, ErrorIndex, Variables);
					break;
				case 4:
					log_message += String.Format("{0} received, community: {1}, request-id: {2}, error-status: {3}, error-index: {4}, variables: {5}",
												message_types[PduType], Community, RequestId, ErrorStatus, ErrorIndex, Variables);
					break;
				case 5:
					log_message += String.Format("{0} received, community {1}, request id: {2}, error status: {3}, error index: {4}, variables: {5}",
												message_types[PduType], Community, RequestId, ErrorStatus, ErrorIndex, Variables);
					break;
			}
			return log_message;
		}





		protected void DecodeSnmpTrap(byte[] message)
		{
			this.root = BerNode.ParseBer(message);
		}


		public string Dump()
		{
			if (this.root.Count > 0)
			{
				return this.root[0].Dump();
			}
			else
			{
				return "No data";
			}
		}

	}

}
