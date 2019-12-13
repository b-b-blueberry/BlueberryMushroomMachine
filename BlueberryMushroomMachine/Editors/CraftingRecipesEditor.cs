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
			var name = ModEntry.Instance.i18n.Get("machine.name");
			var data = asset.AsDictionary<string, string>().Data;
			if (!data.ContainsKey(name))
				data.Add(name, Data.CraftingRecipeData);

			Log.T($"Recipe injected: \"{name}\": \"{data[name]}\"");
		}
	}
}
