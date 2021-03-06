﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Plugins;
using ManagedWinapi.Windows;

using System.Windows.Controls;
using GestureSign.Common.Localization;

namespace GestureSign.CorePlugins
{
    public class Minimize : IPlugin
    {
        #region Private Variables

        IHostControl _HostControl = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.Minimize.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.Minimize.Description"); }
        }

        public UserControl GUI
        {
            get { return null; }
        }

        public string Category
        {
            get { return "Windows"; }
        }

        public bool IsAction
        {
            get { return true; }
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public void ShowGUI(bool IsNew)
        {
            // Nothing to do here
        }

        public bool Gestured(Common.Plugins.PointInfo ActionPoint)
        {
            try
            {
                var className = ActionPoint.Window.ClassName;
                // Don't attempt to minimize tool windows (including Windows Program Manager)
                if (className.Equals("Windows.UI.Core.CoreWindow") ||
                   className.Equals("ImmersiveBackgroundWindow") ||
                   className.Equals("ImmersiveLauncher") ||
                   (ActionPoint.Window.ExtendedStyle & WindowExStyleFlags.TOOLWINDOW) == WindowExStyleFlags.TOOLWINDOW)
                    return false;

                // Minimize window
                ActionPoint.Window.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            }
            catch { return false; }
            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            return true;
            // Nothing to do here
        }

        public string Serialize()
        {
            // Nothing to serialize
            return "";
        }

        #endregion

        #region Host Control

        public IHostControl HostControl
        {
            get { return _HostControl; }
            set { _HostControl = value; }
        }

        #endregion
    }
}
