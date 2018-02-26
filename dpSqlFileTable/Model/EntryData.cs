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
			DataRow dr = dataTable.Rows[0];

			PathLocator = (string)dr["path_locator"];
			StreamId = (Guid)dr["stream_id"];

			if (dr.Table.Columns.Contains("file_stream"))
			{
				object streamData = dr["file_stream"];
				Content = streamData == DBNull.Value ? null : (byte[])streamData;
			}
		}

	}
}
