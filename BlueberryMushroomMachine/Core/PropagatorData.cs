using System.Collections.Generic;
using System.IO;

namespace BlueberryMushroomMachine
{
	class PropagatorData
	{
		internal const string mPropagatorName =
			"Propagator";

		internal static readonly string mMachinePath =
			Path.Combine("assets", "propagator.png");
		internal static readonly string mOverlayPath =
			Path.Combine("assets", "overlay.png");
		internal static readonly string mEventsPath =
			Path.Combine("assets", "events.json");

		internal static int mPropagatorIndex;
		internal static string mObjectData =
			PropagatorMod.i18n.Get("machine.name")
			+ "/0/-300/Crafting -9/" +
			PropagatorMod.i18n.Get("machine.desc")
			+ "/true/true/0";
		internal static string mCraftingRecipeData =
			"388 20 709 1/Home/{0}/true/null";

		internal const string mEvent0001 =
			"46370001/t 600 1200/H";

		internal static Dictionary<int, int> mMushroomSourceRects =
			new Dictionary<int, int> {
				{257, 0},		// Morel
				{281, 1},		// Chantarelle
				{404, 2},		// Common Mushroom
				{420, 3},		// Red Mushroom
				{422, 4},		// Purple Mushroom
			};

		internal static Dictionary<int, float> mMushroomAgingRates =
			new Dictionary<int, float> {
				{257, 2f},		// Morel
				{281, 2f},		// Chantarelle
				{404, 4f},		// Common Mushroom
				{420, 2f},		// Red Mushroom
				{422, 1f},		// Purple Mushroom
			};
	}
}
