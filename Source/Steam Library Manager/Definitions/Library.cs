﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Steam_Library_Manager.Definitions
{
    public class Library
    {
        public bool Main { get; set; }
        public bool Backup { get; set; }
        public int GameCount { get; set; }
        public DirectoryInfo steamAppsPath, commonPath, downloadPath, workshopPath;
        public Framework.AsyncObservableCollection<FrameworkElement> contextMenu { get; set; }
        public string fullPath { get; set; }
        public string prettyFreeSpace { get; set; }
        public int freeSpacePerc { get; set; }
        public long freeSpace;
        public Framework.AsyncObservableCollection<Game> Games { get; set; } = new Framework.AsyncObservableCollection<Game>();

        public void UpdateGameList()
        {
            try
            {
                if (!steamAppsPath.Exists)
                    steamAppsPath.Create();
                else if (Games.Count > 0)
                    Games.Clear();

                // Foreach *.acf file found in library
                //foreach (string acfFilePath in Directory.EnumerateFiles(steamAppsPath.FullName, "*.acf", SearchOption.TopDirectoryOnly))
                Parallel.ForEach(Directory.EnumerateFiles(steamAppsPath.FullName, "*.acf", SearchOption.TopDirectoryOnly), acfFilePath =>
                {
                    // Define a new value and call KeyValue
                    Framework.KeyValue Key = new Framework.KeyValue();

                    // Read the *.acf file as text
                    Key.ReadFileAsText(acfFilePath);

                    // If key doesn't contains a child (value in acf file)
                    if (Key.Children.Count == 0)
                        return;

                    Functions.Games.AddNewGame(acfFilePath, Convert.ToInt32(Key["appID"].Value), !string.IsNullOrEmpty(Key["name"].Value) ? Key["name"].Value : Key["UserConfig"]["name"].Value, Key["installdir"].Value, this, Convert.ToInt64(Key["SizeOnDisk"].Value), false);
                });
                Console.WriteLine(this.Games.Count);
                Parallel.ForEach(Directory.EnumerateDirectories(commonPath.FullName, "*", SearchOption.TopDirectoryOnly), gamedir =>
                {
                    bool found = false;
                    // Look to see if this installdir matches an ACF we already found
                    foreach(Game thisgame in this.Games.ToList())
                    {
                       if (String.Equals(thisgame.commonPath.FullName,gamedir, StringComparison.OrdinalIgnoreCase))
                            found = true;
                    }
                    if (found == false)
                    {
                        Functions.Games.AddNewOrphanedGame(gamedir, this);
                    }
                });

                // Do a loop for each *.zip file in library
                //foreach (string gameArchive in Directory.EnumerateFiles(steamAppsPath.FullName, "*.zip", SearchOption.TopDirectoryOnly))
                Parallel.ForEach(Directory.EnumerateFiles(steamAppsPath.FullName, "*.zip", SearchOption.TopDirectoryOnly), gameArchive =>
                {
                    Functions.Games.readGameDetailsFromZip(gameArchive, this);
                });

                // If library is backup library
                if (Backup)
                {
                    foreach (string skuFile in Directory.EnumerateFiles(fullPath, "*.sis", SearchOption.AllDirectories))
                    {
                        Framework.KeyValue Key = new Framework.KeyValue();

                        Key.ReadFileAsText(skuFile);

                        string[] name = System.Text.RegularExpressions.Regex.Split(Key["name"].Value, " and ");

                        int i = 0;
                        long gameSize = Functions.fileSystem.GetDirectorySize(new DirectoryInfo(skuFile).Parent.FullName, true);
                        foreach (Framework.KeyValue app in Key["apps"].Children)
                        {
                            if (Games.Count(x => x.appID == Convert.ToInt32(app.Value)) > 0)
                                continue;

                            Functions.Games.AddNewGame(skuFile, Convert.ToInt32(app.Value), name[i], Path.GetDirectoryName(skuFile), this, gameSize, false, true);

                            if (name.Count() > 1)
                                i++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public Framework.AsyncObservableCollection<FrameworkElement> generateRightClickMenuItems()
        {
            Framework.AsyncObservableCollection<FrameworkElement> rightClickMenu = new Framework.AsyncObservableCollection<FrameworkElement>();
            try
            {
                foreach (List.contextMenu cItem in List.libraryContextMenuItems.Where(x => x.IsActive))
                {
                    if (Backup && cItem.showToSLMBackup == Enums.menuVisibility.NotVisible)
                        continue;
                    else if (!Backup && cItem.showToNormal == Enums.menuVisibility.NotVisible)
                        continue;

                    if (cItem.IsSeparator)
                        rightClickMenu.Add(new Separator());
                    else
                    {
                        MenuItem slmItem = new MenuItem();

                        slmItem.Tag = this;
                        slmItem.Header = string.Format(cItem.Header, fullPath, prettyFreeSpace);
                        slmItem.Tag = cItem.Action;
                        slmItem.Icon = Functions.fAwesome.getAwesomeIcon(cItem.Icon, cItem.IconColor);

                        rightClickMenu.Add(slmItem);
                    }
                }

                return rightClickMenu;
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"An error happened while parsing context menu, most likely happened duo typo on color name.\n\n{ex}");

                return rightClickMenu;
            }
        }

        public void parseMenuItemAction(string Action)
        {
            switch (Action.ToLowerInvariant())
            {
                // Opens game installation path in explorer
                case "disk":
                    if (steamAppsPath.Exists)
                        System.Diagnostics.Process.Start(steamAppsPath.FullName);
                    break;
                case "deletelibrary":

                    MessageBoxResult moveGamesBeforeDeletion = MessageBox.Show("Move games in Library before deleting the library?", "Move games first?", MessageBoxButton.YesNoCancel);

                    if (moveGamesBeforeDeletion == MessageBoxResult.Yes)
                        //new Forms.moveLibrary(Library).Show();
                        MessageBox.Show("Function not implemented, process cancelled");
                    else if (moveGamesBeforeDeletion == MessageBoxResult.No)
                        removeLibrary(true);

                    break;
                case "deletelibraryslm":

                    foreach (Game Game in Games.ToList())
                    {
                        if (!Game.deleteFiles())
                        {
                            MessageBox.Show(string.Format("An unknown error happened while removing game files. {0}", fullPath));

                            return;
                        }
                        else
                            Game.RemoveFromLibrary();
                    }

                    updateLibraryVisual();

                    MessageBox.Show(string.Format("All game files in library ({0}) successfully removed.", fullPath));
                    break;
                case "movelibrary":
                    //new Forms.moveLibrary(Library).Show();
                    break;

                // Removes a backup library from list
                case "removefromlist":
                    if (Backup)
                    {
                        // Remove the library from our list
                        List.Libraries.Remove(this);

                        if (SLM.selectedLibrary == this)
                            MainWindow.Accessor.gamePanel.ItemsSource = null;
                    }
                    break;
            }
        }

        public void updateLibraryVisual()
        {
            freeSpace = Functions.fileSystem.getAvailableFreeSpace(fullPath);
            prettyFreeSpace = Functions.fileSystem.FormatBytes(freeSpace);
            freeSpacePerc = 100 - ((int)Math.Round((double)(100 * freeSpace) / Functions.fileSystem.getUsedSpace(fullPath)));

            if (MainWindow.Accessor.libraryPanel.Dispatcher.CheckAccess())
            {
                MainWindow.Accessor.libraryPanel.Items.Refresh();
            }
            else
            {
                MainWindow.Accessor.libraryPanel.Dispatcher.Invoke(delegate
                {
                    MainWindow.Accessor.libraryPanel.Items.Refresh();
                }, System.Windows.Threading.DispatcherPriority.Normal);
            }
        }

        public void updateLibraryPath(string newLibraryPath)
        {
            try
            {
                // Make a KeyValue reader
                Framework.KeyValue Key = new Framework.KeyValue();

                // Read vdf file
                Key.ReadFileAsText(Steam.vdfFilePath);

                // Change old library path with new one
                Key.Children[0].Children[0].Children[0].Children.Find(key => key.Value.Contains(fullPath)).Value = newLibraryPath;

                // Update config.vdf file with changes
                Key.SaveToFile(Steam.vdfFilePath, false);
            }
            catch { }
        }

        public void removeLibrary(bool deleteFiles)
        {
            try
            {
                if (deleteFiles)
                    Functions.fileSystem.deleteOldLibrary(this);

                List.Libraries.Remove(this);

                if (Backup)
                {
                    Functions.SLM.Settings.updateBackupDirs();
                    Functions.SLM.Settings.saveSettings();
                }
                else
                {
                    // Make a KeyValue reader
                    Framework.KeyValue Key = new Framework.KeyValue();

                    // Read vdf file
                    Key.ReadFileAsText(Definitions.Steam.vdfFilePath);

                    // Remove old library
                    Key.Children[0].Children[0].Children[0].Children.RemoveAll(x => x.Value == fullPath);

                    int i = 1;
                    foreach (Framework.KeyValue key in Key.Children[0].Children[0].Children[0].Children.FindAll(x => x.Name.Contains("BaseInstallFolder")))
                    {
                        key.Name = string.Format("BaseInstallFolder_{0}", i);
                        i++;
                    }

                    // Update libraryFolders.vdf file with changes
                    Key.SaveToFile(Definitions.Steam.vdfFilePath, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

    }
}
