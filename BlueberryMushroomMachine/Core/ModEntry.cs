using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework.Input;

using StardewValley;
using StardewValley.Menus;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using Harmony;  // el diavolo

namespace BlueberryMushroomMachine
{
	public class ModEntry : Mod
	{
		internal static ModEntry Instance;

		internal Config Config;
		internal ITranslationHelper i18n => Helper.Translation;
		
		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<Config>();

			Log.I("Development spam is on, see TRACE for debugging info.");
			Log.I("Post a complete log via HTTPS://LOG.SMAPI.IO/ on the Nexus page if you hit any errors."
				+ " Thank you!");

			// Debug events.
			if (Config.Debugging)
			{
				// Debug shortcut hotkeys.
				helper.Events.Input.ButtonPressed += OnButtonPressed;
			}

			Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			Helper.Events.GameLoop.DayStarted += OnDayStarted;

			// Harmony setup.
			var harmony = HarmonyInstance.Create($"{Data.AuthorName}.{Data.PackageName}");
			
			harmony.Patch(
				original: AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.createItem)),
				postfix: new HarmonyMethod(typeof(CraftingRecipePatch), nameof(CraftingRecipePatch.Postfix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CraftingPage), "clickCraftingRecipe"),
				prefix: new HarmonyMethod(typeof(CraftingPagePatch), nameof(CraftingPagePatch.Prefix)));
		}

		#region Game Events

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Identify the tilesheet index for the machine, and then continue
			// to inject relevant data into multiple other assets if successful.
			if (!Game1.bigCraftablesInformation.ContainsKey(Data.PropagatorIndex) || Data.PropagatorIndex == 0)
			{
				AddObjectData();
			}
		}
		
		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			// Add Robin's pre-Demetrius-event dialogue.
			if (Game1.player.daysUntilHouseUpgrade.Value == 2 && Game1.player.HouseUpgradeLevel == 2)
				Game1.player.activeDialogueEvents.Add("event.4637.0000.0000", 7);

			// Add the Propagator crafting recipe if the cheat is enabled.
			if (Config.RecipeAlwaysAvailable)
			{
				if (!Game1.player.craftingRecipes.ContainsKey(Data.PropagatorName))
					Game1.player.craftingRecipes.Add(Data.PropagatorName, 0);
			}

			Log.T($"Recipe Cheat: {Config.RecipeAlwaysAvailable.ToString()}");

			// TEMPORARY FIX: Manually rebuild each Propagator in the user's inventory.
			// PyTK ~1.12.13.unofficial seemingly rebuilds inventory objects at ReturnedToTitle,
			// so inventory objects are only rebuilt after the save is reloaded for every session.
			var items = Game1.player.Items;
			for (var i = items.Count-1; i > 0; --i)
			{
				if (items[i] != null)
				{
					if (items[i].Name.StartsWith($"PyTK|Item|{Data.PackageName}.{Data.PropagatorName}"))
					{
						Log.T($"Found {items[i].Name}.");
						Log.T($"Found a broken {Data.PropagatorName} in {Game1.player.Name}'s inventory slot {i}"
							+", rebuilding manually.");
						
						var stack = items[i].Stack;
						Game1.player.removeItemFromInventory(items[i]);
						Game1.player.addItemToInventory(new Propagator() { Stack = stack }, i);
					}
				}
			}

			// TEMPORARY FIX: Manually DayUpdate each Propagator.
			// PyTK 1.9.11+ rebuilds objects at DayEnding, so Cask.DayUpdate is never called.
			// Also fixes 0-index objects from PyTK rebuilding before the new index is generated.
			foreach (var loc in Game1.locations)
			{
				if (loc.Objects.Values.Count() > 0)
				{
					var objects = loc.Objects.Values.Where(o => o.Name.Equals(Data.PropagatorName));
					foreach (Propagator obj in objects)
						if (obj != null)
							obj.TemporaryDayUpdate();
				}
			}
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			e.Button.TryGetKeyboard(out Keys keyPressed);

			// Debug functionalities.
			if (Game1.activeClickableMenu == null)
			{
				if (keyPressed.ToSButton().Equals(Config.GivePropagatorKey))
				{
					Log.T($"{Game1.player.Name} cheated in a machine.");

					Propagator prop = new Propagator(Game1.player.getTileLocation());
					Game1.player.addItemByMenuIfNecessary(prop);
				}
			}
		}

		private void AddObjectData()
		{
			Log.T($"Injecting object data (current index: {Data.PropagatorIndex}).");

			// Identify the index in bigCraftables for the machine.
			Helper.Content.AssetEditors.Add(new Editors.BigCraftablesInfoEditor());

			// Edit all assets that rely on the generated object index.

			// These can potentially input a bad index first, though BigCraftablesInfoEditor.Edit()
			// invalidates the cache once it finishes operations to reassign data with an appropriate index.

			// Inject recipe into the Craftables data sheet.
			Helper.Content.AssetEditors.Add(new Editors.CraftingRecipesEditor());
			// Inject sprite into the Craftables tilesheet.
			Helper.Content.AssetEditors.Add(new Editors.BigCraftablesTilesheetEditor());
			// Inject Demetrius' event.
			Helper.Content.AssetEditors.Add(new Editors.EventsEditor());
		}

		#endregion
	}

	#region Harmony Patches

	class CraftingRecipePatch
	{
		internal static void Postfix(CraftingRecipe __instance, Item __result)
		{
			// Intercept machine crafts with a Propagator subclass,
			// rather than a generic nonfunctional craftable.
			if (__instance.name.Equals(Data.PropagatorName))
				__result = new Propagator(Game1.player.getTileLocation());
		}
	}
	
	class CraftingPagePatch
	{
		internal static bool Prefix(
			List<Dictionary<ClickableTextureComponent, CraftingRecipe>> ___pagesOfCraftingRecipes,
			int ___currentCraftingPage, Item ___heldItem,
			ClickableTextureComponent c, bool playSound = true)
		{
			try
			{
				// Fetch an instance of any clicked-on craftable in the crafting menu.
				Item tempItem = ___pagesOfCraftingRecipes[___currentCraftingPage][c]
					.createItem();

				// Fall through the prefix for any craftables other than the Propagator.
				if (!tempItem.Name.Equals(Data.PropagatorName))
					return true;

				// Behaviours as from base method.
				if (___heldItem == null)
				{
					___pagesOfCraftingRecipes[___currentCraftingPage][c]
						.consumeIngredients(null);
					___heldItem = tempItem;
					if (playSound)
						Game1.playSound("coin");
				}
				if (Game1.player.craftingRecipes.ContainsKey(___pagesOfCraftingRecipes[___currentCraftingPage][c].name))
					Game1.player.craftingRecipes[___pagesOfCraftingRecipes[___currentCraftingPage][c].name]
						+= ___pagesOfCraftingRecipes[___currentCraftingPage][c].numberProducedPerCraft;
				if (___heldItem == null || !Game1.player.couldInventoryAcceptThisItem(___heldItem))
					return false;

				// Add the machine to the user's inventory.
				Propagator prop = new Propagator(Game1.player.getTileLocation());
				Game1.player.addItemToInventoryBool(prop, false);
				___heldItem = null;
				return false;
			}
			catch (Exception e)
			{
				Log.E($"{Data.AuthorName}.{Data.PackageName} failed in" +
					$"{nameof(CraftingPagePatch)}.{nameof(Prefix)}" +
					$"\n{e}");
				return true;
			}
		}
	}

	#endregion
}
