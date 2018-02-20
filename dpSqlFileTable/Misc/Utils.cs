using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dpSqlFileTable
{
	internal static class Utils
	{
		internal static byte[] _StringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
				.Where(x => x % 2 == 0)
				.Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
				.ToArray();
		}

		internal static string _FormatFirectoryName(string directoryName)
		{
			string result = directoryName.Replace("/", "\\").Trim().Trim('\\');
			return result;
		}

	}
}
