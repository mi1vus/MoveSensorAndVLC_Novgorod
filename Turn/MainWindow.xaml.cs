using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//-----
using System.IO.Ports;
using System.IO;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Turn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Таймер
        DispatcherTimer SensorUpdateTimer = new DispatcherTimer();
        // Таймер
        int WaitTime = 60;
        DispatcherTimer RoomEmptyTimer = new DispatcherTimer();
        // Таймер
        int ExitTime = 20;
        DispatcherTimer ExitTimer = new DispatcherTimer();

        bool DebugMode = false;

        // Порт
        string buff = "";
        bool PlayingVideo = false;
        bool SelectVideo = false;
        bool WaitPeople = false;
        bool WaitFree = false;
        string SelectedVideo = "";
        bool FlatSensor = false;
        bool RoomSensor = false;

        SerialPort com;
        object lockObj = new object();
        String indata = "";

        List<Tuple<string, string>> Buttons = new List<Tuple<string, string>>();

        Video SubWindow;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                //System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;  //ОЧЕНЬ СПОРНОЕ РЕШЕНИЕ !!!!!!!!!!
                if (File.Exists(@"D:\Projects\C#_Proj\Нижний - Проект Очередь\Sensor\logo.txt"))
                    File.Delete(@"D:\Projects\C#_Proj\Нижний - Проект Очередь\Sensor\logo.txt");

                var pars = File.ReadAllText(@"D:\Projects\C#_Proj\Нижний - Проект Очередь\Sensor\settings.txt");
                var port = pars.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("port"))
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
                com = new SerialPort(port, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

                WaitTime = int.Parse(pars.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("wait"))
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);
                ExitTime = int.Parse(pars.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("down"))
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);

                DebugMode = bool.Parse(pars.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("debug"))
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);

                var list = File.ReadAllText(@"D:\Projects\C#_Proj\Нижний - Проект Очередь\Sensor\list.txt");
                var lines = list.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var vals = line.Split(new[] { "\" "}, StringSplitOptions.RemoveEmptyEntries);
                    var text = vals[0].Substring(1);
                    var path = vals[1];
                    Buttons.Add(new Tuple<string, string>(text, path));
                }

                //PaintButtons();

                // Подписались на приход данных
                com.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);

                // Задаем интервал таймеру
                SensorUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                // Подписываемся на тики таймера
                SensorUpdateTimer.Tick += new EventHandler(SensorUpdateTimerTick);
                // Стартуем таймер
                SensorUpdateTimer.Start();

                // Задаем интервал таймеру
                RoomEmptyTimer.Interval = new TimeSpan(0, 0, 0, WaitTime, 0);
                // Подписываемся на тики таймера
                RoomEmptyTimer.Tick += new EventHandler(RoomEmptyTimerTick);

                // Задаем интервал таймеру
                ExitTimer.Interval = new TimeSpan(0, 0, 0, ExitTime, 0);
                // Подписываемся на тики таймера
                ExitTimer.Tick += new EventHandler(RoomEmptyTimerTick);

                com.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
               
        public void PaintButtons()
        {
            wrapPanel.Children.Clear();
            var green = new Color { R = 141, G = 199, B = 63 , A = 255};
            var white = new Color { R = 255, G = 255, B = 255, A = 255 };

            foreach (var butt in Buttons)
            {
                double margin = 5d;
                var button = new Button();
                button.Width = (int)(wrapPanel.ActualWidth / 2 - 2 * margin);
                button.Height = (int)(wrapPanel.ActualHeight / (Buttons.Count / 2) - margin * (Buttons.Count / 4));
                button.BorderThickness = new Thickness(0);
                button.BorderBrush = null;
                button.Background = new SolidColorBrush(green);
                button.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                button.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                button.FontSize = button.Height / 4;
                button.Foreground = new SolidColorBrush(white);
                button.Margin = new Thickness(margin);
                button.Content = butt.Item1;
                button.Click += button_Click;
                button.DataContext = butt.Item2;
                wrapPanel.Children.Add(button);
            }
        }

        private void SensorUpdateTimerTick(object sender, EventArgs e)
        {
            lock (lockObj)
            {
                var pairs = buff.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (pairs.Length == 0)
                    return;

                FlatSensor = pairs.Any(t => t.Length == 3 && t.StartsWith("1"));
                RoomSensor = pairs.Any(t => t.Length == 3 && t.EndsWith("1"));

                buff = pairs[pairs.Length - 1];
            }

            if (FlatSensor && SelectVideo  && !WaitPeople)
            {
                WaitPeople = true;
                Logo("FlatSensor && SelectVideo  && !WaitPeople && !WaitFree");
            }

            if (RoomSensor && SelectVideo && WaitPeople && !PlayingVideo && !string.IsNullOrWhiteSpace(SelectedVideo))
            {
                SubWindow.PlayVideo(SelectedVideo);
                PlayingVideo = true;
                WaitPeople = false;
                WaitFree = false;
                GoUpMessage.Visibility = Visibility.Hidden;
                NotEmptyMessage.Visibility = Visibility.Visible;
                Logo("RoomSensor && SelectVideo && WaitPeople && !WaitFree && !string.IsNullOrWhiteSpace(SelectedVideo)");
            }

            if (RoomSensor && WaitFree && SelectVideo && PlayingVideo)
            {
                WaitFree = false;
                RoomEmptyTimer.Stop();
                RoomEmptyTimer = new DispatcherTimer();
                // Задаем интервал таймеру
                RoomEmptyTimer.Interval = new TimeSpan(0, 0, 0, WaitTime, 0);
                // Подписываемся на тики таймера
                RoomEmptyTimer.Tick += new EventHandler(RoomEmptyTimerTick);
                Logo("RoomSensor && WaitFree && SelectVideo && PlayingVideo");
            }

            if (RoomSensor && SelectVideo && PlayingVideo)
            {
                ExitTimer.Stop();
                ExitTimer = new DispatcherTimer();
                // Задаем интервал таймеру
                ExitTimer.Interval = new TimeSpan(0, 0, 0, ExitTime, 0);
                // Подписываемся на тики таймера
                ExitTimer.Tick += new EventHandler(RoomEmptyTimerTick);
                Logo("RoomSensor && SelectVideo && PlayingVideo");
            }

            if (!RoomSensor && !WaitFree && SelectVideo)
            {
                WaitFree = true;
                RoomEmptyTimer.Start();
                Logo("!RoomSensor && !WaitFree && SelectVideo");
            }

            if (FlatSensor && SelectVideo && PlayingVideo && !WaitPeople && !WaitFree)
            {
                WaitFree = true;
                RoomEmptyTimer.Start();
                Logo("FlatSensor && SelectVideo && PlayingVideo && !WaitPeople && !WaitFree");
            }

            if (FlatSensor && SelectVideo && PlayingVideo)
            {
                ExitTimer.Start();
                Logo("FlatSensor && SelectVideo && PlayingVideo");
            }

            label.Content = (FlatSensor ? "1" : "0") + " " + (RoomSensor ? "1" : "0");
            SubWindow.label.Content = (FlatSensor ? "1" : "0") + " " + (RoomSensor ? "1" : "0");
            //// Чето посылаем
            //port.Write("#10\r");
        }

        private void RoomEmptyTimerTick(object sender, EventArgs e)
        {
            Logo("RoomEmptyTimerTick");
            if (SelectVideo)
            {
                WaitFree = false;
                RoomEmptyTimer.Stop();
                RoomEmptyTimer = new DispatcherTimer();
                // Задаем интервал таймеру
                RoomEmptyTimer.Interval = new TimeSpan(0, 0, 0, WaitTime, 0);
                // Подписываемся на тики таймера
                RoomEmptyTimer.Tick += new EventHandler(RoomEmptyTimerTick);

                SubWindow.StopVideo();
                PlayingVideo = false;
                SelectVideo = false;
                WaitPeople = false;
                WaitFree = false;
                SelectedVideo = "";

                GoUpMessage.Visibility = Visibility.Hidden;
                NotEmptyMessage.Visibility = Visibility.Hidden;
                Logo("RoomEmptyTimerTick - WaitFree && SelectVideo");
            }

            //// Чето посылаем
            //port.Write("#10\r");
        }

        // Пришли данные
        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (lockObj)
            {
                //System.Threading.Thread.Sleep(10);
                // Получаем пришедшие данные
                indata = com.ReadExisting();

                buff += indata;
            }
        }

        private void wrapPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PaintButtons();

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)(sender as Button)?.DataContext;
            //SubWindow.PlayVideo(path);
            SelectedVideo = path;
            SelectVideo = true;
            GoUpMessage.Visibility = Visibility.Visible;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Закрываем порт
            if (com.IsOpen)
                com.Close();
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SubWindow = new Video();
            SubWindow.Show();

            var green = new Color { R = 141, G = 199, B = 63, A = 255 };
            var white = new Color { R = 255, G = 255, B = 255, A = 255 };

            double margin = 5d;

            GoUpMessage.Width = (int)(wrapPanel.ActualWidth);
            GoUpMessage.Height = (int)(wrapPanel.ActualHeight);
            GoUpMessage.BorderThickness = new Thickness(0);
            GoUpMessage.BorderBrush = null;
            GoUpMessage.Background = new SolidColorBrush(green);
            GoUpMessage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            GoUpMessage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            GoUpMessage.FontSize = wrapPanel.ActualHeight / 4;
            GoUpMessage.Foreground = new SolidColorBrush(white);
            GoUpMessage.Margin = new Thickness(margin);
            GoUpMessage.Content = new TextBlock() { Text = "Пройдите пожалуйста наверх!", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
            GoUpMessage.Visibility = Visibility.Hidden;

            NotEmptyMessage.Width = (int)(wrapPanel.ActualWidth);
            NotEmptyMessage.Height = (int)(wrapPanel.ActualHeight);
            NotEmptyMessage.BorderThickness = new Thickness(0);
            NotEmptyMessage.BorderBrush = null;
            NotEmptyMessage.Background = new SolidColorBrush(green);
            NotEmptyMessage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            NotEmptyMessage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            NotEmptyMessage.FontSize = wrapPanel.ActualHeight / 4;
            NotEmptyMessage.Foreground = new SolidColorBrush(white);
            NotEmptyMessage.Margin = new Thickness(margin);
            NotEmptyMessage.Content = new TextBlock() { Text = "Занято, ожидайте!", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
            NotEmptyMessage.Visibility = Visibility.Hidden;

            label.Visibility = DebugMode ? Visibility.Visible : Visibility.Hidden;
            SubWindow.label.Visibility = DebugMode ? Visibility.Visible : Visibility.Hidden;
        }

        private void Logo(string msg)
        {
            var path = @"D:\Projects\C#_Proj\Нижний - Проект Очередь\Sensor\logo.txt";
            File.AppendAllText(path,DateTime.Now.ToString() + " --- " +  msg + "\r\n");
        }
    }
}
