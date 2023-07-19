using System;

namespace BlueberryMushroomMachine;
public interface IJsonAssetsAPI
{
	int GetObjectId(string name);

	event EventHandler IdsFixed;
}
