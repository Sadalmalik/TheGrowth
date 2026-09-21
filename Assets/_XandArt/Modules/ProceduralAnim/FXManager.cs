// using GeekyHouse.Subsystem.AssetsManagement;
// using GeekyHouse.Architecture.IOC;
//
// namespace Sadalmalik.ProcAnim
// {
// 	public class FXManager : SharedObject
// 	{
// 		[Inject] private AssetsManager _assetsManager;
//
// 		private FXSettings _settings;
//
// 		public override void Init()
// 		{
// 			_settings = _assetsManager.GetModuleSettings<FXSettings>();
// 		}
//
// 		public AnimationProfile ProfileBySlot(AnimationSlots slot)
//         {
// 			return _settings.ProfileBySlot[slot];
//         }
// 	}
// }