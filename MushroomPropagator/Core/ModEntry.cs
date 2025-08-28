using MushroomPropagator.Interface;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MushroomPropagator
{
	public sealed class ModEntry : Mod
	{
		public class ModData
		{
            // Propagator
			/// <summary>Unique ID of machine item.</summary>
            public string PropagatorId;
			/// <summary>Qualifier used for machine type in item registry.</summary>
			public string PropagatorTypeDefinitionId;
            /// <summary>Dimensions of machine sprite in source texture.</summary>
            public Point MachineSpriteSize;
            /// <summary>Dimensions of overlay sprite in source texture.</summary>
            public Point OverlaySpriteSize;
			/// <summary>Number of mushroom growth stage sprites in overlay texture.</summary>
            public int OverlaySpriteFrames;

            // Mushrooms
            /// <summary>Map of Propagator input item IDs to their respective data.</summary>
            public Dictionary<string, MushroomData> Mushrooms;
            /// <summary>Map of Propagator input item purchase price thresholds to growth rate. Ordered from highest to lowest value.</summary>
			public Dictionary<int, float> MushroomGrowthRatePerPrice;
            /// <summary>Map of Propagator input item purchase price thresholds to maximum quantity held. Ordered from highest to lowest value.</summary>
			public Dictionary<int, int> MushroomMaximumQuantityPerPrice;

            // Events
            /// <summary>Unique ID of Propagator crafting recipe event.</summary>
            public string EventId;

			// GSQs
            /// <summary>Unique ID of GSQ for Propagator usable locations.</summary>
            public string LocationAllowedGameStateQueryId;
            /// <summary>Unique ID of GSQ for FarmCave type restrictions.</summary>
			public string FarmCaveAllowedGameStateQueryId;
        }

		public class MushroomData
		{
			public int OverlaySpriteIndex = -1;
			public float GrowthRate;
			public int MaximumQuantity;
		}

		public static ModEntry Instance { get; private set; }
		public static Config Config { get; private set; }
		public static Texture2D MachineTexture { get; private set; }
		public static Texture2D OverlayTexture { get; private set; }
		public static ModData Data { get; private set; }

		private static Dictionary<string, string> Translations { get; set; }

		public override void Entry(IModHelper helper)
		{
			ModEntry.Instance = this;
			ModEntry.Config = helper.ReadConfig<Config>();

			this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
		}

        public static string GetTranslationOrKey(string key)
        {
            return ModEntry.Translations.GetValueOrDefault(key) ?? key;
        }

        public static string GetTranslationOrNull(string key)
        {
            return ModEntry.Translations.GetValueOrDefault(key);
        }

        private bool CheckRequirements()
        {
            // Check for Content Patcher component
            const string cp = "blueberry.MushroomPropagator.CP";
            if (!this.Helper.ModRegistry.IsLoaded(cp))
            {
                Log.E($"Couldn't find Content Patcher component '{cp}'. Did you install ALL folders from this mod?");
                return false;
            }

			return true;
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

						("disabled_for_fruit_cave", nameof(ModEntry.Config.MushroomCaveOnly), typeof(bool)),
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
							string title = ModEntry.GetTranslationOrKey($"config.title.{i18n}");
							gmcm.AddSectionTitle(
								this.ModManifest,
								text: () => title);
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
							string name = ModEntry.GetTranslationOrNull($"config.name.{i18n}");
                            string description = ModEntry.GetTranslationOrNull($"config.description.{i18n}");
							if (type == typeof(bool))
							{
								gmcm.AddBoolOption(
									mod: this.ModManifest,
									getValue: () => (bool)property.GetValue(ModEntry.Config),
									setValue: (bool value) => onChanged(property: property, value: value),
									name: () => name ?? propertyName,
									tooltip: () => description);
							}
							else if (type == typeof(int))
							{
								gmcm.AddNumberOption(
									mod: this.ModManifest,
									getValue: () => (int)property.GetValue(ModEntry.Config),
									setValue: (int value) => onChanged(property: property, value: value),
									name: () => name ?? propertyName,
									tooltip: () => description,
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
			try
			{
				if (ModEntry.Config.DebugMode)
				{
					Log.D("== CONFIG SUMMARY ==\n"
						  + "\nWorks in locations:"
						  + $"\n    {ModEntry.Config.WorksInCellar} {ModEntry.Config.WorksInFarmCave} {ModEntry.Config.WorksInBuildings}"
						  + $"\n    {ModEntry.Config.WorksInFarmHouse} {ModEntry.Config.WorksInGreenhouse} {ModEntry.Config.WorksOutdoors}\n"
						  + $"\nMushroom Cave:  {ModEntry.Config.MushroomCaveOnly}"
						  + $"\nRecipe Cheat:   {ModEntry.Config.RecipeAlwaysAvailable}"
						  + $"\nQuantity Cheat: {ModEntry.Config.MaximumQuantityLimitsDoubled}"
						  + $"\nDays To Mature: {ModEntry.Config.MaximumDaysToMature}"
						  + $"\nGrowth Pulse:   {ModEntry.Config.PulseWhenGrowing}"
						  + $"\nOnly Tools Pop: {ModEntry.Config.OnlyToolsCanRemoveRootMushrooms}"
						  + $"\nLanguage:       {LocalizedContentManager.CurrentLanguageCode.ToString().ToUpper()}"
						  + $"\nDebugging:      {ModEntry.Config.DebugMode}",
						ModEntry.Config.DebugMode);
				}
			}
			catch (Exception ex)
			{
				Log.E($"Failed to display mod config.{Environment.NewLine}{ex}");
			}

			// Skip loading this mod entirely if required content isn't found
			if (!this.CheckRequirements())
			{
				Log.E("Couldn't find required mods. Mod will not be loaded.");
				return;
			}

			// Defer loading until after SMAPI has registered all mods and CP has loaded all content packs
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.InitLate;
		}

        private void InitLate(object sender, OneSecondUpdateTickedEventArgs e)
        {
			this.Helper.Events.GameLoop.OneSecondUpdateTicked -= this.InitLate;

            ModEntry.Data = Game1.content.Load<ModData>(ModValues.GameContentDataPath);
			ModEntry.MachineTexture = Game1.content.Load<Texture2D>(ModValues.GameContentMachineSpritePath);
			ModEntry.OverlayTexture = Game1.content.Load<Texture2D>(ModValues.GameContentOverlaySpritePath);
			ModEntry.Translations = Game1.content.Load<Dictionary<string, string>>(ModValues.GameContentTranslationsPath);

            if (!this.TryLoadApis())
            {
                Log.E("Failed to register changes with other mods. Mod will not be loaded.");
                return;
            }

            this.RegisterConsoleCommands();

            this.RegisterGameFeatures();

            this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;

            LocalizedContentManager.OnLanguageChange += this.OnLanguageChanged;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			this.Helper.GameContent.InvalidateCache(ModValues.GameContentDataPath);

			// Update player recipes
			if (ModEntry.Config.RecipeAlwaysAvailable
				&& !Game1.player.craftingRecipes.ContainsKey(ModEntry.Data.PropagatorId))
			{
				// Add the Propagator crafting recipe if the cheat is enabled
				Game1.player.craftingRecipes.Add(ModEntry.Data.PropagatorId, 0);
			}
			else if (!ModEntry.Config.RecipeAlwaysAvailable
				&& !Game1.player.eventsSeen.Contains(ModEntry.Data.EventId)
				&& Game1.player.craftingRecipes.ContainsKey(ModEntry.Data.PropagatorId))
			{
				// Remove the Propagator crafting recipe if cheat is disabled and player has not seen the requisite event
				Game1.player.craftingRecipes.Remove(ModEntry.Data.PropagatorId);
			}
        }

        private void OnLanguageChanged(LocalizedContentManager.LanguageCode code)
        {
            this.Helper.GameContent.InvalidateCache(ModValues.GameContentTranslationsPath);
        }

        private void RegisterGameFeatures()
        {
            ItemRegistry.AddTypeDefinition(new PropagatorItemDataDefinition());

            GameStateQuery.Register(ModEntry.Data.LocationAllowedGameStateQueryId, (query, context) => Utils.IsValidMachineLocation(context.Location));
            GameStateQuery.Register(ModEntry.Data.FarmCaveAllowedGameStateQueryId, (query, context) => Game1.MasterPlayer.caveChoice.Value is Farmer.caveMushrooms || !ModEntry.Config.MushroomCaveOnly);
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
