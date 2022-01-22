using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Photomatch_ProofOfConcept_WPF.WPF.Helper
{
	public static class ControlProperties
    {
        private static readonly DependencyProperty MouseHandlerProperty = DependencyProperty.RegisterAttached(
            "MouseHandler",
            typeof(IMouseHandler),
            typeof(ControlProperties),
            new PropertyMetadata(MouseHandlerPropertyChangedCallBack)
        );

        private static readonly DependencyProperty SizeChangedProperty = DependencyProperty.RegisterAttached(
            "SizeChanged",
            typeof(RelayCommand),
            typeof(ControlProperties),
            new PropertyMetadata(SizeChangedPropertyChangedCallBack)
        );

        private static readonly DependencyProperty LoadedProperty = DependencyProperty.RegisterAttached(
            "Loaded",
            typeof(RelayCommand),
            typeof(ControlProperties),
            new PropertyMetadata(LoadedPropertyChangedCallBack)
        );

        public static void SetMouseHandler(this UIElement UIElement, IMouseHandler mouseHandler)
        {
            UIElement.SetValue(MouseHandlerProperty, mouseHandler);
        }

        private static void MouseHandlerPropertyChangedCallBack(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            UIElement uiElement = GetUIElement(dependencyObject);
            IMouseHandler handler = uiElement.GetValue(MouseHandlerProperty) as IMouseHandler;
            if (handler == null) throw new ArgumentException("handler is not " + nameof(IMouseHandler));
            
            uiElement.MouseDown += handler.MouseDown;
            uiElement.MouseUp += handler.MouseUp;
            uiElement.MouseMove += handler.MouseMove;
            uiElement.MouseEnter += handler.MouseEnter;
            uiElement.MouseLeave += handler.MouseLeave;
            uiElement.MouseWheel += handler.MouseWheel;
        }

        public static void SetSizeChanged(this UIElement UIElement, RelayCommand command)
		{
            UIElement.SetValue(SizeChangedProperty, command);
        }

        private static void SizeChangedPropertyChangedCallBack(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
		{
            FrameworkElement frameworkElement = GetFrameworkElement(dependencyObject);
            ICommand command = frameworkElement.GetValue(SizeChangedProperty) as ICommand;
            if (command == null) throw new ArgumentException("command is not " + nameof(ICommand));

            frameworkElement.SizeChanged += (sender, args) => command.Execute(args);
        }

        public static void SetLoaded(this UIElement UIElement, RelayCommand command)
        {
            UIElement.SetValue(LoadedProperty, command);
        }

        private static void LoadedPropertyChangedCallBack(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            FrameworkElement frameworkElement = GetFrameworkElement(dependencyObject);
            ICommand command = frameworkElement.GetValue(LoadedProperty) as ICommand;
            if (command == null) throw new ArgumentException("command is not " + nameof(ICommand));

            frameworkElement.Loaded += (sender, args) => command.Execute(sender);
        }

        private static UIElement GetUIElement(DependencyObject dependencyObject)
		{
            UIElement uiElement = dependencyObject as UIElement;
            if (uiElement == null)
            {
                throw new ArgumentException("dependencyObject is not " + nameof(UIElement));
            }

            return uiElement;
        }

        private static FrameworkElement GetFrameworkElement(DependencyObject dependencyObject)
		{
            FrameworkElement frameworkElement = dependencyObject as FrameworkElement;
            if (frameworkElement == null)
            {
                throw new ArgumentException("dependencyObject is not " + nameof(FrameworkElement));
            }

            return frameworkElement;
        }
    }
}
