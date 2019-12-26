using BaseLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace LayerLibrary
{
	public interface IModLayer
	{
		bool Place(BaseLayerItem item);

		void Remove();
	}

	public abstract class ModLayer<T> : IModLayer where T : ModLayerElement<T>, new()
	{
		public abstract int TileSize { get; }

		public virtual bool Visible => true;

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

		public bool TryGetValue(Point16 position, out T element)
		{
			if (ContainsKey(position))
			{
				element = this[position];
				return true;
			}

			element = null;
			return false;
		}

		public bool TryGetValue(int i, int j, out T element)
		{
			if (ContainsKey(i, j))
			{
				element = this[i, j];
				return true;
			}

			element = null;
			return false;
		}

		public virtual void NetSend(BinaryWriter writer)
		{
			writer.Write(data.Count);

			foreach (KeyValuePair<Point16, T> pair in data)
			{
				writer.Write(pair.Key);
				pair.Value.NetSend(writer);
			}
		}

		public virtual void NetReceive(BinaryReader reader)
		{
			data.Clear();

			int count = reader.ReadInt32();

			for (int i = 0; i < count; i++)
			{
				T element = new T
				{
					Position = reader.ReadPoint16(),
					Frame = Point16.Zero,
					Layer = this
				};
				element.NetReceive(reader);
				data.Add(element.Position, element);
			}

			foreach (T element in data.Values) element.UpdateFrame();
		}

		public virtual List<TagCompound> Save() => data.Values.Select(element => new TagCompound
		{
			["Position"] = element.Position,
			["Data"] = element.Save()
		}).ToList();

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
				data[element.Position] = element;
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

			int startX = (int)((Main.screenPosition.X - zero.X) / 16f) - 3;
			int endX = (int)((Main.screenPosition.X + Main.screenWidth + zero.X) / 16f) + 3;
			int startY = (int)((Main.screenPosition.Y - zero.Y) / 16f) - 3;
			int endY = (int)((Main.screenPosition.Y + Main.screenHeight + zero.Y) / 16f) + 3;

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

		public virtual bool Place(BaseLayerItem item)
		{
			int posX = Player.tileTargetX;
			int posY = Player.tileTargetY;

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
				element.OnPlace();

				element.UpdateFrame();
				foreach (T neighbor in element.GetNeighbors()) neighbor.UpdateFrame();

				return true;
			}

			return false;
		}

		public virtual void Remove()
		{
			int posX = Player.tileTargetX;
			int posY = Player.tileTargetY;

			for (int i = 1; i <= TileSize; i++)
			{
				if (posX % TileSize != 0) posX -= 1;
				if (posY % TileSize != 0) posY -= 1;
			}

			if (ContainsKey(posX, posY))
			{
				T element = this[posX, posY];

				element.OnRemove();
				data.Remove(new Point16(posX, posY));

				foreach (T neighbor in element.GetNeighbors()) neighbor.UpdateFrame();

				Item.NewItem(posX * 16, posY * 16, TileSize * 16, TileSize * 16, element.DropItem);
			}
		}

		public virtual bool Interact() => false;
	}
}