
if (key.Equals(who.getTileLocation()) && !this.objects[key].isPassable())
{
	Tool t1 = (Tool) new Pickaxe();
	t1.DoFunction(Game1.currentLocation, -1, -1, 0, who);
	if (this.objects[key].performToolAction(t1, this))
	{
		this.objects[key].performRemoveAction(
			(Vector2) ((NetFieldBase<Vector2, NetVector2>) this.objects[key].tileLocation),
			Game1.currentLocation);
		this.objects[key].dropItem(
			this,
			who.GetToolLocation(false),
			new Vector2(
				(float) who.GetBoundingBox().Center.X,
				(float) who.GetBoundingBox().Center.Y));
		Game1.currentLocation.Objects.Remove(key);
		return true;
	}

	Tool t2 = (Tool) new Axe();
	t2.DoFunction(Game1.currentLocation, -1, -1, 0, who);
	if (this.objects.ContainsKey(key) && this.objects[key].performToolAction(t2, this))
	{
		this.objects[key].performRemoveAction(
			(Vector2) ((NetFieldBase<Vector2, NetVector2>) this.objects[key].tileLocation),
			Game1.currentLocation);
		this.objects[key].dropItem(
			this, who.GetToolLocation(false),
			new Vector2(
				(float) who.GetBoundingBox().Center.X,
				(float) who.GetBoundingBox().Center.Y));
		Game1.currentLocation.Objects.Remove(key);
		return true;
	}

	if (!this.objects.ContainsKey(key))
		return true;
}

if (this.objects.ContainsKey(key) && (this.objects[key].Type.Equals("Crafting")
	|| this.objects[key].Type.Equals("interactive")))
{
	if (who.ActiveObject == null && this.objects[key].checkForAction(who, false))
		return true;
		
	if (this.objects.ContainsKey(key))
	{
		if (who.CurrentItem == null || !this.objects[key].performObjectDropInAction(who.CurrentItem, false, who))
			return this.objects[key].checkForAction(who, false);
		who.reduceActiveItemByOne();
		return true;
	}
}