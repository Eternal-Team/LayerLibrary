using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
	}

	public abstract class ModLayer<T> : IModLayer where T : ModLayerElement, new()
	{
		public abstract string Name { get; }

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

			if (startX < 4) startX = 4;
			if (endX > Main.maxTilesX - 4) endX = Main.maxTilesX - 4;
			if (startY < 4) startY = 4;
			if (endY > Main.maxTilesY - 4) endY = Main.maxTilesY - 4;

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
			Point16 position = Main.MouseWorld.ToTileCoordinates16();
			if (!data.ContainsKey(position))
			{
				T element = new T
				{
					Position = position,
					Frame = Point16.Zero,
					Layer = this
				};
				data.Add(position, element);

				element.UpdateFrame();

				if (ContainsKey(position.X + 1, position.Y)) this[position.X + 1, position.Y].UpdateFrame();
				if (ContainsKey(position.X - 1, position.Y)) this[position.X - 1, position.Y].UpdateFrame();
				if (ContainsKey(position.X, position.Y + 1)) this[position.X, position.Y + 1].UpdateFrame();
				if (ContainsKey(position.X, position.Y - 1)) this[position.X, position.Y - 1].UpdateFrame();

				return true;
			}

			return false;
		}

		public virtual void Remove(BaseLayerItem<T> item)
		{
			Point16 position = Main.MouseWorld.ToTileCoordinates16();
			if (data.ContainsKey(position))
			{
				data.Remove(position);

				if (ContainsKey(position.X + 1, position.Y)) this[position.X + 1, position.Y].UpdateFrame();
				if (ContainsKey(position.X - 1, position.Y)) this[position.X - 1, position.Y].UpdateFrame();
				if (ContainsKey(position.X, position.Y + 1)) this[position.X, position.Y + 1].UpdateFrame();
				if (ContainsKey(position.X, position.Y - 1)) this[position.X, position.Y - 1].UpdateFrame();

				Item.NewItem(position.X * 16, position.Y * 16, 16, 16, item.item.type);
			}
		}

		public virtual void Interact()
		{
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
			if (Layer.ContainsKey(Position.X - 1, Position.Y)) frameX += 18;
			if (Layer.ContainsKey(Position.X + 1, Position.Y)) frameX += 36;
			if (Layer.ContainsKey(Position.X, Position.Y - 1)) frameY += 18;
			if (Layer.ContainsKey(Position.X, Position.Y + 1)) frameY += 36;
			Frame = new Point16(frameX, frameY);
		}

		public virtual void Draw(SpriteBatch spriteBatch)
		{
			Vector2 position = Position.ToVector2() * 16 - Main.screenPosition;
			Color color = Lighting.GetColor(Position.X, Position.Y);
			spriteBatch.Draw(ModContent.GetTexture(Texture), position, new Rectangle(Frame.X, Frame.Y, 16, 16), color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
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