using StardewModdingAPI;

namespace BlueberryMushroomMachine
{
	class Config
	{
		public int DaysToMature { get; set; } = 8;
		public bool WorksInCellar { get; set; } = true;
		public bool WorksInFarmCave { get; set; } = true;
		public bool WorksInBuildings { get; set; } = false;
		public bool WorksInFarmHouse { get; set; } = true;
		public bool WorksInGreenhouse { get; set; } = false;
		public bool WorksOutdoors { get; set; } = false;

		public bool Debugging { get; set; } = true;
		public SButton GivePropagator { get; set; } = SButton.J;
	}
}