using System.Linq;

using StardewValley;
using StardewModdingAPI;

namespace BlueberryMushroomMachine.Editors
{
	class BigCraftablesInfoEditor : IAssetEditor
	{
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals(@"Data/BigCraftablesInformation");
		}

		public void Edit<T>(IAssetData asset)
		{
			var data = asset.AsDictionary<int, string>().Data;

			// Slide into a free tilesheet index.
			var indicesPerRow = Game1.bigCraftableSpriteSheet.Width / 16;
			var index = data.Keys.Where(id => id < 4000).Max();	// Avoids JA incompatibilities
			index += indicesPerRow - (index % indicesPerRow);	// by not expanding the tilesheet
			Data.PropagatorIndex = index;                       // to be 4096px high.

			Log.T($"Object indexed:  {Data.PropagatorIndex}");

			// Inject custom object data with appending index.
			Data.ObjectData = string.Format(Data.ObjectData,
				ModEntry.Instance.i18n.Get("machine.name"),
				ModEntry.Instance.i18n.Get("machine.desc"));

			if (!data.ContainsKey(Data.PropagatorIndex))
				data.Add(Data.PropagatorIndex, Data.ObjectData);

			// Update not-yet-injected crafting recipe data to match.
			Data.CraftingRecipeData = string.Format(
				Data.CraftingRecipeData, Data.PropagatorIndex);

			Log.T($"Object injected: {data[Data.PropagatorIndex]}");

			// Invalidate cache of possibly-badly-indexed data.
			ModEntry.Instance.Helper.Content.InvalidateCache(@"Data/Events/Farm");
			ModEntry.Instance.Helper.Content.InvalidateCache(@"Data/CraftingRecipes");
			ModEntry.Instance.Helper.Content.InvalidateCache(@"Tilesheets/Craftables");
		}
	}
}
