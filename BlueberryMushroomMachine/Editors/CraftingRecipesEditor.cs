using StardewModdingAPI;

namespace BlueberryMushroomMachine.Editors
{
	class CraftingRecipesEditor : IAssetEditor
	{
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals(@"Data/CraftingRecipes");
		}
		public void Edit<T>(IAssetData asset)
		{
			// Inject crafting recipe data using custom appended index as the result.
			var data = asset.AsDictionary<string, string>().Data;
			if (!data.ContainsKey(Data.PropagatorName))
				data.Add(Data.PropagatorName, Data.CraftingRecipeData);

			Log.D($"Recipe injected: {data[Data.PropagatorName]}");
		}
	}
}
