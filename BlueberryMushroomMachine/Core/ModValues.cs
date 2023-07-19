using System.IO;

namespace BlueberryMushroomMachine
{
	public class ModValues
	{
		// Project
		public const string AuthorName
			= "blueberry";
		public const string PackageName
			= "BlueberryMushroomMachine";
		public static readonly string PropagatorInternalName
			= $"{PackageName}.Propagator";

		// Files
		public static readonly string MachinePath
			= Path.Combine("assets", "propagator.png");
		public static readonly string OverlayPath
			= Path.Combine("assets", "overlay.png");
		public static readonly string EventsPath
			= Path.Combine("assets", "events.json");

		// Console
		public static readonly string SpawnConsoleCommand
			= "bb.bmm.spawn";
		// Objects
		public const string ObjectDataFormat = "{0}/0/-300/Crafting -9/{1}/true/true/0";
		public const string RecipeDataFormat = "388 20 709 1/Home/{0}/true/null";

		public static int PropagatorIndex = 0;
		public static string ObjectData = null;
		public static string RecipeData = null;

		public const int OverlayMushroomFrames = 3;

		// Events
		public const int EventId = 46370001;
	}
}
