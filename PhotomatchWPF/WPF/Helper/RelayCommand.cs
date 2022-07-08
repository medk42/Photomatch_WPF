using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace PhotomatchWPF.WPF.Helper
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> Action;

        public RelayCommand(Action<object> action)
        {
            Action = action;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => Action(parameter);

        public event EventHandler CanExecuteChanged 
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
