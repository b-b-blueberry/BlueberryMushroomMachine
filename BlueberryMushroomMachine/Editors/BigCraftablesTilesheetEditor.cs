using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BlueberryMushroomMachine.Editors
{
	internal static class BigCraftablesTilesheetEditor
	{
		private static IRawTextureData TextureData;
		internal static void Initialize(IModContentHelper helper)
		{
			BigCraftablesTilesheetEditor.TextureData = helper.Load<IRawTextureData>(ModValues.MachinePath);
		}

		public static bool ApplyEdit(AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo(@"TileSheets/Craftables"))
			{
				e.Edit(apply: BigCraftablesTilesheetEditor.EditImpl);
				return true;
			}
			return false;
		}

		public static void EditImpl(IAssetData asset)
		{
			Log.T($"Editing {asset.Name}.",
				ModEntry.Config.DebugMode);

			// Expand the base tilesheet if needed.
			IRawTextureData src = BigCraftablesTilesheetEditor.TextureData;
			IAssetDataForImage dest = asset.AsImage();
			Rectangle srcRect = new(x: 0, y: 0, width: 16, height: 32);
			Rectangle destRect = Propagator.getSourceRectForBigCraftable(index: ModValues.PropagatorIndex);

			// expand if needed
			dest.ExtendImage(minWidth: dest.Data.Bounds.Width, minHeight: destRect.Height);

			// Append machine sprite onto the default tilesheet.
			dest.PatchImage(source: src, sourceArea: srcRect, targetArea: destRect);
		}
	}
}
