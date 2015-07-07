﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Windows.Controls.Primitives;
using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Plugins;
using GestureSign.Common.Gestures;
using GestureSign.UI.Common;
using MahApps.Metro.Controls.Dialogs;


namespace GestureSign.UI
{
    /// <summary>
    /// AvailableAction.xaml 的交互逻辑
    /// </summary>
    public partial class AvailableAction : UserControl
    {
        // public static event EventHandler StartCapture;
        public AvailableAction()
        {
            InitializeComponent();

            var actionsSourceView = new ListCollectionView(ActionInfos);//创建数据源的视图
            actionsSourceView.GroupDescriptions.Add(new PropertyGroupDescription("GestureName"));//在图中添加分组
            actionsSourceView.SortDescriptions.Add(new SortDescription("GestureName", ListSortDirection.Ascending));
            lstAvailableActions.ItemsSource = actionsSourceView;//绑定数据源

            var applicationSourceView = new ListCollectionView(_applications);
            var applicationGroupDesctrption = new PropertyGroupDescription("Group");//设置分组列
            applicationSourceView.GroupDescriptions.Add(applicationGroupDesctrption);//在图中添加分组
            lstAvailableApplication.ItemsSource = applicationSourceView;

            ActionDialog.ActionsChanged += (o, e) =>
            {
                if (lstAvailableApplication.SelectedItem == e.Application)
                {
                    var oldActionInfo =
                          ActionInfos.FirstOrDefault(ai => ai.ActionName.Equals(e.Action.Name, StringComparison.Ordinal));
                    if (oldActionInfo != null)
                    {
                        int index = ActionInfos.IndexOf(oldActionInfo);
                        var newActionInfo = Action2ActionInfo(e.Action);
                        ActionInfos[index] = newActionInfo;
                        RefreshGroup(e.Action.GestureName);
                        SelectAction(newActionInfo);
                    }
                    else RefreshActions(false);
                }
                else
                {
                    BindApplications();
                    _selecteNewestItem = true;
                    lstAvailableApplication.SelectedItem = e.Application;
                }
            };
            AvailableGestures.GestureChanged += (o, e) => { RefreshActions(true); };
            GestureDefinition.GesturesChanged += (o, e) => { RefreshActions(true); };
            EditApplicationFlyout.ApplicationChanged += (o, e) => { BindApplications(); lstAvailableApplication.SelectedItem = e.Application; };

            if (ApplicationManager.Instance.FinishedLoading) { BindApplications(); }
            ApplicationManager.Instance.OnLoadApplicationsCompleted += (o, e) => { this.Dispatcher.Invoke(BindApplications); };
        }


        ObservableCollection<ActionInfo> ActionInfos = new ObservableCollection<ActionInfo>();
        private ObservableCollection<IApplication> _applications = new ObservableCollection<IApplication>();
        public static event ApplicationChangedEventHandler ShowEditApplicationFlyout;
        private Task<ActionInfo> _task;
        CancellationTokenSource _cancelTokenSource;
        private bool _selecteNewestItem;

        public class ActionInfo : INotifyPropertyChanged
        {

            public ActionInfo(string actionName, string description, string gestureName, bool isEnabled)
            {
                IsEnabled = isEnabled;
                ActionName = actionName;
                Description = description;
                GestureName = gestureName;
            }
            private bool isEnabled;
            private string _gestureName;

            public bool IsEnabled
            {
                get
                {
                    return isEnabled;
                }

                set { SetProperty(ref isEnabled, value); }
            }

            public string GestureName
            {
                get { return _gestureName; }
                set { SetProperty(ref _gestureName, value); }
            }

            public string ActionName { get; set; }

            public string Description { get; set; }


            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
            {
                if (object.Equals(storage, value)) return;

                storage = value;
                this.OnPropertyChanged(propertyName);
            }
        }
        private async void cmdEditAction_Click(object sender, RoutedEventArgs e)
        {
            // Make sure at least one item is selected
            if (lstAvailableActions.SelectedItems.Count == 0)
            {
                var mySettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    // NegativeButtonText = "Go away!",
                    // FirstAuxiliaryButtonText = "Cancel",
                    ColorScheme = MetroDialogColorScheme.Accented //: MetroDialogColorScheme.Theme
                };

                MessageDialogResult result = await UIHelper.GetParentWindow(this).ShowMessageAsync("请选择", "编辑前需要先选择一项动作 ", MessageDialogStyle.Affirmative, mySettings);
                // MessageBox.Show("You must select an item before editing", "Please Select an Item", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Get first item selected, associated action, and selected application
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            IAction selectedAction = null;
            IApplication selectedApplication = null;
            string selectedGesture = null;

            selectedApplication = lstAvailableApplication.SelectedItem as IApplication;

            if (selectedApplication == null)
                // Select action from global application list
                selectedAction = ApplicationManager.Instance.GetGlobalApplication().Actions.FirstOrDefault(a => a.Name == selectedItem.ActionName);
            else
                // Select action from selected application list
                selectedAction = selectedApplication.Actions.FirstOrDefault(a => a.Name == selectedItem.ActionName);
            if (selectedAction == null) return;
            // Get currently assigned gesture
            selectedGesture = selectedAction.GestureName;

            // Set current application, current action, and current gestures
            ApplicationManager.Instance.CurrentApplication = selectedApplication;
            GestureManager.Instance.GestureName = selectedGesture;

            ActionDialog actionDialog = new ActionDialog(this, selectedAction, selectedApplication);
            actionDialog.ShowDialog();
        }

        private async void cmdDeleteAction_Click(object sender, RoutedEventArgs e)
        {
            // Verify that we have an item selected
            if (lstAvailableActions.SelectedItems.Count == 0)
            {
                await UIHelper.GetParentWindow(this).ShowMessageAsync("未选择项目", "删除前需要先选择一项 ", MessageDialogStyle.Affirmative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    ColorScheme = MetroDialogColorScheme.Accented
                });
                // MessageBox.Show("Please select and item before trying to delete.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Confirm user really wants to delete selected items
            if (await UIHelper.GetParentWindow(this).ShowMessageAsync("删除确认", "确定要删除这个动作吗？", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
            {
                AffirmativeButtonText = "确定",
                NegativeButtonText = "取消",
                ColorScheme = MetroDialogColorScheme.Accented
            }) != MessageDialogResult.Affirmative) return;


            // Loop through selected actions
            for (int i = lstAvailableActions.SelectedItems.Count - 1; i >= 0; i--)
            {
                // Grab selected item
                ActionInfo selectedAction = lstAvailableActions.SelectedItems[i] as ActionInfo;

                // Get the name of the action
                string strActionName = selectedAction.ActionName;

                IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;

                selectedApp.RemoveAction(selectedApp.Actions.FirstOrDefault(a => a.Name.Equals(strActionName, StringComparison.Ordinal)));

            }
            RefreshActions(false);
            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();
        }

        private void lstAvailableActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableRelevantButtons();
        }
        //如果有全选需求，再分别选择：界面+保存的数据
        private void ActionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ActionInfo actionInfo = UIHelper.GetParentDependencyObject<ListBoxItem>(sender as CheckBox).Content as ActionInfo;
            if (actionInfo == null) return;
            IApplication app = lstAvailableApplication.SelectedItem as IApplication;
            if (app == null) return;
            ApplicationManager.Instance.GetAnyDefinedAction(actionInfo.ActionName, app.Name).IsEnabled = (sender as CheckBox).IsChecked.Value;
            ApplicationManager.Instance.SaveApplications();
        }





        private void btnAddAction_Click(object sender, RoutedEventArgs e)
        {
            if (GestureManager.Instance.Gestures.Length == 0)
            {
                UIHelper.GetParentWindow(this).ShowMessageAsync("无可用手势", "添加动作前需要先添加至少一项手势 ", MessageDialogStyle.Affirmative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    ColorScheme = MetroDialogColorScheme.Accented
                });
                return;
            }
            ActionDialog actionDialog = new ActionDialog(this, lstAvailableApplication.SelectedItem as IApplication);
            actionDialog.Show();
        }



        private void BindApplications()
        {
            CopyActionMenuItem.Items.Clear();
            _applications.Clear();

            // Add global actions to global applications group
            var userApplications =
                ApplicationManager.Instance.Applications.Where(app => (app is UserApplication)).OrderBy(app => app.Name);
            var globalApplication = ApplicationManager.Instance.GetAllGlobalApplication();

            foreach (var app in globalApplication.Union(userApplications))
            {
                _applications.Add(app);

                MenuItem menuItem = new MenuItem() { Header = app.Name };
                menuItem.Click += CopyActionMenuItem_Click;
                CopyActionMenuItem.Items.Add(menuItem);
            }
        }

        private void RefreshActions(bool refreshAll)
        {
            var selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApplication == null) return;
            if (refreshAll)
            {
                if (_task != null && _task.Status.HasFlag(TaskStatus.Running))
                {
                    _cancelTokenSource.Cancel();
                    _task.Wait(500);
                }
                ActionInfos.Clear();
                AddActionsToGroup(selectedApplication.Actions);
            }
            else
            {
                _selecteNewestItem = true;
                var newApp =
                    selectedApplication.Actions.Where(
                        a => !ActionInfos.Any
                            (ai => ai.ActionName.Equals(a.Name, StringComparison.Ordinal))).ToList();
                var deletedApp =
                    ActionInfos.Where(
                        ai =>
                            !selectedApplication.Actions.Any(a => a.Name.Equals(ai.ActionName, StringComparison.Ordinal)))
                        .ToList();

                if (newApp.Count == 1 && deletedApp.Count == 1)
                {
                    var newActionInfo = Action2ActionInfo(newApp[0]);
                    ActionInfos[ActionInfos.IndexOf(deletedApp[0])] = newActionInfo;
                    RefreshGroup(newApp[0].GestureName);
                    SelectAction(newActionInfo);
                }
                else
                {
                    foreach (ActionInfo ai in deletedApp)
                        ActionInfos.Remove(ai);
                    AddActionsToGroup(newApp);
                }
            }
        }
        private void AddActionsToGroup(List<IAction> actions)
        {
            _cancelTokenSource = new CancellationTokenSource();

            _task = new Task<ActionInfo>(() =>
            {
                Thread.Sleep(20);
                ActionInfo actionInfo = null;
                foreach (Applications.Action currentAction in actions)
                {
                    actionInfo = Action2ActionInfo(currentAction);

                    if (_cancelTokenSource.IsCancellationRequested) break;

                    lstAvailableApplication.Dispatcher.Invoke(() =>
                    {
                        ActionInfos.Add(actionInfo);
                    });
                }
                return actionInfo;
            }, _cancelTokenSource.Token);
            _task.ContinueWith((t) =>
            {
                ActionInfo ai = t.Result;
                if (ai != null && _selecteNewestItem)
                    lstAvailableActions.Dispatcher.Invoke(() =>
                    {
                        _selecteNewestItem = false;
                        SelectAction(ai);
                    });

            });
            _task.Start();
        }

        void RefreshGroup(string gestureName)
        {
            var temp = ActionInfos.Where(ai => ai.GestureName.Equals(gestureName, StringComparison.Ordinal)).ToList();
            foreach (ActionInfo ai in temp)
            {
                int i = ActionInfos.IndexOf(ai);
                ActionInfos.Remove(ai);
                ActionInfos.Insert(i, ai);
            }
        }
        void SelectAction(ActionInfo actionInfo)
        {
            lstAvailableActions.SelectedItem = actionInfo;
            lstAvailableActions.UpdateLayout();
            lstAvailableActions.ScrollIntoView(actionInfo);
            EnableRelevantButtons();
        }

        private ActionInfo Action2ActionInfo(IAction action)
        {
            string description;
            string pluginName;
            // Ensure this action has a plugin
            if (PluginManager.Instance.PluginExists(action.PluginClass, action.PluginFilename))
            {

                // Get plugin for this action
                IPluginInfo pluginInfo =
                    PluginManager.Instance.FindPluginByClassAndFilename(action.PluginClass,
                        action.PluginFilename);

                // Feed settings to plugin
                if (!pluginInfo.Plugin.Deserialize(action.ActionSettings))
                    action.ActionSettings = pluginInfo.Plugin.Serialize();

                pluginName = pluginInfo.Plugin.Name;
                description = pluginInfo.Plugin.Description;
            }
            else
            {
                pluginName = String.Empty;
                description = "无关联动作";
            }

            return new ActionInfo(
                !String.IsNullOrEmpty(action.Name) ? action.Name : pluginName,
                description,
                action.GestureName,
                action.IsEnabled);
        }

        private void EnableRelevantButtons()
        {
            bool flag = lstAvailableActions.SelectedItems.Count == 1;
            cmdEdit.IsEnabled = flag;
            MoveDownButton.Visibility = MoveUpButton.Visibility = flag ? Visibility.Visible : Visibility.Hidden;
            cmdDelete.IsEnabled = (lstAvailableActions.SelectedItems.Count > 0);
        }

        private void availableGesturesComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            Binding bind = new Binding
            {
                Source =
                    ((UIHelper.GetParentDependencyObject<TabControl>(this)).FindName(
                        "availableGestures") as AvailableGestures).lstAvailableGestures,
                Mode = BindingMode.OneWay,
                Path = new PropertyPath("Items")
            };
            comboBox.SetBinding(ComboBox.ItemsSourceProperty, bind);

            dynamic dataContext = comboBox.DataContext;

            foreach (GestureItem item in comboBox.Items)
            {
                if (item.Name == dataContext.Name)
                    comboBox.SelectedIndex = comboBox.Items.IndexOf(item);
            }

        }


        private void availableGesturesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            Expander expander = UIHelper.GetParentDependencyObject<Expander>(sender as ComboBox);
            if (expander == null) return;

            var firstListBoxItem = UIHelper.FindVisualChild<ListBoxItem>(expander);
            if (firstListBoxItem == null) return;
            var listBoxItemParent = UIHelper.GetParentDependencyObject<StackPanel>(firstListBoxItem);
            if (listBoxItemParent == null) return;
            var listBoxItems = listBoxItemParent.Children;
            foreach (ListBoxItem listBoxItem in listBoxItems)
            {
                ActionInfo ai = listBoxItem.Content as ActionInfo;
                IApplication app = lstAvailableApplication.SelectedItem as IApplication;
                if (ai != null && ((GestureItem)e.AddedItems[0]).Name != ai.GestureName)
                {
                    if (app != null)
                    {
                        IAction action = app.Actions.First(a => a.Name.Equals(ai.ActionName, StringComparison.Ordinal));
                        ai.GestureName = action.GestureName = ((GestureItem)e.AddedItems[0]).Name;

                        ApplicationManager.Instance.SaveApplications();
                    }
                }
                else return;
            }
        }

        private void CopyActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            if (selectedItem == null) return;
            var menuItem = (MenuItem)sender;
            var targetApplication = ApplicationManager.Instance.Applications.Find(
                   a => !(a is IgnoredApplication) && a.Name == menuItem.Header.ToString().Trim());

            if (targetApplication.Actions.Exists(a => a.Name == selectedItem.ActionName))
            {
                UIHelper.GetParentWindow(this).ShowMessageAsync("此动作已存在", String.Format("在 {0} 中已经存在 {1} 动作", menuItem.Header, selectedItem.ActionName),
                    MessageDialogStyle.Affirmative, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "确定",
                        ColorScheme = MetroDialogColorScheme.Accented
                    });
                return;
            }
            IApplication currentApp = lstAvailableApplication.SelectedItem as IApplication;
            if (currentApp == null) return;
            IAction selectedAction = ApplicationManager.Instance.GetAnyDefinedAction(selectedItem.ActionName, currentApp.Name);
            if (selectedAction == null) return;
            Applications.Action newAction = new Applications.Action()
            {
                ActionSettings = selectedAction.ActionSettings,
                GestureName = selectedAction.GestureName,
                IsEnabled = selectedAction.IsEnabled,
                Name = selectedAction.Name,
                PluginClass = selectedAction.PluginClass,
                PluginFilename = selectedAction.PluginFilename
            };
            targetApplication.AddAction(selectedAction);

            if (targetApplication != currentApp)
            {
                _selecteNewestItem = true;
                lstAvailableApplication.SelectedItem = targetApplication;
            }
            ApplicationManager.Instance.SaveApplications();
        }

        private void ImportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog() { Filter = "动作文件|*.json;*.act", Title = "导入动作定义文件", CheckFileExists = true };
            if (ofdApplications.ShowDialog().Value)
            {
                int addcount = 0;
                var newApps = System.IO.Path.GetExtension(ofdApplications.FileName).Equals(".act", StringComparison.OrdinalIgnoreCase) ?
                    FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, false, true) :
                    FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(IgnoredApplication), typeof(Applications.Action) }, false);

                if (newApps != null)
                {
                    foreach (IApplication newApp in newApps)
                    {
                        if (newApp is IgnoredApplication) continue;
                        if (ApplicationManager.Instance.ApplicationExists(newApp.Name))
                        {
                            var existingApp = ApplicationManager.Instance.Applications.Find(a => a.Name == newApp.Name);
                            foreach (IAction newAction in newApp.Actions)
                            {
                                if (existingApp.Actions.Exists(action => action.Name.Equals(newAction.Name)))
                                {
                                    var result = MessageBox.Show(String.Format("在 \"{0}\" 中已经存在 \"{1}\" 动作，是否覆盖？", existingApp.Name, newAction.Name), "已存在同名动作", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        existingApp.Actions.RemoveAll(ac => ac.Name.Equals(newAction.Name));
                                        existingApp.AddAction(newAction);
                                        addcount++;
                                    }
                                    else if (result == MessageBoxResult.Cancel) goto End;
                                }
                                else
                                {
                                    existingApp.AddAction(newAction);
                                    addcount++;
                                }
                            }
                        }
                        else
                        {
                            addcount += newApp.Actions.Count;
                            ApplicationManager.Instance.AddApplication(newApp);
                        }
                    }
                }
            End:
                if (addcount != 0)
                {
                    ApplicationManager.Instance.SaveApplications();
                    BindApplications();
                    lstAvailableApplication.SelectedIndex = 0;
                }
                MessageBox.Show(String.Format("已添加 {0} 个动作", addcount), "导入完成");
            }
        }

        private void ExportAllActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog() { Filter = "动作文件|*.act", FileName = "动作文件.act", Title = "导出动作定义文件", AddExtension = true, DefaultExt = "act", ValidateNames = true };
            if (sfdApplications.ShowDialog().Value)
            {
                FileManager.SaveObject(ApplicationManager.Instance.Applications.Where(app => !(app is IgnoredApplication)).ToList(), sfdApplications.FileName, true);
            }
        }
        private void ExportEnableActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog() { Filter = "动作文件|*.act", FileName = "动作文件.act", Title = "导出当前程序中已启用的动作", AddExtension = true, DefaultExt = "act", ValidateNames = true };
            if (sfdApplications.ShowDialog().Value)
            {
                IApplication currentApp = lstAvailableApplication.SelectedItem as IApplication;
                if (currentApp != null)
                {
                    List<IApplication> exportedApp = new List<IApplication>(1);
                    if (currentApp is GlobalApplication)
                    {
                        exportedApp.Add(new GlobalApplication()
                        {
                            Actions = currentApp.Actions.Where(a => a.IsEnabled).ToList(),
                            Group = currentApp.Group,
                            IsRegEx = currentApp.IsRegEx,
                            MatchString = currentApp.MatchString,
                            MatchUsing = currentApp.MatchUsing
                        });
                    }
                    else
                    {
                        UserApplication userApplication = currentApp as UserApplication;
                        if (userApplication != null)
                            exportedApp.Add(new UserApplication()
                            {
                                Actions = userApplication.Actions.Where(a => a.IsEnabled).ToList(),
                                Group = userApplication.Group,
                                IsRegEx = userApplication.IsRegEx,
                                MatchString = userApplication.MatchString,
                                MatchUsing = userApplication.MatchUsing,
                                AllowSingleStroke = userApplication.AllowSingleStroke,
                                InterceptTouchInput = userApplication.InterceptTouchInput,
                                Name = userApplication.Name
                            });
                    }
                    FileManager.SaveObject(exportedApp, sfdApplications.FileName, true);
                }
            }
        }


        private void ActionContextMenu_Opening(object sender, RoutedEventArgs e)
        {
            CopyActionMenuItem.IsEnabled = lstAvailableActions.SelectedIndex != -1;
        }
        private void lstAvailableApplication_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            UserApplication userApplication = lstAvailableApplication.SelectedItem as UserApplication;
            if (userApplication == null)
            {
                InterceptTouchInputMenuItem.IsChecked =
                    InterceptTouchInputMenuItem.IsEnabled =
                    AllowSingleMenuItem.IsChecked =
                    AllowSingleMenuItem.IsEnabled = false;
            }
            else
            {
                AllowSingleMenuItem.IsEnabled = true;
                InterceptTouchInputMenuItem.IsEnabled = AppConfig.UiAccess;
                InterceptTouchInputMenuItem.IsChecked = userApplication.InterceptTouchInput;
                AllowSingleMenuItem.IsChecked = userApplication.AllowSingleStroke;
            }
        }

        private void InterceptTouchInputMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UserApplication selectedItem = lstAvailableApplication.SelectedItem as UserApplication;
            if (selectedItem == null) return;
            var menuItem = (MenuItem)sender;
            selectedItem.InterceptTouchInput = menuItem.IsChecked;

            ApplicationManager.Instance.SaveApplications();

        }
        private void AllowSingleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var userApplication = lstAvailableApplication.SelectedItem as UserApplication;
            if (userApplication != null)
            {
                userApplication.AllowSingleStroke = menuItem.IsChecked;
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void lstAvailableApplication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            RefreshActions(true);
            IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApp == null) return;
            EditAppButton.IsEnabled = selectedApp is UserApplication;
            EnableAllButton.IsChecked = selectedApp.Actions.All(a => a.IsEnabled);
        }

        private void EditAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowEditApplicationFlyout != null && lstAvailableApplication.SelectedItem != null)
                ShowEditApplicationFlyout(this,
                    new ApplicationChangedEventArgs(lstAvailableApplication.SelectedItem as IApplication));
        }

        private async void DeleteAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (await UIHelper.GetParentWindow(this).ShowMessageAsync("删除确认", "确定删除该程序吗，控制该程序的相关动作也将一并删除 ",
              MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
              {
                  AffirmativeButtonText = "确定",
                  NegativeButtonText = "取消",
                  ColorScheme = MetroDialogColorScheme.Accented
              }) == MessageDialogResult.Affirmative)
            {
                ApplicationManager.Instance.RemoveApplication((IApplication)lstAvailableApplication.SelectedItem);

                BindApplications();
                lstAvailableApplication.SelectedIndex = 0;
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (lstAvailableActions.SelectedItem as ActionInfo);
            var actionInfoGroup =
                  ActionInfos.Where(ai => ai.GestureName.Equals(selected.GestureName, StringComparison.Ordinal)).ToList();
            int index = actionInfoGroup.IndexOf(selected);
            if (index > 0)
            {
                ActionInfos.Move(ActionInfos.IndexOf(selected), ActionInfos.IndexOf(actionInfoGroup[index - 1]));
                RefreshGroup(selected.GestureName);
                lstAvailableActions.SelectedItem = selected;


                IApplication selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
                if (selectedApplication == null) return;

                int selectedIndex = selectedApplication.Actions.FindIndex(a => a.Name.Equals(selected.ActionName, StringComparison.Ordinal));
                int lastIndex = selectedApplication.Actions.FindLastIndex(selectedIndex - 1,
                    a => a.GestureName.Equals(selected.GestureName, StringComparison.Ordinal));

                var temp = selectedApplication.Actions[lastIndex];
                selectedApplication.Actions[lastIndex] = selectedApplication.Actions[selectedIndex];
                selectedApplication.Actions[selectedIndex] = temp;

                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (lstAvailableActions.SelectedItem as ActionInfo);
            var actionInfoGroup =
                  ActionInfos.Where(ai => ai.GestureName.Equals(selected.GestureName, StringComparison.Ordinal)).ToList();
            int index = actionInfoGroup.IndexOf(selected);
            if (index + 1 < actionInfoGroup.Count)
            {

                ActionInfos.Move(ActionInfos.IndexOf(selected), ActionInfos.IndexOf(actionInfoGroup[index + 1]));
                RefreshGroup(selected.GestureName);
                lstAvailableActions.SelectedItem = selected;


                IApplication selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
                if (selectedApplication == null) return;

                int selectedIndex = selectedApplication.Actions.FindIndex(a => a.Name.Equals(selected.ActionName, StringComparison.Ordinal));
                int nextIndex = selectedApplication.Actions.FindIndex(selectedIndex + 1,
                    a => a.GestureName.Equals(selected.GestureName, StringComparison.Ordinal));

                var temp = selectedApplication.Actions[nextIndex];
                selectedApplication.Actions[nextIndex] = selectedApplication.Actions[selectedIndex];
                selectedApplication.Actions[selectedIndex] = temp;

                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void EnableAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var toggleButton = ((ToggleButton)sender);

                IApplication app = lstAvailableApplication.SelectedItem as IApplication;
                if (app == null) return;

                app.Actions.ForEach(a => a.IsEnabled = toggleButton.IsChecked.Value);
                ApplicationManager.Instance.SaveApplications();

                foreach (ActionInfo ai in ActionInfos)
                {
                    ai.IsEnabled = toggleButton.IsChecked.Value;
                }
            }
            catch { }
        }


    }
    [ValueConversion(typeof(IApplication), typeof(string))]
    public class HeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var app = value as IApplication;
            if (app != null)
            {
                return String.Format("{0}  ( {1}个动作 )", app.Name, app.Actions.Count);
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
    public class ApplicationListHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string name = values[0] as string;
            int count = (int)values[1];
            return String.IsNullOrEmpty(name) ? String.Format("未分组 {0}程序", count) : String.Format("{0} {1}程序", name, count);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
        }
    }

    [ValueConversion(typeof(int), typeof(Visibility))]
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int index = (int)value;
            if (index == -1)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
