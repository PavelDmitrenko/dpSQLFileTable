﻿using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace dpSqlFileTable
{
	public class FileTable
	{

		public readonly Directory Directory;
		public readonly File File;
		public bool DebugMode;

		public FileTable(ConnectionStringSettings connectionStringSettings, string dbName)
		{
			Directory = new Directory(connectionStringSettings, dbName);
			File = new File(connectionStringSettings, dbName, null, DebugMode);
		}

		public FileTable(ConnectionStringSettings connectionStringSettings, string dbName, string hashTable)
		{
			Directory = new Directory(connectionStringSettings: connectionStringSettings, fileTable: dbName, hashTable: hashTable, debugMode: DebugMode);
			File = new File(connectionStringSettings, dbName, hashTable, DebugMode);
		}

	}
}
