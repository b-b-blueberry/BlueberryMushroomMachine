using System;
using System.Reflection;

using Microsoft.Xna.Framework.Input;

using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using Harmony;  // el diavolo

namespace BlueberryMushroomMachine
{
	public class PropagatorMod : Mod
	{
		internal static Config mConfig;
		internal static IModHelper mHelper;
		internal static IMonitor mMonitor;
		internal static ITranslationHelper i18n => mHelper.Translation;

		public override void Entry(IModHelper helper)
		{
			// Internals.
			mHelper = helper;
			mMonitor = Monitor;
			mConfig = helper.ReadConfig<Config>();

			// Debug events.
			if (mConfig.Debugging)
			{
				// Debug shortcut hotkeys.
				helper.Events.Input.ButtonPressed += OnButtonPressed;
			}


			// Inject sprite into the Craftables tilesheet, then use this to index object metadata.
			mHelper.Content.AssetEditors.Add(new Editors.BigCraftablesTilesheetEditor());
			mHelper.Events.GameLoop.GameLaunched += OnGameLaunched;
			
			// Instantiate Harmony.
			var harmony = HarmonyInstance.Create("blueberry.BlueberryMushroomMachine");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
		
		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Inject custom events.
			mHelper.Content.AssetEditors.Add(new Editors.EventsEditor());

			// Edit later all assets that rely on a generated object index.
			mHelper.Content.AssetEditors.Add(new Editors.BigCraftablesInfoEditor());
			mHelper.Content.AssetEditors.Add(new Editors.CraftingRecipesEditor());
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			e.Button.TryGetKeyboard(out Keys keyPressed);

			// Debug functionalities.
			if (keyPressed.ToSButton().Equals(mConfig.GivePropagatorKey))
			{
				mMonitor.Log("Cheated in a Propagator.", LogLevel.Trace);

				Propagator prop = new Propagator(Game1.player.getTileLocation());
				Game1.player.addItemByMenuIfNecessary(prop);
			}
		}
	}

	#region Harmony Patches
	[HarmonyPatch(typeof(FarmHouse))]
	[HarmonyPatch("showSpouseRoom")]
	[HarmonyPatch(new Type[]{})]
	class PropagatorSpouseRoomPatch
	{
		static void Postfix(FarmHouse __instance)
		{
			if (__instance.upgradeLevel == 3)
				if (!Game1.player.craftingRecipes.ContainsKey(PropagatorData.mPropagatorName))
					Game1.player.craftingRecipes.Add(PropagatorData.mPropagatorName, 0);
		}
	}
	[HarmonyPatch(typeof(FarmHouse))]
	[HarmonyPatch("setMapForUpgradeLevel")]
	[HarmonyPatch(new Type[] { typeof(int) })]
	class PropagatorUpgradeLevelPatch
	{
		static void Postfix(int level)
		{
			if (level == 3)
				if (!Game1.player.craftingRecipes.ContainsKey(PropagatorData.mPropagatorName))
					Game1.player.craftingRecipes.Add(PropagatorData.mPropagatorName, 0);
		}
	}
	
	[HarmonyPatch(typeof(CraftingRecipe))]
	[HarmonyPatch("createItem")]
	[HarmonyPatch(new Type[] { })]
	class PropagatorCraftingRecipeCreatePatch
	{
		static void Postfix(CraftingRecipe __instance, Item __result)
		{
			if (__instance.name.Equals(PropagatorData.mPropagatorName))
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

			// Fall through the prefix for any craftables other than this machine.
			if (!tempItem.Name.Equals(PropagatorData.mPropagatorName))
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
