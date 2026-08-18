#if IOS
using UIKit;

namespace AppVendedores2025.Platforms.iOS
{
    public class StatusBarHiddenController : UIViewController
    {
        private readonly UIViewController _child;
        public StatusBarHiddenController(UIViewController child) => _child = child;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            AddChildViewController(_child);
            _child.View.Frame = View.Bounds;
            _child.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            View.AddSubview(_child.View);
            _child.DidMoveToParentViewController(this);
        }

        public override bool PrefersStatusBarHidden() => true;
        public override UIStatusBarStyle PreferredStatusBarStyle() => UIStatusBarStyle.LightContent;

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            try { SetNeedsStatusBarAppearanceUpdate(); } catch { }
        }
    }
}
#endif
