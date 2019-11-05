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
			IDictionary<string, string> data = null, json = null;
			
			if (asset.AssetNameEquals("Data\\Events\\Farm"))
			{
				// Event 0001: Farm, Demetrius
				// Receive Propagator recipe upon house upgrade level 3
				if (Game1.player.HouseUpgradeLevel >= 3)
				{
					data = asset.AsDictionary<string, string>().Data;
					json = PropagatorMod.mHelper.Content.Load<IDictionary<string, string> >
						(PropagatorData.mEventsPath);
					json[PropagatorData.mEvent0001] =
						string.Format(json[PropagatorData.mEvent0001],
							PropagatorMod.i18n.Get("event.0001.0000"),
							PropagatorMod.i18n.Get("event.0001.0001"),
							PropagatorMod.i18n.Get("event.0001.0002"),
							PropagatorMod.i18n.Get("event.0001.0003"));
				}
			}

			// Populate the target event data file with all the new data.
			if (data != null && json != null)
				foreach (var kv in json)
					data.Add(kv.Key, kv.Value);
		}
	}
}
