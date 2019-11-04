using System.Collections.Generic;

using StardewModdingAPI;

namespace BlueberryMushroomMachine
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
			data.Add(PropagatorData.mPropagatorIndex, PropagatorData.mObjectData);

			PropagatorMod.mMonitor.Log("BigCraftablesInfoEditor"
				+ "\nEdited : " + data[PropagatorData.mPropagatorIndex],
				LogLevel.Trace);
		}
	}
}
