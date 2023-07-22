using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Xml.Serialization;
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
		public static Point PropagatorSize => new(x: 16, y: 32);
		public static Point OverlaySize => new(x: 24, y: 32);
		public static int PropagatorWorkingMinutes => 999999;

		public Propagator() : this(tileLocation: Vector2.Zero)
		{
		}

		public Propagator(Vector2 tileLocation)
		{
			this.TileLocation = tileLocation;
			this.Initialise();
			this.DefaultDaysToMature = ModEntry.Config.MaximumDaysToMature;
		}

		protected override string loadDisplayName()
		{
			return Propagator.PropagatorDisplayName;
		}

		public override string getDescription()
		{
			return ModEntry.I18n.Get("machine.desc");
		}

		/// <summary>
		/// Assigns members based on definition given in <see cref="Game1.bigCraftablesInformation"/> from <see cref="ModValues.ObjectData"/>.
		/// </summary>
		private void Initialise()
		{
			if (ModEntry.Config.DebugMode)
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
				ModEntry.Config.DebugMode);
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
				ModEntry.Config.DebugMode);
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
				ModEntry.Config.DebugMode);

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
				ModEntry.Config.DebugMode);

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
			if (ModEntry.Config.DebugMode)
			{
				Log.D($"PopExposedMushroom(forceRemoveSource: {forceRemoveSource})"
					+ $" (item:" +
					$" [{this.SourceMushroomIndex}]" +
					$" {this.SourceMushroomName ?? "N/A"}x{this.heldObject?.Value?.Stack ?? 0}" +
					$" Q{this.SourceMushroomQuality})" +
					$" ({this.DaysToMature}/{this.DefaultDaysToMature} days +{this.RateToMature})" +
					$" at {Game1.currentLocation?.Name} {this.TileLocation}",
					ModEntry.Config.DebugMode);
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
					ModEntry.Config.DebugMode);
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
				ModEntry.Config.DebugMode);
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
		public override void DayUpdate(GameLocation location)
		{
			Log.D($"DayUpdate (item:" +
				$" [{this.SourceMushroomIndex}]" +
				$" {this.SourceMushroomName ?? "N/A"}x{this.heldObject?.Value?.Stack ?? 0}" +
				$" Q{this.SourceMushroomQuality}" +
				$" ({this.DaysToMature}/{this.DefaultDaysToMature} days +{this.RateToMature})" +
				$" at {location.Name} {this.TileLocation}",
				ModEntry.Config.DebugMode);

			// Grow mushrooms overnight
			this.GrowHeldMushroom();
		}

		/// <summary>
		/// Updates object quantity as the per-day maturity timer counts up to its threshold for this type of mushroom.
		/// </summary>
		public void GrowHeldMushroom()
		{
			if (this.SourceMushroomIndex <= 0)
			{
				Log.D("==> No source mushroom",
					ModEntry.Config.DebugMode);
				return;
			}
			else if (this.heldObject.Value is null)
			{
				// Set the extra mushroom object
				this.PutExtraHeldMushroom(daysToMature: 0);
				Log.D("==> Set first extra mushroom",
					ModEntry.Config.DebugMode);
				return;
			}
			else
			{
				// Stop adding to the stack when its limit is reached
				if (this.heldObject.Value.Stack >= this.MaximumStack)
				{
					Log.D("==> Reached max stack size, ready for harvest",
						ModEntry.Config.DebugMode);
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
				ModEntry.Config.DebugMode);
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
					ModEntry.Config.DebugMode);
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
				ModEntry.Config.DebugMode);

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
				ModEntry.Config.DebugMode);

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
					ModEntry.Config.DebugMode);
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
					Game1.showRedMessage(message: ModEntry.I18n.Get("error.truffle"));
				}
				return false;
			}

			// Ignore things that are not mushrooms
			if (dropIn is not Object obj || obj.bigCraftable.Value || !ModEntry.IsValidMushroom(o: obj))
			{
				if (!probe)
				{
					Log.D($"Invalid mushroom: [{dropIn.ParentSheetIndex}] {dropIn.Name}",
						ModEntry.Config.DebugMode);
				}
				return false;
			}

			// Ignore if location is not appropriate
			if (who is not null)
			{
				if (!((who.currentLocation is Cellar && ModEntry.Config.WorksInCellar)
					|| (who.currentLocation is FarmCave or IslandFarmCave && ModEntry.Config.WorksInFarmCave)
					|| (who.currentLocation is BuildableGameLocation && ModEntry.Config.WorksInBuildings)
					|| (who.currentLocation is FarmHouse && ModEntry.Config.WorksInFarmHouse)
					|| (who.currentLocation.IsGreenhouse && ModEntry.Config.WorksInGreenhouse)
					|| (who.currentLocation.IsOutdoors && ModEntry.Config.WorksOutdoors)))
				{
					// Ignore bad machine locations
					if (!probe)
					{
						Game1.showRedMessage(message: ModEntry.I18n.Get("error.location"));
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
				else if (!ModEntry.Config.OnlyToolsCanRemoveRootMushrooms)
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
			Point scaleSizeToPulse(Point size, Vector2 pulse) => (size.ToVector2() * Game1.pixelZoom + new Vector2(x: pulse.X, y: pulse.Y / 2)).ToPoint();
			
			Point shake = this.shakeTimer < 1
				? Point.Zero
				: new Point(x: Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			Vector2 pulse = ModEntry.Config.PulseWhenGrowing
				? this.getScale() * Game1.pixelZoom
				: Vector2.One;
			Vector2 position = Game1.GlobalToLocal(
				viewport: Game1.viewport,
				globalPosition: new Vector2(x: x, y: y - 1) * Game1.tileSize);
			Rectangle destination = new(
				location: (position - pulse / 2).ToPoint() + shake,
				size: scaleSizeToPulse(size: Propagator.PropagatorSize, pulse: pulse));
			Rectangle source = ModEntry.GetMachineSourceRect(
				location: Game1.currentLocation,
				tile: this.TileLocation);
			float layerDepth = Math.Max(0.0f, ((y + 1) * Game1.tileSize - 24) / 10000f)
				+ (Game1.currentLocation.IsOutdoors ? 0f : x * 1f / 10000f);
			bool isFlipped = ModEntry.GetMachineIsFlipped(tile: this.TileLocation);

			// Draw the base sprite
			Propagator.DrawMachine(
				spriteBatch: b,
				destination: destination,
				origin: Vector2.Zero,
				color: Color.White,
				alpha: alpha,
				layerDepth: layerDepth,
				source: source,
				isFlipped: isFlipped);

			// End here if no mushrooms are held
			if (this.SourceMushroomIndex < 1)
			{
				return;
			}

			// Draw the held object overlay
			bool isBasicMushroom = Enum.IsDefined(enumType: typeof(ModEntry.Mushrooms), value: this.SourceMushroomIndex);
			int whichFrame = ModEntry.GetOverlayGrowthFrame(
				currentDays: this.DaysToMature,
				goalDays: this.DefaultDaysToMature,
				quantity: this.heldObject.Value?.Stack ?? 0,
				max: this.MaximumStack);
			int frames = ModValues.OverlayMushroomFrames;

			if (isBasicMushroom)
			{
				// Centre mushroom overlay on base sprite
				destination.Offset(amount: (source.Size.ToVector2() - Propagator.OverlaySize.ToVector2()) * Game1.pixelZoom / 2);
				destination.Size = scaleSizeToPulse(size: Propagator.OverlaySize, pulse: pulse);
				source = ModEntry.GetOverlaySourceRect(
					location: Game1.currentLocation,
					index: this.SourceMushroomIndex,
					whichFrame: whichFrame);
			}
			else
			{
				// Scale custom mushroom object sprite to growth ratio
				float growthRatio = whichFrame / frames;
				float growthScale = Math.Min(0.8f, growthRatio) + 0.2f;
				destination = new Rectangle(
					x: (int)(position.X - pulse.X / 2f) + shake.X + (int)(32 * (1 - growthScale))
						+ (int)(pulse.X * growthScale / 4),
					y: (int)(position.Y - pulse.Y / 2f) + shake.Y + 48 + (int)(32 * (1 - growthScale))
						+ (int)(pulse.Y * growthScale / 8),
					width: (int)((Game1.tileSize + pulse.X) * growthScale),
					height: (int)((Game1.tileSize + pulse.Y / 2f) * growthScale));
			}


			b.Draw(
				texture: isBasicMushroom ? ModEntry.OverlayTexture : Game1.objectSpriteSheet,
				destinationRectangle: destination,
				sourceRectangle: source,
				color: Color.White,
				rotation: 0f,
				origin: Vector2.Zero,
				effects: isFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
				layerDepth: Math.Max(0.0f, ((y + 1) * Game1.tileSize - 24) / 10000f)
					+ (Game1.currentLocation.IsOutdoors ? 0f : x * 1f / 10000f) + 1f / 10000f + 1f / 10000f);
		}

		// Other draw method overrides added only to use custom machine texture in place of objects/craftables texture for base sprite:

		public override void draw(SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha = 1f)
		{
			if (this.isTemporarilyInvisible)
			{
				return;
			}

			Vector2 scaleFactor = this.getScale() * Game1.pixelZoom;
			Vector2 position = Game1.GlobalToLocal(
				viewport: Game1.viewport,
				globalPosition: new Vector2(xNonTile, yNonTile));
			Rectangle destination = new(
				x: (int)(position.X - (scaleFactor.X / 2f)) + ((this.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
				y: (int)(position.Y - (scaleFactor.Y / 2f)) + ((this.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
				width: (int)(Game1.tileSize + scaleFactor.X),
				height: (int)(Game1.tileSize * 2 + (scaleFactor.Y / 2f)));
			Propagator.DrawMachine(
				spriteBatch: spriteBatch,
				destination: destination,
				origin: Vector2.Zero,
				color: Color.White,
				alpha: alpha,
				layerDepth: layerDepth,
				source: ModEntry.GetMachineSourceRect(location: Game1.currentLocation, tile: this.TileLocation),
				isFlipped: ModEntry.GetMachineIsFlipped(tile: this.TileLocation));
		}

		public override void drawAsProp(SpriteBatch b)
		{
			if (this.isTemporarilyInvisible)
			{
				return;
			}

			Vector2 scale = this.getScale() * Game1.pixelZoom;
			Vector2 position = Game1.GlobalToLocal(
				viewport: Game1.viewport,
				globalPosition: (this.TileLocation + new Vector2(x: 0, y: -1)) * Game1.tileSize);
			Rectangle destination = new(
				x: (int)(position.X - (scale.X / 2f)),
				y: (int)(position.Y - (scale.Y / 2f)),
				width: (int)(Game1.tileSize + scale.X),
				height: (int)(Game1.tileSize * 2 + (scale.Y / 2f)));
			float layerDepth = Math.Clamp(value: ((this.TileLocation.Y + 1) * Game1.tileSize - 1) / 10000f, min: 0, max: 1);
			Propagator.DrawMachine(
				spriteBatch: b,
				destination: destination,
				origin: Vector2.Zero,
				color: Color.White,
				alpha: 1f,
				layerDepth: layerDepth,
				source: ModEntry.GetMachineSourceRect(location: Game1.currentLocation, tile: this.TileLocation),
				isFlipped: ModEntry.GetMachineIsFlipped(tile: this.TileLocation));
		}

		public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
		{
			Rectangle destination = new(
				location: objectPosition.ToPoint(),
				size: (Propagator.PropagatorSize.ToVector2() * Game1.pixelZoom).ToPoint());
			float layerDepth = Math.Max(0f, (f.getStandingY() + 3f) / 10000f);
			Propagator.DrawMachine(
				spriteBatch: spriteBatch,
				destination: destination,
				origin: Vector2.Zero,
				color: Color.White,
				alpha: 1f,
				layerDepth: layerDepth,
				source: ModEntry.GetMachineSourceRect(location: Game1.currentLocation, tile: this.TileLocation));
		}

		public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
		{
			const float tinyScale = 3f;
			bool shouldDrawStackNumber = ((drawStackNumber == StackDrawType.Draw && this.maximumStackSize() > 1 && this.Stack > 1)
					|| drawStackNumber == StackDrawType.Draw_OneInclusive)
				&& scaleSize > 0.3f
				&& this.Stack != int.MaxValue;
			if (this.IsRecipe)
			{
				shouldDrawStackNumber = false;
				transparency = 0.5f;
				scaleSize *= 0.75f;
			}

			float scale = Game1.pixelZoom * (((double)scaleSize < 0.2) ? scaleSize : (scaleSize / 2f));
			Vector2 position = location + new Vector2(value: 1) * Game1.tileSize / 2;
			Rectangle destination = new(
				location: position.ToPoint(),
				size: (Propagator.PropagatorSize.ToVector2() * scale).ToPoint());
			Propagator.DrawMachine(
				spriteBatch: spriteBatch,
				destination: destination,
				origin: Propagator.PropagatorSize.ToVector2() / 2,
				color: color,
				alpha: transparency,
				layerDepth: layerDepth,
				source: Game1.uiMode ? null : ModEntry.GetMachineSourceRect(location: Game1.currentLocation, tile: this.TileLocation));

			if (shouldDrawStackNumber)
			{
				Utility.drawTinyDigits(
					toDraw: this.Stack,
					b: spriteBatch,
					position: location + new Vector2(
						x: Game1.tileSize - Utility.getWidthOfTinyDigitString(this.Stack, tinyScale * scaleSize) + (tinyScale * scaleSize),
						y: Game1.tileSize - (18f * scaleSize) + 2f),
					scale: tinyScale * scaleSize,
					layerDepth: 1f,
					c: color);
			}

			if (this.IsRecipe)
			{
				const int size = Game1.smallestTileSize;
				spriteBatch.Draw(
					texture: Game1.objectSpriteSheet,
					position: location + new Vector2(value: size),
					sourceRectangle: Game1.getSourceRectForStandardTileSheet(
						tileSheet: Game1.objectSpriteSheet,
						tilePosition: 451,
						width: size,
						height: size),
					color: color,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: tinyScale,
					effects: SpriteEffects.None,
					layerDepth: layerDepth + 0.0001f);
			}
		}

		public override Item getOne()
		{
			return new Propagator(tileLocation: Vector2.Zero);
		}

		protected static void DrawMachine(SpriteBatch spriteBatch, Rectangle destination, Vector2 origin, Color color, float alpha, float layerDepth, Rectangle? source = null, bool isFlipped = false)
		{
			spriteBatch.Draw(
				texture: ModEntry.MachineTexture,
				destinationRectangle: destination,
				sourceRectangle: source ?? new Rectangle(location: Point.Zero, size: Propagator.PropagatorSize),
				color: Color.White * alpha,
				rotation: 0f,
				origin: origin,
				effects: isFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
				layerDepth: layerDepth);
		}
	}
}
