using System.Collections.Generic;

using StardewModdingAPI;

namespace BlueberryMushroomMachine.Editors
{
	class BigCraftablesInfoEditor : IAssetEditor
	{
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals("Data/BigCraftablesInformation");
		}
		public void Edit<T>(IAssetData asset)
		{
			// Inject custom object data with appending index.
			IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
			data.Add(PropagatorData.PropagatorIndex, PropagatorData.ObjectData);
		}
	}
}
