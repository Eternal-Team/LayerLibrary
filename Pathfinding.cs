using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.DataStructures;

namespace LayerLibrary
{
	public static class Pathfinding
	{
		private class Node
		{
			public int X;
			public int Y;

			public int F;
			public int G;
			public int H;

			public Node Parent;
		}

		private static int ComputeHScore(Node current, Node target) => Math.Abs(target.X - current.X) + Math.Abs(target.Y - current.Y);

		private static IEnumerable<Node> GetNeighborNodes<T>(IEnumerable<T> Tiles, Node node) where T : ModLayerElement<T>, new()
		{
			return Tiles.First(tube => tube.Position.X == node.X && tube.Position.Y == node.Y).GetNeighbors().Select(neighbor => new Node { X = neighbor.Position.X, Y = neighbor.Position.Y });
		}

		public static Stack<Point16> FindPath<T>(List<T> network, Point16 startPos, Point16 endPos) where T : ModLayerElement<T>, new()
		{
			Node current = null;
			var start = new Node { X = startPos.X, Y = startPos.Y };
			var target = new Node { X = endPos.X, Y = endPos.Y };
			var openList = new List<Node>();
			var closedList = new List<Node>();
			int g = 0;

			openList.Add(start);

			while (openList.Count > 0)
			{
				var lowest = openList.Min(l => l.F);
				current = openList.First(l => l.F == lowest);

				closedList.Add(current);

				openList.Remove(current);

				if (closedList.FirstOrDefault(l => l.X == target.X && l.Y == target.Y) != null) break;

				var adjacentSquares = GetNeighborNodes(network, current);
				g++;

				foreach (var adjacentSquare in adjacentSquares)
				{
					if (closedList.FirstOrDefault(l => l.X == adjacentSquare.X && l.Y == adjacentSquare.Y) != null) continue;

					if (openList.FirstOrDefault(l => l.X == adjacentSquare.X && l.Y == adjacentSquare.Y) == null)
					{
						adjacentSquare.G = g;
						adjacentSquare.H = ComputeHScore(adjacentSquare, target);
						adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
						adjacentSquare.Parent = current;

						openList.Insert(0, adjacentSquare);
					}
					else
					{
						if (g + adjacentSquare.H < adjacentSquare.F)
						{
							adjacentSquare.G = g;
							adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
							adjacentSquare.Parent = current;
						}
					}
				}
			}

			Stack<Point16> points = new Stack<Point16>();
			while (current != null)
			{
				points.Push(new Point16(current.X, current.Y));

				current = current.Parent;
			}

			return points;
		}
	}
}