using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using System.Collections.Generic;

namespace BlueberryMushroomMachine
{
	public class PropagatorItemDataDefinition : BaseItemDataDefinition
	{
		public override string Identifier => ModEntry.Data.PropagatorTypeDefinitionId;

		public override Item CreateItem(ParsedItemData data)
		{
			return new Propagator();
		}

		public override bool Exists(string itemId)
		{
			return itemId == ModEntry.Data.PropagatorId;
		}

		public override IEnumerable<string> GetAllIds()
		{
			return [ModEntry.Data.PropagatorId];
		}

		public override ParsedItemData GetData(string itemId)
		{
			return new ParsedItemData(
				itemType: this,
				itemId: itemId,
				spriteIndex: 0,
				textureName: ModEntry.MachineTexture.Name,
				internalName: itemId,
				displayName: Propagator.PropagatorDisplayName,
				description: Propagator.PropagatorDescription,
				category: Propagator.BigCraftableCategory,
				objectType: null,
				rawData: null,
				isErrorItem: false,
				excludeFromRandomSale: true);
		}

		public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex)
		{
			return Utils.GetMachineSourceRect(location: Game1.currentLocation, tile: Vector2.Zero);
		}
	}
}
