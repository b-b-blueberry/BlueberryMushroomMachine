using System.Collections.Generic;

using StardewModdingAPI;

namespace BlueberryMushroomMachine
{
	class CraftingRecipesEditor : IAssetEditor
	{
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals("Data/CraftingRecipes");
		}
		public void Edit<T>(IAssetData asset)
		{
			// Inject crafting recipe data using custom appended index as the result.
			IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
			data.Add(PropagatorData.mPropagatorName, PropagatorData.mCraftingRecipeData);

			PropagatorMod.mMonitor.Log("CraftingRecipesEditor"
				+ "Edited : " + data[PropagatorData.mPropagatorName],
				LogLevel.Trace);
		}
	}
}
