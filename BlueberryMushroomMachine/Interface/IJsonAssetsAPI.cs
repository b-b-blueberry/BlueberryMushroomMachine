using System;

namespace BlueberryMushroomMachine.Interface;
public interface IJsonAssetsAPI
{
    int GetObjectId(string name);

    event EventHandler IdsFixed;
}
