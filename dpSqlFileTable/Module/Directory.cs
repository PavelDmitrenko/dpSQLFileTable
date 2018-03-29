using System.Configuration;
using System.Threading.Tasks;
using dpQuery;

namespace dpSqlFileTable
{
	public sealed class Directory : Base
	{

		internal Directory(ConnectionStringSettings connectionStringSettings, string fileTable, string hashTable = null, bool debugMode = false) 
			: base(connectionStringSettings, fileTable, hashTable, debugMode)
		{

		}

		public async Task<EntryData> CreateDirectoryAsync(string directoryName)
		{
			EntryData result = null;

			await qs.BeginTransactionAsync( transaction =>
			{
				return base.CreateDirectory(directoryName, transaction);
			});

			return result;
		}

	}

}
