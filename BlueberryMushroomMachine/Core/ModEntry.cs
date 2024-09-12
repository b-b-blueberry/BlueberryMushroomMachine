using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueberryMushroomMachine.Editors;
using BlueberryMushroomMachine.Interface;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace BlueberryMushroomMachine
{
	public sealed class ModEntry : Mod
	{
		public class ModData
		{
			// Objects
			public int OverlayMushroomFrames;
			public string RecipeFormat;

			// Mushrooms
			public Dictionary<string, MushroomData> Mushrooms;
			public Dictionary<int, float> MushroomGrowthRatePerPrice;
			public Dictionary<int, int> MushroomMaximumQuantityPerPrice;

			// Events
			public int EventId;
		}

		public class MushroomData
		{
			public int SourceRectIndex;
			public float GrowthRate;
			public int MaximumQuantity;
		}

		public static ModEntry Instance { get; private set; }
		public static Config Config { get; private set; }
		public static ITranslationHelper I18n => ModEntry.Instance.Helper.Translation;
		public static Texture2D MachineTexture { get; private set; }
		public static Texture2D OverlayTexture { get; private set; }
		public static ModData Data { get; private set; }

		public override void Entry(IModHelper helper)
		{
			ModEntry.Instance = this;
			ModEntry.Config = helper.ReadConfig<Config>();

			this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
		}

		private bool TryLoadApis()
		{
			// SpaceCore setup
			try
			{
				ISpaceCoreAPI spacecoreApi = this.Helper.ModRegistry
					.GetApi<ISpaceCoreAPI>
					("spacechase0.SpaceCore");
				spacecoreApi.RegisterSerializerType(typeof(Propagator));
				ItemRegistry.AddTypeDefinition(new PropagatorItemDataDefinition());
			}
			catch (Exception e)
			{
				Log.E($"Failed to register Propagator objects with SpaceCore.{Environment.NewLine}{e}");
				return false;
			}

			// Generic Mod Config Menu setup
			try
			{
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
							void onChanged(PropertyInfo property, object value)
							{
								object current = property.GetValue(ModEntry.Config);
								if (current != value)
								{
									Log.D($"Config edit: {property.Name} - {current} => {value}",
										ModEntry.Config.DebugMode);
									property.SetValue(ModEntry.Config, value);
								}
							}
							PropertyInfo property = typeof(Config).GetProperty(propertyName, flags);
							Translation name = I18n.Get($"config.name.{i18n}");
							Translation description = I18n.Get($"config.description.{i18n}");
							if (type == typeof(bool))
							{
								gmcm.AddBoolOption(
									mod: this.ModManifest,
									getValue: () => (bool)property.GetValue(ModEntry.Config),
									setValue: (bool value) => onChanged(property: property, value: value),
									name: () => name.HasValue() ? name : propertyName,
									tooltip: () => description.HasValue() ? description : null);
							}
							else if (type == typeof(int))
							{
								gmcm.AddNumberOption(
									mod: this.ModManifest,
									getValue: () => (int)property.GetValue(ModEntry.Config),
									setValue: (int value) => onChanged(property: property, value: value),
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
			catch (Exception e)
			{
				Log.E($"Failed to add Generic Mod Config Menu behaviours.{Environment.NewLine}{e}");
			}

			return true;
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Display mod config
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
				Log.E($"Failed to display mod config.{Environment.NewLine}{ex}");
			}

			// Load behaviours for required and optional mods
			if (!this.TryLoadApis())
			{
				Log.E("Failed to load required mods. Mod will not be loaded.");
				return;
			}

			// Add SMAPI console commands
			this.RegisterConsoleCommands();

			// Event handlers
			this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
			this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

			// Load mushroom overlay texture for all filled machines
			ModEntry.Data = Game1.content.Load<ModData>(ModValues.GameContentDataPath);
			ModEntry.MachineTexture = Game1.content.Load<Texture2D>(ModValues.GameContentMachinePath);
			ModEntry.OverlayTexture = Game1.content.Load<Texture2D>(ModValues.GameContentOverlayPath);
		}

		private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
		{
			// Handle mod assets
			if (e.NameWithoutLocale.IsEquivalentTo(ModValues.GameContentDataPath))
				e.LoadFromModFile<ModData>(relativePath: ModValues.DataPath, priority: AssetLoadPriority.Exclusive);
			else if (e.NameWithoutLocale.IsEquivalentTo(ModValues.GameContentMachinePath))
				e.LoadFromModFile<Texture2D>(relativePath: ModValues.MachinePath, priority: AssetLoadPriority.Exclusive);
			else if (e.NameWithoutLocale.IsEquivalentTo(ModValues.GameContentOverlayPath))
				e.LoadFromModFile<Texture2D>(relativePath: ModValues.OverlayPath, priority: AssetLoadPriority.Exclusive);

			// Handle asset requests
			_ = CraftingRecipesEditor.ApplyEdit(e) || EventsEditor.ApplyEdit(e);
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
				&& !Game1.player.craftingRecipes.ContainsKey(ModValues.PropagatorItemId))
			{
				// Add the Propagator crafting recipe if the cheat is enabled
				Game1.player.craftingRecipes.Add(ModValues.PropagatorItemId, 0);
			}
			else if (!ModEntry.Config.RecipeAlwaysAvailable
				&& !Game1.player.eventsSeen.Contains(ModEntry.Data.EventId.ToString())
				&& Game1.player.craftingRecipes.ContainsKey(ModValues.PropagatorItemId))
			{
				// Remove the Propagator crafting recipe if cheat is disabled and player has not seen the requisite event
				Game1.player.craftingRecipes.Remove(ModValues.PropagatorItemId);
			}
		}

		private void RegisterConsoleCommands()
		{
			// Commands usable when debugging

			if (ModEntry.Config.DebugMode)
			{
				this.Helper.ConsoleCommands.Add(
					name: ModValues.GrowConsoleCommand,
					documentation: "DEBUG: Grows mushrooms held by propagators in the current location.",
					callback: (string cmd, string[] args) =>
					{
						foreach (Propagator propagator in Utils.GetMachinesIn(Game1.currentLocation))
						{
							Log.D($"Grow (item: [{propagator.SourceMushroomItemId}x{propagator.heldObject?.Value?.Stack ?? 0}]" +
								$" Q{propagator.SourceMushroomQuality}" +
								$" ({propagator.Growth}/{Propagator.DefaultDaysToGrow} days +{propagator.GrowthRatePerDay})" +
								$" at {Game1.currentLocation.Name} {propagator.TileLocation}",
								ModEntry.Config.DebugMode);

							propagator.GrowHeldObject();
						}
					});

				this.Helper.ConsoleCommands.Add(
					name: ModValues.StatusConsoleCommand,
					documentation: "DEBUG: Prints state of propagators in the current location.",
					callback: (string cmd, string[] args) =>
					{
						// TODO: DEBUG: 
						foreach (Propagator propagator in Utils.GetMachinesIn(Game1.currentLocation))
						{
							Log.D($"Status (item: [{propagator.SourceMushroomItemId}x{propagator.heldObject?.Value?.Stack ?? 0}]" +
								$" Q{propagator.SourceMushroomQuality}" +
								$" ({propagator.Growth}/{Propagator.DefaultDaysToGrow} days +{propagator.GrowthRatePerDay})" +
								$" at {Game1.currentLocation.Name} {propagator.TileLocation}",
								ModEntry.Config.DebugMode);
						}
					});
			}
		}
	}
}
