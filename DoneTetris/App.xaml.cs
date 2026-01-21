using DoneListWithTetris;
using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
namespace DoneTetris
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var dbPath = DbInitializer.GetDbPath();
            DbInitializer.Initialize(dbPath);
            this.DispatcherUnhandledException += (s, ex) =>
            {
                try
                {
                    File.AppendAllText(
                        "crash.log",
                        $"[{DateTime.Now}]\n{ex.Exception}\n\n"
                    );
                }
                catch { }

                // ここでは何も表示しない
                Environment.Exit(1);
            };

            base.OnStartup(e);
        }
    }

}
