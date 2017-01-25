using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace COMessengerClient.Properties
{
    class SettingsCommands
    {

        public static readonly RoutedUICommand SaveSettings = RegisterNewCommand("SaveSettings", "SaveSettings", SaveSettings_Execute, SaveSettings_CanExecute);


        private static void SaveSettings_CanExecute(Object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private static void SaveSettings_Execute(Object sender, ExecutedRoutedEventArgs e)
        {
            Settings.Default.Save();
        }

        public static readonly RoutedUICommand RevertSettings = RegisterNewCommand("RevertSettings", "RevertSettings", RevertSettings_Execute, RevertSettings_CanExecute);

        private static void RevertSettings_CanExecute(Object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private static void RevertSettings_Execute(Object sender, ExecutedRoutedEventArgs e)
        {
            Settings.Default.Reload();
        }



        private static RoutedUICommand RegisterNewCommand(string commandDescription, string commandName, ExecutedRoutedEventHandler execHandler, CanExecuteRoutedEventHandler canExecHandler)
        {
            RoutedUICommand NewCommand = new RoutedUICommand(commandDescription, commandName, typeof(Window));

            CommandManager.RegisterClassCommandBinding(type: typeof(Window),
                                                        commandBinding: new CommandBinding(command: NewCommand,
                                                                                            executed: new ExecutedRoutedEventHandler(execHandler),
                                                                                            canExecute: new CanExecuteRoutedEventHandler(canExecHandler)));

            return NewCommand;
        }
    }
}
