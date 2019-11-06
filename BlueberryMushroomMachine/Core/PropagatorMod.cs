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
		internal static Config Config;
		internal static new IModHelper Helper;
		internal static new IMonitor Monitor;
		internal static ITranslationHelper i18n => Helper.Translation;

		public override void Entry(IModHelper helper)
		{
			// Internals.
			Helper = helper;
			Monitor = base.Monitor;
			Config = helper.ReadConfig<Config>();

			// Debug events.
			if (Config.Debugging)
			{
				// Debug shortcut hotkeys.
				helper.Events.Input.ButtonPressed += OnButtonPressed;
			}

			// Setup dialogue addition checks.
			Helper.Events.GameLoop.DayStarted += OnDayStarted;

			// Inject sprite into the Craftables tilesheet, then use this to index object metadata.
			Helper.Content.AssetEditors.Add(new Editors.BigCraftablesTilesheetEditor());
			Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			
			// Harmony setup.
			var harmony = HarmonyInstance.Create("blueberry.BlueberryMushroomMachine");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
		
		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Inject custom events.
			Helper.Content.AssetEditors.Add(new Editors.EventsEditor());

			// Edit later all assets that rely on a generated object index.
			Helper.Content.AssetEditors.Add(new Editors.BigCraftablesInfoEditor());
			Helper.Content.AssetEditors.Add(new Editors.CraftingRecipesEditor());
		}

		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			if (Game1.player.daysUntilHouseUpgrade.Value == 2 && Game1.player.HouseUpgradeLevel == 2)
				// Add Robin's pre-Demetrius-event dialogue.
				Game1.player.activeDialogueEvents.Add("event.4637.0000.0000", 7);
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			e.Button.TryGetKeyboard(out Keys keyPressed);

			// Debug functionalities.
			if (keyPressed.ToSButton().Equals(Config.GivePropagatorKey))
			{
				Monitor.Log(Game1.player.Name + " cheated in a Propagator.", LogLevel.Trace);

				Propagator prop = new Propagator(Game1.player.getTileLocation());
				Game1.player.addItemByMenuIfNecessary(prop);
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
