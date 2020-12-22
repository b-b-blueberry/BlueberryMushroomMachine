using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BlueberryMushroomAutomation
{
	public class ModEntry : Mod
	{
		public override void Entry(IModHelper helper)
		{
			Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Automate setup
			var automateApi = Helper.ModRegistry.GetApi<Core.IAutomateAPI>("Pathoschild.Automate");
			automateApi.AddFactory(new Core.PropagatorFactory());
		}
	}
}
