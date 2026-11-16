using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Xamarin.Forms;
using Saplin.CPDT.UICore;
using Saplin.CPDT.UICore.Controls;

// First-stage Android TV navigation. Works with the existing Xamarin.Forms controls
// without changing the disk-test engine or the Android minimum SDK.
namespace Saplin.CPDT.Droid
{
    internal sealed class TvDpadNavigation
    {
        private VisualElement selected;
        private Color originalBackground;

        public bool Handle(Keycode key)
        {
            var page = Xamarin.Forms.Application.Current?.MainPage;
            if (page == null) return false;

            var items = new List<VisualElement>();
            Collect(page, items);
            if (items.Count == 0)
            {
                ClearSelection();
                return false;
            }

            // Do not keep focus on a control hidden by a page transition or popup.
            if (selected != null && !items.Contains(selected))
                ClearSelection();

            if (key == Keycode.DpadCenter || key == Keycode.Enter || key == Keycode.NumpadEnter)
            {
                if (selected == null) Select(items[0]);
                else Activate(selected);
                return true;
            }

            if (key != Keycode.DpadDown && key != Keycode.DpadUp &&
                key != Keycode.DpadLeft && key != Keycode.DpadRight)
                return false;

            if (selected == null)
            {
                Select(items[0]);
                return true;
            }

            var source = Center(selected);
            VisualElement next = null;
            double best = double.PositiveInfinity;
            foreach (var candidate in items)
            {
                if (candidate == selected) continue;
                var target = Center(candidate);
                var dx = target.X - source.X;
                var dy = target.Y - source.Y;

                double primary, secondary;
                switch (key)
                {
                    case Keycode.DpadUp: primary = -dy; secondary = Math.Abs(dx); break;
                    case Keycode.DpadDown: primary = dy; secondary = Math.Abs(dx); break;
                    case Keycode.DpadLeft: primary = -dx; secondary = Math.Abs(dy); break;
                    default: primary = dx; secondary = Math.Abs(dy); break;
                }

                if (primary <= 1) continue;
                // Favor controls on the same row/column, then nearby controls.
                double score = primary + secondary * 2.5;
                if (score < best)
                {
                    best = score;
                    next = candidate;
                }
            }

            if (next != null) Select(next);
            return true;
        }

        private static void Collect(VisualElement root, List<VisualElement> controls)
        {
            if (!root.IsVisible || !root.IsEnabled || root.Opacity <= 0 ||
                root.Width <= 0 || root.Height <= 0)
                return;

            // Popups are drawn above the page: restrict navigation to the active overlay.
            // Other overlays can still use Android's standard Back handling.
            if (root is MainPage && ViewModelContainer.NavigationViewModel.IsAnyPopupVisible)
            {
                var popup = FindVisiblePopup(root);
                if (popup != null)
                {
                    Collect(popup, controls);
                    return;
                }
            }

            var button = root as Button;
            var label = root as ExtendedLabel;
            if ((button != null && button.Command != null) || label != null)
                controls.Add(root);

            var layout = root as Layout<View>;
            if (layout != null)
                foreach (var child in layout.Children) Collect(child, controls);

            var content = root as ContentView;
            if (content?.Content != null) Collect(content.Content, controls);

            var scroll = root as ScrollView;
            if (scroll?.Content != null) Collect(scroll.Content, controls);

            var page = root as ContentPage;
            if (page?.Content != null) Collect(page.Content, controls);
        }

        private static VisualElement FindVisiblePopup(VisualElement root)
        {
            var layout = root as Layout<View>;
            if (layout == null)
            {
                var page = root as ContentPage;
                return page?.Content == null ? null : FindVisiblePopup(page.Content);
            }

            foreach (var child in layout.Children)
            {
                if (child is Popups && child.IsVisible) return child;
                var result = FindVisiblePopup(child);
                if (result != null) return result;
            }
            return null;
        }

        private struct Point2 { public double X, Y; public Point2(double x, double y) { X = x; Y = y; } }

        private static Point2 Center(VisualElement control)
        {
            double x = control.X + control.Width / 2;
            double y = control.Y + control.Height / 2;
            var parent = control.Parent;
            while (parent is VisualElement)
            {
                var view = (VisualElement)parent;
                x += view.X;
                y += view.Y;
                var scroll = view as ScrollView;
                if (scroll != null)
                {
                    x -= scroll.ScrollX;
                    y -= scroll.ScrollY;
                }
                parent = view.Parent;
            }
            return new Point2(x, y);
        }

        private void Select(VisualElement element)
        {
            if (element == selected) return;
            ClearSelection();
            selected = element;
            originalBackground = element.BackgroundColor;
            element.BackgroundColor = Color.FromHex("#986500");

            var parent = element.Parent;
            while (parent != null)
            {
                var scroll = parent as ScrollView;
                if (scroll != null)
                {
                    // Ensure the current item is visible when using the remote.
                    scroll.ScrollToAsync(element, ScrollToPosition.MakeVisible, false);
                    break;
                }
                parent = parent.Parent;
            }
        }

        private void ClearSelection()
        {
            if (selected == null) return;
            selected.BackgroundColor = originalBackground;
            selected = null;
        }

        private static void Activate(VisualElement element)
        {
            var button = element as Button;
            if (button != null)
            {
                if (button.Command?.CanExecute(button.CommandParameter) == true)
                    button.Command.Execute(button.CommandParameter);
                return;
            }

            (element as ExtendedLabel)?.DoClick();
        }

        public void Reset() { ClearSelection(); }
    }
}
