﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using InteractiveDataDisplay.WPF;
using NLog;
using NLog.Config;
using NLog.Targets.Wrappers;
using ServoPIDControl.Annotations;
using ServoPIDControl.Helper;
using ServoPIDControl.Model;

namespace ServoPIDControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [UsedImplicitly]
    public partial class MainWindow
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ArduinoCom _arduinoCom = new ArduinoCom();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            Model.PropertyChanged += ModelOnPropertyChanged;
            ServosDataGrid.AutoGeneratedColumns += ServosDataGridOnAutoGeneratedColumns;
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Model.ComPorts) when Model.ConnectedPort == null && Model.ComPorts != null:
                    Model.ConnectedPort = Model.ComPorts.FirstOrDefault();
                    break;

                case nameof(Model.CurrentGraphServo):
                    UpdateGraph();
                    break;
            }
        }

        private void UpdateGraph()
        {
            ChartGrid.Children.Clear();

            if (Model.CurrentGraphServo == null)
                return;

            // add each series with color based on index
            foreach (var (series, i) in Model.CurrentGraphServo.AllTimeSeries.Select((x, i) => (x, i)))
            {
                var lg = new LineGraph
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(255,
                        (byte) ((i & 1) != 0 ? 255 : 100),
                        (byte) ((i & 2) != 0 ? 200 : 50),
                        (byte) (i % 4 == 0 ? 255 : 50))),
                    Description = series.Name,
                    ShowMarkers = false,
                    StrokeThickness = 2,
                };
                ChartGrid.Children.Add(lg);
                lg.Plot(series.X, series.Y);
            }

            Chart.XLabelProvider = new LabelProvider();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ConnectNLogToUi();

            _arduinoCom.Model = Model;

            if (Model.ConnectedPort == null && Model.ComPorts != null)
                Model.ConnectedPort = Model.ComPorts.FirstOrDefault();

            Log.Info("Ready!\r\n");
        }

        private void ConnectNLogToUi()
        {
            var wpfTarget = new WpfRichTextBoxTarget
            {
                ControlName = LogBox.Name,
                FormName = GetType().Name,
                MaxLines = 100,
                UseDefaultRowColoringRules = true,
                AutoScroll = true,
                // ReSharper disable StringLiteralTypo
                Layout = "${processtime} [${level:uppercase=true}] " +
                         "${logger:shortName=true}: ${message} " +
                         "${exception:innerFormat=tostring:maxInnerExceptionLevel=10:separator=,:format=tostring}",
                // ReSharper restore StringLiteralTypo
            };

#if DEBUG
            var logLevel = LogLevel.Debug;
         #else   
            var logLevel = LogLevel.Info;
#endif

            var asyncWrapper = new AsyncTargetWrapper {Name = "RichTextAsync", WrappedTarget = wpfTarget};
            LogManager.Configuration.AddTarget(asyncWrapper.Name, asyncWrapper);
            LogManager.Configuration.LoggingRules.Insert(0, new LoggingRule("*", logLevel, asyncWrapper));
            LogManager.ReconfigExistingLoggers();
        }

        private void ServosDataGridOnAutoGeneratedColumns(object sender, EventArgs e)
        {
            foreach (var name in new[]
            {
                nameof(ServoPidModel.Times), nameof(ServoPidModel.SetPoints),
                nameof(ServoPidModel.Inputs), nameof(ServoPidModel.Outputs),
                nameof(ServoPidModel.AllTimeSeries)
            })
            {
                var c = ServosDataGrid.Columns.First(c2 => (string) c2.Header == name);
                ServosDataGrid.Columns.Remove(c);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _arduinoCom.Model = null;
        }

        private void ServosDataGrid_OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var servo = (ServoPidModel) ServosDataGrid.SelectedCells.FirstOrDefault().Item;
            if (Model.CurrentGraphServo != null)
                Model.CurrentGraphServo.TimePointRecorded -= CurrentGraphServoOnTimePointRecorded;

            Model.CurrentGraphServo = servo;

            if (Model.CurrentGraphServo != null)
            {
                servo.TimePointRecorded += CurrentGraphServoOnTimePointRecorded;
                Tab.TabIndex = 1;
            }
            else
            {
                Tab.TabIndex = 0;
            }
        }

        private void Tab_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Tab.TabIndex == 0)
                Model.CurrentGraphServo.TimePointRecorded -= CurrentGraphServoOnTimePointRecorded;
            else if (Model.CurrentGraphServo != null && Tab.TabIndex == 1)
                Model.CurrentGraphServo.TimePointRecorded -= CurrentGraphServoOnTimePointRecorded;
        }

        private void CurrentGraphServoOnTimePointRecorded(object sender, EventArgs e)
        {
            if (Tab.TabIndex == 1)
                UpdateGraph();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            _arduinoCom.SendCommand(Command.LoadEeprom);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _arduinoCom.SendCommand(Command.SaveEeprom);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _arduinoCom.SendCommand(Command.ResetToDefault);
        }
    }
}