# SQLFileTable
Writes&reads to/from MSSQL FileTable structure from C# code.  
Supports deduplication (based on MD5 hashes of file contents).  

## Usage
#### Constructor
```csharp
FileTable(ConnectionStringSettings connectionStringSettings, string dbName, string hashTable)
```

#### Deduplication
In order to use deduplication functionality provide "hashTable" value to ctor, and set "userDeduplaction" value to true.  


#### Methods
```csharp
File.WriteAllBytesAsync(string path, byte[] bytes, bool useDeduplication = false)
Directory.CreateDirectoryAsync(string directoryName)
```

## Usage examples
```csharp
FileTable fileTable = new FileTable(ConfigurationManager.ConnectionStrings["FileDBConnectionString"], "FileDBName.SchemaName.TableName");
EntryData entry = await fileTable.File.WriteAllBytesAsync("Folder\Subfolder\filename.txt", System.IO.File.ReadAllBytes(@"C:\file.txt"), true);
```

## Dependencies
 
[dpQuery](https://github.com/PavelDmitrenko/dpQuery)
