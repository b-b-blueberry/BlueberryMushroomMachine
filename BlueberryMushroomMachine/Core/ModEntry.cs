using BlueberryMushroomMachine.Editors;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

		public static ModEntry Instance { get; private set; }
		public static Config Config { get; private set; }
		public static ITranslationHelper I18n => ModEntry.Instance.Helper.Translation;
		public static Texture2D MachineTexture { get; private set; }
		public static Texture2D OverlayTexture { get; private set; }

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
			ModEntry.MachineTexture = this.Helper.ModContent.Load<Texture2D>(ModValues.MachinePath);
			ModEntry.OverlayTexture = this.Helper.ModContent.Load<Texture2D>(ModValues.OverlayPath);

			// Harmony setup
			HarmonyPatches.Apply(uniqueID: this.ModManifest.UniqueID);
		}

		private void LoadApis()
		{
			// SpaceCore setup
			ISpaceCoreAPI spacecoreApi = this.Helper.ModRegistry
				.GetApi<ISpaceCoreAPI>
				("spacechase0.SpaceCore");
			spacecoreApi.RegisterSerializerType(typeof(Propagator));

			// JA setup
			ModEntry._jsonAssetsAPI = this.Helper.ModRegistry
				.GetApi<IJsonAssetsAPI>
				("spacechase0.JsonAssets");
			if (ModEntry._jsonAssetsAPI is not null)
			{
				ModEntry._jsonAssetsAPI.IdsFixed += (object sender, EventArgs e) => this.FixIds();
			}
			else
			{
				Log.D($"Json Assets not found, deshuffling will not happen",
					ModEntry.Config.DebugMode);
			}

			// GMCM setup
			IGenericModConfigMenuApi gmcm = this.Helper.ModRegistry
				.GetApi<IGenericModConfigMenuApi>
				("spacechase0.GenericModConfigMenu");
			if (gmcm is not null)
			{
				// Register config
				gmcm.Register(
					mod: this.ModManifest,
					reset: () => ModEntry.Config = new(),
					save: () => this.Helper.WriteConfig(ModEntry.Config));

				// Register config options
				var entries = new (string i18n, string propertyName, Type type)[] {
					("working_rules", null, null),

					("disabled_for_fruit_cave", nameof(ModEntry.Config.DisabledForFruitCave), typeof(bool)),
					("recipe_always_available", nameof(ModEntry.Config.RecipeAlwaysAvailable), typeof(bool)),
					("maximum_days_to_mature", nameof(ModEntry.Config.MaximumDaysToMature), typeof(int)),
					("maximum_quantity_limits_doubled", nameof(ModEntry.Config.MaximumQuantityLimitsDoubled), typeof(bool)),
					("only_tools_remove_root_mushrooms", nameof(ModEntry.Config.OnlyToolsCanRemoveRootMushrooms), typeof(bool)),
					("pulse_when_growing", nameof(ModEntry.Config.PulseWhenGrowing), typeof(bool)),

					("working_areas", null, null),

					("works_in_cellar", nameof(ModEntry.Config.WorksInCellar), typeof(bool)),
					("works_in_farm_cave", nameof(ModEntry.Config.WorksInFarmCave), typeof(bool)),
					("works_in_buildings", nameof(ModEntry.Config.WorksInBuildings), typeof(bool)),
					("works_in_farmhouse", nameof(ModEntry.Config.WorksInFarmHouse), typeof(bool)),
					("works_in_greenhouse", nameof(ModEntry.Config.WorksInGreenhouse), typeof(bool)),
					("works_outdoors", nameof(ModEntry.Config.WorksOutdoors), typeof(bool))
				};
				foreach ((string i18n, string propertyName, Type type) in entries)
				{
					BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
					if (propertyName is null)
					{
						Translation title = ModEntry.I18n.Get($"config.title.{i18n}");
						gmcm.AddSectionTitle(
							this.ModManifest,
							text: () => title.HasValue() ? title : i18n);
					}
					else
					{
						PropertyInfo property = typeof(Config).GetProperty(propertyName, flags);
						Translation name = I18n.Get($"config.name.{i18n}");
						Translation description = I18n.Get($"config.description.{i18n}");
						if (type == typeof(bool))
						{
							gmcm.AddBoolOption(
								mod: this.ModManifest,
								getValue: () => (bool)property.GetValue(ModEntry.Config),
								setValue: (bool value) =>
								{
									Log.D($"Config edit: {property.Name} - {property.GetValue(ModEntry.Config)} => {value}",
										ModEntry.Config.DebugMode);
									property.SetValue(ModEntry.Config, value);
								},
								name: () => name.HasValue() ? name : propertyName,
								tooltip: () => description.HasValue() ? description : null);
						}
						else if (type == typeof(int))
						{
							gmcm.AddNumberOption(
								mod: this.ModManifest,
								getValue: () => (int)property.GetValue(ModEntry.Config),
								setValue: (int value) =>
								{
									Log.D($"Config edit: {property.Name} - {property.GetValue(ModEntry.Config)} => {value}",
										ModEntry.Config.DebugMode);
									property.SetValue(ModEntry.Config, value);
								},
								name: () => name.HasValue() ? name : propertyName,
								tooltip: () => description.HasValue() ? description : null,
								min: 1,
								max: 28,
								formatValue: (int value) => $"{value:0}");
						}
						else
						{
							Log.D($"Unsupported config entry type {type}",
								ModEntry.Config.DebugMode);
						}
					}
				}
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
					foreach (Propagator propagator in ModEntry.GetMachinesIn(location))
					{
						if (propagator.SourceMushroomName is not null
							&& ModEntry._jsonAssetsAPI.GetObjectId(name: propagator.SourceMushroomName) is int id
							&& id > 0 && id != propagator.SourceMushroomIndex)
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
		}

		private void RegisterConsoleCommands()
		{
			// Commands usable by all players

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

			// Commands usable when debugging

			if (ModEntry.Config.DebugMode)
			{
				this.Helper.ConsoleCommands.Add(
					name: ModValues.GrowConsoleCommand,
					documentation: "DEBUG: Grows mushrooms held by propagators in the current location.",
					callback: (string cmd, string[] args) =>
					{
						foreach (Propagator propagator in ModEntry.GetMachinesIn(Game1.currentLocation))
						{
							Log.D($"Grow (item: [{propagator.SourceMushroomIndex}]" +
								$" {propagator.SourceMushroomName}" +
								$" Q{propagator.SourceMushroomQuality}" +
								$" at {Game1.currentLocation.Name} {propagator.TileLocation}",
								ModEntry.Config.DebugMode);

							propagator.GrowHeldMushroom();
						}
					});

				this.Helper.ConsoleCommands.Add(
					name: ModValues.StatusConsoleCommand,
					documentation: "DEBUG: Prints state of propagators in the current location.",
					callback: (string cmd, string[] args) =>
					{
						// TODO: DEBUG: 
						foreach (Propagator propagator in ModEntry.GetMachinesIn(Game1.currentLocation))
						{
							Log.D($"Status (item: [{propagator.SourceMushroomIndex}]" +
								$" {propagator.SourceMushroomName ?? "N/A"}x{propagator.heldObject?.Value?.Stack ?? 0}" +
								$" Q{propagator.SourceMushroomQuality}" +
								$" ({propagator.DaysToMature}/{propagator.DefaultDaysToMature} days +{propagator.RateToMature})" +
								$" at {Game1.currentLocation.Name} {propagator.TileLocation}",
								ModEntry.Config.DebugMode);
						}
					});

				this.Helper.ConsoleCommands.Add(
					name: ModValues.FixIdsConsoleCommand,
					documentation: "DEBUG: Manually fix IDs of objects held by mushroom propagators.",
					callback: (string cmd, string[] args) => this.FixIds());
			}
		}

		private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
		{
			_ = BigCraftablesInfoEditor.ApplyEdit(e)
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
		/// Fetches all propagator machines in a given location.
		/// </summary>
		/// <param name="location">Location to search.</param>
		/// <returns>All objects of type propagator.</returns>
		public static IEnumerable<Propagator> GetMachinesIn(GameLocation location)
		{
			return location.Objects.Values.Where((Object o) => o is Propagator).Cast<Propagator>();
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
