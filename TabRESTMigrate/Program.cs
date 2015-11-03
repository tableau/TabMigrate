using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnlineContentDownloader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //UI form we are going to show
            var form = new FormSiteExportImport();

            //See if we have command line arguments to run
            var commandLine = new CommandLineParser();
            if(CommandLineParser.HasUseableCommandLine(commandLine))
            {
                form.SetStartupCommandLine(commandLine);
            }

            Application.Run(form);
        }
    }
}
