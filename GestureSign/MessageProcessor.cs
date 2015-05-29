﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.UI;
using Point = System.Drawing.Point;

namespace GestureSign
{
    class MessageProcessor : IMessageProcessor
    {
        public static event EventHandler OnInitialized;
        public void ProcessMessages(NamedPipeServerStream server)
        {
            try
            {
                BinaryFormatter binForm = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    server.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    object data = binForm.Deserialize(memoryStream);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string message = data as string;
                        if (message != null)
                        {
                            switch (message)
                            {
                                case "MainWindow":
                                {

                                    foreach (Window win in Application.Current.Windows)
                                    {
                                        if (win.GetType() == typeof(MainWindow))
                                        {
                                            win.Activate();
                                            return;
                                        }
                                    }
                                    MainWindow mw = new MainWindow();
                                    mw.Show();
                                    mw.Activate();
                                    break;
                                }
                                case "Exit":
                                {
                                    Application.Current.Shutdown();
                                    break;
                                }
                                case "InitializationCompleted":
                                {
                                    if (OnInitialized != null)
                                        OnInitialized(this, EventArgs.Empty);
                                    break;
                                }
                                case "Initialize":
                                Guide guide = new Guide();
                                guide.Show();
                                guide.Activate();
                                break;
                            }
                        }
                        else
                        {
                            var newGesture = data as Tuple<string, List<List<Point>>>;
                            if (newGesture == null) return;
                            GestureDefinition gu = new GestureDefinition(newGesture.Item2, newGesture.Item1, false);
                            gu.Show();
                            gu.Activate();
                        }
                    });
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

        }
    }
}
