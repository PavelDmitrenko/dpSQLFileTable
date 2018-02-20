﻿using System.Configuration;
using System.Threading.Tasks;
using DpQuery;

namespace dpSqlFileTable
{
	public sealed class Directory : Base
	{

		internal Directory(ConnectionStringSettings connectionStringSettings, string fileTable, string hashTable = null) : base(connectionStringSettings, fileTable, hashTable)
		{

		}

		public async Task<EntryData> CreateDirectoryAsync(string directoryName)
		{
			EntryData result = null;

			await qs.BeginTransaction(transaction =>
			{
				result = base.CreateDirectory(directoryName, transaction);
			});

			return result;
		}

	}

}
