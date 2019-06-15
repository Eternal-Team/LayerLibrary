using BaseLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace LayerLibrary
{
	public abstract class ModLayerElement<T> where T : ModLayerElement<T>, new()
	{
		public ModLayer<T> Layer;

		public Point16 Position;
		public Point16 Frame;

		public abstract string Texture { get; }

		public virtual void UpdateFrame()
		{
			short frameX = 0, frameY = 0;
			short offset = (short)(Layer.TileSize * 16 + 2);
			if (Layer.ContainsKey(Position.X - Layer.TileSize, Position.Y)) frameX += offset;
			if (Layer.ContainsKey(Position.X + Layer.TileSize, Position.Y)) frameX += (short)(offset * 2);
			if (Layer.ContainsKey(Position.X, Position.Y - Layer.TileSize)) frameY += offset;
			if (Layer.ContainsKey(Position.X, Position.Y + Layer.TileSize)) frameY += (short)(offset * 2);
			Frame = new Point16(frameX, frameY);
		}

		public virtual void OnRemove()
		{
		}

		public virtual void OnPlace()
		{
		}

		public virtual void Draw(SpriteBatch spriteBatch)
		{
			Vector2 position = Position.ToVector2() * 16 - Main.screenPosition;

			for (int x = 0; x < Layer.TileSize; x++)
			{
				for (int y = 0; y < Layer.TileSize; y++)
				{
					Color color = Lighting.GetColor(Position.X + x, Position.Y + y);
					spriteBatch.Draw(ModContent.GetTexture(Texture), position+new Vector2(x*16,y*16), new Rectangle(Frame.X + 16 * x, Frame.Y + 16 * y, 16, 16), color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
				}
			}
		}

		public virtual void Update()
		{
		}

		public virtual TagCompound Save() => new TagCompound();

		public virtual void Load(TagCompound tag)
		{
		}

		public IEnumerable<T> GetNeighbors()
		{
			if (Layer.ContainsKey(Position.X + Layer.TileSize, Position.Y)) yield return Layer[Position.X + Layer.TileSize, Position.Y];
			if (Layer.ContainsKey(Position.X - Layer.TileSize, Position.Y)) yield return Layer[Position.X - Layer.TileSize, Position.Y];
			if (Layer.ContainsKey(Position.X, Position.Y + Layer.TileSize)) yield return Layer[Position.X, Position.Y + Layer.TileSize];
			if (Layer.ContainsKey(Position.X, Position.Y - Layer.TileSize)) yield return Layer[Position.X, Position.Y - Layer.TileSize];
		}

		public T GetNeighbor(Side side)
		{
			switch (side)
			{
				case Side.Bottom: return Layer.ContainsKey(Position.X, Position.Y + Layer.TileSize) ? Layer[Position.X, Position.Y + Layer.TileSize] : null;
				case Side.Top: return Layer.ContainsKey(Position.X, Position.Y - Layer.TileSize) ? Layer[Position.X, Position.Y - Layer.TileSize] : null;
				case Side.Left: return Layer.ContainsKey(Position.X - Layer.TileSize, Position.Y) ? Layer[Position.X - Layer.TileSize, Position.Y] : null;
				case Side.Right: return Layer.ContainsKey(Position.X + Layer.TileSize, Position.Y) ? Layer[Position.X + Layer.TileSize, Position.Y] : null;
			}

			return null;
		}
	}
}