#define COM

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
    public enum State
    {
        EmptyRoom = 1,
        PeopleUpOnFloor,
        PlayingVideo,
        PeopleDownToFlore,
    }
    
    /// <summary>
        /// Interaction logic for MainWindow.xaml
        /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        [DllImport("User32.Dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
        
        // Таймер
        DispatcherTimer SensorUpdateTimer = new DispatcherTimer();
        // Таймер
        int WaitTime = 60;
        DispatcherTimer RoomEmptyTimer = new DispatcherTimer();
        // Таймер
        int ExitTime = 20;
        DispatcherTimer ExitTimer = new DispatcherTimer();

        bool DebugMode = false;
        string Dir = @"";// @"D:\Projects\C#_Proj\Нижний - Проект Очередь\Sensor\";

        // Порт
        string buff = "";
        State RoomState;
        bool RoomEmptyTimerStarted = false;
        bool FirstFlat = false;
        //bool WaitPeople = false;
        //bool WaitFree = false;
        string SelectedVideo = "";
        private bool _FlatSensor = false;
        private bool _RoomSensor = false;

        public bool FlatSensor
        {
            get { return _FlatSensor; }
            set
            {
                if (value != _FlatSensor)
                {
                    Logo("FlatSensor = " + value.ToString());
                    _FlatSensor = value;
                }
            }
        }

        public bool RoomSensor
        {
            get { return _RoomSensor; }
            set
            {
                if (value != _RoomSensor)
                {
                    Logo("RoomSensor = " + value.ToString());
                    _RoomSensor = value;
                }
            }
        }

#if COM
        SerialPort com;
#endif
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
                if (File.Exists(Dir + "logo.txt"))
                    File.Delete(Dir + "logo.txt");

                var pars = File.ReadAllText(Dir + "settings.txt");
                var port = pars.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("port"))
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
#if COM
                com = new SerialPort(port, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
#endif

                WaitTime = int.Parse(pars.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("wait"))
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);
                ExitTime = int.Parse(pars.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("down"))
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);

                DebugMode = bool.Parse(pars.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("debug"))
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);

                var list = File.ReadAllText(Dir + "list.txt");
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
#if COM
                com.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
#endif

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

#if COM
                com.Open();
#endif                
                RoomState = State.EmptyRoom;
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
#if COM
            lock (lockObj)
            {
                var pairs = buff.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (pairs.Length == 0)
                    return;

                FlatSensor = pairs.Any(t => t.Length == 3 && t.StartsWith("1"));
                RoomSensor = pairs.Any(t => t.Length == 3 && t.EndsWith("1"));

                buff = pairs[pairs.Length - 1];
            }
#endif

            // На лесенке появился зритель
            if (FlatSensor && !string.IsNullOrWhiteSpace(SelectedVideo) && RoomState == State.EmptyRoom)
            {
                FirstFlat = true;
                RoomState = State.PeopleUpOnFloor;
                Logo("FlatSensor && !string.IsNullOrWhiteSpace(SelectedVideo) && RoomState == State.EmptyRoom");
            }
            //зритель вошел в комнату
            if (RoomSensor && !string.IsNullOrWhiteSpace(SelectedVideo) && RoomState == State.PeopleUpOnFloor)
            {
                SubWindow.PlayVideo(SelectedVideo);
                RoomState = State.PlayingVideo;
                GoUpMessage.Visibility = Visibility.Hidden;
                NotEmptyMessage.Visibility = Visibility.Visible;
                Logo("RoomSensor && !string.IsNullOrWhiteSpace(SelectedVideo) && RoomState == State.PeopleUpOnFloor");
            }
            //зритель поднялся и освободил лесенку
            if (!FlatSensor && FirstFlat)
            {
                FirstFlat = false;
                Logo("!FlatSensor && FirstFlat");
            }
            //в комнате нет движения
            if (!RoomSensor && !string.IsNullOrWhiteSpace(SelectedVideo) && RoomState == State.PlayingVideo && !RoomEmptyTimerStarted)
            {
                RoomEmptyTimerStarted = true;
                RoomEmptyTimer.Start();
                Logo("!RoomSensor && !string.IsNullOrWhiteSpace(SelectedVideo) && RoomState == State.PlayingVideo && !RoomEmptyTimerStarted");
            }
            //зрители продолжают находиться в комнате
            if (RoomSensor && RoomState == State.PlayingVideo && RoomEmptyTimerStarted)
            {
                RoomEmptyTimerStarted = false;
                RoomEmptyTimer.Stop();
                RoomEmptyTimer = new DispatcherTimer();
                // Задаем интервал таймеру
                RoomEmptyTimer.Interval = new TimeSpan(0, 0, 0, WaitTime, 0);
                // Подписываемся на тики таймера
                RoomEmptyTimer.Tick += new EventHandler(RoomEmptyTimerTick);

                ExitTimer.Stop();
                ExitTimer = new DispatcherTimer();
                // Задаем интервал таймеру
                ExitTimer.Interval = new TimeSpan(0, 0, 0, ExitTime, 0);
                // Подписываемся на тики таймера
                ExitTimer.Tick += new EventHandler(RoomEmptyTimerTick);

                Logo("RoomSensor && RoomState == State.PlayingVideo && RoomEmptyTimerStarted");
            }
            //зрители пытаются выйти
            if (!FirstFlat && FlatSensor && RoomState == State.PlayingVideo)
            {
                RoomState = State.PeopleDownToFlore;
                ExitTimer.Start();
                Logo("FlatSensor && RoomState == State.PlayingVideo");
            }

            

            label.Content = (FlatSensor ? "1" : "0") + " " + (RoomSensor ? "1" : "0");
            SubWindow.label.Content = (FlatSensor ? "1" : "0") + " " + (RoomSensor ? "1" : "0");
            //// Чето посылаем
            //port.Write("#10\r");
        }

        private void RoomEmptyTimerTick(object sender, EventArgs e)
        {
            if (true/*RoomState != State.EmptyRoom*/)
            {
                //WaitFree = false;
                RoomEmptyTimer.Stop();
                RoomEmptyTimer = new DispatcherTimer();
                // Задаем интервал таймеру
                RoomEmptyTimer.Interval = new TimeSpan(0, 0, 0, WaitTime, 0);
                // Подписываемся на тики таймера
                RoomEmptyTimer.Tick += new EventHandler(RoomEmptyTimerTick);

                ExitTimer.Stop();
                ExitTimer = new DispatcherTimer();
                // Задаем интервал таймеру
                ExitTimer.Interval = new TimeSpan(0, 0, 0, ExitTime, 0);
                // Подписываемся на тики таймера
                ExitTimer.Tick += new EventHandler(RoomEmptyTimerTick);

                SubWindow.StopVideo();
                RoomState = State.EmptyRoom;
                RoomEmptyTimerStarted = false;
                FirstFlat = false;
                SelectedVideo = null;

                GoUpMessage.Visibility = Visibility.Hidden;
                NotEmptyMessage.Visibility = Visibility.Hidden;
                Logo("RoomEmptyTimerTick");
                //Logo("RoomEmptyTimerTick - WaitFree && SelectVideo");
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
#if COM
                 indata = com.ReadExisting();
                buff += indata;
#endif
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
            GoUpMessage.Visibility = Visibility.Visible;

            RoomEmptyTimerStarted = true;
            RoomEmptyTimer.Start();
            Logo("button_Click");

            POINT p = new POINT();
            p.x = Convert.ToInt16("0");
            p.y = Convert.ToInt16("0");

            ////ClientToScreen(this.h Handle, ref p);
            SetCursorPos(p.x, p.y);
            label.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Закрываем порт
#if COM
            if (com.IsOpen)
            com.Close();
#endif
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SubWindow = new Video();
            //SubWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            //.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            SubWindow.Show();

            var green = new Color { R = 141, G = 199, B = 63, A = 255 };
            var white = new Color { R = 255, G = 255, B = 255, A = 255 };
            var red = new Color { R = 243, G = 4, B = 4, A = 255 };

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
            GoUpMessage.Content = new TextBlock() { Text = "Пройдите, пожалуйста, наверх!", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
            GoUpMessage.Visibility = Visibility.Hidden;

            NotEmptyMessage.Width = (int)(wrapPanel.ActualWidth);
            NotEmptyMessage.Height = (int)(wrapPanel.ActualHeight);
            NotEmptyMessage.BorderThickness = new Thickness(0);
            NotEmptyMessage.BorderBrush = null;
            NotEmptyMessage.Background = new SolidColorBrush(red);
            NotEmptyMessage.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            NotEmptyMessage.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            NotEmptyMessage.FontSize = wrapPanel.ActualHeight / 4;
            NotEmptyMessage.Foreground = new SolidColorBrush(white);
            NotEmptyMessage.Margin = new Thickness(margin);
            NotEmptyMessage.Content = new TextBlock() { Text = "Занято, ожидайте!", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
            NotEmptyMessage.Visibility = Visibility.Hidden;

            label.Visibility = DebugMode ? Visibility.Visible : Visibility.Hidden;
            SubWindow.label.Visibility = DebugMode ? Visibility.Visible : Visibility.Hidden;
            checkBox1.Visibility = DebugMode ? Visibility.Visible : Visibility.Hidden;
            checkBox2.Visibility = DebugMode ? Visibility.Visible : Visibility.Hidden;

            //var a = System.Windows.SystemParameters.WorkArea;
        }

        private void unclicked_Click(object sender, RoutedEventArgs e)
        {
            //string path = (string)(sender as Button)?.DataContext;
            ////SubWindow.PlayVideo(path);
            //SelectedVideo = path;
            //SelectVideo = true;
            //GoUpMessage.Visibility = Visibility.Visible;

            POINT p = new POINT();
            p.x = Convert.ToInt16("0");
            p.y = Convert.ToInt16("0");

            ////ClientToScreen(this.h Handle, ref p);
            SetCursorPos(p.x, p.y);
            label.Focus();
            //GoUpMessage.
            //Cursor.Position = new Point(400, 700);
                        ////var a = System.Windows.SystemParameters.WorkArea;
            ////Создание объекта для генерации чисел
            //Random rnd = new Random();

            ////Получить очередное (в данном случае - первое) случайное число
            //int value = rnd.Next() % Buttons.Count;
            //SubWindow.PlayVideo(path/*Buttons[value].Item2*/);
        }

        private void Logo(string msg)
        {
            var path = Dir + "logo.txt";
            File.AppendAllText(path,DateTime.Now.ToString() + " --- " +  msg + "\r\n");
        }

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            FlatSensor = true;
        }

        private void checkBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            FlatSensor = false;
        }

        private void checkBox2_Checked(object sender, RoutedEventArgs e)
        {
            RoomSensor = true;
        }

        private void checkBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            RoomSensor = false;
        }
    }
}
