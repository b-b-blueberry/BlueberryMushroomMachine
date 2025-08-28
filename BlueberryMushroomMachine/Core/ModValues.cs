using System.IO;

namespace BlueberryMushroomMachine
{
	public class ModValues
	{
		// Assets
		public static readonly string GameContentDataPath
			= Path.Combine("Mods", "blueberry.MushroomPropagator", "Assets", "Data");
		public static readonly string GameContentMachineSpritePath
			= Path.Combine("Mods", "blueberry.MushroomPropagator", "Assets", "Machine");
		public static readonly string GameContentOverlaySpritePath
			= Path.Combine("Mods", "blueberry.MushroomPropagator", "Assets", "Overlay");
        public static readonly string GameContentTranslationsPath
            = Path.Combine("Mods", "blueberry.MushroomPropagator", "Assets", "Translations");

        // Console
        public static readonly string ConsoleCommandPrefix
			= "bb.mm.";
		public static readonly string GrowConsoleCommand
			= ModValues.ConsoleCommandPrefix + "grow";
		public static readonly string StatusConsoleCommand
			= ModValues.ConsoleCommandPrefix + "status";
	}
}
