﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using QuickGraph;

namespace Kts.AStar.Tests
{
	[TestFixture]
	public class GridTests
	{
		sealed class PointDbl
		{
			public PointDbl(double x, double y)
			{
				X = x; Y = y;
			}

			public readonly double X, Y;

			public override int GetHashCode()
			{
				return ((float)X).GetHashCode() ^ ((float)Y).GetHashCode();
			}

			public override bool Equals(object obj)
			{
				var other = obj as PointDbl;
				if (other == null)
					return false;
				return (float)X == (float)other.X && (float)Y == (float)other.Y;
			}
		}

		sealed class PointInt
		{
			public PointInt(int x, int y)
			{
				X = x; Y = y;
			}

			public readonly int X, Y;

			public override int GetHashCode()
			{
				return X.GetHashCode() ^ Y.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				var other = obj as PointInt;
				if (other == null)
					return false;
				return X == other.X && Y == other.Y;
			}

			public override string ToString()
			{
				return string.Format("X: {0}, Y: {1}", X, Y);
			}
		}

		[Test]
		public void RunSmallGrid()
		{
			// |_|_|X|_|_|
			// |S|_|X|_|E|
			// |_|_|X|_|_|
			// |_|_|_|_|_|

			var start = new PointInt(0, 2);

			Func<PointInt, IEnumerable<PointInt>> getNeighbors = p => new List<PointInt>(4)
			{
				new PointInt(p.X - 1, p.Y),
				new PointInt(p.X + 1, p.Y),
				new PointInt(p.X, p.Y - 1),
				new PointInt(p.X, p.Y + 1),
			};

			Func<PointInt, PointInt, double> getScoreBetween = (p1, p2) =>
			{
				if (p2.X == 2 && p2.Y > 0) return double.PositiveInfinity;
				return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
			};

			var destination = new PointInt(4, 2);
			Func<PointInt, double> getHeuristicScore = p =>
			{
				var dx = p.X - destination.X;
				var dy = p.Y - destination.Y;
				return Math.Sqrt(dx * dx + dy * dy);
			};

			double distance;
			var results = AStarUtilities.FindMinimalPath(start, destination, getNeighbors, getScoreBetween, getHeuristicScore, out distance);

			Assert.AreEqual(start, results[0]);
			Assert.AreEqual(new PointInt(1, 1), results[2]);
			Assert.AreEqual(new PointInt(2, 0), results[4]);
			Assert.AreEqual(new PointInt(3, 1), results[6]);
			Assert.AreEqual(destination, results[8]);
		}

		[Test]
		public void SpeedTest()
		{
			Func<PointInt, IEnumerable<PointInt>> getNeighbors = p => new[]
			{
				new PointInt(p.X - 1, p.Y + 0), // L
				new PointInt(p.X + 1, p.Y + 0), // R
				new PointInt(p.X + 0, p.Y - 1), // B
				new PointInt(p.X + 0, p.Y + 1), // T
				new PointInt(p.X - 1, p.Y + 1), // TL
				new PointInt(p.X + 1, p.Y + 1), // TR
				new PointInt(p.X - 1, p.Y - 1), // BL
				new PointInt(p.X + 1, p.Y - 1), // BR
			};

			Func<PointInt, PointInt, double> getScoreBetween = (p1, p2) =>
			{
				var dx = p1.X - p2.X;
				var dy = p1.Y - p2.Y;
				return Math.Sqrt(dx * dx + dy * dy);
			};

			var rand = new Random(42);

			for (int i = 0; i < 1; i++)
			{
				var start = new PointInt(rand.Next(1000), rand.Next(1000));
				var destination = new PointInt(rand.Next(1000), rand.Next(1000));

				Func<PointInt, double> getHeuristicScore = p =>
				{
					var dx = p.X - destination.X;
					var dy = p.Y - destination.Y;
					return Math.Sqrt(dx * dx + dy * dy);
				};

				Console.WriteLine("Going from {0} to {1}", start, destination);

				var sw = Stopwatch.StartNew();
				double distance;
				var results = AStarUtilities.FindMinimalPath(start, destination, getNeighbors, getScoreBetween, getHeuristicScore, out distance);

				Console.WriteLine("Done in {0}s.", sw.Elapsed.TotalSeconds);
				Console.WriteLine("Expansions: {0}", AStarUtilities.LastExpansionCount);
				Console.WriteLine("Result Count: {0}", results.Count);
				Console.WriteLine("Distance: {0}", distance);

				Assert.AreEqual(start, results.First());
				Assert.AreEqual(destination, results.Last());
			}
		}

		[Test]
		public void QuickgraphComparision()
		{
			Func<Edge<PointInt>, double> getScoreBetween = edge =>
			{
				var p1 = edge.Source;
				var p2 = edge.Target;
				var dx = p1.X - p2.X;
				var dy = p1.Y - p2.Y;
				return Math.Sqrt(dx * dx + dy * dy);
			};
			var rand = new Random(42);

			for (int i = 0; i < 1; i++)
			{
				var start = new PointInt(rand.Next(1000), rand.Next(1000));
				var destination = new PointInt(rand.Next(1000), rand.Next(1000));

				Func<PointInt, double> getHeuristicScore = p =>
				{
					var dx = p.X - destination.X;
					var dy = p.Y - destination.Y;
					return Math.Sqrt(dx * dx + dy * dy);
				};

				var algorithm = new QuickGraph.Algorithms.ShortestPath.AStarShortestPathAlgorithm<PointInt, Edge<PointInt>>(
					new VertexGraphGenerator(), getScoreBetween, getHeuristicScore);

				int expansions = 0;
				algorithm.FinishVertex += vertex =>
				{
					expansions++;
					if (vertex.Equals(destination))
						algorithm.Abort();
				};

				var sw = Stopwatch.StartNew();
				
				algorithm.Compute(start);
	
				Console.WriteLine("Done in {0}s.", sw.Elapsed.TotalSeconds);
				Console.WriteLine("Expansions: {0}", expansions);
				Console.WriteLine("Distance: {0}", algorithm.Distances[destination]);
			}
		}

		private class VertexGraphGenerator : IVertexListGraph<PointInt, Edge<PointInt>>
		{
			public bool IsDirected { get { return false; } }
			public bool AllowParallelEdges { get { return false; } }
			public bool ContainsVertex(PointInt vertex)
			{
				return vertex.X >= 0 && vertex.X < 1000 && vertex.Y >= 0 && vertex.Y < 1000;
			}

			public bool IsOutEdgesEmpty(PointInt v)
			{
				return false;
			}

			public int OutDegree(PointInt v)
			{
				return 8;
			}

			public IEnumerable<Edge<PointInt>> OutEdges(PointInt v)
			{
				IEnumerable<Edge<PointInt>> edges;
				if (TryGetOutEdges(v, out edges))
					return edges;
				throw new NotSupportedException();
			}

			public bool TryGetOutEdges(PointInt p, out IEnumerable<Edge<PointInt>> edges)
			{
				edges = new[]
				{
					new Edge<PointInt>(p, new PointInt(p.X - 1, p.Y + 0)), // L
					new Edge<PointInt>(p, new PointInt(p.X + 1, p.Y + 0)), // R
					new Edge<PointInt>(p, new PointInt(p.X + 0, p.Y - 1)), // B
					new Edge<PointInt>(p, new PointInt(p.X + 0, p.Y + 1)), // T
					new Edge<PointInt>(p, new PointInt(p.X - 1, p.Y + 1)), // TL
					new Edge<PointInt>(p, new PointInt(p.X + 1, p.Y + 1)), // TR
					new Edge<PointInt>(p, new PointInt(p.X - 1, p.Y - 1)), // BL
					new Edge<PointInt>(p, new PointInt(p.X + 1, p.Y - 1)), // BR
				}.Where(e => ContainsEdge(e.Source, e.Target));
				return true;
			}

			public Edge<PointInt> OutEdge(PointInt v, int index)
			{
				throw new NotImplementedException();
			}

			public bool ContainsEdge(PointInt source, PointInt target)
			{
				return Math.Abs(source.X - target.X) <= 1 && Math.Abs(source.Y - target.Y) <= 1;
			}

			public bool TryGetEdges(PointInt source, PointInt target, out IEnumerable<Edge<PointInt>> edges)
			{
				// source is parent of target or vice versa
				throw new NotImplementedException();
			}

			public bool TryGetEdge(PointInt source, PointInt target, out Edge<PointInt> edge)
			{
				throw new NotImplementedException();
			}

			private List<PointInt> _vertices; 

			public bool IsVerticesEmpty { get { return false; } }
			public int VertexCount { get { return 1000000; } }

			public IEnumerable<PointInt> Vertices
			{
				get
				{
					if (_vertices == null)
					{
						_vertices = new List<PointInt>(1000000);
						for (int i = 0; i < 1000; i++)
						{
							for (int j = 0; j < 1000; j++)
							{
								_vertices.Add(new PointInt(i, j));
							}
						}
					}
					return _vertices;
				}
			}
		}
	}
}