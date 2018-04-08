using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using dpQuery;

namespace dpSqlFileTable
{
	public class Base
	{
		private readonly string _fileTable;
		private readonly string _hashTable;
		protected internal readonly Qs qs;

		protected internal Base(ConnectionStringSettings connectionStringSettings, string fileTable, string hashTable, bool debugMode)
		{
			_fileTable = new Regex(@"[^a-zA-Z0-9\._-]").Replace(fileTable.Trim(), "");
			_hashTable = hashTable;
			qs = new Qs(connectionStringSettings);
			qs.DebugMode = debugMode;
		}

		private async Task<EntryData> UpdateEntry(string entryName, EntryData entryData, byte[] content, SqlTransaction transaction)
		{
			qs.ExecuteNonQueryInTransaction(FormatQuery(SqlStrings.UpdateEntry),
				transaction,
				p =>
				{
					p.AddWithValue("@EntryName", entryName);
					p.AddWithValue("@StreamId", entryData.StreamId);
					p.AddWithValue("@PathLocator", entryData.PathLocator);
					p.AddWithValue("@FileStream", content ?? Utils._StringToByteArray("0x"));
				}, CommandType.Text);

			EntryData result = await GetEntryData(entryData.StreamId, false, transaction);
			result.Content = content;
			return result;

		}

		protected internal async Task<EntryData> CreateDirectory(string directoryName, SqlTransaction transaction)
		{

			EntryData result = new EntryData();

			if (!string.IsNullOrEmpty(directoryName))
			{
				string[] spl = directoryName.Split('\\');
				EntryData parent = null;

				foreach (var directory in spl)
				{
					EntryData pathLocator = await GetEntryData(entryName: directory, parentLocator: parent, isDirectory: true, transaction: transaction);
					result = pathLocator ?? await CreateEntry(entryName: directory, parentLocator: parent, isDirectory: true, content: null, transaction: transaction);
					parent = result;
				}
			}
			else
			{
				result = null;
			}

			return result;
		}

		protected internal async Task<EntryData> GetEntryData(string entryName, EntryData parentLocator, bool isDirectory, SqlTransaction transaction)
		{

			EntryData result = new EntryData();

			DataTable dt = await qs.FillDataTableInTransactionAsync(() => parentLocator == null
					? FormatQuery(SqlStrings.GetEntryDataRoot)
					: FormatQuery(SqlStrings.GetEntryDataWithParent),
				transaction,
				p =>
				{
					if (parentLocator != null)
						p.AddWithValue("@ParentPathLocator", parentLocator.PathLocator);

					p.AddWithValue("@EntryType", isDirectory ? 1 : 0);
					p.AddWithValue("@Name", entryName);
				}, CommandType.Text);

			if (dt.Rows.Count == 1)
			{
				result.LoadFromDataTable(dt);
			}
			else
			{
				result = null;
			}

			return result;
		}

		protected internal async Task<EntryData> GetEntryData(Guid streamId, bool getContent, SqlTransaction transaction)
		{
			EntryData result = new EntryData();

			DataTable dt = await qs.FillDataTableInTransactionAsync(FormatQuery(getContent ? SqlStrings.GetEntryDataWithContentByStreamId : SqlStrings.GetEntryDataByStreamId), 
				transaction,
				p =>
				{
					p.AddWithValue("@StreamId", streamId);
				}, CommandType.Text);

			if (dt.Rows.Count != 0)
			{
				result.LoadFromDataTable(dt);
			}
			else
			{
				result = null;
			}

			return result;
		}

		private async Task<EntryData> CreateEntry(string entryName, EntryData parentLocator, bool isDirectory, byte[] content, SqlTransaction transaction)
		{
			EntryData result = new EntryData();

			DataTable dt = await qs.FillDataTableInTransactionAsync(FormatQuery(SqlStrings.CreateEntry),
				transaction,
				p =>
				{
					var varbinary = p.Add("FileStream", SqlDbType.VarBinary);
					varbinary.Value = content ?? (object)DBNull.Value;

					p.AddWithValue("@EntryName", entryName);
					p.AddWithValue("@EntryType", isDirectory ? 1 : 0);
					p.AddWithValue("@ParentLocator", parentLocator == null ? "/" : parentLocator.PathLocator);
				}, CommandType.Text);

			if (dt.Rows.Count != 0)
			{
				result.LoadFromDataTable(dt);
			}

			result.Content = content;
			return result;
		}

		protected async Task<EntryData> WriteAllBytes(string path, byte[] bytes, bool allowDeduplication)
		{
			EntryData newOrUpdatedEntry = null;

			// In case of duplication allowed, trying to find clone of object in hash table
			if (allowDeduplication)
			{
				Guid hash = GetMD5Hash(bytes);
				EntryData duplicatedEntry = null;

				await qs.BeginTransactionAsync(async connection =>
				{
					   object streamId = await qs.ExecuteScalarInTransactionAsync(FormatQuery(SqlStrings.GetStreamIdByHash),
						connection,
						p =>
						{
							p.AddWithValue("@Hash", hash);
						}, CommandType.Text);

					if (streamId != null)
					{
						duplicatedEntry = await GetEntryData((Guid)streamId, false, connection);
						if (duplicatedEntry != null) // Original file was removed
						{
							duplicatedEntry.IsDuplicate = true;
							duplicatedEntry.Hash = hash;
						}
					}
				});

				if (duplicatedEntry != null) // Clone finded, returning
					return duplicatedEntry; 
			}

			string directory = Utils._FormatFirectoryName(System.IO.Path.GetDirectoryName(path));
			string file = System.IO.Path.GetFileName(path);

			await qs.BeginTransactionAsync(async transaction =>
			{
				EntryData directoryLocator = await CreateDirectory(directory, transaction);
				EntryData entryPathLocator = await GetEntryData(file, directoryLocator, false, transaction);
	
				newOrUpdatedEntry = entryPathLocator == null ? await CreateEntry(file, directoryLocator, false, bytes, transaction)
														: await UpdateEntry(file, entryPathLocator, bytes, transaction);

				// Insert hash info if hashTable specified
				if (allowDeduplication)
				{
					newOrUpdatedEntry.Hash = GetMD5Hash(newOrUpdatedEntry.Content);
					await UpdateHashTable(newOrUpdatedEntry, transaction);
				}
			});

			return newOrUpdatedEntry;

		}

		private async Task UpdateHashTable(EntryData entry, SqlTransaction transaction)
		{
			entry.Hash = GetMD5Hash(entry.Content);

			await qs.ExecuteNonQueryInTransactionAsync(FormatQuery(SqlStrings.UpdateHashTable), transaction, 
				p =>
			{
				p.AddWithValue("@StreamId", entry.StreamId);
				p.AddWithValue("@Hash", entry.Hash);
			}, 
			CommandType.Text);
		}

		private Guid GetMD5Hash(byte[] bytes)
		{
			using (MD5 md5 = MD5.Create())
			{
				byte[] hash = md5.ComputeHash(bytes);
				return new Guid(hash);
			}
		}

		private string FormatQuery(string sql)
		{
			sql = sql.Replace("{DBNAME}", _fileTable);

			if (!string.IsNullOrEmpty(_hashTable))
				sql = sql.Replace("{HASHTABLENAME}", _hashTable);

			sql = Regex.Replace(sql, @"\s+", " ");
			return sql;
		}

	}
}
