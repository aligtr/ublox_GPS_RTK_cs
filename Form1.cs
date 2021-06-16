using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms.ToolTips;
using GMap.NET.WindowsForms.Markers;
using System.Drawing.Drawing2D;
using System.IO;

namespace receiver
{
    public partial class Form1 : Form
    {
        public string[] ReceivedMessage = null;
       
        public Form1()
        {
            InitializeComponent();

        }

        UBXMessage msg;

        StreamWriter sw = File.CreateText("coordinates_frame.csv");// Файл, где будут лежать данные от RTK
        List<PointLatLng> myRoutList = new List<PointLatLng>(); //Создаем коллекцию, для хранения точек полигона
        private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            msg.recTimeSpeedPos();//декодирование координат, скорости и времени
            msg.recRelPos();//декодирование положения относительно базы

            myRoutList.Add(new PointLatLng(msg.lat, msg.lon));
            //myRoutList = new List<PointLatLng>() { new PointLatLng(msg.lat, msg.lon) };
            string csv = string.Format("{0};{1}.{2};{3}.{4};{5};{6};{7};{8};{9};{10}.{11}", (int)msg.time, (int)msg.lat, 
                (int)((double)(msg.lat % 1) * 10000000), (int)msg.lon, (int)((double)(msg.lon % 1) * 10000000),
                (int)msg.relN, (int)msg.relE, (int)msg.relVN, 
                (int)msg.relVE, (int)msg.gVel, (int)msg.headMot, (int)((double)(msg.headMot % 1) * 100000));
            sw.WriteLine(csv);
        }

        private void Button1_Click(object sender, EventArgs e)//подключение к последовательному порту
        {
            if (comboBox1.Text.Equals("")) return;
            if (!serialPort1.IsOpen)
            {
                serialPort1.PortName = comboBox1.Text;
                serialPort1.BaudRate = 115200;
                serialPort1.Encoding = Encoding.ASCII;
                serialPort1.ReadBufferSize = 1024;
                serialPort1.ReadTimeout = 1000;
                serialPort1.Open();
            }
            else
            {
                serialPort1.Close();
                serialPort1.PortName = comboBox1.Text;
                serialPort1.BaudRate = 115200;
                serialPort1.Encoding = Encoding.ASCII;
                serialPort1.ReadBufferSize = 1024;
                serialPort1.ReadTimeout = 1000;
                serialPort1.Open();
            }
        }

        private void Button3_Click(object sender, EventArgs e)//сканирование доступных поключений
        {
            string[] ArrayComPortsNames = null;
            string ComPortName = null;
            ArrayComPortsNames = SerialPort.GetPortNames();
            comboBox1.Items.Clear();
            if (ArrayComPortsNames.Length != 0)
            {
                Array.Sort(ArrayComPortsNames);
                foreach (string s in ArrayComPortsNames)
                {
                    comboBox1.Items.Add(s);
                }
                ComPortName = ArrayComPortsNames[0];
                comboBox1.Text = ArrayComPortsNames[0];
            }
        }

        private void Button2_Click(object sender, EventArgs e)//закрытие порта
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }
        }

        private void dispInfo()//отображение данных (не карта)
        {
            pictureBox1.Refresh();
            pictureBox2.Refresh();
            pictureBox3.Refresh();
            Pen pen = new Pen(Color.Black, 8);
            Graphics g = pictureBox1.CreateGraphics();

            float c = Math.Min(pictureBox1.Width, pictureBox1.Height)-16;//отображение курса в виде компаса
            g.DrawEllipse(pen, new RectangleF(0, 0, c, c));
            pen.StartCap = LineCap.ArrowAnchor;
            pen.EndCap = LineCap.RoundAnchor;
            float x = c / 2 * (1 - (float)Math.Cos(msg.headMot + Math.PI / 2));
            float y = c / 2 * (1 - (float)Math.Sin(msg.headMot + Math.PI / 2));
            g.DrawLine(pen, x, y, c / 2, c / 2);

            g = pictureBox3.CreateGraphics();//вывод спутниковых даты и времение
            Font myFont = new Font("Arial", 20);
            string s = msg.year.ToString() + ":" + msg.month.ToString()
                + ":" + msg.day.ToString() + "\n" + msg.hour.ToString()
                + ":" + msg.min.ToString() + ":" + msg.sec.ToString();
            g.DrawString(s, myFont, Brushes.Red, new PointF(2, 2));

            g = pictureBox2.CreateGraphics();//вывод скорости
            s = ((float)msg.gVel/1000).ToString();
            g.DrawString(s, myFont, Brushes.Red, new PointF(2, 2));
            SolidBrush myBr = new SolidBrush(Color.Black);
            g.FillRectangle(myBr, 0, 45, 10 + (float)msg.gVel/100, 40);
        }

        private void button4_Click(object sender, EventArgs e)//сброс карты
        {
            gMapControl1.Overlays.Clear();
            //gMapControl1.Refresh();
            //routOverlay.Clear();
        }

        GMapOverlay routOverlay = new GMap.NET.WindowsForms.GMapOverlay();
        
        private void routUpdate(List<PointLatLng> routList)//запись локальной траектории
        {
            //GMapOverlay routOverlay = new GMap.NET.WindowsForms.GMapOverlay();
            GMapRoute robotRout = new GMapRoute(routList, "rout");
            gMapControl1.Overlays.Clear();
            robotRout.Stroke = new Pen(Brushes.DarkBlue, 10);
            //routOverlay.Routes.Add(robotRout);
            if (routOverlay.Routes.Count < 2) routOverlay.Routes.Add(new GMapRoute(routList, "rout"));
            routOverlay.Routes[0] = robotRout;
            //gMapControl1.Refresh();
            gMapControl1.Overlays.Add(routOverlay); //Выводим роут на карту
            //routOverlay.Clear();
        }   

        private void Form1_Load(object sender, EventArgs e)//инициализация начального экрана (карты)
        {
            //Настройки для компонента GMap.
            gMapControl1.Bearing = 0;

            //CanDragMap - Если параметр установлен в True,
            //пользователь может перетаскивать карту 
            ///с помощью правой кнопки мыши. 
            gMapControl1.CanDragMap = true;

            //Указываем, что перетаскивание карты осуществляется 
            //с использованием левой клавишей мыши.
            //По умолчанию - правая.
            gMapControl1.DragButton = MouseButtons.Left;

            gMapControl1.GrayScaleMode = true;

            //MarkersEnabled - Если параметр установлен в True,
            //любые маркеры, заданные вручную будет показаны.
            //Если нет, они не появятся.
            gMapControl1.MarkersEnabled = true;

            //Указываем значение максимального приближения.
            gMapControl1.MaxZoom = 25;

            //Указываем значение минимального приближения.
            gMapControl1.MinZoom = 2;

            //Устанавливаем центр приближения/удаления
            //курсор мыши.
            gMapControl1.MouseWheelZoomType =
                GMap.NET.MouseWheelZoomType.MousePositionAndCenter;

            //Отказываемся от негативного режима.
            gMapControl1.NegativeMode = false;

            //Разрешаем полигоны.
            gMapControl1.PolygonsEnabled = true;

            //Разрешаем маршруты
            gMapControl1.RoutesEnabled = true;

            //Скрываем внешнюю сетку карты
            //с заголовками.
            gMapControl1.ShowTileGridLines = false;

            //Указываем, что при загрузке карты будет использоваться 
            //18ти кратной приближение.
            gMapControl1.Zoom = 18;
            
            //Указываем что будем использовать карты Google.
            gMapControl1.MapProvider =
                GMap.NET.MapProviders.GMapProviders.GoogleMap;
            GMap.NET.GMaps.Instance.Mode =
                GMap.NET.AccessMode.ServerOnly;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gMapControl1.CacheLocation = @"cache1";
            gMapControl1.ShowCenter = false;
            //Если вы используете интернет через прокси сервер,
            //указываем свои учетные данные.
            GMap.NET.MapProviders.GMapProvider.WebProxy =
                System.Net.WebRequest.GetSystemWebProxy();
            GMap.NET.MapProviders.GMapProvider.WebProxy.Credentials =
                System.Net.CredentialCache.DefaultCredentials;

            //Устанавливаем координаты центра карты для загрузки.
            gMapControl1.Position = new GMap.NET.PointLatLng(59.7, 30.37);
                        
            msg = new UBXMessage(serialPort1);
        }

        private void button5_Click(object sender, EventArgs e)//установка центра карты в начало траектории
        {
            if (myRoutList.Count > 0)
                gMapControl1.Position = myRoutList.First();
        }

        private void timer1_Tick(object sender, EventArgs e)//обновление данных карты 
        {

            if (myRoutList.Count > 1000)
            {
                GMapRoute robotRout = new GMapRoute(myRoutList, "rout");
                robotRout.Stroke = new Pen(Brushes.DarkBlue, 10);
                routOverlay.Routes.Add(robotRout);
                myRoutList.Clear();
            }
            routUpdate(myRoutList);//.GetRange(myRoutList.Count - 2, 2)); // сброс траектории
            //if (myRoutList.Count > 10) myRoutList.Clear();
            dispInfo();
        }
    }
}
