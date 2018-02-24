using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DpQuery;

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

		protected internal EntryData UpdateEntry(string entryName, EntryData entryData, byte[] content, SqlTransaction transaction)
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

			return GetEntryData(entryData.StreamId, false, transaction);

		}

		protected internal EntryData CreateDirectory(string directoryName, SqlTransaction transaction)
		{

			EntryData result = new EntryData();

			if (!string.IsNullOrEmpty(directoryName))
			{
				string[] spl = directoryName.Split('\\');
				EntryData parent = null;

				foreach (var directory in spl)
				{
					EntryData pathLocator = GetEntryData(entryName: directory, parentLocator: parent, isDirectory: true, transaction: transaction);
					result = pathLocator ?? CreateEntry(entryName: directory, parentLocator: parent, isDirectory: true, content: null, transaction: transaction);
					parent = result;
				}
			}
			else
			{
				result = null;
			}

			return result;
		}

		protected internal EntryData GetEntryData(string entryName, EntryData parentLocator, bool isDirectory, SqlTransaction transaction)
		{

			EntryData result = new EntryData();

			DataTable dt = qs.FillDataTableInTransaction(() => parentLocator == null
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

		protected internal EntryData GetEntryData(Guid streamId, bool getContent, SqlTransaction transaction)
		{

			EntryData result = new EntryData();

			DataTable dt = qs.FillDataTableInTransaction(FormatQuery(getContent ? SqlStrings.GetEntryDataWithContentByStreamId : SqlStrings.GetEntryDataByStreamId), 
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

		private EntryData CreateEntry(string entryName, EntryData parentLocator, bool isDirectory, byte[] content, SqlTransaction transaction)
		{
			EntryData result = new EntryData();

			DataTable dt = qs.FillDataTableInTransaction(FormatQuery(SqlStrings.CreateEntry),
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

				await qs.BeginTransactionAsync(transaction =>
				{
					object streamId = qs.ExecuteScalarInTransaction(FormatQuery(SqlStrings.GetStreamIdByHash),
						transaction,
						p =>
						{
							p.AddWithValue("@Hash", hash);
						}, CommandType.Text);

					if (streamId != DBNull.Value)
					{
						duplicatedEntry = GetEntryData((Guid)streamId, false, transaction);
					}
				});

				if (duplicatedEntry != null) // Clone finded, returning
					return duplicatedEntry; 
			}

			string directory = Utils._FormatFirectoryName(System.IO.Path.GetDirectoryName(path));
			string file = System.IO.Path.GetFileName(path);

			await qs.BeginTransactionAsync(async transaction =>
			{
				EntryData directoryLocator = CreateDirectory(directory, transaction);
				EntryData entryPathLocator = GetEntryData(file, directoryLocator, false, transaction);

				newOrUpdatedEntry = entryPathLocator == null ? CreateEntry(file, directoryLocator, false, bytes, transaction)
														: UpdateEntry(file, entryPathLocator, bytes, transaction);
			});

			return newOrUpdatedEntry;

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
