using System.Collections.Generic;
using System.IO;

namespace BlueberryMushroomMachine
{
	class PropagatorData
	{
		internal const string PropagatorName =
			"Propagator";

		internal static readonly string MachinePath =
			Path.Combine("assets", "propagator.png");
		internal static readonly string OverlayPath =
			Path.Combine("assets", "overlay.png");
		internal static readonly string EventsPath =
			Path.Combine("assets", "events.json");

		internal static int PropagatorIndex;
		internal static string ObjectData =
			PropagatorMod.i18n.Get("machine.name")
			+ "/0/-300/Crafting -9/" +
			PropagatorMod.i18n.Get("machine.desc")
			+ "/true/true/0";
		internal static string CraftingRecipeData =
			"388 20 709 1/Home/{0}/true/null";
		
		internal static Dictionary<int, int> MushroomSourceRects =
			new Dictionary<int, int> {
				{257, 0},		// Morel
				{281, 1},		// Chantarelle
				{404, 2},		// Common Mushroom
				{420, 3},		// Red Mushroom
				{422, 4},		// Purple Mushroom
			};

		internal static Dictionary<int, int> MushroomGrowingRates =
			new Dictionary<int, int> {
				{257, 2},		// Morel
				{281, 2},		// Chantarelle
				{404, 4},		// Common Mushroom
				{420, 2},		// Red Mushroom
				{422, 1},		// Purple Mushroom
			};

		internal static Dictionary<int, int> MushroomQuantityLimits =
			new Dictionary<int, int> {
				{257, 4},		// Morel
				{281, 4},		// Chantarelle
				{404, 6},		// Common Mushroom
				{420, 3},		// Red Mushroom
				{422, 2},		// Purple Mushroom
			};
	}
}
