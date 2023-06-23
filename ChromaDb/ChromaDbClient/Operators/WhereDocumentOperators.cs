using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromaDbClient.Operators
{
	public class WhereDocumentOperators : LogicalOperators
	{
		protected WhereDocumentOperators(string value) : base(value)
		{
		}

		public static WhereDocumentOperators Contains => new("$contains");
	}
}
