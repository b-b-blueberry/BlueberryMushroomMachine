using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewModdingAPI;

namespace BlueberryMushroomMachine.Editors
{
	class BigCraftablesTilesheetEditor : IAssetEditor
	{
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals("TileSheets/Craftables");
		}
		public void Edit<T>(IAssetData asset)
		{
			// Slide into a free tilesheet index.
			PropagatorData.PropagatorIndex = Game1.bigCraftableSpriteSheet.Height / 16
					* (Game1.bigCraftableSpriteSheet.Width / 16 / 2);

			// Update not-yet-injected crafting recipe data to match.
			PropagatorData.CraftingRecipeData = string.Format(
				PropagatorData.CraftingRecipeData, PropagatorData.PropagatorIndex);

			// Expand the base tilesheet.
			Texture2D src = PropagatorMod.Helper.Content.Load<Texture2D>(PropagatorData.MachinePath);
			IAssetDataForImage dest = asset.AsImage();
			Rectangle srcRect = new Rectangle(0, 0, 16, 32);
			Rectangle destRect = Propagator.getSourceRectForBigCraftable(PropagatorData.PropagatorIndex);

			if (destRect.Bottom > dest.Data.Height)
			{
				Texture2D original = dest.Data;
				Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, original.Width, destRect.Bottom);
				dest.ReplaceWith(texture);
				dest.PatchImage(original);
			}

			// Append machine sprite onto the default tilesheet.
			dest.PatchImage(src, srcRect, destRect);
		}
	}
}
