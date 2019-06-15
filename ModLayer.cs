using BaseLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace LayerLibrary
{
	public interface IModLayer
	{
		bool ContainsKey(int i, int j);

		int TileSize { get; }
	}

	public abstract class ModLayer<T> : IModLayer where T : ModLayerElement, new()
	{
		public abstract string Name { get; }

		public virtual bool Visible => true;

		public abstract int TileSize { get; }

		protected Dictionary<Point16, T> data;

		public ModLayer()
		{
			data = new Dictionary<Point16, T>();
		}

		public T this[int i, int j]
		{
			get => data[new Point16(i, j)];
			set => data[new Point16(i, j)] = value;
		}

		public T this[Point16 position]
		{
			get => data[position];
			set => data[position] = value;
		}

		public bool ContainsKey(Point16 position) => data.ContainsKey(position);

		public bool ContainsKey(int i, int j) => data.ContainsKey(new Point16(i, j));

		public virtual List<TagCompound> Save()
		{
			List<TagCompound> list = new List<TagCompound>();
			foreach (T element in data.Values)
			{
				list.Add(new TagCompound
				{
					["Position"] = element.Position,
					["Data"] = element.Save()
				});
			}

			return list;
		}

		public virtual void Load(List<TagCompound> list)
		{
			data.Clear();

			foreach (TagCompound tag in list)
			{
				T element = new T
				{
					Position = tag.Get<Point16>("Position"),
					Frame = Point16.Zero,
					Layer = this
				};
				element.Load(tag.GetCompound("Data"));
				data.Add(element.Position, element);
			}

			foreach (T element in data.Values) element.UpdateFrame();
		}

		public virtual void Draw(SpriteBatch spriteBatch)
		{
			if (!Visible) return;

			//if (Main.LocalPlayer.GetHeldItem().modItem == null) return;
			//if (!(Main.LocalPlayer.GetHeldItem().modItem is BaseCable) && !Main.LocalPlayer.GetHeldItem().modItem.GetType().HasAttribute<EnergyTileAttribute>() && !(Main.LocalPlayer.GetHeldItem().modItem is Wrench)) return;

			DrawPreview(Main.spriteBatch);

			if (data.Count <= 0) return;

			Vector2 zero = new Vector2(Main.offScreenRange);
			if (Main.drawToScreen) zero = Vector2.Zero;

			int startX = (int)((Main.screenPosition.X - zero.X) / 16f);
			int endX = (int)((Main.screenPosition.X + Main.screenWidth + zero.X) / 16f);
			int startY = (int)((Main.screenPosition.Y - zero.Y) / 16f);
			int endY = (int)((Main.screenPosition.Y + Main.screenHeight + zero.Y) / 16f);

			foreach (KeyValuePair<Point16, T> pair in data)
			{
				if (pair.Key.X > startX && pair.Key.X < endX && pair.Key.Y > startY && pair.Key.Y < endY)
				{
					pair.Value.Draw(spriteBatch);
				}
			}
		}

		public virtual void DrawPreview(SpriteBatch spriteBatch)
		{
		}

		public virtual void Update()
		{
			foreach (T element in data.Values) element.Update();
		}

		public virtual bool Place(BaseLayerItem<T> item)
		{
			int posX = (int)(Main.MouseWorld.X / 16f);
			int posY = (int)(Main.MouseWorld.Y / 16f);

			for (int i = 1; i <= TileSize; i++)
			{
				if (posX % TileSize != 0) posX -= 1;
				if (posY % TileSize != 0) posY -= 1;
			}

			if (!ContainsKey(posX, posY))
			{
				T element = new T
				{
					Position = new Point16(posX, posY),
					Frame = Point16.Zero,
					Layer = this
				};
				data.Add(new Point16(posX, posY), element);

				element.UpdateFrame();

				foreach (T neighbor in GetNeighbors(new Point16(posX, posY))) neighbor.UpdateFrame();

				return true;
			}

			return false;
		}

		public virtual void Remove(BaseLayerItem<T> item)
		{
			int posX = (int)(Main.MouseWorld.X / 16f);
			int posY = (int)(Main.MouseWorld.Y / 16f);

			for (int i = 1; i <= TileSize; i++)
			{
				if (posX % TileSize != 0) posX -= 1;
				if (posY % TileSize != 0) posY -= 1;
			}

			if (ContainsKey(posX, posY))
			{
				this[posX, posY].OnRemove();
				data.Remove(new Point16(posX, posY));

				foreach (T neighbor in GetNeighbors(new Point16(posX, posY))) neighbor.UpdateFrame();

				Item.NewItem(posX * 16, posY * 16, TileSize * 16, TileSize * 16, item.item.type);
			}
		}

		public virtual void Interact()
		{
		}

		public IEnumerable<T> GetNeighbors(Point16 Position)
		{
			if (ContainsKey(Position.X + TileSize, Position.Y)) yield return this[Position.X + TileSize, Position.Y];
			if (ContainsKey(Position.X - TileSize, Position.Y)) yield return this[Position.X - TileSize, Position.Y];
			if (ContainsKey(Position.X, Position.Y + TileSize)) yield return this[Position.X, Position.Y + TileSize];
			if (ContainsKey(Position.X, Position.Y - TileSize)) yield return this[Position.X, Position.Y - TileSize];
		}

		public T GetNeighbor(int x, int y, Side side)
		{
			if (!ContainsKey(x, y)) throw new Exception($"Layer contains no element at position {{X: {x}; Y: {y}}}");

			switch (side)
			{
				case Side.Bottom: return ContainsKey(x, y + TileSize) ? this[x, y + TileSize] : null;
				case Side.Top: return ContainsKey(x, y - TileSize) ? this[x, y - TileSize] : null;
				case Side.Left: return ContainsKey(x - TileSize, y) ? this[x - TileSize, y] : null;
				case Side.Right: return ContainsKey(x + TileSize, y) ? this[x + TileSize, y] : null;
			}

			return null;
		}
	}

	public abstract class ModLayerElement
	{
		public IModLayer Layer;

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

		public virtual void Draw(SpriteBatch spriteBatch)
		{
			Vector2 position = Position.ToVector2() * 16 - Main.screenPosition;
			Color color = Lighting.GetColor(Position.X, Position.Y);
			spriteBatch.Draw(ModContent.GetTexture(Texture), position, new Rectangle(Frame.X, Frame.Y, Layer.TileSize * 16, Layer.TileSize * 16), color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
		}

		public virtual void Update()
		{
		}

		public virtual TagCompound Save() => new TagCompound();

		public virtual void Load(TagCompound tag)
		{
		}
	}
}