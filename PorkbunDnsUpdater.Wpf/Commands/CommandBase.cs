
using System.Windows.Input;

namespace PorkbunDnsUpdater.Commands
{
    public class CommandBase(Action<object> executeAction) : ICommand
    {
        private Action<object> execute = executeAction;

        public event EventHandler? CanExecuteChanged;

        public virtual bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            execute(parameter!);
        }

        protected void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }
}
