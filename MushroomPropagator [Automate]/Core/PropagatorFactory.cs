using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;

namespace MushroomPropagator_Automate
{
	public class PropagatorFactory : IAutomationFactory
	{
		public IAutomatable GetFor(Object obj, GameLocation location, in Vector2 tile)
		{
			if (obj is MushroomPropagator.Propagator propagator)
				return new PropagatorMachine(propagator, location, tile);
			return null;
		}

		public IAutomatable GetFor(TerrainFeature feature, GameLocation location, in Vector2 tile)
		{
			return null;
		}

		public IAutomatable GetFor(Building building, GameLocation location, in Vector2 tile)
		{
			return null;
		}

		public IAutomatable GetForTile(GameLocation location, in Vector2 tile)
		{
			return null;
		}
	}
}
