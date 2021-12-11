using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Photomatch_ProofOfConcept_WPF.WPF.Helper
{
    public static class MouseBehavior
    { 
        private static readonly DependencyProperty MouseDownCommandProperty = DependencyProperty.RegisterAttached(
            "MouseDownCommand",
            typeof(ICommand),
            typeof(MouseBehavior),
            new PropertyMetadata(MouseDownCommandPropertyChangedCallBack)
        );

        private static readonly DependencyProperty MouseUpCommandProperty = DependencyProperty.RegisterAttached(
            "MouseUpCommand",
            typeof(ICommand),
            typeof(MouseBehavior),
            new PropertyMetadata(MouseUpCommandPropertyChangedCallBack)
        );

        private static readonly DependencyProperty MouseMoveCommandProperty = DependencyProperty.RegisterAttached(
            "MouseMoveCommand",
            typeof(ICommand),
            typeof(MouseBehavior),
            new PropertyMetadata(MouseMoveCommandPropertyChangedCallBack)
        );

        public static void SetMouseDownCommand(this UIElement inUIElement, ICommand inCommand)
        {
            inUIElement.SetValue(MouseDownCommandProperty, inCommand);
        }

        public static void SetMouseUpCommand(this UIElement inUIElement, ICommand inCommand)
        {
            inUIElement.SetValue(MouseUpCommandProperty, inCommand);
        }

        public static void SetMouseMoveCommand(this UIElement inUIElement, ICommand inCommand)
        {
            inUIElement.SetValue(MouseMoveCommandProperty, inCommand);
        }

        private static ICommand GetMouseCommand(UIElement inUIElement, DependencyProperty property)
        {
            return (ICommand)inUIElement.GetValue(property);
        }

        private static void MouseDownCommandPropertyChangedCallBack(DependencyObject inDependencyObject, DependencyPropertyChangedEventArgs inEventArgs)
        {
            UIElement uiElement = ToUiElement(inDependencyObject);
            uiElement.MouseDown += (sender, args) =>
            {
                GetMouseCommand(uiElement, MouseDownCommandProperty).Execute(args);
                args.Handled = true;
            };
        }

        private static void MouseUpCommandPropertyChangedCallBack(DependencyObject inDependencyObject, DependencyPropertyChangedEventArgs inEventArgs)
        {
            UIElement uiElement = ToUiElement(inDependencyObject);
            uiElement.MouseUp += (sender, args) =>
            {
                GetMouseCommand(uiElement, MouseUpCommandProperty).Execute(args);
                args.Handled = true;
            };
        }

        private static void MouseMoveCommandPropertyChangedCallBack(DependencyObject inDependencyObject, DependencyPropertyChangedEventArgs inEventArgs)
        {
            UIElement uiElement = ToUiElement(inDependencyObject);
            uiElement.MouseMove += (sender, args) =>
            {
                GetMouseCommand(uiElement, MouseMoveCommandProperty).Execute(args);
                args.Handled = true;
            };
        }

        public static UIElement ToUiElement(DependencyObject dependency)
		{
            UIElement uiElement = dependency as UIElement;
            if (null == uiElement)
            {
                throw new ArgumentException("inDependencyObject is not UIElement");
            }

            return uiElement;
        }
    }
}
