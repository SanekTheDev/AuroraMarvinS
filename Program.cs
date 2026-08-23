namespace AuroraMarvin
{
    using System;
    using System.Windows.Forms;

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AuroraMarvinView view = new AuroraMarvinView();
            IAuroraMarvinModel mdl = new AuroraMarvinModel();
            _ = new AuroraMarvinController(view, mdl);
            Application.Run(view);
        }
    }
}