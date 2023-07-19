using HarmonyLib; // el diavolo
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace BlueberryMushroomMachine
{
	internal class HarmonyPatches
	{
		public static void Apply(string uniqueID)
		{
			Harmony harmony = new(id: uniqueID);

			harmony.Patch(
				original: AccessTools.Method(type: typeof(CraftingRecipe), name: "createItem"),
				postfix: new HarmonyMethod(methodType: typeof(HarmonyPatches), methodName: nameof(HarmonyPatches.CraftingRecipe_CreateItem_Postfix)));
			harmony.Patch(
				original: AccessTools.Method(type: typeof(CraftingPage), name: "performHoverAction"),
				postfix: new HarmonyMethod(methodType: typeof(HarmonyPatches), methodName: nameof(HarmonyPatches.CraftingPage_HoverAction_Postfix)));
			harmony.Patch(
				original: AccessTools.Method(type: typeof(CraftingPage), name: "clickCraftingRecipe"),
				prefix: new HarmonyMethod(methodType: typeof(HarmonyPatches), methodName: nameof(HarmonyPatches.CraftingPage_ClickCraftingRecipe_Prefix)));
		}

		internal static void CraftingRecipe_CreateItem_Postfix(CraftingRecipe __instance, ref Item __result)
		{
			if (__instance.name != ModValues.PropagatorInternalName)
			{
				return;
			}

			// Intercept machine crafts with a Propagator subclass,
			// rather than a generic nonfunctional craftable
			__result = new Propagator(tileLocation: Game1.player.getTileLocation());
		}

		internal static void CraftingPage_HoverAction_Postfix(CraftingRecipe ___hoverRecipe)
		{
			if (___hoverRecipe?.name != ModValues.PropagatorInternalName)
			{
				return;
			}

			// Add display name in crafting pages
			___hoverRecipe.DisplayName = Propagator.PropagatorDisplayName;
		}

		internal static bool CraftingPage_ClickCraftingRecipe_Prefix(
			CraftingPage __instance,
			List<Dictionary<ClickableTextureComponent, CraftingRecipe>> ___pagesOfCraftingRecipes,
			int ___currentCraftingPage,
			ref Item ___heldItem,
			ClickableTextureComponent c,
			bool playSound = true)
		{
			try
			{
				CraftingRecipe recipe = ___pagesOfCraftingRecipes[___currentCraftingPage][c];

				// Fetch an instance of any clicked-on craftable in the crafting menu
				Item tempItem = recipe.createItem();

				// Fall through the prefix for any craftables other than the Propagator
				if (tempItem.Name != ModValues.PropagatorInternalName)
				{
					return true;
				}

				// Behaviours as from base method
				if (___heldItem is null)
				{
					recipe.consumeIngredients(additional_materials: __instance._materialContainers);
					___heldItem = tempItem;
					if (playSound)
					{
						Game1.playSound("coin");
					}
				}
				if (Game1.player.craftingRecipes.TryGetValue(recipe.name, out int numberCrafted))
				{
					Game1.player.craftingRecipes[recipe.name] = numberCrafted + recipe.numberProducedPerCraft;
				}
				if (___heldItem is null || !Game1.player.couldInventoryAcceptThisItem(___heldItem))
				{
					return false;
				}

				// Add the machine to the user's inventory
				Propagator propagator = new(tileLocation: Game1.player.getTileLocation());
				Game1.player.addItemToInventoryBool(item: propagator);
				___heldItem = null;
				return false;
			}
			catch (Exception e)
			{
				Log.E($"{ModValues.AuthorName}.{ModValues.PackageName} failed in {nameof(CraftingPage_ClickCraftingRecipe_Prefix)}"
						+ $"\n{e}");
				return true;
			}
		}
	}
}
