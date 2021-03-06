﻿using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using ManagedWinapi.Windows;

namespace GestureSign.Common.Applications
{
    public interface IApplicationManager
    {
        event ApplicationChangedEventHandler ApplicationChanged;
        bool ApplicationExists(string ApplicationName);
        List<IApplication> Applications { get; }
        SystemWindow CaptureWindow { get; }
        IApplication CurrentApplication { get; set; }
        void AddApplication(IApplication Application);
        IEnumerable<IAction> GetRecognizedDefinedAction(string GestureName);
        IEnumerable<IApplication> GetApplicationFromPoint(Point testPoint);
        IApplication[] GetApplicationFromWindow(SystemWindow Window, bool userApplicationOnly);
        IApplication[] GetAvailableUserApplications();
        IEnumerable<IAction> GetEnabledDefinedAction(string GestureName, IEnumerable<IApplication> Application, bool UseGlobal);
        IApplication GetExistingUserApplication(string ApplicationName);
        IApplication GetGlobalApplication();
        SystemWindow GetWindowFromPoint(Point Point);
        Task<bool> LoadApplications();
        void RemoveGlobalAction(string ActionName);
        void RemoveNonGlobalAction(string ActionName);
        bool SaveApplications();
    }
}
