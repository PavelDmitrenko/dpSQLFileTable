using System;
using System.Collections.Generic;
using System.Text;

namespace dpSqlFileTable
{
	internal static class SqlStrings
	{
		public static readonly string CreateEntry = @"DECLARE @OutputTbl TABLE (stream_id uniqueidentifier, path_locator NVARCHAR(max));
							INSERT INTO {DBNAME} (name, is_directory, file_stream, path_locator) 

							OUTPUT INSERTED.stream_id, CAST(INSERTED.path_locator AS VARCHAR(MAX)) INTO @OutputTbl(stream_id, path_locator)

							SELECT @EntryName, @EntryType, @FileStream, @ParentLocator
							+ CONVERT(VARCHAR(20), CONVERT(BIGINT, SUBSTRING(CONVERT(BINARY(16), NEWID()), 1, 6))) + '.'
							+ CONVERT(VARCHAR(20), CONVERT(BIGINT, SUBSTRING(CONVERT(BINARY(16), NEWID()), 7, 6))) + '.'
							+ CONVERT(VARCHAR(20), CONVERT(BIGINT, SUBSTRING(CONVERT(BINARY(16), NEWID()), 13, 4))) + '/';
							SELECT stream_id, path_locator FROM @OutputTbl;";

		public static readonly string GetEntryDataByStreamId =
						@"SELECT TOP 1 stream_id, CAST(path_locator AS VARCHAR(MAX)) as path_locator 
						FROM {DBNAME} WHERE stream_id = @StreamId;";

		public static readonly string GetEntryDataWithContentByStreamId =
			@"SELECT TOP 1 stream_id, CAST(path_locator AS VARCHAR(MAX)) as path_locator, file_stream 
						FROM {DBNAME} WHERE stream_id = @StreamId;";

		public static readonly string GetEntryDataRoot = 
						@"SELECT TOP 1 stream_id, CAST(path_locator AS VARCHAR(MAX)) as path_locator 
						FROM {DBNAME} WHERE name=@Name and is_directory=@EntryType AND parent_path_locator is null;";

		public static readonly string GetEntryDataWithParent =
						@"SELECT TOP 1 stream_id, CAST(path_locator AS VARCHAR(MAX)) as path_locator 
						FROM {DBNAME} WHERE name=@Name and is_directory = @EntryType AND parent_path_locator = @ParentPathLocator;";

		public static readonly string UpdateEntry =
						@"UPDATE {DBNAME} SET name = @EntryName, file_stream = @FileStream 
						WHERE stream_id = @StreamId AND path_locator = @PathLocator;";

		public static readonly string GetStreamIdByHash =
						@"SELECT TOP 1 StreamID FROM {HASHTABLENAME} WHERE Hash=@Hash;";

	}
	
}
