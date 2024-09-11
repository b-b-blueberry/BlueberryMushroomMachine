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

		// Assets
		public static readonly string GameContentDataPath
			= Path.Combine("Mods", $"{ModValues.AuthorName}.{ModValues.PackageName}.Assets", "Data");
		public static readonly string GameContentMachinePath
			= Path.Combine("Mods", $"{ModValues.AuthorName}.{ModValues.PackageName}.Assets", "Machine");
		public static readonly string GameContentOverlayPath
			= Path.Combine("Mods", $"{ModValues.AuthorName}.{ModValues.PackageName}.Assets", "Overlay");

		// Files
		public static readonly string DataPath
			= Path.Combine("assets", "data.json");
		public static readonly string MachinePath
			= Path.Combine("assets", "propagator.png");
		public static readonly string OverlayPath
			= Path.Combine("assets", "overlay.png");
		public static readonly string EventsPath
			= Path.Combine("assets", "events.json");

		// Console
		public static readonly string ConsoleCommandPrefix
			= "bb.mm.";
		public static readonly string GiveConsoleCommand
			= ModValues.ConsoleCommandPrefix + "give";
		public static readonly string GrowConsoleCommand
			= ModValues.ConsoleCommandPrefix + "grow";
		public static readonly string StatusConsoleCommand
			= ModValues.ConsoleCommandPrefix + "status";
		public static readonly string FixIdsConsoleCommand
			= ModValues.ConsoleCommandPrefix + "fix_ids";

		// Objects
		public const int OverlayMushroomFrames = 4;
		public const string RecipeDataFormat = "388 20 709 1/Home/{0}/true/null";

		public static string RecipeData { get; set; } = null;

		// Events
		public const int EventId = 46370001;
	}
}
