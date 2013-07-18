using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Roslyn.Services;

namespace Trailbreaker.RecorderApplication
{
    internal class Start
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (!Directory.Exists(Exporter.outputPath))
            {
                Directory.CreateDirectory(Exporter.outputPath);
            }

            FindSolution();

            var gui = new GUI();
            Application.Run(gui);
        }

        public static void FindSolution()
        {
            IEnumerable<IProject> projects;
            Configuration config =
                ConfigurationManager.OpenExeConfiguration(Path.GetFileName(Assembly.GetExecutingAssembly().Location) +
                                                          ".config");

            var solutionDialog = new OpenFileDialog();
            solutionDialog.InitialDirectory = "C:\\";
            solutionDialog.Filter = "Solution files (*.sln)|*.sln";

            if (config.AppSettings.Settings[Exporter.OutputSolutionSetting] == null)
            {
                config.AppSettings.Settings.Add(Exporter.OutputSolutionSetting, null);
            }

            while (config.AppSettings.Settings[Exporter.OutputSolutionSetting].Value == null ||
                   Exporter.workspace == null ||
                   Exporter.pageObjectLibrary == null || Exporter.pageObjectTestLibrary == null ||
                   !File.Exists(config.AppSettings.Settings[Exporter.OutputSolutionSetting].Value))
            {
                if (config.AppSettings.Settings[Exporter.OutputSolutionSetting].Value == null)
                {
                    MessageBox.Show("You must select a .NET solution with a Page Object Library named " +
                                    Exporter.pageObjectLibraryName + " and a Page Object Test Library named " +
                                    Exporter.pageObjectTestLibraryName + "!",
                                    "Configuration", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    if (solutionDialog.ShowDialog() == DialogResult.OK)
                    {
                        Exporter.workspace = Workspace.LoadSolution(solutionDialog.FileName);
                        config.AppSettings.Settings[Exporter.OutputSolutionSetting].Value = solutionDialog.FileName;
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
                else
                {
                    Exporter.workspace =
                        Workspace.LoadSolution(config.AppSettings.Settings[Exporter.OutputSolutionSetting].Value);
                }

                if (Exporter.pageObjectLibrary == null)
                {
                    projects = Exporter.workspace.CurrentSolution.GetProjectsByName(Exporter.pageObjectLibraryName);
                    foreach (IProject proj in projects)
                    {
                        Exporter.pageObjectLibrary = proj;
                    }
                    if (Exporter.pageObjectLibrary == null)
                    {
                        MessageBox.Show(
                            "The selected solution does not contain a project named " + Exporter.pageObjectLibraryName +
                            ".",
                            "Configuration", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        config.AppSettings.Settings[Exporter.OutputSolutionSetting].Value = null;
                        Exporter.workspace = null;
                        Exporter.pageObjectLibrary = null;
                        Exporter.pageObjectTestLibrary = null;
                        continue;
                    }
                }

                if (Exporter.pageObjectTestLibrary == null)
                {
                    projects = Exporter.workspace.CurrentSolution.GetProjectsByName(Exporter.pageObjectTestLibraryName);
                    foreach (IProject proj in projects)
                    {
                        Exporter.pageObjectTestLibrary = proj;
                    }
                    if (Exporter.pageObjectTestLibrary == null)
                    {
                        MessageBox.Show(
                            "The currently chosen solution does not contain a project named " +
                            Exporter.pageObjectTestLibraryName + ".",
                            "Configuration", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        config.AppSettings.Settings[Exporter.OutputSolutionSetting].Value = null;
                        Exporter.workspace = null;
                        Exporter.pageObjectLibrary = null;
                        Exporter.pageObjectTestLibrary = null;
                        continue;
                    }
                }
            }

            config.Save(ConfigurationSaveMode.Modified);

            Exporter.solutionPath = config.AppSettings.Settings[Exporter.OutputSolutionSetting].Value;
        }
    }
}