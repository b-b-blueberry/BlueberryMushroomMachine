using System.Collections.Generic;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BlueberryMushroomMachine.Editors
{
	internal static class EventsEditor
	{
		public static bool ApplyEdit(AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo(@"Characters/Dialogue/Robin"))
			{
				e.Edit(apply: (IAssetData asset) =>
				{
					IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
					const string key = "event.4637.0000.0000";
					if (!data.ContainsKey(key))
					{
						data.Add(key, ModEntry.Instance.I18n.Get(key));
					}
				});
				return true;
			}
			else if (e.NameWithoutLocale.IsEquivalentTo(@"Data/Events/Farm"))
			{
				e.Edit(apply: EventsEditor.ApplyEventEdit);
				return true;
			}
			return false;
		}

		public static void ApplyEventEdit(IAssetData asset)
		{
			IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
			IDictionary<string, string> json = ModEntry.Instance.Helper.ModContent.Load
				<IDictionary<string, string>>
				(ModValues.EventsPath);

			foreach (string key in json.Keys)
			{
				if (key.StartsWith(ModValues.EventId.ToString()))
				{
					if (Game1.player.HouseUpgradeLevel >= 3)
					{
						Log.D("Event conditions:" +
							  $" disabled=[{ModEntry.Instance.Config.DisabledForFruitCave}]" +
							  $" caveChoice=[{Game1.MasterPlayer.caveChoice}]",
							ModEntry.Instance.Config.DebugMode);

						if (ModEntry.Instance.Config.DisabledForFruitCave && Game1.MasterPlayer.caveChoice.Value != Farmer.caveMushrooms)
						{
							return;
						}

						if (!data.ContainsKey(key))
						{
							string value = string.Format(
								json[key],
								ModEntry.Instance.I18n.Get("event.4637.0001.0000"),
								ModEntry.Instance.I18n.Get("event.4637.0001.0001"),
								ModEntry.Instance.I18n.Get("event.4637.0001.0002"),
								ModEntry.Instance.I18n.Get("event.4637.0001.0003"),
								ModValues.PropagatorInternalName);
							Log.D($"Injecting event.",
								ModEntry.Instance.Config.DebugMode);
							data.Add(key, value);
						}
					}
				}
			}
		}
	}
}
