using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewValley.Objects;
using StardewValley.Locations;

using StardewModdingAPI;

using PyTK.CustomElementHandler;

/** TODO:
 *		event with demetrius upon house upgrade 3 and event not yet seen
 *				If you are making events, use the last four digits of your Nexus ID plus 4 more numbers for event IDs.
 *				So 40770001, 40770002, etc
 **/

namespace BlueberryMushroomMachine
{
	public class Propagator : Cask, ISaveElement
	{
		// Custom members
		private bool mProduceDouble;
		private static Texture2D mOverlayTexture;
		private readonly static int daysToSilver = PropagatorMod.mHelper.ReadConfig<Config>().DaysToMature / 2;
		private readonly static int daysToGold = PropagatorMod.mHelper.ReadConfig<Config>().DaysToMature / 3;
		private readonly static int daysToIridium = 0;

		// Hidden members
		public new readonly int defaultDaysToMature = PropagatorMod.mHelper.ReadConfig<Config>().DaysToMature;

		public Propagator()
		{
		}
		
		public Propagator(Vector2 tileLocation)
		{
			// Take derived fields.
			IsRecipe = isRecipe;
			TileLocation = tileLocation;
			loadDefaultValues();

			// Load custom fields.
			mProduceDouble = false;

			// Load derived fields.
			loadObjectData();
		}

		protected override string loadDisplayName()
		{
			return PropagatorMod.i18n.Get("machine.name");
		}

		public override string getDescription()
		{
			return PropagatorMod.i18n.Get("machine.desc");
		}

		private void loadDefaultValues()
		{
			canBeSetDown.Value = true;
			bigCraftable.Value = true;
			initializeLightSource(TileLocation, false);
		}
		
		/// <summary>
		/// Initialises the machine with a collection of preset values.
		/// </summary>
		private void loadObjectData()
		{
			loadOverlayTexture();

			Name = PropagatorData.mPropagatorName;
			ParentSheetIndex = PropagatorData.mPropagatorIndex;

			string[] strArray1 = PropagatorData.mObjectData.Split('/');
			displayName = strArray1[0];
			price.Value = Convert.ToInt32(strArray1[1]);
			edibility.Value = Convert.ToInt32(strArray1[2]);
			string[] strArray2 = strArray1[3].Split(' ');
			type.Value = strArray2[0];
			if (strArray2.Length > 1)
				Category = Convert.ToInt32(strArray2[1]);
			setOutdoors.Value = Convert.ToBoolean(strArray1[5]);
			setIndoors.Value = Convert.ToBoolean(strArray1[6]);
			fragility.Value = Convert.ToInt32(strArray1[7]);
			isLamp.Value = strArray1.Length > 8 && strArray1[8].Equals("true");

			boundingBox.Value = new Rectangle(
					(int)TileLocation.X * 64,
					(int)TileLocation.Y * 64,
					64,
					64);
		}

		/// <summary>
		/// Instantiates any mushroom objects currently attached to
		/// the machine when the farm is loaded.
		/// </summary>
		private void loadHeldObject(int index, int quality, int stack)
		{
			if (index >= 0 && quality >= 0)
			{
				Item obj = new StardewValley.Object(index, stack)
						{ Quality = quality };
				if (PropagatorData.mMushroomAgingRates.TryGetValue(index, out float rate))
					putObject(obj.getOne(), rate);
			}
		}
		
		/// <summary>
		/// Shortcut for loading the collective texture for all mushroom overlays.
		/// </summary>
		private void loadOverlayTexture()
		{
			mOverlayTexture = PropagatorMod.mHelper.Content.Load<Texture2D>(PropagatorData.mOverlayPath);
		}
		
		/// <summary>
		/// Generates a clipping rectangle for the overlay appropriate
		/// to the current held mushroom, and its quality.
		/// </summary>
		/// <param name="whichMushroom">Tilesheet index of the held mushroom.</param>
		/// <param name="whichQuality">Quality of the held mushroom.</param>
		/// <returns></returns>
		private Rectangle getSourceRectForOverlay(int whichMushroom, int whichQuality)
		{
			return new Rectangle(whichQuality * 16, whichMushroom * 32, 16, 32);
		}

		/// <summary>
		/// Adds an instance of the given item to be held by the machine,
		/// and resets all countdown variables.
		/// </summary>
		/// <param name="dropIn">Generic instance of a mushroom item.</param>
		/// <param name="rate">Predetermined countdown rate in days for this type of mushroom.</param>
		private void putObject(Item dropIn, float rate)
		{
			heldObject.Value = dropIn.getOne() as StardewValley.Object;
			agingRate.Value = rate;
			daysToMature.Value = defaultDaysToMature;
			minutesUntilReady.Value = 999999;
			if (heldObject.Value.Quality == 1)
				daysToMature.Value = daysToSilver;
			else if (heldObject.Value.Quality == 2)
				daysToMature.Value = daysToGold;
			else if (heldObject.Value.Quality == 4)
			{
				daysToMature.Value = daysToIridium;
				minutesUntilReady.Value = 999999;
			}
		}

		/// <summary>
		/// Ejects a duplicate of the originally-inserted mushroom if the machine
		/// has held it overnight, otherwise ejects the original mushroom and resets to empty.
		/// </summary>
		/// <param name="remove">
		/// Whether or not to nullify the held mushroom, ejecting the originally-inserted
		/// mushroom and leaving the machine empty.
		/// </param>
		private void popObject(bool remove)
		{
			// Incorporate Gatherer's skill effects for extra production.
			int qty = 1;
			if (mProduceDouble && Game1.player.professions.Contains(Farmer.gatherer)
					&& new Random().Next(5) == 0)
				qty = 2;

			// Extract held object.
			Game1.playSound("coin");
			for (int i = 0; i < qty; i++)
				Game1.createItemDebris(heldObject.Value, tileLocation.Value * 64f, -1, null, -1);

			// Reset the harvest.
			StardewValley.Object obj = heldObject.Value;
			if (remove)
			{
				heldObject.Value = null;
				minutesUntilReady.Value = -1;
			}
			else
			{
				putObject(obj.getOne(), PropagatorData.mMushroomAgingRates[obj.ParentSheetIndex]);
				heldObject.Value.Quality = 0;
				minutesUntilReady.Value = 999999;
			}

			mProduceDouble = false;
			readyForHarvest.Value = false;
			daysToMature.Value = defaultDaysToMature;
		}

		/// <summary>
		/// Behaviours for tool actions to uproot the machine itself.
		/// </summary>
		private void popMachine()
		{
			// Extract the machine.
			Vector2 key = Game1.player.GetToolLocation(false) / 64f;
			key.X = (int)key.X;
			key.Y = (int)key.Y;
			Vector2 toolLocation = Game1.player.GetToolLocation(false);
			Rectangle boundingBox = Game1.player.GetBoundingBox();
			double x = boundingBox.Center.X;
			double y = boundingBox.Center.Y;
			Vector2 playerPosition = new Vector2((float)x, (float)y);
			Game1.currentLocation.debris.Add(new Debris(this, toolLocation, playerPosition));
			Game1.currentLocation.Objects.Remove(key);
		}

		/// <summary>
		/// Runs through all start-of-day checks.
		/// </summary>
		/// <param name="location">Used for default game behaviours.</param>
		public override void DayUpdate(GameLocation location)
		{
			base.DayUpdate(location);
			if (heldObject.Value == null)
				return;
			mProduceDouble = true;
			minutesUntilReady.Value = 999999;
			daysToMature.Value -= agingRate;
			checkForMaturity();
		}

		/// <summary>
		/// Updates item quality as the per-day maturity timer counts down.
		/// </summary>
		public new void checkForMaturity()
		{
			// Mature the held item, increasing the value.
			if (daysToMature <= daysToIridium)
			{
				heldObject.Value.Quality = 4;
			}
			else if (daysToMature <= daysToGold)
			{
				heldObject.Value.Quality = 2;
			}
			else
			{
				if (daysToMature > daysToSilver)
					return;
				heldObject.Value.Quality = 1;
			}
		}

		/// <summary>
		/// Override method for any player cursor passive or active interactions with the machine.
		/// Permits triggering behaviours to pop mushrooms before they're ready with the action hotkey.
		/// </summary>
		/// <param name="who">Farmer interacting with the machine.</param>
		/// <param name="justCheckingForActivity">Whether the cursor hovered or clicked.</param>
		/// <returns>Whether to continue with base routine.</returns>
		public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
		{
			if (!justCheckingForActivity && who != null
					&& who.currentLocation.isObjectAtTile(who.getTileX(), who.getTileY() - 1)
					&& who.currentLocation.isObjectAtTile(who.getTileX(), who.getTileY() + 1)
					&& who.currentLocation.isObjectAtTile(who.getTileX() + 1, who.getTileY())
					&& who.currentLocation.isObjectAtTile(who.getTileX() - 1, who.getTileY())
					&& !who.currentLocation.getObjectAtTile(who.getTileX(), who.getTileY() - 1).isPassable()
					&& !who.currentLocation.getObjectAtTile(who.getTileX(), who.getTileY() + 1).isPassable()
					&& !who.currentLocation.getObjectAtTile(who.getTileX() - 1, who.getTileY()).isPassable()
					&& !who.currentLocation.getObjectAtTile(who.getTileX() + 1, who.getTileY()).isPassable())
				performToolAction(null, who.currentLocation);

			if (justCheckingForActivity)
				return true;

			return base.checkForAction(who, justCheckingForActivity);
		}

		/// <summary>
		/// Allows the user to pop extra mushrooms before they're ready,
		/// and pop root mushrooms without extras.
		/// Author's note: The mushrooms are never ready.
		/// </summary>
		/// <param name="location"></param>
		/// <returns>Whether to continue with base routine.</returns>
		public override bool performUseAction(GameLocation location)
		{
			if (heldObject.Value != null)
			{
				popObject(false);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Overrides the usual hit-with-tool behaviour to change the requirements
		/// and allow for popping held mushrooms at different stages.
		/// </summary>
		/// <param name="t">Tool type. Default method.</param>
		/// <param name="location">Current location. Default method.</param>
		/// <returns>Whether or not to continue with base routine.</returns>
		public override bool performToolAction(Tool t, GameLocation location)
		{
			// Ignore usages that wouldn't trigger actions for other machines.
			if (t == null || !t.isHeavyHitter() || t is StardewValley.Tools.MeleeWeapon)
				return base.performToolAction(t, location);

			location.playSound("woodWhack");
			if (heldObject.Value != null)
				// Extract any held mushrooms from machine.
				popObject(true);
			else
				// Extract machine from location.
				popMachine();
			return false;
		}

		/// <summary>
		/// Overrides usual use-with-item behaviours to limit the set to working in
		/// specific locations with specific items, as well as other funky behaviour.
		/// </summary>
		/// <param name="dropIn">Our candidate item.</param>
		/// <param name="probe"></param>
		/// <param name="who">Farmer using the machine.</param>
		/// <returns>
		/// Whether or not to continue with base functionalities.
		/// Falls through unless a mushroom was dropped in under good circumstances.
		/// </returns>
		public override bool performObjectDropInAction(Item dropIn, bool probe, Farmer who)
		{
			// Ignore usages with inappropriate items.
			if (dropIn != null && dropIn is StardewValley.Object
					&& (dropIn as StardewValley.Object).bigCraftable.Value)
				return false;

			// Extract held mushrooms prematurely.
			if (heldObject.Value != null)
				if (!readyForHarvest && mProduceDouble)
					// Get a copy of the root mushroom.
					popObject(false);
				else
					// Remove the root mushroom if it hasn't settled overnight.
					popObject(true);

			// Determine if being used in an appropriate location.
			if (!probe && who != null)
			{
				bool flag = false;
				if (who.currentLocation is Cellar && PropagatorMod.mConfig.WorksInCellar)
					flag = true;
				else if (who.currentLocation is FarmCave && PropagatorMod.mConfig.WorksInFarmCave)
					flag = true;
				else if (who.currentLocation is BuildableGameLocation && PropagatorMod.mConfig.WorksInBuildings)
					flag = true;
				else if (who.currentLocation is FarmHouse && PropagatorMod.mConfig.WorksInFarmHouse)
					flag = true;
				else if (who.currentLocation.IsGreenhouse && PropagatorMod.mConfig.WorksInGreenhouse)
					flag = true;
				else if (who.currentLocation.IsOutdoors && PropagatorMod.mConfig.WorksOutdoors)
					flag = true;
				
				if (!flag)
				{
					// Ignore bad machine locations.
					Game1.showRedMessage(PropagatorMod.i18n.Get("error.location"));
					return false;
				}
			}
			
			// Ignore Truffles.
			if (!probe && dropIn.ParentSheetIndex.Equals(430))
			{
				Game1.showRedMessage(PropagatorMod.i18n.Get("error.truffle"));
				return false;
			}

			// nanda kore wa
			if (quality >= 4)
				return false;

			// Ignore wrong items.
			if (!PropagatorData.mMushroomAgingRates.TryGetValue(dropIn.ParentSheetIndex, out float num))
				return false;

			// Accept the deposited item.
			if (!probe)
			{
				putObject(dropIn, num);
				who.currentLocation.playSound("Ship");
			}
			return true;
		}

		/// <summary>
		/// Awkward override to specifically place a Propagator instead of a BigCraftable Object.
		/// </summary>
		/// <param name="location"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="who"></param>
		/// <returns></returns>
		public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
		{
			Vector2 index1 = new Vector2(x / 64, y / 64);
			health = 10;

			// Determine player.
			if (who != null)
				owner.Value = who.UniqueMultiplayerID;
			else
				owner.Value = Game1.player.UniqueMultiplayerID;

			// Spawn object.
			location.objects.Add(index1, new Propagator(index1));
			location.playSound("hammer");
			if (!performDropDownAction(who))
			{
				//mMirrorSprite = new Random().Next(1) == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				StardewValley.Object one = (StardewValley.Object)getOne();
				one.shakeTimer = 50;
				one.TileLocation = index1;

				// Avoid placement conflicts.
				if (location.objects.ContainsKey(index1))
				{
					if (location.objects[index1].ParentSheetIndex != ParentSheetIndex)
					{
						Game1.createItemDebris(location.objects[index1], index1 * 64f, Game1.random.Next(4), null, -1);
						location.objects[index1] = one;
					}
				}
				else
					location.objects.Add(index1, one);
				one.initializeLightSource(index1, false);
			}
			location.playSound("woodyStep");
			return true;
		}

		#region Object Draw Overrides
		public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
		{
			spriteBatch.Draw(
					Game1.bigCraftableSpriteSheet,
					objectPosition,
					new Rectangle?(getSourceRectForBigCraftable(ParentSheetIndex)),
					Color.White,
					0.0f,
					Vector2.Zero,
					4f,
					SpriteEffects.None,
					Math.Max(0.0f, (float)(f.getStandingY() + 2) / 10000f));
		}

		public override void drawInMenu(
				SpriteBatch spriteBatch,
				Vector2 location,
				float scaleSize,
				float transparency,
				float layerDepth,
				bool drawStackNumber,
				Color color,
				bool drawShadow)
		{
			if (isRecipe)
			{
				transparency = 0.5f;
				scaleSize *= 0.75f;
			}
			if (bigCraftable)
			{
				int num = 0;
				if (heldObject.Value != null)
					PropagatorData.mMushroomSourceRects.TryGetValue(heldObject.Value.ParentSheetIndex, out num);

				spriteBatch.Draw(
						Game1.bigCraftableSpriteSheet,
						location + new Vector2(32f, 32f),
						new Rectangle?(getSourceRectForBigCraftable(ParentSheetIndex)),
						color * transparency, 
						0.0f, 
						new Vector2(8f, 16f),
						(float)(4.0 * (scaleSize < 0.2 ? scaleSize : scaleSize / 2.0)), 
						SpriteEffects.None, 
						layerDepth);
			}
			if (!isRecipe)
				return;
			spriteBatch.Draw(
					Game1.objectSpriteSheet,
					location + new Vector2(16f, 16f),
					Game1.getSourceRectForStandardTileSheet(
							Game1.objectSpriteSheet, 
							451, 
							16, 
							16), 
					color, 
					0.0f,
					Vector2.Zero, 
					3f, 
					SpriteEffects.None, 
					layerDepth + 0.0001f);
		}

		public override void drawAsProp(SpriteBatch b)
		{
			int x = (int)tileLocation.X;
			int y = (int)tileLocation.Y;
			int num = 0;
			if (heldObject.Value != null)
				PropagatorData.mMushroomSourceRects.TryGetValue(heldObject.Value.ParentSheetIndex, out num);
			Vector2 vector2 = getScale() * 4f;
			Vector2 local = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 64));
			Rectangle destRect = new Rectangle(
					(int)(local.X - vector2.X / 2.0), 
					(int)(local.Y - vector2.Y / 2.0), 
					(int)(64.0 + vector2.X), 
					(int)(128.0 + vector2.Y / 2.0));
			b.Draw(
					Game1.bigCraftableSpriteSheet,
					destRect,
					new Rectangle?(getSourceRectForBigCraftable(ParentSheetIndex)),
					Color.White, 
					0.0f, 
					Vector2.Zero,
					SpriteEffects.None, 
					Math.Max(0.0f, ((y + 1) * 64 - 1) / 10000f));
				
			if (minutesUntilReady <= 0)
				return;

			// Draw the held object overlay.
			if (heldObject.Value == null)
				return;
			b.Draw(
					mOverlayTexture,
					destRect,
					getSourceRectForOverlay(num, heldObject.Value.Quality),
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					Math.Max(0.0f, ((y + 1) * 64 - 1) / 10000f + 1f / 10000f));
			b.Draw(
					Game1.objectSpriteSheet, 
					getLocalPosition(Game1.viewport) + new Vector2(32f, 0.0f), 
					new Microsoft.Xna.Framework.Rectangle?(
							Game1.getSourceRectForStandardTileSheet(
									Game1.objectSpriteSheet, 435, -1, -1)),
					Color.White,
					scale.X, 
					new Vector2(32f, 32f),
					1f,
					SpriteEffects.None, 
					Math.Max(0.0f, ((y + 1) * 64 - 1) / 10000f + 1f / 10000f));
		}

		public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
		{
			// Draw the base sprite.
			int num = 0;
			if (heldObject.Value != null)
				PropagatorData.mMushroomSourceRects.TryGetValue(heldObject.Value.ParentSheetIndex, out num);
			Vector2 vector2 = getScale() * 4f;
			Vector2 local = Game1.GlobalToLocal(Game1.viewport, new Vector2( x * 64, y * 64 - 64));
			Rectangle destRect = new Rectangle(
					(int)(local.X - vector2.X / 2.0) + (shakeTimer > 0 ? Game1.random.Next(-1, 2) : 0), 
					(int)(local.Y - vector2.Y / 2.0) + (shakeTimer > 0 ? Game1.random.Next(-1, 2) : 0), 
					(int)(64.0 + vector2.X), 
					(int)(128.0 + vector2.Y / 2.0));
			spriteBatch.Draw(
					Game1.bigCraftableSpriteSheet,
					destRect,
					new Rectangle?(getSourceRectForBigCraftable(ParentSheetIndex)),
					Color.White * alpha, 
					0.0f, 
					Vector2.Zero,
					SpriteEffects.None,
					Math.Max(0.0f, ((y + 1) * 64 - 24) / 10000f) + x * 1f / 10000f);
			
			if (heldObject.Value == null)
				return;

			// Draw the held item overlay.
			spriteBatch.Draw(
					mOverlayTexture,
					destRect,
					getSourceRectForOverlay(num, heldObject.Value.Quality),
					Color.White * alpha,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					Math.Max(0.0f, ((y + 1) * 64 - 24) / 10000f) + x * 1f / 10000f + 1f / 10000f);
		}
		#endregion

		public override Item getOne()
		{
			return new Propagator();
		}

		/* PyTK ISaveElement */

		public object getReplacement()
		{
			PropagatorMod.mMonitor.Log("getReplacement()",
				LogLevel.Trace);

			return new Cask();
		}

		public Dictionary<string, string> getAdditionalSaveData()
		{
			PropagatorMod.mMonitor.Log("getAdditionalSaveData()",
				LogLevel.Trace);

			int putIndex = heldObject.Value != null ? heldObject.Value.ParentSheetIndex : -1;
			int putQuality = heldObject.Value != null ? heldObject.Value.Quality : -1;
			int putStack = heldObject.Value != null ? heldObject.Value.Stack : -1;

			return new Dictionary<string, string>()
			{
				{ "tileLocationX", TileLocation.X.ToString() },
				{ "tileLocationY", TileLocation.Y.ToString() },
				{ "heldObjectIndex", putIndex.ToString() },
				{ "heldObjectQuality", putQuality.ToString() },
				{ "heldObjectStack", putStack.ToString() },
				{ "produceDouble", mProduceDouble.ToString() }
			};
		}

		public void rebuild(Dictionary<string, string> additionalSaveData, object replacement)
		{
			PropagatorMod.mMonitor.Log("rebuild(additionalSaveData, replacement)",
				LogLevel.Trace);

			loadDefaultValues();
			loadObjectData();
			
			float.TryParse(additionalSaveData["tileLocationX"], out float x);
			float.TryParse(additionalSaveData["tileLocationY"], out float y);
			TileLocation = new Vector2(x, y);

			int.TryParse(additionalSaveData["heldObjectIndex"], out int heldObjectIndex);
			int.TryParse(additionalSaveData["heldObjectQuality"], out int heldObjectQuality);
			int.TryParse(additionalSaveData["heldObjectStack"], out int heldObjectStack);
			loadHeldObject(heldObjectIndex, heldObjectQuality, heldObjectStack);

			bool.TryParse(additionalSaveData["produceDouble"], out bool produceDouble);
			mProduceDouble = produceDouble;
		}
	}
}
