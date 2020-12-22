using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley;

namespace BlueberryMushroomMachine.Core
{
	public class PropagatorMachine : IMachine
	{
		private readonly Propagator Entity;
		public string MachineTypeID { get; } = $"{ModValues.PackageName}";
		public GameLocation Location { get; }
		public Rectangle TileArea { get; }

		/// <summary>Construct an instance.</summary>
		/// <param name="entity">The underlying entity.</param>
		/// <param name="location">The location which contains the machine.</param>
		/// <param name="tile">The tile covered by the machine.</param>
		public PropagatorMachine(Propagator entity, GameLocation location, in Vector2 tile)
		{
			Entity = entity;
			Location = location;
			TileArea = new Rectangle((int)tile.X, (int)tile.Y, 1, 1);
		}

		/// <summary>Get the machine's processing state.</summary>
		public MachineState GetState()
		{
			if (Entity.heldObject.Value == null)
			{
				return MachineState.Empty;
			}

			return Entity.readyForHarvest.Value ? MachineState.Done : MachineState.Processing;
		}

		/// <summary>Get the output item.</summary>
		public ITrackedStack GetOutput()
		{
			return new TrackedItem(Entity.heldObject.Value, onEmpty: item =>
			{
				Entity.PopExtraHeldMushrooms(giveNothing: true);
			});
		}


		/// <summary>Provide input to the machine.</summary>
		/// <param name="input">The available items.</param>
		/// <returns>Returns whether the machine started processing an item.</returns>
		public bool SetInput(IStorage input)
		{
			return false;
		}
	}
}
