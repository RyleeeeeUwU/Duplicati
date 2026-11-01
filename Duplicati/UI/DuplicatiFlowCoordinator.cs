using BeatSaberMarkupLanguage;
using HMUI;

namespace Duplicati.UI
{
    internal sealed class DuplicatiFlowCoordinator : FlowCoordinator
    {
        private DuplicatiViewController _viewController;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                SetTitle("Duplicati");
                showBackButton = true;
                _viewController = BeatSaberUI.CreateViewController<DuplicatiViewController>();
            }

            if (addedToHierarchy)
                ProvideInitialViewControllers(_viewController);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}
