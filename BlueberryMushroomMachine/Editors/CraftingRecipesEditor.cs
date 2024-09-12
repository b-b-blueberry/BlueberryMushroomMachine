using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Collections.Generic;

namespace BlueberryMushroomMachine.Editors
{
	internal static class CraftingRecipesEditor
	{
		public static bool ApplyEdit(AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo(@"Data/CraftingRecipes"))
			{
				e.Edit(EditImpl);
				return true;
			}
			return false;
		}
		public static void EditImpl(IAssetData asset)
		{
			Log.T($"Editing {asset.Name}.",
				ModEntry.Config.DebugMode);

			// Inject crafting recipe data using custom appended index as the result
			IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
			data[ModValues.PropagatorItemId] = string.Format(ModEntry.Data.RecipeFormat, ModValues.PropagatorItemId);
		}
	}
}
