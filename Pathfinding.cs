using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.DataStructures;

namespace LayerLibrary
{
	public static class Pathfinding
	{
		private class Node<T> where T : ModLayerElement<T>, new()
		{
			public T Element;
			public Point16 Position => Element.Position;

			public int F;
			public int G;
			public int H;

			public Node<T> Parent;

			public Node(T element)
			{
				Element = element;
			}
		}

		private static int ComputeHScore<T>(T current, T target) where T : ModLayerElement<T>, new() => Math.Abs(target.Position.X - current.Position.X) + Math.Abs(target.Position.Y - current.Position.Y);

		public static Stack<Point16> FindPath<T>(T startPos, T endPos) where T : ModLayerElement<T>, new()
		{
			Node<T> current = default;

			var openList = new List<Node<T>>();
			var closedList = new List<Node<T>>();
			int g = 0;

			openList.Add(new Node<T>(startPos));

			while (openList.Count > 0)
			{
				int lowest = openList.Min(node => node.F);
				current = openList.First(node => node.F == lowest);

				closedList.Add(current);

				openList.Remove(current);

				if (closedList.FirstOrDefault(node => node.Position.X == endPos.Position.X && node.Position.Y == endPos.Position.Y) != default) break;

				IEnumerable<Node<T>> adjacentSquares = current.Element.GetNeighbors().Select(neighbor => new Node<T>(neighbor));
				g++;

				foreach (var adjacentSquare in adjacentSquares)
				{
					if (closedList.FirstOrDefault(node => node.Position.X == adjacentSquare.Position.X && node.Position.Y == adjacentSquare.Position.Y) != default) continue;

					if (openList.FirstOrDefault(node => node.Position.X == adjacentSquare.Position.X && node.Position.Y == adjacentSquare.Position.Y) == default)
					{
						adjacentSquare.G = g;
						adjacentSquare.H = ComputeHScore(adjacentSquare.Element, endPos);
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
			while (current != default)
			{
				points.Push(new Point16(current.Position.X, current.Position.Y));

				current = current.Parent;
			}

			return points;
		}
	}
}