using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using static BlueberryMushroomMachine.ModEntry;
using Object = StardewValley.Object;

namespace BlueberryMushroomMachine
{
	internal static class Utils
	{
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
		/// <param name="currentStack">Current count of mushrooms.</param>
		/// <param name="goalStack">Maximum amount of mushrooms of this type.</param>
		/// <returns>Frame for mushroom growth progress.</returns>
		public static int GetOverlayGrowthFrame(float currentDays, int goalDays, int currentStack, int goalStack)
		{
			int frames = ModEntry.Data.OverlayMushroomFrames - 1;
			float maths = currentStack == goalStack ? frames : frames
				* (currentStack - 1 + (currentDays / goalDays))
				* goalDays / (goalStack * goalDays);
			return (int)Math.Clamp(value: maths, min: 0, max: frames);
		}

		/// <summary>
		/// Generates a clipping rectangle for the mushroom overlay,
		/// appropriate to the current held mushroom, and its held quantity.
		/// Undefined mushrooms will use their default object rectangle.
		/// </summary>
		/// <returns>Source rectangle for mushroom overlay from overlay texture.</returns>
		public static Rectangle GetOverlaySourceRect(GameLocation location, string itemId, int whichFrame)
		{
			int frames = ModEntry.Data.OverlayMushroomFrames;
			bool isBasicMushroom = ModEntry.Data.Mushrooms.ContainsKey(itemId);
			Point size = isBasicMushroom
				? Propagator.OverlaySize
				: new Point(x: Game1.smallestTileSize, y: Game1.smallestTileSize);
			return isBasicMushroom
				? new Rectangle(
					x: (Utils.IsDarkLocation(location) ? size.X * frames : 0) + whichFrame * size.X,
					y: GetMushroomSourceRectIndex(itemId: itemId) * size.Y,
					width: size.X,
					height: size.Y)
				: ItemRegistry.GetDataOrErrorItem(itemId).GetSourceRect();
		}

		/// <summary>
		/// Generates a clipping rectangle for the propagator machine,
		/// appropriate to the current location.
		/// </summary>
		/// <returns>Source rectangle for propagator from machine texture.</returns>
		public static Rectangle GetMachineSourceRect(GameLocation location, Vector2 tile)
		{
			// random magical maths to pick a value
			// based on a predictable but scattered pattern for the current tile
			return Game1.getSourceRectForStandardTileSheet(
					tileSheet: ModEntry.MachineTexture,
					tilePosition: (Utils.IsDarkLocation(location) ? 2 : 0) + ((tile.X + tile.Y) % 3 == 1 ? 1 : 0),
					width: Propagator.MachineSize.X,
					height: Propagator.MachineSize.Y);
		}

		/// <summary>
		/// Assigns an arbitrary flip value to some given tile coordinates.
		/// </summary>
		/// <returns>Whether object at the current tile is flipped.</returns>
		public static bool GetMachineIsFlipped(Vector2 tile)
		{
			// random magical maths to pick a value
			// based on a predictable but scattered pattern for the current tile
			// distinct from arbitrary alternate sprite value
			return (tile.X + tile.Y) % 4 == 1;
		}

		/// <summary>
		/// Check for dark locations, used to determine the visual style of the propagator.
		/// </summary>
		/// <param name="location">Location to check.</param>
		/// <returns>Whether the given location is 'dark', or otherwise cave-flavoured.</returns>
		public static bool IsDarkLocation(GameLocation location)
		{
			return location is FarmCave or IslandFarmCave;
		}

		public static bool IsValidMushroom(Object o)
		{
			// From the vanilla Utility.IsPerfectlyNormalObjectAtParentSheetIndex or whatever that method was again
			// Don't want to start growing wallpaper
			if (o is null || !o.HasTypeObject())
				return false;

			return ModEntry.Data.Mushrooms.ContainsKey(o.ItemId)
				|| ModEntry.Config.OtherObjectsThatCanBeGrown.Contains(o.ItemId)
				|| ((o.Category == Object.VegetableCategory || o.Category == Object.GreensCategory)
					&& (o.ItemId.Contains("mushroom", StringComparison.InvariantCultureIgnoreCase)
						|| o.ItemId.Contains("fungus", StringComparison.InvariantCultureIgnoreCase)));
		}

		public static int GetMushroomSourceRectIndex(string itemId)
		{
			return ModEntry.Data.Mushrooms.TryGetValue(itemId, out MushroomData entry)
				? entry.SourceRectIndex
				: -1;
		}

		public static void GetMushroomGrowthRate(Object o, out float rate)
		{
			rate = ModEntry.Data.Mushrooms.TryGetValue(o.ItemId, out MushroomData entry)
				? entry.GrowthRate
				: ModEntry.Data.MushroomGrowthRatePerPrice.FirstOrDefault(pair => pair.Key < o.Price).Value;
		}

		public static void GetMushroomMaximumQuantity(Object o, out int quantity)
		{
			quantity = ModEntry.Data.Mushrooms.TryGetValue(o.ItemId, out MushroomData entry)
				? entry.MaximumQuantity
				: ModEntry.Data.MushroomMaximumQuantityPerPrice.FirstOrDefault(pair => pair.Key < o.Price).Value;
			quantity *= ModEntry.Config.MaximumQuantityLimitsDoubled ? 2 : 1;
		}
	}
}
