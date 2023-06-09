using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromaDbClient.Interfaces
{
	public interface ISerializable<T>
	{
		string ToJsonString();
		T FromJsonString(string jsonString);
	}
}
