using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using DpQuery;

namespace dpSqlFileTable
{
	public sealed class File : Base
	{
		internal File(ConnectionStringSettings connectionStringSettings, string fileTable, string hashTable = null) : base(connectionStringSettings, fileTable, hashTable)
		{

		}

		public async Task<EntryData> WriteAllBytesAsync(string path, byte[] bytes, bool useDeduplication = false)
		{
			return await base.WriteAllBytes(path, bytes, useDeduplication);
		}

	}
}
