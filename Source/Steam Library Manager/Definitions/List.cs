﻿using System.Globalization;
using System.Windows.Media;

namespace Steam_Library_Manager.Definitions
{
    // Our Library and Game definitions exists there
    public class List
    {
        // Make a new list for Library details
        public static Framework.AsyncObservableCollection<Library> Libraries = new Framework.AsyncObservableCollection<Library>();
        public static Framework.AsyncObservableCollection<contextMenu> libraryContextMenuItems = new Framework.AsyncObservableCollection<contextMenu>();
        public static Framework.AsyncObservableCollection<contextMenu> gameContextMenuItems = new Framework.AsyncObservableCollection<contextMenu>();

        public class contextMenu
        {
            public bool IsActive { get; set; } = true;
            public string Header { get; set; }
            public string Action { get; set; }
            public FontAwesome.WPF.FontAwesomeIcon Icon { get; set; } = FontAwesome.WPF.FontAwesomeIcon.None;
            public Brush IconColor { get; set; }
            public Enums.menuVisibility showToNormal { get; set; } = Enums.menuVisibility.Visible;
            public Enums.menuVisibility showToSLMBackup { get; set; } = Enums.menuVisibility.Visible;
            public Enums.menuVisibility showToSteamBackup { get; set; } = Enums.menuVisibility.Visible;
            public Enums.menuVisibility showToCompressed { get; set; } = Enums.menuVisibility.Visible;
            public bool IsSeparator { get; set; }
        }

        public class Language
        {
            public string shortName { get; set; }
            public string displayName { get; set; }

            public CultureInfo culture;
            public string externalFileName;
            public bool isDefault, requiresExternalFile;
        }

    }
}
