﻿using System.Collections.Generic;
using StardewModdingAPI;

namespace BlueberryMushroomMachine
{
	public class Config
	{
		public bool DisabledForFruitCave { get; set; } = true;
		public bool RecipeAlwaysAvailable { get; set; } = false;
		public int MaximumDaysToMature { get; set; } = 4;
		public bool MaximumQuantityLimitsDoubled { get; set; } = false;
		public bool OnlyToolsCanRemoveRootMushrooms { get; set; } = false;
		public bool PulseWhenGrowing { get; set; } = true;
		public List<string> OtherObjectsThatCanBeGrown { get; set; } = new()
		{
			"Example Mushroom Name",
			"Example Item Not Called Fungus",
		};

		public bool WorksInCellar { get; set; } = true;
		public bool WorksInFarmCave { get; set; } = true;
		public bool WorksInBuildings { get; set; } = false;
		public bool WorksInFarmHouse { get; set; } = false;
		public bool WorksInGreenhouse { get; set; } = false;
		public bool WorksOutdoors { get; set; } = false;

		public bool DebugMode { get; set; } = false;
	}
}