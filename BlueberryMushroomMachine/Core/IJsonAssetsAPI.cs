using System;

namespace BlueberryMushroomMachine.Core;
public interface IJsonAssetsAPI
{
    int GetObjectId(string name);

    event EventHandler IdsFixed;
}
