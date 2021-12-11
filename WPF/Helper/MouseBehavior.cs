using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Photomatch_ProofOfConcept_WPF.WPF.Helper
{
    public static class MouseBehavior
    {
        private static readonly DependencyProperty MouseHandlerProperty = DependencyProperty.RegisterAttached(
            "MouseHandler",
            typeof(IMouseHandler),
            typeof(MouseBehavior),
            new PropertyMetadata(MouseHandlerPropertyChangedCallBack)
        );

        public static void SetMouseHandler(this UIElement UIElement, IMouseHandler mouseHandler)
        {
            UIElement.SetValue(MouseHandlerProperty, mouseHandler);
        }

        private static void MouseHandlerPropertyChangedCallBack(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            UIElement uiElement = dependencyObject as UIElement;
            if (uiElement == null)
            {
                throw new ArgumentException("dependencyObject is not UIElement");
            }

            IMouseHandler handler = (IMouseHandler)uiElement.GetValue(MouseHandlerProperty);
            if (handler == null)
            {
                throw new ArgumentException("handler is not IMouseHandler");
            }

            uiElement.MouseDown += handler.MouseDown;
            uiElement.MouseUp += handler.MouseUp;
            uiElement.MouseMove += handler.MouseMove;
        }
    }
}
