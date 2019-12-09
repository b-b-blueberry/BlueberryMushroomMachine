using System.Collections.Generic;

namespace BlueberryMushroomMachine
{
	public interface IJsonAssetsApi
	{
		int GetBigCraftableId(string name);
		IDictionary<string, int> GetAllBigCraftableIds();
	}
}
