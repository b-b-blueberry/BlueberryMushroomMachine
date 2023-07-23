﻿using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace BlueberryMushroomAutomation
{
	public class PropagatorFactory : IAutomationFactory
	{
		public IAutomatable GetFor(Object obj, GameLocation location, in Vector2 tile)
		{
			if (obj is BlueberryMushroomMachine.Propagator propagator)
				return new PropagatorMachine(propagator, location, tile);
			return null;
		}

		public IAutomatable GetFor(TerrainFeature feature, GameLocation location, in Vector2 tile)
		{
			return null;
		}

		public IAutomatable GetFor(Building building, BuildableGameLocation location, in Vector2 tile)
		{
			return null;
		}

		public IAutomatable GetForTile(GameLocation location, in Vector2 tile)
		{
			return null;
		}
	}
}
