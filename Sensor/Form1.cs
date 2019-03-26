using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//-----
using System.IO.Ports;
using System.IO;

namespace Sensor
{
    public partial class Form1 : Form
    {
        // Таймер
        Timer timer = new Timer();
        // Порт
        bool RoomEmpty = true;
        string buff;
        bool IRSensor = false;
        bool OptSensor = false;

        SerialPort com;
        object lockObj = new object();
        String indata = "";
        public Form1()
        {
            InitializeComponent();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;  //ОЧЕНЬ СПОРНОЕ РЕШЕНИЕ !!!!!!!!!!

            var pars = File.ReadAllText(@"D:\Projects\C#_Proj\Нижний - Проект Очередь\Sensor\settings.txt");
            var port = pars.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("port"))
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
            com = new SerialPort(port, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

            // Подписались на приход данных
            com.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);

            // Задаем интервал таймеру
            timer.Interval = 100;
            // Подписываемся на тики таймера
            timer.Tick += new EventHandler(timer1_Tick);
            // Стартуем таймер
            timer.Start();
            com.Open();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lock (lockObj)
            {
                var pairs = buff.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (pairs.Length == 0)
                    return;

                IRSensor = pairs.Any(t => t.Length == 3 && t.StartsWith("1"));
                OptSensor = pairs.Any(t => t.Length == 3 && t.EndsWith("1"));

                buff = pairs[pairs.Length - 1];

                label1.Text = (IRSensor ? "1":"0") + " " + (OptSensor ? "1" : "0");
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Закрываем порт
            if (com.IsOpen) com.Close();
        }

        private void axVLCPlugin21_ControlAdded(object sender, ControlEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (axVLCPlugin21.playlist.isPlaying)
                axVLCPlugin21.playlist.stop();
            axVLCPlugin21.playlist.items.clear();
            var uri = new Uri(@"C:\Users\Max\Downloads\IMG_5447.MOV");
            var convertedURI = uri.AbsoluteUri;
            axVLCPlugin21.playlist.add(convertedURI);
            axVLCPlugin21.playlist.play();
        }
    }
}
