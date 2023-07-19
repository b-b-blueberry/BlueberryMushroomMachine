using BlueberryMushroomMachine.Editors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

using System;
using System.Linq;
using Object = StardewValley.Object;

namespace BlueberryMushroomMachine
{
	public sealed class ModEntry : Mod
	{
		public enum Mushrooms
		{
			Morel = 257,
			Chantarelle = 281,
			Common = 404,
			Red = 420,
			Purple = 422
		}

		internal static ModEntry Instance;
		internal static Config Config;
		internal static ITranslationHelper I18n => ModEntry.Instance.Helper.Translation;

		public static Texture2D OverlayTexture { get => ModEntry._overlayTexture; }

		private static Texture2D _overlayTexture;
		private static IJsonAssetsAPI _jsonAssetsAPI;

		public override void Entry(IModHelper helper)
		{
			ModEntry.Instance = this;
			ModEntry.Config = helper.ReadConfig<Config>();

			this.RegisterConsoleCommands();

			this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
			this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
			this.Helper.Events.GameLoop.ReturnedToTitle += this.OnTitleScreen;

			// Load mushroom overlay texture for all filled machines
			ModEntry._overlayTexture = this.Helper.ModContent.Load<Texture2D>(ModValues.OverlayPath);

			// Harmony setup
			HarmonyPatches.Apply(uniqueID: this.ModManifest.UniqueID);

			// Load textures
			BigCraftablesTilesheetEditor.Initialize(helper: helper.ModContent);
		}

		private void LoadApis()
		{
			// SpaceCore setup
			ISpaceCoreAPI spacecoreApi = this.Helper.ModRegistry.GetApi<ISpaceCoreAPI>("spacechase0.SpaceCore");
			spacecoreApi.RegisterSerializerType(typeof(Propagator));

			ModEntry._jsonAssetsAPI = this.Helper.ModRegistry.GetApi<IJsonAssetsAPI>("spacechase0.JsonAssets");
			if (ModEntry._jsonAssetsAPI is null)
			{
				Log.D($"Json Assets not found, deshuffling will not happen",
					ModEntry.Config.DebugMode);
			}
			else
			{
				ModEntry._jsonAssetsAPI.IdsFixed += (object? sender, EventArgs e) => this.FixIds();
			}
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			try
			{
				if (ModEntry.Config.DebugMode)
				{
					Log.D("== CONFIG SUMMARY ==\n"
						  + "\nWorks in locations:"
						  + $"\n    {ModEntry.Config.WorksInCellar} {ModEntry.Config.WorksInFarmCave} {ModEntry.Config.WorksInBuildings}"
						  + $"\n    {ModEntry.Config.WorksInFarmHouse} {ModEntry.Config.WorksInGreenhouse} {ModEntry.Config.WorksOutdoors}\n"
						  + $"\nMushroom Cave:  {ModEntry.Config.DisabledForFruitCave}"
						  + $"\nRecipe Cheat:   {ModEntry.Config.RecipeAlwaysAvailable}"
						  + $"\nQuantity Cheat: {ModEntry.Config.MaximumQuantityLimitsDoubled}"
						  + $"\nDays To Mature: {ModEntry.Config.MaximumDaysToMature}"
						  + $"\nGrowth Pulse:   {ModEntry.Config.PulseWhenGrowing}"
						  + $"\nOnly Tools Pop: {ModEntry.Config.OnlyToolsCanRemoveRootMushrooms}"
						  + $"\nCustom Objects: {ModEntry.Config.OtherObjectsThatCanBeGrown.Aggregate("", (s, s1) => $"{s}\n    {s1}")}\n"
						  + $"\nLanguage:       {LocalizedContentManager.CurrentLanguageCode.ToString().ToUpper()}"
						  + $"\nDebugging:      {ModEntry.Config.DebugMode}",
						ModEntry.Config.DebugMode);
				}
			}
			catch (Exception ex)
			{
				Log.E($"Error in printing mod configuration.\n{ex}");
			}

			this.LoadApis();

			this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
		}

		private void FixIds()
		{
			try
			{
				Utility.ForAllLocations((GameLocation location) =>
				{
					foreach (Propagator propagator in location.Objects.Values.Where((Object o) => o is Propagator).Cast<Propagator>())
					{
						int id = _jsonAssetsAPI.GetObjectId(name: propagator.SourceMushroomName);
						if (id != -1 && id != propagator.SourceMushroomIndex)
						{
							Log.D($"Updating mushroom ID for mushroom propagator located at" +
								$" {location.NameOrUniqueName}::{propagator.TileLocation}:" +
								$" {propagator.SourceMushroomName} {propagator.SourceMushroomIndex} => {id}",
								ModEntry.Config.DebugMode);
							propagator.SourceMushroomIndex = id;
						}
					}
				});
			}
			catch (Exception e)
			{
				Log.E($"Error while deshuffling held mushrooms\n\n{e}");
			}
		}

		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			// Add Robin's pre-Demetrius-event dialogue
			if (Game1.player.daysUntilHouseUpgrade.Value == 2 && Game1.player.HouseUpgradeLevel == 2)
			{
				Game1.player.activeDialogueEvents.Add("event.4637.0000.0000", 7);
			}

			// Update player recipes
			if (ModEntry.Config.RecipeAlwaysAvailable
				&& !Game1.player.craftingRecipes.ContainsKey(ModValues.PropagatorInternalName))
			{
				// Add the Propagator crafting recipe if the cheat is enabled
				Game1.player.craftingRecipes.Add(ModValues.PropagatorInternalName, 0);
			}
			else if (!ModEntry.Config.RecipeAlwaysAvailable
				&& !Game1.player.eventsSeen.Contains(ModValues.EventId)
				&& Game1.player.craftingRecipes.ContainsKey(ModValues.PropagatorInternalName))
			{
				// Remove the Propagator crafting recipe if cheat is disabled and player has not seen the requisite event
				Game1.player.craftingRecipes.Remove(ModValues.PropagatorInternalName);
			}

			// Manually DayUpdate each Propagator
			Utility.ForAllLocations((GameLocation location) =>
			{
				foreach (Propagator propagator in location.Objects.Values.Where((Object o) => o is Propagator).Cast<Propagator>())
				{
					propagator.DayUpdate();
				}
			});
		}

		private void RegisterConsoleCommands()
		{
			this.Helper.ConsoleCommands.Add(
				name: ModValues.SpawnConsoleCommand,
				documentation: "Add one (or a given number of) mushroom propagator(s) to your inventory.",
				callback: (string cmd, string[] args) =>
				{
					// Debug spawning for Propagator: Can't be spawned in with CJB Item Spawner as it subclasses Object
					Propagator propagator = new(tileLocation: Game1.player.getTileLocation())
					{
						Stack = args.Length > 0 && int.TryParse(args[0], out int stack) ? stack : 1
					};
					Game1.player.addItemByMenuIfNecessary(item: propagator);
					Log.D($"{Game1.player.Name} spawned in a"
						  + $" [{ModValues.PropagatorIndex}] {ModValues.PropagatorInternalName} ({propagator.DisplayName}).");
				});
		}

		private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
		{
			_ = BigCraftablesInfoEditor.ApplyEdit(e)
				|| BigCraftablesTilesheetEditor.ApplyEdit(e)
				|| CraftingRecipesEditor.ApplyEdit(e)
				|| EventsEditor.ApplyEdit(e);
		}

		private void OnTitleScreen(object sender, ReturnedToTitleEventArgs e)
		{
			ModValues.PropagatorIndex = 0;
			ModValues.ObjectData = null;
			ModValues.RecipeData = null;
		}

		/// <summary>
		/// Determines the frame to be used for showing held mushroom growth.
		/// </summary>
		/// <param name="currentDays">Current days since last growth.</param>
		/// <param name="goalDays">Number of days when next growth happens.</param>
		/// <param name="quantity">Current count of mushrooms.</param>
		/// <param name="max">Maximum amount of mushrooms of this type.</param>
		/// <returns>Frame for mushroom growth progress.</returns>
		public static int GetOverlayGrowthFrame(float currentDays, int goalDays, int quantity, int max)
		{
			float maths = (quantity - 1 + ((float)currentDays / goalDays)) * goalDays / ((max - 1) * goalDays)
				* ModValues.OverlayMushroomFrames;
			return Math.Clamp(value: (int)maths, min: 0, max: ModValues.OverlayMushroomFrames);
		}

		/// <summary>
		/// Generates a clipping rectangle for the overlay appropriate
		/// to the current held mushroom, and its held quantity.
		/// Undefined mushrooms will use their default object rectangle.
		/// </summary>
		/// <returns></returns>
		public static Rectangle GetOverlaySourceRect(int index, int whichFrame)
		{
			return Enum.IsDefined(enumType: typeof(Mushrooms), value: index)
				? new Rectangle(
					x: whichFrame * 16,
					y: GetMushroomSourceRectIndex(index: index) * 32,
					width: 16,
					height: 32)
				: Game1.getSourceRectForStandardTileSheet(
					tileSheet: Game1.objectSpriteSheet,
					tilePosition: index,
					width: Game1.smallestTileSize,
					height: Game1.smallestTileSize);
		}

		public static bool IsValidMushroom(Object o)
		{
			// From the vanilla Utility.IsPerfectlyNormalObjectAtParentSheetIndex or whatever that method was again
			// Don't want to start growing wallpaper
			Type type = o.GetType();
			if (o is null || (type != typeof(Object) && type != typeof(ColoredObject)))
			{
				return false;
			}

			return Enum.IsDefined(enumType: typeof(Mushrooms), value: o.ParentSheetIndex)
				|| ModEntry.Config.OtherObjectsThatCanBeGrown.Contains(o.Name)
				|| ((o.Category == Object.VegetableCategory || o.Category == Object.GreensCategory)
					&& (o.Name.Contains("mushroom", StringComparison.InvariantCultureIgnoreCase)
						|| o.Name.Contains("fungus", StringComparison.InvariantCultureIgnoreCase)));
		}

		public static int GetMushroomSourceRectIndex(int index)
		{
			return index switch
			{
				(int)Mushrooms.Morel => 0,
				(int)Mushrooms.Chantarelle => 1,
				(int)Mushrooms.Common => 2,
				(int)Mushrooms.Red => 3,
				(int)Mushrooms.Purple => 4,
				_ => -1
			};
		}

		public static void GetMushroomGrowthRate(Object o, out float rate)
		{
			rate = o.ParentSheetIndex switch
			{
				(int)Mushrooms.Morel => 0.5f,
				(int)Mushrooms.Chantarelle => 0.5f,
				(int)Mushrooms.Common => 1.0f,
				(int)Mushrooms.Red => 0.5f,
				(int)Mushrooms.Purple => 0.25f,
				_ => o.Price < 50 ? 1.0f : o.Price < 100 ? 0.75f : o.Price < 200 ? 0.5f : 0.25f
			};
		}

		public static void GetMushroomMaximumQuantity(Object o, out int quantity)
		{
			quantity = o.ParentSheetIndex switch
			{
				(int)Mushrooms.Morel => 4,
				(int)Mushrooms.Chantarelle => 4,
				(int)Mushrooms.Common => 6,
				(int)Mushrooms.Red => 3,
				(int)Mushrooms.Purple => 2,
				_ => o.Price < 50 ? 5 : o.Price < 100 ? 4 : o.Price < 200 ? 3 : 2
			};
			quantity *= ModEntry.Config.MaximumQuantityLimitsDoubled ? 2 : 1;
		}
	}
}
