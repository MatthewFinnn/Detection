using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

// Избавиться от 100% нагрузки на ЦП (готово)
// Использование Thread для ImageBox2 (готово)
// Отписаться от событий на зум (готово)
// Повторная детекция одного лица (готово)
// Избавиться от переполнения памяти (готово)
// Удаление временных изображений (готово)
// Продумать способ выбора необходимой камеры (если их несколько) (невозможно, отсутствует оборудование)
// или выдать сообщение при отсутствии камеры (готово)
// Проверить что будет, если в кадр попадет более одного лица!!!
// EigenObjectRecognizer();

namespace _1
{
    public partial class Form3 : Form
    {
        bool capTrue = false;
        Capture cap = null;
        //CascadeClassifier cascadeEye;
        CascadeClassifier cascade;
        Rectangle[] Faces = null;
        Image<Gray, Byte> grayImage;
        Image<Bgr, Byte> image;
        Image<Bgr, Byte> image1;
        int imgCnt = 0,
            imgCntX = 0,
            cadrCnt = 0;
        int FacesLength = 0;
        Rectangle rect;
        Thread th1;

        public Form3()
        {
            InitializeComponent();
            Load += Form3_Load;
            FormClosed += Form3_FormClosed;
            button1.Click += button1_Click;
            Application.Idle += Application_Idle;
        }

        void Form3_FormClosed(object sender, FormClosedEventArgs e)
        {
            button1.Text = "START";
            string appStr = Application.StartupPath;
            string s;
            string[] files = Directory.GetFiles(@"data\image\");
            foreach (string file in files)
            {
                // При закрытии удаляем захваченные кадры
                s = appStr + "\\" + file;
                File.Delete(s);
            }
        }

        void Application_Idle(object sender, EventArgs e)
        {
            image = cap.RetrieveBgrFrame();
            imageBox1.Image = image;
            if (imgCnt != imgCntX)
            {
                // Отображаем захваченные лица в ListView
                imageList1.Images.Add(image1.ToBitmap());
                if (imgCnt < 10)
                    listViewImg.Items.Add("Image000" + imgCnt, imgCnt - 1);
                else if (imgCnt >= 10 && imgCnt < 100)
                    listViewImg.Items.Add("Image00" + imgCnt, imgCnt - 1);
                else if (imgCnt >= 100 && imgCnt < 1000)
                    listViewImg.Items.Add("Image0" + imgCnt, imgCnt - 1);
                else
                    listViewImg.Items.Add("Image" + imgCnt, imgCnt - 1);
                listViewImg.Sorting = SortOrder.Descending;
                imgCntX = imgCnt;
            }
        }

        void Form3_Load(object sender, EventArgs e)
        {
            // Захватываем кадры с камеры, если камера по-умолчанию отсутствует, выводим сообщение
            try
            {
                cap = new Capture();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Камера по-умолчанию не найдена\n"+ex.Message,"ERROR");
                Application.Exit();
            }
            // Используем алгоритм каскадного распознавания образов
            cascade = new CascadeClassifier("data\\haarcascades\\haarcascade_frontalface_alt.xml");
            listViewImg.LargeImageList = imageList1;
        }

        void button1_Click(object sender, EventArgs e)
        {
            if (!capTrue)
            {
                button1.Text = "STOP";
                // Выполняем обработку кадра в новом потоке
                th1 = new Thread(z2);
                th1.Start();
            }
            else
            {
                button1.Text = "START";
            }
            capTrue = !capTrue;
        }

        void z2()
        {
            while (button1.Text != "START")
            {
                image1 = image.Clone();
                rect = image1.ROI;
                grayImage = image1.Convert<Gray, Byte>();
                // Ищем признаки лица
                Faces = cascade.DetectMultiScale(grayImage, 1.1, 3, Size.Empty, Size.Empty);
                if (Faces.Length == 0) FacesLength = 0;
                if (cadrCnt == 20) cadrCnt = 0;
                if (Faces.Length != FacesLength || (cadrCnt == 0 && FacesLength != 0))
                {
                    cadrCnt = 0;
                    FacesLength = Faces.Length;
                    foreach (var face in Faces)
                    {
                        image1.ROI = rect;
                        //Eсли есть - обводим его. Первый аргумент - координаты, второй - цвет линии, третий - толщина
                        image1.Draw(face, new Bgr(255, 0, 0), 2);
                        imageBox2.Image = image1;
                        image1.ROI = face;
                        imgCnt++;
                        // Сохраняем изображение
                        if (imgCnt < 10)
                            image1.Save("data\\image\\image000" + imgCnt + ".jpg");
                        else if (imgCnt >= 10 && imgCnt < 100)
                            image1.Save("data\\image\\image00" + imgCnt + ".jpg");
                        else if (imgCnt >= 100 && imgCnt < 1000)
                            image1.Save("data\\image\\image0" + imgCnt + ".jpg");
                        else
                            image1.Save("data\\image\\image" + imgCnt + ".jpg");
                        Thread.Sleep(100);
                    }                
                }
                Thread.Sleep(250);
                cadrCnt++;
                // Освобождаем память
                image1.Dispose();
                grayImage.Dispose();
            }
        }
    }
}
