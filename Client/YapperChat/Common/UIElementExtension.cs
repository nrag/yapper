using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace YapperChat.Common
{
    public static class UIElementExtensions
    {
        /// <summary>
        /// Gets the relative position of the given UIElement to this.
        /// </summary>
        public static Point GetRelativePosition(this UIElement element, UIElement other)
        {
            if (element == null)
            {
                return new Point(0, 0);
            }

            return element.TransformToVisual(other)
                          .Transform(new Point(0, 0));
        }
    }
}
