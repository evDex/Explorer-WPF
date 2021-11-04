using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Explorer
{
    public class DelegateCommand : ICommand
    {
        protected Predicate<object> _canExecute;
        protected Action<object> _Execute;

        public DelegateCommand(Action<object> Execute,
                       Predicate<object> canExecute)
        {
            _Execute = Execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _Execute(parameter);
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
