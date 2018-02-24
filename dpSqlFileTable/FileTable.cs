using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace dpSqlFileTable
{
	public class FileTable
	{

		public readonly Directory Directory;
		public readonly File File;

		public FileTable(ConnectionStringSettings connectionStringSettings, string dbName, bool debugMode = false)
		{
			Directory = new Directory(connectionStringSettings, dbName);
			File = new File(connectionStringSettings, dbName, null, debugMode);
		}

		public FileTable(ConnectionStringSettings connectionStringSettings, string dbName, string hashTable, bool debugMode = false)
		{
			Directory = new Directory(connectionStringSettings, dbName, hashTable, debugMode);
			File = new File(connectionStringSettings, dbName, hashTable, debugMode);
		}

	}
}
