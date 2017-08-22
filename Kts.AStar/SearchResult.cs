using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kts.AStar
{
	public class SearchResult<T>
	{
		public List<T> Path { get; set; }
		public double Distance { get; set; }
		public bool Success { get; set; }
		public SearchResult(bool success, double distance, List<T> path)
		{
			Success = success;
			Distance = distance;
			Path = path;
		}
	}
}
