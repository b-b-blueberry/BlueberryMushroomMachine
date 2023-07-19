using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using Object = StardewValley.Object;

namespace BlueberryMushroomMachine
{
	[XmlType("Mods_BlueberryMushroomMachine")]
	public class Propagator : Object
	{
		// Source mushroom (placed by player)
		public string SourceMushroomName;
		public int SourceMushroomIndex;
		public int SourceMushroomQuality;

		// Extra mushrooms (grown overnight)
		public readonly int DefaultDaysToMature;
		public float RateToMature;
		public float DaysToMature;
		public int MaximumStack;

		// Common definitions
		public static string PropagatorDisplayName => ModEntry.I18n.Get("machine.name");
		public const int PropagatorWorkingMinutes = 999999;

		public Propagator() : this(tileLocation: Vector2.Zero)
		{
		}

		public Propagator(Vector2 tileLocation)
		{
			this.TileLocation = tileLocation;
			this.Initialise();
			this.DefaultDaysToMature = ModEntry.Instance.Config.MaximumDaysToMature;
		}

		protected override string loadDisplayName()
		{
			return Propagator.PropagatorDisplayName;
		}

		public override string getDescription()
		{
			return ModEntry.Instance.I18n.Get("machine.desc");
		}

		/// <summary>
		/// Assigns members based on definition given in <see cref="Game1.bigCraftablesInformation"/> from <see cref="ModValues.ObjectData"/>.
		/// </summary>
		private void Initialise()
		{
			if (ModEntry.Instance.Config.DebugMode)
			{
				Log.D($"Initialise [{ModValues.PropagatorIndex}] {ModValues.PropagatorInternalName} at {this.TileLocation}",
					ModEntry.Config.DebugMode);
			}

			this.Name = ModValues.PropagatorInternalName;
			this.ParentSheetIndex = ModValues.PropagatorIndex;
			this.DisplayName = this.loadDisplayName();

			string[] fields = ModValues.ObjectData.Split('/');
			this.Price = Convert.ToInt32(fields[1]);
			this.Edibility = Convert.ToInt32(fields[2]);
			string[] typeAndCategory = fields[3].Split(' ');
			this.Type = typeAndCategory[0];
			if (typeAndCategory.Length > 1)
				this.Category = Convert.ToInt32(typeAndCategory[1]);
			this.setOutdoors.Value = Convert.ToBoolean(fields[5]);
			this.setIndoors.Value = Convert.ToBoolean(fields[6]);
			this.Fragility = Convert.ToInt32(fields[7]);
			this.isLamp.Value = fields.Length > 8 && Convert.ToBoolean(fields[8]);

			this.CanBeSetDown = true;
			this.bigCraftable.Value = true;
			this.initializeLightSource(tileLocation: this.TileLocation);

			this.boundingBox.Value = new Rectangle(
				location: (this.TileLocation * Game1.tileSize).ToPoint(),
				size: new Point(Game1.tileSize));
		}

		/// <summary>
		/// Adds an instance of the given item to be used as a source mushroom by the machine,
		/// and resets all growth and harvest variables.
		/// </summary>
		/// <param name="dropIn">Some instance of an object, hopefully a mushroom.</param>
		public void PutSourceMushroom(Object dropIn)
		{
			ModEntry.GetMushroomGrowthRate(o: dropIn, rate: out this.RateToMature);
			ModEntry.GetMushroomMaximumQuantity(o: dropIn, quantity: out this.MaximumStack);

			this.SourceMushroomName = dropIn.Name;
			this.SourceMushroomIndex = dropIn.ParentSheetIndex;
			this.SourceMushroomQuality = dropIn.Quality;
			this.DaysToMature = 0;
			this.MinutesUntilReady = Propagator.PropagatorWorkingMinutes;

			Log.D($"PutSourceMushroom(item: [{dropIn.ParentSheetIndex}] {dropIn.Name} Q{dropIn.Quality}), stack to {this.MaximumStack}" +
				$" at {Game1.currentLocation?.Name} {this.TileLocation}",
				ModEntry.Instance.Config.DebugMode);
		}

		public void PutExtraHeldMushroom(float daysToMature)
		{
			this.heldObject.Value = new Object(parentSheetIndex: this.SourceMushroomIndex, initialStack: 1);
			this.DaysToMature = daysToMature;
			this.readyForHarvest.Value = false;
			this.MinutesUntilReady = Propagator.PropagatorWorkingMinutes;
		}

		public bool PopByAction()
		{
			Log.D($"PopByAction at {Game1.currentLocation?.Name} {this.TileLocation}",
				ModEntry.Instance.Config.DebugMode);
			if (this.SourceMushroomIndex > 0)
			{
				this.PopExposedMushroom(forceRemoveSource: false);
				return true;
			}
			return false;
		}

		public bool PopByTool()
		{
			Log.D($"PopByTool at {Game1.currentLocation?.Name} {this.TileLocation}",
				ModEntry.Instance.Config.DebugMode);

			if (this.SourceMushroomIndex > 0)
			{
				// Extract any held mushrooms from machine
				this.PopExposedMushroom(forceRemoveSource: true);
			}
			else
			{
				// Extract machine from location
				this.PopMachine();
			}
			return false;
		}

		public void PopExtraHeldMushrooms(bool giveNothing)
		{
			Log.D($"PopExtraHeldMushrooms at {Game1.currentLocation?.Name} {this.TileLocation}",
				ModEntry.Instance.Config.DebugMode);

			// Incorporate Gatherer's skill effects for bonus quantity
			int popQuantity = this.heldObject.Value.Stack;
			if (Game1.player.professions.Contains(Farmer.gatherer) && new Random().Next(5) == 0)
			{
				popQuantity += 1;
			}
			// Incorporate Botanist's skill effects for bonus quality
			int popQuality = Game1.player.professions.Contains(Farmer.botanist)
				? Object.bestQuality
				: this.SourceMushroomQuality;
			Object popObject = new(
				parentSheetIndex: this.SourceMushroomIndex,
				initialStack: 1,
				isRecipe: false,
				price: -1,
				quality: popQuality);

			// Create mushroom item drops in the world from the machine
			if (!giveNothing)
			{
				for (int i = 0; i < popQuantity; ++i)
				{
					Game1.createItemDebris(
						item: popObject.getOne(),
						origin: (this.TileLocation + new Vector2(0.5f)) * Game1.tileSize,
						direction: -1);
				}
			}

			// Clear the extra mushroom data
			this.heldObject.Value = null;
		}

		/// <summary>
		/// Pops the extra mushrooms in the 'heldItem' slot if they exist, otherwise pops the source mushroom and resets to 'empty'.
		/// </summary>
		/// <param name="forceRemoveSource">
		/// Whether or not to pop the source mushroom in addition to any extra mushrooms, leaving the machine considered 'empty'.
		/// </param>
		public void PopExposedMushroom(bool forceRemoveSource)
		{
			if (ModEntry.Instance.Config.DebugMode)
			{
				Log.D($"PopExposedMushroom(forceRemoveSource: {forceRemoveSource})"
					+ $" (item:" +
					$" [{this.SourceMushroomIndex}]" +
					$" {this.SourceMushroomName ?? "N/A"}x{this.heldObject?.Value?.Stack ?? 0}" +
					$" Q{this.SourceMushroomQuality})" +
					$" ({this.DaysToMature}/{this.DefaultDaysToMature} days +{this.RateToMature})" +
					$" at {Game1.currentLocation?.Name} {this.TileLocation}",
					ModEntry.Instance.Config.DebugMode);
			}

			Game1.playSound("harvest");
			bool popSource = forceRemoveSource || this.heldObject.Value is null;

			// Pop the extra mushrooms, leaving the source mushroom to continue producing
			if (this.heldObject.Value is not null)
			{
				this.PopExtraHeldMushrooms(giveNothing: false);
				this.MinutesUntilReady = Propagator.PropagatorWorkingMinutes;
			}

			// Pop the source mushroom, resetting the machine to default
			if (popSource)
			{
				Log.D("PopExposed source",
					ModEntry.Instance.Config.DebugMode);
				Object o = new (parentSheetIndex: this.SourceMushroomIndex, initialStack: 1)
				{
					Quality = SourceMushroomQuality
				};
				Game1.createItemDebris(
					item: o,
					origin: (this.TileLocation + new Vector2(0.5f)) * Game1.tileSize, direction: -1);
				this.MaximumStack = 1;
				this.SourceMushroomName = null;
				this.SourceMushroomIndex = 0;
				this.SourceMushroomQuality = 0;
				this.MinutesUntilReady = -1;
			}

			// Reset growing and harvesting info
			this.readyForHarvest.Value = false;
			this.DaysToMature = 0;
		}

		/// <summary>
		/// Behaviours for tool actions to uproot the machine itself.
		/// </summary>
		public void PopMachine()
		{
			Log.D($"PopMachine at {Game1.currentLocation?.Name} {this.TileLocation}",
				ModEntry.Instance.Config.DebugMode);
			Vector2 toolPosition = Game1.player.GetToolLocation();
			Vector2 propagatorPosition = this.boundingBox.Center.ToVector2();
			Vector2 key = Vector2.Floor(toolPosition / Game1.tileSize);
			Game1.currentLocation.debris.Add(new Debris(
				item: new Propagator(tileLocation: this.TileLocation),
				debrisOrigin: toolPosition,
				targetLocation: propagatorPosition));
			Game1.currentLocation.Objects.Remove(key);
		}

		/// <summary>
		/// Perform all start-of-day checks for the Propagator to handle held object events.
		/// </summary>
		internal void DayUpdate()
		{
			Log.D($"DayUpdate (item:" +
				$" [{this.SourceMushroomIndex}]" +
				$" {this.SourceMushroomName ?? "N/A"}x{this.heldObject?.Value?.Stack ?? 0}" +
				$" Q{this.SourceMushroomQuality}" +
				$" ({this.DaysToMature}/{this.DefaultDaysToMature} days +{this.RateToMature})" +
				$" at {location.Name} {this.TileLocation}",
				ModEntry.Instance.Config.DebugMode);

			// Indexing inconsistencies with JA/CFR
			this.ParentSheetIndex = ModValues.PropagatorIndex;

			// Grow mushrooms overnight
			if (this.SourceMushroomIndex > 0)
			{
				this.GrowHeldMushroom();
			}
		}

		/// <summary>
		/// Updates object quantity as the per-day maturity timer counts up to its threshold for this type of mushroom.
		/// </summary>
		internal void GrowHeldMushroom()
		{
			if (this.heldObject.Value is null)
			{
				// Set the extra mushroom object
				this.PutExtraHeldMushroom(daysToMature: 0);
				Log.D("==> Set first extra mushroom",
					ModEntry.Instance.Config.DebugMode);
				return;
			}
			else
			{
				// Stop adding to the stack when its limit is reached
				if (this.heldObject.Value.Stack >= this.MaximumStack)
				{
					Log.D("==> Reached max stack size, ready for harvest",
						ModEntry.Instance.Config.DebugMode);
					this.MinutesUntilReady = 0;
					this.readyForHarvest.Value = true;
					return;
				}

				// Progress the growth of the stack per each mushroom's rate
				this.DaysToMature += (int)Math.Floor(Math.Max(1, this.DefaultDaysToMature * this.RateToMature));
				this.MinutesUntilReady = Propagator.PropagatorWorkingMinutes;

				if (this.DaysToMature >= this.DefaultDaysToMature)
				{
					// When the held mushroom reaches a maturity stage, the stack grows
					++this.heldObject.Value.Stack;
					this.DaysToMature = 0;
				}
			}
			Log.D($"==> Grown to {this.heldObject.Value.Stack}/{this.MaximumStack}" +
				$" ({this.DaysToMature}/{this.DefaultDaysToMature} days +{this.RateToMature})",
				ModEntry.Instance.Config.DebugMode);
		}

		/// <summary>
		/// Override method for any player cursor passive or active interactions with the machine.
		/// Permits triggering behaviours to pop mushrooms before they're ready with the action hotkey.
		/// </summary>
		/// <param name="who">Farmer interacting with the machine.</param>
		/// <param name="justCheckingForActivity">Whether the cursor hovered or clicked.</param>
		/// <returns>Whether to continue with base method.</returns>
		public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
		{
			if (!justCheckingForActivity)
			{
				Log.D($"checkForAction at {Game1.currentLocation?.Name} {this.TileLocation}",
					ModEntry.Instance.Config.DebugMode);
			}

			Point tile = new(x: who.getTileX(), y: who.getTileY());
			if (!justCheckingForActivity && who is not null
					&& who.currentLocation.isObjectAtTile(tile.X, tile.Y - 1)
					&& who.currentLocation.isObjectAtTile(tile.X, tile.Y + 1)
					&& who.currentLocation.isObjectAtTile(tile.X + 1, tile.Y)
					&& who.currentLocation.isObjectAtTile(tile.X - 1, tile.Y)
					&& !who.currentLocation.getObjectAtTile(tile.X, tile.Y - 1).isPassable()
					&& !who.currentLocation.getObjectAtTile(tile.X, tile.Y + 1).isPassable()
					&& !who.currentLocation.getObjectAtTile(tile.X - 1, tile.Y).isPassable()
					&& !who.currentLocation.getObjectAtTile(tile.X + 1, tile.Y).isPassable())
			{
				this.performToolAction(t: null, location: who.currentLocation);
			}

			return justCheckingForActivity || base.checkForAction(who: who, justCheckingForActivity: justCheckingForActivity);
		}

		/// <summary>
		/// Allows the user to pop extra mushrooms before they're ready,
		/// and pop root mushrooms without extras.
		/// Author's note: The mushrooms are never ready.
		/// </summary>
		/// <returns>Whether to continue with base behaviour.</returns>
		public override bool performUseAction(GameLocation location)
		{
			Log.D($"performUseAction at {Game1.currentLocation?.Name} {this.TileLocation}",
				ModEntry.Instance.Config.DebugMode);

			return this.PopByAction();
		}

		/// <summary>
		/// Overrides the usual hit-with-tool behaviour to change the requirements
		/// and allow for popping held mushrooms at different stages.
		/// </summary>
		/// <returns>Whether or not to continue with base behaviour.</returns>
		public override bool performToolAction(Tool t, GameLocation location)
		{
			Log.D($"performToolAction at {Game1.currentLocation?.Name} {this.TileLocation}",
				ModEntry.Instance.Config.DebugMode);

			// Ignore usages that wouldn't trigger actions for other machines
			if (t is null || !t.isHeavyHitter() || t is StardewValley.Tools.MeleeWeapon)
			{
				return base.performToolAction(t, location);
			}

			location.playSound("woodWhack");
			return this.PopByTool();
		}

		/// <summary>
		/// Overrides usual use-with-item behaviours to limit the set to working in
		/// specific locations with specific items, as well as other funky behaviour.
		/// </summary>
		/// <param name="dropIn">Our candidate item.</param>
		/// <param name="probe">Base game check for determining outcomes without consequences.</param>
		/// <param name="who">Farmer using the machine.</param>
		/// <returns>
		/// Whether the dropIn object is appropriate for this machine in this context.
		/// </returns>
		public override bool performObjectDropInAction(Item dropIn, bool probe, Farmer who)
		{
			if (!probe)
			{
				Log.D($"performObjectDropInAction(dropIn:{dropIn?.Name ?? "null"}) at {Game1.currentLocation?.Name} {this.TileLocation}",
					ModEntry.Instance.Config.DebugMode);
			}

			// Ignore usages with inappropriate items
			if (dropIn is null)
			{
				return false;
			}

			// Ignore Truffles
			if (Utility.IsNormalObjectAtParentSheetIndex(dropIn, 430))
			{
				if (!probe)
				{
					Game1.showRedMessage(message: ModEntry.Instance.I18n.Get("error.truffle"));
				}
				return false;
			}

			// Ignore things that are not mushrooms
			if (dropIn is not Object obj || obj.bigCraftable.Value || !ModEntry.IsValidMushroom(o: obj))
			{
				if (!probe)
				{
					Log.D($"Invalid mushroom: [{dropIn.ParentSheetIndex}] {dropIn.Name}",
						ModEntry.Instance.Config.DebugMode);
				}
				return false;
			}

			// Ignore if location is not appropriate
			if (who is not null)
			{
				if (!((who.currentLocation is Cellar && ModEntry.Instance.Config.WorksInCellar)
					|| (who.currentLocation is FarmCave && ModEntry.Instance.Config.WorksInFarmCave)
					|| (who.currentLocation is BuildableGameLocation && ModEntry.Instance.Config.WorksInBuildings)
					|| (who.currentLocation is FarmHouse && ModEntry.Instance.Config.WorksInFarmHouse)
					|| (who.currentLocation.IsGreenhouse && ModEntry.Instance.Config.WorksInGreenhouse)
					|| (who.currentLocation.IsOutdoors && ModEntry.Instance.Config.WorksOutdoors)))
				{
					// Ignore bad machine locations
					if (!probe)
					{
						Game1.showRedMessage(message: ModEntry.Instance.I18n.Get("error.location"));
					}
					return false;
				}
			}

			// Don't make state changes if just checking
			if (probe)
			{
				return true;
			}

			// Extract held mushrooms prematurely
			if (this.SourceMushroomIndex > 0)
			{
				if (this.heldObject.Value is not null)
				{
					// Get a copy of the root mushroom
					this.PopExposedMushroom(forceRemoveSource: false);
				}
				else if (!ModEntry.Instance.Config.OnlyToolsCanRemoveRootMushrooms)
				{
					// Remove the root mushroom if it hasn't settled overnight
					this.PopExposedMushroom(forceRemoveSource: true);
				}
			}

			// Accept the deposited item as the new source mushroom
			this.PutSourceMushroom(dropIn: obj);
			who?.currentLocation.playSound("Ship");
			return true;
		}

		/// <summary>
		/// Awkward override to specifically place a Propagator instead of a BigCraftable Object.
		/// </summary>
		/// <returns>True</returns>
		public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
		{
			Vector2 tile = new Vector2(x: x, y: y) / Game1.tileSize;
			this.health = 10;

			// Determine player
			this.owner.Value = who?.UniqueMultiplayerID ?? Game1.player.UniqueMultiplayerID;

			// Add this propagator to the location as a placed object
			if (!this.performDropDownAction(who) && this.getOne() is Propagator propagator)
			{
				propagator.TileLocation = tile;
				propagator.shakeTimer = 50;

				if (location.objects.ContainsKey(tile))
				{
					if (location.objects[tile] is not Propagator)
					{
						Game1.createItemDebris(
							item: location.objects[tile],
							origin: tile * Game1.tileSize, direction: -1);
						location.objects[tile] = propagator;
					}
				}
				else
				{
					location.objects.Add(tile, propagator);
				}
				propagator.initializeLightSource(tileLocation: tile);
			}
			location.playSound("woodyStep");
			return true;
		}

		public override void draw(SpriteBatch b, int x, int y, float alpha = 1f)
		{
			Point shake = this.shakeTimer < 1
				? Point.Zero
				: new Point(x: Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			Vector2 pulseAmount = ModEntry.Instance.Config.PulseWhenGrowing
				? this.getScale() * Game1.pixelZoom
				: Vector2.One;
			Vector2 position = Game1.GlobalToLocal(
				viewport: Game1.viewport,
				globalPosition: new Vector2(x: x, y: y - 1) * Game1.tileSize);
			Rectangle destRect = new(
					(int)(position.X - pulse.X / 2f) + shake.X,
					(int)(position.Y - pulse.Y / 2f) + shake.Y,
					(int)(Game1.tileSize + pulse.X),
					(int)(Game1.tileSize * 2 + pulse.Y / 2f));

			// Draw the base sprite
			b.Draw(
					texture: Game1.bigCraftableSpriteSheet,
					destinationRectangle: destRect,
					sourceRectangle: sourceRect,
					color: Color.White * alpha,
					rotation: 0f,
					origin: Vector2.Zero,
					effects: SpriteEffects.None,
					layerDepth: Math.Max(0.0f, ((y + 1) * Game1.tileSize - 24) / 10000f)
						+ (Game1.currentLocation.IsOutdoors ? 0f : x * 1f / 10000f));

			// End here if no mushrooms are held
			if (this.SourceMushroomIndex < 1)
			{
				return;
			}

			// Draw the held object overlay
			bool isCustomMushroom = !Enum.IsDefined(enumType: typeof(ModEntry.Mushrooms), value: this.SourceMushroomIndex);
			int whichFrame = ModEntry.GetOverlayGrowthFrame(
				currentDays: this.DaysToMature,
				goalDays: this.DefaultDaysToMature,
				quantity: this.heldObject.Value?.Stack ?? 0,
				max: this.MaximumStack);
			sourceRect = ModEntry.GetOverlaySourceRect(index: this.SourceMushroomIndex, whichFrame: whichFrame);

			if (isCustomMushroom)
			{
				float growthRatio = (whichFrame + 1f) / (ModValues.OverlayMushroomFrames + 1f);
				float growthScale = Math.Min(0.8f, growthRatio) + 0.2f;
				destRect = new Rectangle(
					x: (int)(position.X - pulseAmount.X / 2f) + shake.X + (int)(32 * (1 - growthScale))
						+ (int)(pulseAmount.X * growthScale / 4),
					y: (int)(position.Y - pulseAmount.Y / 2f) + shake.Y + 48 + (int)(32 * (1 - growthScale))
						+ (int)(pulseAmount.Y * growthScale / 8),
					width: (int)((Game1.tileSize + pulseAmount.X) * growthScale),
					height: (int)((Game1.tileSize + pulseAmount.Y / 2f) * growthScale));
			}

			b.Draw(
				texture: !isCustomMushroom ? ModEntry.OverlayTexture : Game1.objectSpriteSheet,
				destinationRectangle: destRect,
				sourceRectangle: sourceRect,
				color: Color.White,
				rotation: 0f,
				origin: Vector2.Zero,
				effects: SpriteEffects.None,
				layerDepth: Math.Max(0.0f, ((y + 1) * Game1.tileSize - 24) / 10000f)
					+ (Game1.currentLocation.IsOutdoors ? 0f : x * 1f / 10000f) + 1f / 10000f + 1f / 10000f);
		}

		public override Item getOne()
		{
			return new Propagator(tileLocation: Vector2.Zero);
		}
	}
}
