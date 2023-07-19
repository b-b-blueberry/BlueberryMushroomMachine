using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BlueberryMushroomAutomation
{
	public class ModEntry : Mod
	{
		public override void Entry(IModHelper helper)
		{
			this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Automate setup
			IAutomateAPI automateApi = this.Helper.ModRegistry.GetApi<IAutomateAPI>("Pathoschild.Automate");
			automateApi.AddFactory(new PropagatorFactory());
		}
	}
}
