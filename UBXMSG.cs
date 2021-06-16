using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace receiver
{
    public class UBXMessage
    {
        private SerialPort serialPort1;
        public string Text = null;
        public Int32 relN = 0;//положение по Х относительно базы
        public Int32 relE = 0;//положение по У относительно базы
        public UInt16 year = 0;
        public byte month = 0;
        public byte day = 0;
        public byte hour = 0;
        public byte min = 0;
        public byte sec = 0;
        public Int32 relVN = 0;//скорость по Х относительно базы
        public Int32 relVE = 0;//скорость по У относительно базы
        public Int32 gVel = 0;//длина вектора скорости
        public double headMot = 0;//курс
        public UInt32 time = 0;
        public double lon = 0;//долгота
        public double lat = 0;//широта
        public UBXMessage(SerialPort serialPort)
        {
            serialPort1 = serialPort;
        }
        public void recTimeSpeedPos()//декодирвоание скорости времени и положения
        {
            byte[] buff4 = new byte[4];
            byte[] buff2 = new byte[2];
            byte[] buff1 = new byte[1];

            byte[] str = { 0xb5, 0x62, 0x01, 0x07 };

            serialPort1.ReadTo(Encoding.ASCII.GetString(str));

            byte[] nbuff = new byte[2];
            serialPort1.Read(nbuff, 0, 2);

            serialPort1.Read(buff4, 0, 4);
            time = BitConverter.ToUInt32(buff4, 0)/10;
            Text += time.ToString() + ':';

            Text += "date:";
            serialPort1.Read(buff2, 0, 2);
            year = BitConverter.ToUInt16(buff2, 0);
            Text += year.ToString() + ':';

            serialPort1.Read(buff1, 0, 1);
            month = buff1[0];
            Text += month.ToString() + ':';

            serialPort1.Read(buff1, 0, 1);
            day = buff1[0];
            Text += day.ToString() + ':' + ':' + ':';

            serialPort1.Read(buff1, 0, 1);
            hour = buff1[0];
            Text += hour.ToString() + ':';

            serialPort1.Read(buff1, 0, 1);
            min = buff1[0];
            Text += min.ToString() + ':';

            serialPort1.Read(buff1, 0, 1);
            sec = buff1[0];
            Text += sec.ToString() + '\n';

            nbuff = null;
            nbuff = new byte[13];
            serialPort1.Read(nbuff, 0, 13);

            Text += "real coordinate:";
            serialPort1.Read(buff4, 0, 4);
            lon = (double)BitConverter.ToInt32(buff4, 0) / 10000000;
            Text += lon.ToString() + ';';

            serialPort1.Read(buff4, 0, 4);
            lat = (double)BitConverter.ToInt32(buff4, 0) / 10000000;
            Text += lat.ToString() + ';';

            nbuff = null;
            nbuff = new byte[16];
            serialPort1.Read(nbuff, 0, 16);

            Text += "speed:";
            serialPort1.Read(buff4, 0, 4);
            relVN = BitConverter.ToInt32(buff4, 0);
            Text += relVN.ToString() + ';';

            serialPort1.Read(buff4, 0, 4);
            relVE = BitConverter.ToInt32(buff4, 0);
            Text += relVE.ToString() + ';';

            nbuff = null;
            nbuff = new byte[4];
            serialPort1.Read(nbuff, 0, 4);

            serialPort1.Read(buff4, 0, 4);
            gVel = BitConverter.ToInt32(buff4, 0);
            Text += gVel.ToString() + '\n';

            Text += "course:";
            serialPort1.Read(buff4, 0, 4);
            headMot = (double)BitConverter.ToInt32(buff4, 0) / 100000;
            Text += headMot.ToString() + '\n';
        }

        public void recRelPos()//декодирование положения относительно базы
        {
            byte[] buff = new byte[4];

            Text += "relation coordinate:";
            byte[] str = { 0xb5, 0x62, 0x01, 0x3c };
            serialPort1.ReadTo(Encoding.ASCII.GetString(str));

            byte[] nbuff = new byte[10];
            serialPort1.Read(nbuff, 0, 10);

            serialPort1.Read(buff, 0, 4);
            relN = BitConverter.ToInt32(buff, 0);
            Text += relN.ToString() + ';';

            serialPort1.Read(buff, 0, 4);
            relE = BitConverter.ToInt32(buff, 0);
            Text += relE.ToString() + '\n';
        }
    }
}
