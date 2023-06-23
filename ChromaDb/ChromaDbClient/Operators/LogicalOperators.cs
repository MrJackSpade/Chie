using ChromaDbClient.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromaDbClient.Operators
{
	public class LogicalOperators : IOperator
	{
		public string Value { get;private  set; }

		protected LogicalOperators(string value)
		{
			this.Value = value;
		}

		public static LogicalOperators And => new("$and");
		public static LogicalOperators Or => new("$or");

	}
}
