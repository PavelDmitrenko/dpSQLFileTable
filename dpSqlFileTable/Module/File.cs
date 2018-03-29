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
		internal File(ConnectionStringSettings connectionStringSettings, string fileTable, string hashTable = null, bool debugMode = false) 
			: base(connectionStringSettings, fileTable, hashTable, debugMode)
		{

		}

		public async Task<EntryData> WriteAllBytesAsync(string path, byte[] bytes, bool useDeduplication = false)
		{
			return await base.WriteAllBytes(path, bytes, useDeduplication);
		}

		public async Task<byte[]> ReadAllBytesAsync(Guid streamId)
		{
			EntryData entryData  = null;
			await qs.BeginTransactionAsync(async transaction =>
				{
					entryData = await GetEntryData(streamId: streamId, getContent: true, transaction: transaction);
				});
			return entryData.Content;
		}

		public async Task<byte[]> ReadAllBytes(Guid streamId)
		{
			EntryData entryData = null;
			qs.BeginTransaction(async transaction =>
				{
					entryData = await GetEntryData(streamId: streamId, getContent: true, transaction: transaction);
				});
			return entryData.Content;
		}

		public async Task<bool> ExistsAsync(string path)
		{
			EntryData pathLocator = null;

			await qs.BeginTransactionAsync(async transaction =>
			{
				pathLocator = await GetEntryData(entryName: path, parentLocator: null, isDirectory: false, transaction: transaction);
			});

			return pathLocator != null;

		}

	}
}
