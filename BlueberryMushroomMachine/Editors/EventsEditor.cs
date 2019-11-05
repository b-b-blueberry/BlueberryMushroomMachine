using System.Collections.Generic;
using StardewValley;
using StardewModdingAPI;

namespace BlueberryMushroomMachine.Editors
{
	class EventsEditor : IAssetEditor
	{
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return asset.AssetNameEquals("Data\\Events\\Farm");
		}

		public void Edit<T>(IAssetData asset)
		{
			var data = asset.AsDictionary<string, string>().Data;
			var json = PropagatorMod.mHelper.Content
				.Load<IDictionary<string, string>>
				(PropagatorData.mEventsPath);

			if (asset.AssetNameEquals("Data\\Events\\Farm"))
			{
				foreach (var kv in json)
				{
					if (kv.Key.StartsWith("46370001"))
					{
						// Event 0001: Farm, Demetrius
						// Receive Propagator recipe upon house upgrade level 3
						// and player did not choose the Fruit Cave.
						if (Game1.player.HouseUpgradeLevel >= 3)
						{
							if (PropagatorMod.mConfig.DisabledForFruitCave
								&& Game1.player.caveChoice.Value != 2)
								return;

							json[kv.Key] = string.Format(json[kv.Key],
								PropagatorMod.i18n.Get("event.0001.0000"),
								PropagatorMod.i18n.Get("event.0001.0001"),
								PropagatorMod.i18n.Get("event.0001.0002"),
								PropagatorMod.i18n.Get("event.0001.0003"));
						}
					}
				}
			}

			// Populate the target event data file with all the new data.
			if (data != null && json != null)
				foreach (var kv in json)
					data.Add(kv.Key, kv.Value);
		}
	}
}
