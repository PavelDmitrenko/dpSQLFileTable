using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace dpSqlFileTable
{
	public class EntryData
	{
		public Guid StreamId;
		public string PathLocator;
		public byte[] Content; 

		public EntryData()
		{
		}

		public void LoadFromDataTable(DataTable dataTable)
		{
			PathLocator = (string)dataTable.Rows[0]["path_locator"];
			StreamId = (Guid)dataTable.Rows[0]["stream_id"];

			object streamData = dataTable.Rows[0]["file_stream"];

			Content = streamData == DBNull.Value ? null: (byte[])streamData;
			
		}

	}
}
