using System;
using System.Reflection;

using Microsoft.Xna.Framework.Input;

using StardewValley;
using StardewValley.Menus;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using Harmony;  // el diavolo

namespace BlueberryMushroomMachine
{
	public class PropagatorMod : Mod
	{
		internal static Config SConfig;
		internal static IModHelper SHelper;
		internal static IMonitor SMonitor;
		internal static ITranslationHelper i18n => SHelper.Translation;
		private bool isInit;
		
		public override void Entry(IModHelper helper)
		{
			// Internals.
			SHelper = helper;
			SMonitor = Monitor;
			SConfig = helper.ReadConfig<Config>();

			// Debug events.
			if (SConfig.Debugging)
			{
				// Debug shortcut hotkeys.
				helper.Events.Input.ButtonPressed += OnButtonPressed;
			}

			SHelper.Events.GameLoop.GameLaunched += OnGameLaunched;
			SHelper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			SHelper.Events.GameLoop.DayStarted += OnDayStarted;

			// Harmony setup.
			var harmony = HarmonyInstance.Create("blueberry.BlueberryMushroomMachine");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
		
		internal static void InjectCustomObjectData()
		{
			// Inject recipe into the Craftables data sheet.
			SHelper.Content.AssetEditors.Add(new Editors.CraftingRecipesEditor());
			// Inject sprite into the Craftables tilesheet.
			SHelper.Content.AssetEditors.Add(new Editors.BigCraftablesTilesheetEditor());
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Add Demetrius' event.
			SHelper.Content.AssetEditors.Add(new Editors.EventsEditor());
		}

		private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			// Identify the tilesheet index for the machine.
			if (PropagatorData.PropagatorIndex == 0)
				SHelper.Content.AssetEditors.Add(new Editors.BigCraftablesInfoEditor());
		}

		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			// Edit all assets that rely on the generated object index.
			if (!isInit)
				InjectCustomObjectData();
			isInit = true;

			// Add Robin's pre-Demetrius-event dialogue.
			if (Game1.player.daysUntilHouseUpgrade.Value == 2 && Game1.player.HouseUpgradeLevel == 2)
				Game1.player.activeDialogueEvents.Add("event.4637.0000.0000", 7);

			// Add the Propagator crafting recipe if the cheat is enabled.
			SMonitor.Log("Recipe always available: " + SConfig.RecipeAlwaysAvailable.ToString(), LogLevel.Trace);
			if (SConfig.RecipeAlwaysAvailable)
			{
				SMonitor.Log(Game1.player.Name + " cheated in the recipe.", LogLevel.Trace);
				Game1.player.craftingRecipes.Add(PropagatorData.PropagatorName, 0);
			}
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			e.Button.TryGetKeyboard(out Keys keyPressed);

			// Debug functionalities.
			if (Game1.activeClickableMenu == null)
			{
				if (keyPressed.ToSButton().Equals(SConfig.GivePropagatorKey))
				{
					SMonitor.Log(Game1.player.Name + " cheated in a Propagator.", LogLevel.Trace);

					Propagator prop = new Propagator(Game1.player.getTileLocation());
					Game1.player.addItemByMenuIfNecessary(prop);
				}
			}
		}
	}

	#region Harmony Patches

	[HarmonyPatch(typeof(CraftingRecipe))]
	[HarmonyPatch("createItem")]
	[HarmonyPatch(new Type[] { })]
	class PropagatorCraftingRecipeCreatePatch
	{
		static void Postfix(CraftingRecipe __instance, Item __result)
		{
			// Intercept machine crafts with a Propagator subclass,
			// rather than a generic nonfunctional craftable.
			if (__instance.name.Equals(PropagatorData.PropagatorName))
				__result = new Propagator(Game1.player.getTileLocation());
		}
	}
	
	[HarmonyPatch(typeof(CraftingPage))]
	[HarmonyPatch("clickCraftingRecipe")]
	[HarmonyPatch(new Type[] { typeof(ClickableTextureComponent), typeof(bool) })]
	class PropagatorCraftingPagePatch
	{
		static bool Prefix(CraftingPage __instance, int ___currentCraftingPage, Item ___heldItem,
			ClickableTextureComponent c, bool playSound = true)
		{
			// Fetch an instance of any clicked-on craftable in the crafting menu.
			Item tempItem = __instance
				.pagesOfCraftingRecipes[___currentCraftingPage][c]
				.createItem();

			// Fall through the prefix for any craftables other than the Propagator.
			if (!tempItem.Name.Equals(PropagatorData.PropagatorName))
				return true;

			// Behaviours as from base method.
			if (___heldItem == null)
			{
				__instance.pagesOfCraftingRecipes[___currentCraftingPage][c]
					.consumeIngredients();
				___heldItem = tempItem;
				if (playSound)
					Game1.playSound("coin");
			}
			if (Game1.player.craftingRecipes.ContainsKey(__instance.pagesOfCraftingRecipes[___currentCraftingPage][c].name))
				Game1.player.craftingRecipes[__instance.pagesOfCraftingRecipes[___currentCraftingPage][c].name]
					+= __instance.pagesOfCraftingRecipes[___currentCraftingPage][c].numberProducedPerCraft;
			if (___heldItem == null || !Game1.player.couldInventoryAcceptThisItem(___heldItem))
				return false;
			
			// Add the machine to the user's inventory.
			Propagator prop = new Propagator(Game1.player.getTileLocation());
			Game1.player.addItemToInventoryBool(prop, false);
			___heldItem = null;
			return false;
		}
	}

	#endregion
}
