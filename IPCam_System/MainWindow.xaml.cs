using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using VisioForge.Types;
using VisioForge.Types.OutputFormat;
using VisioForge.Types.Sources;
using VisioForge.Types.VideoEffects;
using VisioForge.Controls.VideoCapture;
using VisioForge.Controls.UI.WPF;
using System.Net;
using System.Windows.Threading;
using WMPLib;
using System.Collections.Generic;
using IPCam_System.Properties;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;

namespace IPCam_System
{
    public partial class MainWindow : Window
    {
        private int MainCamCollumn, AllCamCollumn;

        private bool EditVideoSet = true, EditCams = false, Cam1Load=false, Cam2Load=false, Cam3Load = false, Cam4Load = false, Cam5Load = false, Cam6Load = false, Cam7Load = false, Cam8Load = false, Cam9Load = false, OpenVideoPanel=true;

        private Camera cam1, cam2, cam3, cam4, cam5, cam6, cam7, cam8, cam9;

        private DispatcherTimer timer_cam1 = new DispatcherTimer();
        private DispatcherTimer timer_cam2 = new DispatcherTimer();
        private DispatcherTimer timer_cam3 = new DispatcherTimer();
        private DispatcherTimer timer_cam4 = new DispatcherTimer();
        private DispatcherTimer timer_cam5 = new DispatcherTimer();
        private DispatcherTimer timer_cam6 = new DispatcherTimer();
        private DispatcherTimer timer_cam7 = new DispatcherTimer();
        private DispatcherTimer timer_cam8 = new DispatcherTimer();
        private DispatcherTimer timer_cam9 = new DispatcherTimer();
        private DispatcherTimer PlayerTimer = new DispatcherTimer();
        private DispatcherTimer DeleterTimer = new DispatcherTimer();

        private readonly VideoCaptureCore VideoCaptureSetter=new VideoCaptureCore();
        System.Threading.Mutex mut;

        public MainWindow()
        {
            bool createdNew;
            string mutName = "IPCam_System";
            mut = new System.Threading.Mutex(true, mutName, out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Приложение уже запущено!","Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
                Environment.Exit(0);
            }
            InitializeComponent();
            DateSelectFiles.DisplayDateEnd = DateTime.Now;
        }

        private readonly SaveFileDialog screenshotSaveDialog = new SaveFileDialog()
        {
            FileName = "image.jpg",
            Filter = "JPEG|*.jpg|BMP|*.bmp|PNG|*.png|GIF|*.gif|TIFF|*.tiff",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\VisioForge\\"
        };
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //System.Windows.Forms.Application.EnableVisualStyles();
            if (ListBoxCameras_SettingVideo.SelectedItem == null)
            {
                TabVideoSet.IsEnabled = false;
            }
            Process ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.AboveNormal;
            Camers cams = DeserializeXML();
            CheckWithLoad(cams);
            if (cams != null)
            {
                foreach (Camera cam in cams.CamersList)
                {
                    ListBoxCameras_SettingVideo.Items.Add(cam);
                    ListBoxCameras.Items.Add(cam);
                }
            }
            DeleterTimer.Interval = TimeSpan.FromDays(1);
            DeleterTimer.Tick += DeleterTimer_Tick;
            DeleterTimer.Start();
            GetFiles();
            CamsComboAdder();
            CheckBeforeLoadMainWindowSettings();
            DeleteFilesAfterLoad(Settings.Default.Days);
        }

        private void DeleterTimer_Tick(object sender, EventArgs e)
        {
            DeleteFilesAfterLoad(Settings.Default.Days);
        }

        private void CheckWithLoad(Camers cams)
        {
            int changed = 0;
            int deleted = 0;
            for(int i = 0; i < cams.CamersList.Count; i++)
            {
                for(int g = 0; g < cams.CamersList.Count;g++)
                {
                    if (i != g)
                    {
                        if (cams.CamersList[i].Name == cams.CamersList[g].Name&&cams.CamersList[i].ConnectString!=cams.CamersList[g].ConnectString)
                        {
                            cams.CamersList[g].Name = cams.CamersList[g].Name + "_[cp]";
                            changed++;
                        }
                        else if (cams.CamersList[i].ConnectString == cams.CamersList[g].ConnectString)
                        {
                            if (i < g)
                            {
                                cams.CamersList.Remove(cams.CamersList[g]);
                            }
                            else
                            {
                                cams.CamersList.Remove(cams.CamersList[i]);
                            }
                            deleted++;
                        }
                    }
                }
                if (deleted > 0 || changed > 0)
                {
                    MessageBox.Show("При загрузке списка камер найдены совпадения в списке.\nУдаленных элементов списка (c повторяющейся строкой подключения): "+deleted+"\nЭлементов с изменённым наименованием: "+changed,"Информация",MessageBoxButton.OK,MessageBoxImage.Information);
                }
            }
        }

        private void SaveAfterEditCams()
        {
            if (EditVideoSet)
            {
                Serialize(ListBoxCameras_SettingVideo);
                ListBoxCameras.Items.Clear();
                Camers cams = DeserializeXML();
                foreach (Camera cam in cams.CamersList)
                {
                    ListBoxCameras.Items.Add(cam);
                }
            }
            else if(EditCams)
            {
                Serialize(ListBoxCameras);
                ListBoxCameras_SettingVideo.Items.Clear();
                Camers cams = DeserializeXML();
                foreach (Camera cam in cams.CamersList)
                {
                    ListBoxCameras_SettingVideo.Items.Add(cam);
                }
            }
        }

        private void DellCam_Click(object sender, RoutedEventArgs e)
        {
            Camera cam=null;
            if (ListBoxCameras.SelectedItem != null)
            {
                cam = (Camera)ListBoxCameras.SelectedItem;
                if (!CancelIfCamLoad(cam))
                {
                    MessageBoxResult result = new MessageBoxResult();
                    result = MessageBox.Show("Вы действительно хотите удалить данные о данной камере со следующими исходными данными?: " + "\nНазвание - " + cam.Name + "\nСтрока подключения - " + cam.ConnectString + "\nIp адрес - " + cam.Ip + "\nПорт - " + cam.Port + "\nЛогин - " + cam.Login + "\nПароль - " + cam.Password, "Удаление данных о камере", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        ListBoxCameras.Items.Remove(ListBoxCameras.SelectedItem);
                        ListBoxCameras.Items.Refresh();
                        SaveAfterEditCams();
                        CamsComboAdder();
                        GetFiles();
                    }
                }
                else
                {
                    MessageBox.Show("Нельзя удалить камеру загруженную на главном экране!","Ошибка при удалении",MessageBoxButton.OK,MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Сначала выделите запись с данными о камере", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        private void ClearFieldsAdderOrEditerCamsInfo()
        {
            CamName.Text = null;
            CamString.Text = null;
            IpText.Text = null;
            PortText.Text = null;
            Password.Text = null;
            Login.Text = null;
            ListBoxCameras.SelectedItem = null;
        }

        private void XmlSerialize(Camers cams)
        {
            XmlSerializer xml = new XmlSerializer(typeof(Camers));
            if (File.Exists("CamersList.xml"))
            {
                using (FileStream fs = new FileStream("CamersList.xml", FileMode.Truncate, FileAccess.ReadWrite))
                {
                    xml.Serialize(fs, cams);
                }
            }
            else
            {
                using (FileStream fs = new FileStream("CamersList.xml", FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    xml.Serialize(fs, cams);
                }
            }

        }

        private Camers DeserializeXML()
        {
            Camers cams = null;
            try
            {
                XmlSerializer xml = new XmlSerializer(typeof(Camers));
                using (FileStream fs = new FileStream("CamersList.xml", FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    cams = (Camers)xml.Deserialize(fs);
                }
            }
            catch { cams = null; }

            return cams;

        }

        private void AddCam_Click(object sender, RoutedEventArgs e)
        {
            if (CamName.Text != ""&& CamString.Text != ""&&IpText.Text!=""&& PortText.Text.Replace("_","").Length>=2)
            {
                if (CheckIpCam(IpText.Text))
                {
                    if (CheckNameCamera(false, CamName.Text))
                    {
                        if (!CheckConnectCam(IpText.Text, Convert.ToInt32(PortText.Text.Replace("_", ""))))
                        {
                            MessageBox.Show("Подключения к камере на текущий момент нет. Возможно данные не верны\nКамера добавлена", "Подключения к камере нет");

                        }
                        Camera cam = new Camera(CamName.Text, CamString.Text, "Auto (VLC engine)", Login.Text, Password.Text, "AVI", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "1:30", false, 0, 0, 0, 0, false, false, false, false, 100, false, false, IpText.Text, Convert.ToInt32(PortText.Text.Replace("_", "")), false, false, 0, 2, "Красный", false, false, false, true, false, "Разница двух кадров", "");
                        ListBoxCameras.Items.Add(cam);
                        ClearFieldsAdderOrEditerCamsInfo();
                        AdderAndEditorCamsGrid.Visibility = Visibility.Collapsed;
                        UnBlockIfNotAdderCamOrEditer();
                        SaveAfterEditCams();
                        ClearFieldsAdderOrEditerCamsInfo();
                        CamsComboAdder();
                    }
                    else
                    {
                        MessageBox.Show("Камера с таким наименованием уже есть!","Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Ip адрес неверен.");
                }
            }

            else
            {
                MessageBox.Show("Проверьте указанные данные!","Ошибка добавления новой камеры",MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Serialize(ListBox list)
        {
            Camers cams = new Camers();
            if (list != null)
            {
                for (int i = 0; i < list.Items.Count; i++)
                {
                    cams.CamersList.Add((Camera)list.Items[i]);
                }
                XmlSerialize(cams);
            }
        }

        private void EditCam_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxCameras.SelectedItem != null)
            {
                MessageBoxResult result = new MessageBoxResult();
                Camera cam = (Camera)ListBoxCameras.SelectedItem;
                result = MessageBox.Show("Вы действительно хотите изменить данные о камере со следующими исходными данными?:\nНазвание - "+cam.Name+"\nСтрока подключения - "+cam.ConnectString + "\nIp адрес - " + cam.Ip + "\nПорт - " + cam.Port + "\nЛогин - "+cam.Login+"\nПароль - "+cam.Password, "Изменение данных о камере", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (CamName.Text != "" && CamString.Text != ""&&IpText.Text!=""&&PortText.Text!="")
                    {
                        if (CheckIpCam(IpText.Text))
                        {
                            if(CheckNameCamera(true, CamName.Text))
                            {
                                if (!CheckConnectCam(IpText.Text, Convert.ToInt32(PortText.Text.Replace("_", ""))))
                                {
                                    MessageBox.Show("Подключения к камере на текущий момент нет. Возможно данные не верны\nИзменения сохранены", "Подключения к камере нет");
                                }
                                Camera camera = new Camera(CamName.Text, CamString.Text, cam.TypeCam, Login.Text, Password.Text, cam.OutPutType, cam.OutPutPath, cam.OutPutTime, cam.AudioEx, cam.Lightness, cam.Saturation, cam.Contrast, cam.Darkness, cam.Invert, cam.Grayscale, cam.FlipX, cam.FlipY, cam.Sound, cam.Stretch, cam.Recordtest, IpText.Text.Replace("_", ""), Convert.ToInt32(PortText.Text.Replace("_", "")), cam.MotionDecEnabled, cam.MotionHightLightEnabled, cam.MotionHightLightThres, cam.MotionFrameLimit, cam.MotionHightLightColor, cam.MotionCompare_Red, cam.MotionCompare_Green, cam.MotionCompare_Blue, cam.MotionCompare_GrayScale, cam.MotionDetectionExEnabled, cam.MotionDetetionExDetector, cam.MotionDetetionExCore);
                                ListBoxCameras.Items[ListBoxCameras.SelectedIndex] = camera;
                                SaveAfterEditCams();
                                ClearFieldsAdderOrEditerCamsInfo();
                                UnBlockIfNotAdderCamOrEditer();
                                CamsComboAdder();
                                GetFiles();
                            }
                            else
                            {
                                MessageBox.Show("Камера с таким наименованием уже есть!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Ip адрес неверен.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Проверьте указанные данные!","Ошибка при изменении",MessageBoxButton.OK,MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Не выбрано ни одной камеры", "Ошибка при изменении данных о камере", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseAdderAndEditorGrid_Click(object sender, RoutedEventArgs e)
        {
            UnBlockIfNotAdderCamOrEditer();
        }

        private void OpenEditorCamsGrid_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxCameras.SelectedItem != null)
            {
                Camera cam;
                cam = (Camera)ListBoxCameras.SelectedItem;
                if (!CancelIfCamLoad(cam))
                {
                    IpText.Text = "";
                    AddCam.Visibility = Visibility.Collapsed;
                    EditCam.Visibility = Visibility.Visible;
                    BlockIfAdderCamOrEditer();
                    if (cam != null)
                    {
                        CamName.Text = cam.Name;
                        CamString.Text = cam.ConnectString;
                        Login.Text = cam.Login;
                        Password.Text = cam.Password;
                        IpText.Text = cam.Ip;
                        PortText.Text = cam.Port.ToString();
                    }
                    AnimatorGrid(AdderAndEditorCamsGrid);
                }
                else
                {
                    MessageBox.Show("Изменение загруженной камеры на главном экране невозможно!","Изменение загруженной камеры",MessageBoxButton.OK,MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Сначала выделите запись","Предупреждение",MessageBoxButton.OK,MessageBoxImage.Information);
            }
        }

        private void OpenAdderCamsGrid_Click(object sender, RoutedEventArgs e)
        {
            AddCam.Visibility = Visibility.Visible;
            EditCam.Visibility = Visibility.Collapsed;
            ContentCams.IsEnabled = false;
            AdderAndEditorCamsGrid.Visibility = Visibility.Visible;
            AnimatorGrid(AdderAndEditorCamsGrid);
            BlockIfAdderCamOrEditer();
            ClearFieldsAdderOrEditerCamsInfo();
        }

        public void UnBlockIfNotAdderCamOrEditer()
        {
            ContentCams.IsEnabled = true;
            GridMenu.IsEnabled = true;
            ButtonOpen.IsEnabled = true;
            AdderAndEditorCamsGrid.Visibility = Visibility.Collapsed;
        }

        public void BlockIfAdderCamOrEditer()
        {
            ContentCams.IsEnabled = false;
            GridMenu.IsEnabled = false;
            ButtonOpen.IsEnabled = false;
            AdderAndEditorCamsGrid.Visibility = Visibility.Visible;
        }

        private void ListMainMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (ListMainMenu.SelectedIndex)
            {
                case 0:
                    {
                        ContentCams.Visibility = Visibility.Collapsed;
                        MainGrid.Visibility = Visibility.Visible;
                        ContentVideoGrid.Visibility = Visibility.Collapsed;
                        AnimatorGrid(MainGrid);
                        EditVideoSet = true;
                        EditCams = false;
                    }
                    break;
                case 1:
                    {
                        ContentCams.Visibility = Visibility.Visible;
                        MainGrid.Visibility = Visibility.Collapsed;
                        ContentVideoGrid.Visibility = Visibility.Collapsed;
                        EditVideoSet = false;
                        EditCams = true;
                        AnimatorGrid(ContentCams);

                    }
                    break;
                case 2:
                    {
                        ContentCams.Visibility = Visibility.Collapsed;
                        MainGrid.Visibility = Visibility.Collapsed;
                        ContentVideoGrid.Visibility = Visibility.Visible;
                        AnimatorGrid(ContentVideoGrid);
                    }
                    break;
            }
        }

        private void SaveVideoSettingsBut_Click(object sender, RoutedEventArgs e)
        {
            if (VideoCapture1.Status==VFVideoCaptureStatus.Free)
            {
                if (ListBoxCameras_SettingVideo.SelectedItem != null)
                {
                    MessageBoxResult result = new MessageBoxResult();
                    Camera cam = (Camera)ListBoxCameras_SettingVideo.SelectedItems[0];
                    if (!CancelIfCamLoad(cam))
                    {
                        result = MessageBox.Show("Вы действительно хотите изменить данные о камере?", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            if (PathOutput.Text != "" && TimeOutput.SelectedTime.HasValue)
                            {
                                Camera camera = new Camera(cam.Name, cam.ConnectString, IPCameraType.Text, cam.Login, cam.Password, OutputFormat.Text, cam.OutPutPath, TimeOutput.Text, AudioExCheck.IsChecked.Value, SliderLightness.Value, SliderSaturation.Value, SliderContrast.Value, SliderDarkness.Value, InvertCheckBox.IsChecked.Value, GreyscaleCheckBox.IsChecked.Value, FlipXCheckBox.IsChecked.Value, FlipYCheckBox.IsChecked.Value, SoundOnSetter.Value, StretchCheck.IsChecked.Value, VideoRecordTestCheck.IsChecked.Value, cam.Ip, cam.Port, MotionDetect.IsChecked.Value, HightLight.IsChecked.Value, tbMotDetHLThreshold.Value, Convert.ToInt32(edMotDetFrameInterval.Text), ComboColorHightLight.Text, cbCompareRed.IsChecked.Value, cbCompareGreen.IsChecked.Value, cbCompareBlue.IsChecked.Value, cbCompareGreyscale.IsChecked.Value, MotionDetectEx.IsChecked.Value, MotionDetectionExDetector.Text, MotionDetectionExProcessor.Text);
                                ListBoxCameras_SettingVideo.Items[ListBoxCameras_SettingVideo.SelectedIndex] = camera;
                                SaveAfterEditCams();
                                UnBlockIfNotAdderCamOrEditer();

                            }
                            else
                            {
                                MessageBox.Show("Проверьте заполнение полей", "Ошибка при сохранении настроек", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        SaveAfterEditCams();
                    }
                    else
                    {
                        MessageBox.Show("Сохранение настроек камеры загруженной на главном экране не возможно!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Не выбрано ни одной камеры", "Ошибка при изменении данных о камере", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Перед сохранением настроек выключите текущую камеру!");
            }
        }

        private void VideoCapture1_OnError(object sender, ErrorsEventArgs e)
        {
            TabVideoSet.SelectedIndex = 5;
            LogsRich.Text += e.Message+Environment.NewLine;
        }

        private void EditPathBut_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();

            System.Windows.Forms.DialogResult result = folderBrowser.ShowDialog();

            if (!string.IsNullOrWhiteSpace(folderBrowser.SelectedPath))
            {
                string files = folderBrowser.SelectedPath;
                PathOutput.Text = files.ToString();
            }
        }

        private void SliderLightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            IVFVideoEffectLightness lightness;
            var effect = VideoCapture1.Video_Effects_Get("Lightness");
            if (effect == null)
            {
                lightness = new VFVideoEffectLightness(true, (int)SliderLightness.Value);
                VideoCapture1.Video_Effects_Add(lightness);
            }
            else
            {
                lightness = effect as IVFVideoEffectLightness;
                if (lightness != null)
                {
                    lightness.Value = (int)SliderLightness.Value;
                }
            }
        }

        private void SliderContrast_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            IVFVideoEffectContrast contrast;
            var effect = VideoCapture1.Video_Effects_Get("Contrast");
            if (effect == null)
            {
                contrast = new VFVideoEffectContrast(true, (int)SliderContrast.Value);
                VideoCapture1.Video_Effects_Add(contrast);
            }
            else
            {
                contrast = effect as IVFVideoEffectContrast;
                if (contrast != null)
                {
                    contrast.Value = (int)SliderContrast.Value;
                }
            }
        }

        private void SliderSaturation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoCapture1 != null)
            {
                IVFVideoEffectSaturation saturation;
                var effect = VideoCapture1.Video_Effects_Get("Saturation");
                if (effect == null)
                {
                    saturation = new VFVideoEffectSaturation((int)SliderSaturation.Value);
                    VideoCapture1.Video_Effects_Add(saturation);
                }
                else
                {
                    saturation = effect as IVFVideoEffectSaturation;
                    if (saturation != null)
                    {
                        saturation.Value = (int)SliderSaturation.Value;
                    }
                }
            }
        }

        private void SliderDarkness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            IVFVideoEffectDarkness darkness;
            var effect = VideoCapture1.Video_Effects_Get("Darkness");
            if (effect == null)
            {
                darkness = new VFVideoEffectDarkness(true, (int)SliderDarkness.Value);
                VideoCapture1.Video_Effects_Add(darkness);
            }
            else
            {
                darkness = effect as IVFVideoEffectDarkness;
                if (darkness != null)
                {
                    darkness.Value = (int)SliderDarkness.Value;
                }
            }
        }

        private void GreyscaleCheckBox_Click(object sender, RoutedEventArgs e)
        {
            IVFVideoEffectGrayscale grayscale;
            var effect = VideoCapture1.Video_Effects_Get("Grayscale");
            if (effect == null)
            {
                grayscale = new VFVideoEffectGrayscale(GreyscaleCheckBox.IsChecked == true);
                VideoCapture1.Video_Effects_Add(grayscale);
            }
            else
            {
                grayscale = effect as IVFVideoEffectGrayscale;
                if (grayscale != null)
                {
                    grayscale.Enabled = GreyscaleCheckBox.IsChecked == true;
                }
            }
        }

        private void InvertCheckBox_Click(object sender, RoutedEventArgs e)
        {
            IVFVideoEffectInvert invert;
            var effect = VideoCapture1.Video_Effects_Get("Invert");
            if (effect == null)
            {
                invert = new VFVideoEffectInvert(InvertCheckBox.IsChecked == true);
                VideoCapture1.Video_Effects_Add(invert);
            }
            else
            {
                invert = effect as IVFVideoEffectInvert;
                if (invert != null)
                {
                    invert.Enabled = InvertCheckBox.IsChecked == true;
                }
            }
        }

        private void ListBoxCameras_SettingVideo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoCapture1.Stop();
            Camera cam = new Camera();
            if (ListBoxCameras_SettingVideo.SelectedItem != null)
            {
                cam = (Camera)ListBoxCameras_SettingVideo.SelectedItem;
                IPCameraType.Text = cam.TypeCam;
                AudioExCheck.IsChecked = cam.AudioEx;
                OutputFormat.Text = cam.OutPutType;
                PathOutput.Text = cam.OutPutPath;
                TimeOutput.SelectedTime = System.DateTime.Parse(cam.OutPutTime);
                SliderLightness.Value = cam.Lightness;
                SliderContrast.Value = cam.Contrast;
                SliderSaturation.Value = cam.Saturation;
                SliderDarkness.Value = cam.Darkness;
                GreyscaleCheckBox.IsChecked = cam.Grayscale;
                InvertCheckBox.IsChecked = cam.Invert;
                FlipXCheckBox.IsChecked = cam.FlipX;
                FlipYCheckBox.IsChecked = cam.FlipY;
                SoundOnSetter.Value = cam.Sound;
                StretchCheck.IsChecked = cam.Stretch;
                VideoRecordTestCheck.IsChecked = cam.Recordtest;
                MotionDetect.IsChecked = cam.MotionDecEnabled;
                HightLight.IsChecked = cam.MotionHightLightEnabled;
                tbMotDetHLThreshold.Value = cam.MotionHightLightThres;
                ComboColorHightLight.Text = cam.MotionHightLightColor;
                cbCompareRed.IsChecked = cam.MotionCompare_Red;
                cbCompareGreen.IsChecked = cam.MotionCompare_Green;
                cbCompareBlue.IsChecked = cam.MotionCompare_Blue;
                cbCompareGreyscale.IsChecked = cam.MotionCompare_GrayScale;
                edMotDetFrameInterval.Text = cam.MotionFrameLimit.ToString();
                MotionDetectionExDetector.Text = cam.MotionDetetionExDetector;
                MotionDetectionExProcessor.Text = cam.MotionDetetionExCore;
                TabVideoSet.IsEnabled = true;
            }
            else
            {
                TabVideoSet.IsEnabled = false;
            }
        }

        private void CamLoadonSettings_Click(object sender, RoutedEventArgs e)
        {
            if (VideoCapture1.Status==VFVideoCaptureStatus.Free)
            {
                Camera cam = new Camera();
                if (ListBoxCameras_SettingVideo.SelectedItem != null)
                {
                    cam = (Camera)ListBoxCameras_SettingVideo.SelectedItem;
                    if (!CancelIfCamLoad(cam))
                    {
                        if (CheckConnectCam(cam.Ip, cam.Port))
                        {
                            ListBoxCameras_SettingVideo.IsEnabled = false;
                            VideoCapture1.Video_Effects_Enabled = true;
                            VideoCapture1.Video_Effects_Clear();
                            VideoCapture1.IP_Camera_Source = new IPCameraSourceSettings
                            {
                                URL = cam.ConnectString,
                                Login = cam.Login,
                                Password = cam.Password,
                            };
                            switch (IPCameraType.SelectedIndex)
                            {
                                case 0:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.Auto_VLC;
                                    }
                                    break;
                                case 1:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.Auto_FFMPEG;
                                    }
                                    break;
                                case 2:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.Auto_LAV;
                                    }
                                    break;
                                case 3:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.RTSP_HTTP_FFMPEG;
                                    }
                                    break;
                                case 4:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.HTTP_FFMPEG;
                                    }
                                    break;
                                case 5:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.MMS_WMV;
                                    }
                                    break;
                                case 6:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.RTSP_UDP_FFMPEG;
                                    }
                                    break;
                                case 7:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.RTSP_TCP_FFMPEG;
                                    }
                                    break;
                                case 8:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.RTSP_HTTP_FFMPEG;
                                    }
                                    break;
                                case 9:
                                    {
                                        VideoCapture1.IP_Camera_Source.Type = VFIPSource.HTTP_MJPEG_LowLatency;
                                    }
                                    break;
                            }
                            LoadCamOnSet(cam.Name + " [ТЕСТОВАЯ]");
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                        else
                        {
                            MessageBox.Show("Нет соединения с камерой. Проверьте данные", "Нет соединения с камерой");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Включение камеры загруженной на главном экране не возможно!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }                    
                }
            }
            else
            {
                MessageBox.Show("Камера уже включена!");
            }
            
        }

        public void LoadCamOnSet(string camname)
        {
            if (AudioExCheck.IsChecked == true)
            {
                VideoCapture1.Audio_PlayAudio = true;
                VideoCapture1.IP_Camera_Source.AudioCapture = true;
            }
            else
            {
                VideoCapture1.Audio_RecordAudio = false;
                VideoCapture1.Audio_PlayAudio = false;
                VideoCapture1.IP_Camera_Source.AudioCapture = false;
            }
            //Тестовая запись
            if (VideoRecordTestCheck.IsChecked==true)
            {
                SetOutPutFormatVideoAndSetCapture(VideoCapture1, OutputFormat.Text, PathOutput.Text, AudioExCheck.IsChecked.Value,camname);
            }
            else
            {
                VideoCapture1.Mode = VFVideoCaptureMode.IPPreview;
                //VideoCapture1.Audio_PlayAudio = true;
            }
            if (MotionDetect.IsChecked == true)
            {
                MotionDetection(VideoCapture1, MotionDetect.IsChecked.Value, cbCompareRed.IsChecked.Value, cbCompareGreen.IsChecked.Value, cbCompareBlue.IsChecked.Value, cbCompareGreyscale.IsChecked.Value, HightLight.IsChecked.Value, (int)tbMotDetHLThreshold.Value, Convert.ToInt32(edMotDetFrameInterval.Text), ComboColorHightLight.Text);
            }
            else if (MotionDetectEx.IsChecked==true)
            {
                MotionDetectionEx(VideoCapture1, MotionDetectionExDetector.Text, MotionDetectionExProcessor.Text);
                VideoCapture1.Motion_Detection = null;
            }
            else
            {
                VideoCapture1.Motion_Detection = null;
                VideoCapture1.Motion_DetectionEx = null;
            }
            if (StretchCheck.IsChecked == true)
            {
                VideoCapture1.Video_Renderer.StretchMode = VFVideoRendererStretchMode.Stretch;
            }
            VideoCapture1.Video_Renderer.Video_Renderer = VFVideoRendererWPF.WPF;
            VideoCapture1.Video_Renderer_Update();
            LogsRich.Text = "";
            VideoCapture1.Start();
            ConfigureVideoEffects();
        }

        private static void SetOutPutFormatVideoAndSetCapture(VideoCapture video, string format, string path,bool audioex, string camname)
        {
            video.Mode = VFVideoCaptureMode.IPCapture;
            video.Audio_RecordAudio = audioex;
            switch (format)
            {
                case "AVI":
                    {
                        video.Output_Format = new VFAVIOutput();
                        video.Output_Filename = path + "\\" + DateTime.Now.ToString("dd/MM/yyyy") + "\\"+ camname + " [" + DateTime.Now.ToString("HH.mm.ss") + "].avi";
                    }
                    break;
                case "MKV (Matroska)":
                    {
                        video.Output_Format = new VFMKVv1Output();
                        video.Output_Filename = path + "\\" + DateTime.Now.ToString("dd/MM/yyyy") + "\\" + camname + " [" + DateTime.Now.ToString("HH.mm.ss") + "].wkv";
                    }
                    break;
                case "WMV (Windows Media Video)":
                    {
                        video.Output_Format = new VFWMVOutput();
                        video.Output_Filename = path + "\\" + DateTime.Now.ToString("dd/MM/yyyy") + "\\" + camname + " [" + DateTime.Now.ToString("HH.mm.ss") + "].wmv";
                    }
                    break;
                case "MP4 v8/v10":
                    {
                        video.Output_Format = new VFMP4v8v10Output();
                        video.Output_Filename = path + "\\" + DateTime.Now.ToString("dd/MM/yyyy") + "\\" + camname + " [" + DateTime.Now.ToString("HH.mm.ss") + "].mp4";
                    }
                    break;
                case "MP4 v11":
                    {
                        video.Output_Format = new VFMP4v11Output(); 
                        //video.Output_Filename = path +"//myfile.mp4";
                        video.Output_Filename = path +"\\"+ DateTime.Now.ToString("dd/MM/yyyy") + "\\" + camname + " [" + DateTime.Now.ToString("HH.mm.ss") + "].mp4";
                    }
                    break;
            }

        }

        private static void MotionDetection(VideoCapture video,bool enabled,bool campare_red,bool compare_green,bool compare_blue,bool compare_grayscale,bool hightlight_enabled,int threshold,int frameInterval,string color)
        {
            video.Motion_Detection = new MotionDetectionSettings
            {
                Enabled = enabled,
                Compare_Red = campare_red,
                Compare_Green = compare_green,
                Compare_Blue = compare_blue,
                Compare_Greyscale = compare_grayscale,
                Highlight_Enabled = hightlight_enabled,
                Highlight_Threshold = threshold,
                FrameInterval = frameInterval,
            };
            switch (color)
            {
                case "Красный":
                    {
                        video.Motion_Detection.Highlight_Color = VFMotionCHLColor.Red;
                    }
                    break;
                case "Зелёный":
                    {
                        video.Motion_Detection.Highlight_Color = VFMotionCHLColor.Green;
                    }
                    break;
                case "Синий":
                    {
                        video.Motion_Detection.Highlight_Color = VFMotionCHLColor.Blue;
                    }
                    break;
            }
            video.MotionDetection_Update();
        }

        private static bool CheckIpCam(string ip)
        {
            bool isIPAddres = false;
            Match match = Regex.Match(ip, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            if (match.Success)
            {
                isIPAddres = true;
            }
            return isIPAddres;
        }

        private static bool CheckConnectCam(string ip,int port)
        {
            VideoCaptureCore core = new VideoCaptureCore();
            core.OnError += Core_OnError;
            return core.IP_Camera_CheckAvailable(IPAddress.Parse(ip),port,System.Net.Sockets.ProtocolType.Unspecified);
        }

        private static void Core_OnError(object sender, ErrorsEventArgs e)
        {
            //
        }

        private static void MotionDetectionEx(VideoCapture video, string detector, string core)
        {
            MotionDetectorType detectortype=new MotionDetectorType();
            MotionProcessorType processortype = new MotionProcessorType();
            switch (detector)
            {
                case "Разница двух кадров":
                    {
                        detectortype = MotionDetectorType.TwoFramesDifference;
                    }
                    break;
                case "Пользовательская разница кадров":
                    {
                        detectortype = MotionDetectorType.CustomFrameDifference;
                    }
                    break;
                case "Простое фоновое моделирование":
                    {
                        detectortype = MotionDetectorType.SimpleBackgroundModeling;
                    }
                    break;
                default:
                    detectortype = MotionDetectorType.SimpleBackgroundModeling;
                    break;
            }
            switch (core)
            {
                case "Blob подсчета объектов":
                    {
                        processortype = MotionProcessorType.BlobCountingObjects;
                    }
                    break;
                case "Обработка области движения сетки":
                    {
                        processortype = MotionProcessorType.GridMotionAreaProcessing;
                    }
                    break;
                case "Подсветка области движения":
                    {
                        processortype = MotionProcessorType.MotionAreaHighlighting;
                    }
                    break;
                case "Подсветка границ движения":
                    {
                        processortype = MotionProcessorType.MotionBorderHighlighting;
                    }
                    break;
                default:
                    processortype = MotionProcessorType.None;
                    break;
            }
            video.Motion_DetectionEx = new MotionDetectionExSettings
            {
                DetectorType=detectortype,
                ProcessorType=processortype,
            };

        }
        
        private void FlipXCheckBox_Click(object sender, RoutedEventArgs e)
        {
            IVFVideoEffectFlipRight flip;
            var effect = VideoCapture1.Video_Effects_Get("FlipRight");
            if (effect == null)
            {
                flip = new VFVideoEffectFlipRight(FlipXCheckBox.IsChecked == true);
                VideoCapture1.Video_Effects_Add(flip);
            }
            else
            {
                flip = effect as IVFVideoEffectFlipRight;
                if (flip != null)
                {
                    flip.Enabled = FlipXCheckBox.IsChecked.Value;
                }
            }
        }

        private void FlipYCheckBox_Click(object sender, RoutedEventArgs e)
        {
            IVFVideoEffectFlipDown flip;
            var effect = VideoCapture1.Video_Effects_Get("FlipDown");
            if (effect == null)
            {
                flip = new VFVideoEffectFlipDown(FlipYCheckBox.IsChecked == true);
                VideoCapture1.Video_Effects_Add(flip);
            }
            else
            {
                flip = effect as IVFVideoEffectFlipDown;
                if (flip != null)
                {
                    flip.Enabled = FlipYCheckBox.IsChecked == true;
                }
            }
        }

        private void ConfigureVideoEffects()
        {
            if (SliderLightness.Value > 0)
            {
                SliderLightness_ValueChanged(null, null);
            }

            if (SliderSaturation.Value < 255)
            {
                SliderSaturation_ValueChanged(null, null);
            }

            if (SliderContrast.Value > 0)
            {
                SliderContrast_ValueChanged(null, null);
            }

            if (SliderDarkness.Value > 0)
            {
                SliderDarkness_ValueChanged(null, null);
            }

            if (GreyscaleCheckBox.IsChecked == true)
            {
                GreyscaleCheckBox_Click(null, null);
            }

            if (InvertCheckBox.IsChecked == true)
            {
                InvertCheckBox_Click(null, null);
            }

            if (FlipXCheckBox.IsChecked == true)
            {
                FlipXCheckBox_Click(null, null);
            }

            if (FlipYCheckBox.IsChecked == true)
            {
                FlipYCheckBox_Click(null, null);
            }
        }

        private void CamunLoadonSetting_Click(object sender, RoutedEventArgs e)
        {
            ListBoxCameras_SettingVideo.IsEnabled = true;
            VideoCapture1.Stop();
            VideoCapture1.Video_Effects_Clear();
            VideoCapture1.Video_Effects_Enabled = false;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (ViewOne.IsChecked==true)
            {
                Grid.SetColumn(CamersScreen, 0);
                Grid.SetColumn(MainCam,1);
                MessageBox.Show("Изменения применены");
            }
            else
            {
                Grid.SetColumn(CamersScreen, 1);
                Grid.SetColumn(MainCam, 0);
                MessageBox.Show("Изменения применены");
            }
        }

        private void NotEnabledSpace(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void CamunLoadonScreen_Click(object sender, RoutedEventArgs e)
        {
            if (screenshotSaveDialog.ShowDialog() == true)
            {
                var filename = screenshotSaveDialog.FileName;
                var ext = Path.GetExtension(filename).ToLowerInvariant();
                switch (ext)
                {
                    case ".bmp":
                        VideoCapture1.Frame_Save(filename, VFImageFormat.BMP, 0);
                        break;
                    case ".jpg":
                        VideoCapture1.Frame_Save(filename, VFImageFormat.JPEG, 85);
                        break;
                    case ".gif":
                        VideoCapture1.Frame_Save(filename, VFImageFormat.GIF, 0);
                        break;
                    case ".png":
                        VideoCapture1.Frame_Save(filename, VFImageFormat.PNG, 0);
                        break;
                    case ".tiff":
                        VideoCapture1.Frame_Save(filename, VFImageFormat.TIFF, 0);
                        break;
                }
            }
        }

        private void SelectDetectionMod_Click(object sender, RoutedEventArgs e)
        {
            if (MotionDetect.IsChecked == true)
            {
                MotionDetectEx.IsChecked = false;
                MotionDetect.IsChecked = true;
            }
        }

        private void LoadAllCams_Click(object sender, RoutedEventArgs e)
        {
            Camers cams=new Camers();
            if (ListBoxCameras.Items.Count > 0)
            {
                for(int i = 0; i < ListBoxCameras.Items.Count; i++)
                {
                    cams.CamersList.Add((Camera)ListBoxCameras.Items[i]);
                }
            }
            else
            {
                cams = null;
            }
            if (cams != null)
            {
                if (Cam1Load || Cam2Load || Cam3Load || Cam4Load || Cam5Load || Cam6Load || Cam7Load || Cam8Load || Cam9Load)
                {
                    MessageBox.Show("Сначала выключите камеры (указанные на данный момент камеры считаются загруженными)");
                }
                else
                {
                    MessageBox.Show("Ожидайте, производится загрузка камер");
                    OnloadCamsOnMainScreenBut.IsEnabled = false;
                    LoadAllCams.IsEnabled = false;
                    ScreenMainCam.IsEnabled = false;
                    OnLoadMainVideoCap.IsEnabled = false;
                    if ((int)Settings.Default.RepiatInfoLoadCams == 0)
                    {
                        MessageBox.Show("Если настройки (данные) камеры указаны неверно, она в любом случае будет считаться загруженной (если выбрана в какой-либо секции).", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        Settings.Default.RepiatInfoLoadCams = 1;
                        Settings.Default.Save();
                    }
                    for (int i = 1; i < 9; i++)
                    {
                        switch (i)
                        {
                            case 1:
                                {
                                    foreach (Camera cam in cams.CamersList)
                                    {
                                        if (cam.Name == Cam1Combo.Text)
                                        {
                                            LoadFirstSettingsOnMainScreen(cam, Cam1_Cap);
                                            cam1 = cam;
                                            string timecam1 = cam1.OutPutTime;
                                            DateTime parsedDateTime = DateTime.Parse(timecam1);
                                            timer_cam1.Interval = TimeSpan.FromMinutes(parsedDateTime.Minute) + TimeSpan.FromHours(parsedDateTime.Hour);
                                            timer_cam1.Tick += Cam1_timer_Tick;
                                            Cam1Load = true;
                                        }
                                    }
                                }
                                break;
                            case 2:
                                {
                                    foreach (Camera cam in cams.CamersList)
                                    {
                                        if (cam.Name == Cam2Combo.Text)
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            LoadFirstSettingsOnMainScreen(cam, Cam2_Cap);
                                            cam2 = cam;
                                            string timecam2 = cam2.OutPutTime;
                                            DateTime parsedDateTime = DateTime.Parse(timecam2);
                                            timer_cam2.Interval = TimeSpan.FromMinutes(parsedDateTime.Minute) + TimeSpan.FromHours(parsedDateTime.Hour);
                                            timer_cam2.Tick += Cam2_timer_Tick;
                                            Cam2Load = true;
                                        }
                                    }
                                }
                                break;
                            case 3:
                                {
                                    foreach (Camera cam in cams.CamersList)
                                    {
                                        if (cam.Name == Cam3Combo.Text)
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            LoadFirstSettingsOnMainScreen(cam, Cam3_Cap);
                                            cam3 = cam;
                                            string timecam3 = cam3.OutPutTime;
                                            DateTime parsedDateTime = DateTime.Parse(timecam3);
                                            timer_cam3.Interval = TimeSpan.FromMinutes(parsedDateTime.Minute) + TimeSpan.FromHours(parsedDateTime.Hour);
                                            timer_cam3.Tick += Cam3_timer_Tick;
                                            Cam3Load = true;
                                        }
                                    }
                                }
                                break;
                            case 4:
                                {
                                    foreach (Camera cam in cams.CamersList)
                                    {
                                        if (cam.Name == Cam4Combo.Text)
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            LoadFirstSettingsOnMainScreen(cam, Cam4_Cap);
                                            cam4 = cam;
                                            string timecam4 = cam4.OutPutTime;
                                            DateTime parsedDateTime = DateTime.Parse(timecam4);
                                            timer_cam4.Interval = TimeSpan.FromMinutes(parsedDateTime.Minute) + TimeSpan.FromHours(parsedDateTime.Hour);
                                            timer_cam4.Tick += Cam4_timer_Tick;
                                            Cam4Load = true;
                                        }
                                    }
                                }
                                break;
                            case 5:
                                {
                                    foreach (Camera cam in cams.CamersList)
                                    {
                                        if (cam.Name == Cam5Combo.Text)
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            LoadFirstSettingsOnMainScreen(cam, Cam5_Cap);
                                            cam5 = cam;
                                            string timecam5 = cam5.OutPutTime;
                                            DateTime parsedDateTime = DateTime.Parse(timecam5);
                                            timer_cam5.Interval = TimeSpan.FromMinutes(parsedDateTime.Minute) + TimeSpan.FromHours(parsedDateTime.Hour);
                                            timer_cam5.Tick += Cam5_timer_Tick;
                                            Cam5Load = true;
                                        }
                                    }
                                }
                                break;
                            case 6:
                                {
                                    foreach (Camera cam in cams.CamersList)
                                    {
                                        if (cam.Name == Cam6Combo.Text)
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            LoadFirstSettingsOnMainScreen(cam, Cam6_Cap);
                                            cam6 = cam;
                                            string timecam6 = cam6.OutPutTime;
                                            DateTime parsedDateTime = DateTime.Parse(timecam6);
                                            timer_cam6.Interval = TimeSpan.FromMinutes(parsedDateTime.Minute) + TimeSpan.FromHours(parsedDateTime.Hour);
                                            timer_cam6.Tick += Cam6_timer_Tick;
                                            Cam6Load = true;
                                        }
                                    }
                                }
                                break;
                            case 7:
                                {
                                    foreach (Camera cam in cams.CamersList)
                                    {
                                        if (cam.Name == Cam7Combo.Text)
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            LoadFirstSettingsOnMainScreen(cam, Cam7_Cap);
                                            cam7 = cam;
                                            string timecam7 = cam7.OutPutTime;
                                            DateTime parsedDateTime = DateTime.Parse(timecam7);
                                            timer_cam7.Interval = TimeSpan.FromMinutes(parsedDateTime.Minute) + TimeSpan.FromHours(parsedDateTime.Hour);
                                            timer_cam7.Tick += Cam7_timer_Tick;
                                            Cam7Load = true;
                                        }
                                    }
                                }
                                break;
                            case 8:
                                {
                                    foreach (Camera cam in cams.CamersList)
                                    {
                                        if (cam.Name == Cam8Combo.Text)
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            LoadFirstSettingsOnMainScreen(cam, Cam8_Cap);
                                            cam8 = cam;
                                            string timecam8 = cam8.OutPutTime;
                                            DateTime parsedDateTime = DateTime.Parse(timecam8);
                                            timer_cam8.Interval = TimeSpan.FromMinutes(parsedDateTime.Minute) + TimeSpan.FromHours(parsedDateTime.Hour);
                                            timer_cam8.Tick += Cam8_timer_Tick;
                                            Cam8Load = true;
                                        }
                                    }
                                }
                                break;
                            case 9:
                                {
                                    foreach (Camera cam in cams.CamersList)
                                    {
                                        if (cam.Name == Cam9Combo.Text)
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            LoadFirstSettingsOnMainScreen(cam, Cam9_Cap);
                                            cam9 = cam;
                                            string timecam9 = cam9.OutPutTime;
                                            DateTime parsedDateTime = DateTime.Parse(timecam9);
                                            timer_cam9.Interval = TimeSpan.FromMinutes(parsedDateTime.Minute) + TimeSpan.FromHours(parsedDateTime.Hour);
                                            timer_cam9.Tick += Cam9_timer_Tick;
                                            Cam9Load = true;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    if (cam1 != null)
                    {
                        timer_cam1.Start();
                    }

                    if (cam2 != null)
                    {
                        timer_cam2.Start();
                    }

                    if (cam3 != null)
                    {
                        timer_cam3.Start();
                    }

                    if (cam4 != null)
                    {
                        timer_cam4.Start();
                    }

                    if (cam5 != null)
                    {
                        timer_cam5.Start();
                    }

                    if (cam6 != null)
                    {
                        timer_cam6.Start();
                    }

                    if (cam7 != null)
                    {
                        timer_cam7.Start();
                    }

                    if (cam8 != null)
                    {
                        timer_cam8.Start();
                    }

                    if (cam9 != null)
                    {
                        timer_cam9.Start();
                    }
                    OnloadCamsOnMainScreenBut.IsEnabled = true;
                    LoadAllCams.IsEnabled = true;
                    ScreenMainCam.IsEnabled = true;
                    OnLoadMainVideoCap.IsEnabled = true;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    BlockAndUnblockComboCams();
                }                
            }

        }

        private void Cam4_timer_Tick(object sender, EventArgs e)
        {
            TimersTicks(timer_cam4, cam4, Cam4_Cap);
        }

        private void Cam5_timer_Tick(object sender, EventArgs e)
        {
            TimersTicks(timer_cam5, cam5, Cam5_Cap);
        }

        private void Cam6_timer_Tick(object sender, EventArgs e)
        {
            TimersTicks(timer_cam6, cam6, Cam6_Cap);
        }

        private void Cam7_timer_Tick(object sender, EventArgs e)
        {
            TimersTicks(timer_cam7, cam7, Cam7_Cap);
        }

        private void Cam8_timer_Tick(object sender, EventArgs e)
        {
            TimersTicks(timer_cam8, cam8, Cam8_Cap);
        }

        private void Cam9_timer_Tick(object sender, EventArgs e)
        {
            TimersTicks(timer_cam9, cam9, Cam9_Cap);
        }

        private void Cam3_timer_Tick(object sender, EventArgs e)
        {
            TimersTicks(timer_cam3, cam3, Cam3_Cap);
        }

        private void Cam2_timer_Tick(object sender, EventArgs e)
        {
            TimersTicks(timer_cam2, cam2, Cam2_Cap);
        }

        private void Cam1_timer_Tick(object sender, EventArgs e)
        {
            TimersTicks(timer_cam1,cam1,Cam1_Cap);
        }

        private void TimersTicks(DispatcherTimer timer,Camera cam, VideoCapture video)
        {
            timer.Stop();
            video.Stop();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GetFiles();
            System.Threading.Thread.Sleep(10000);
            LoadFirstSettingsOnMainScreen(cam, video);
            timer.Start();
        }

        private void SettEffectsAllIpCameras(VideoCapture video, int contrast_value,int lightness_value, int saturation_value, int darkness_value,bool grayscale_value,bool invert_value, bool flipX_value, bool flipY_value)
        {
            if (video != null)
            {
                IVFVideoEffectContrast contrast;
                var effect = video.Video_Effects_Get("Contrast");
                if (effect == null)
                {
                    contrast = new VFVideoEffectContrast(true, contrast_value);
                    video.Video_Effects_Add(contrast);
                }
                IVFVideoEffectLightness lightness;
                var effect_two = video.Video_Effects_Get("Lightness");
                if (effect_two == null)
                {
                    lightness = new VFVideoEffectLightness(true, lightness_value);
                    video.Video_Effects_Add(lightness);
                }
                IVFVideoEffectSaturation saturation;
                var effect_three = video.Video_Effects_Get("Saturation");
                if (effect_three == null)
                {
                    saturation = new VFVideoEffectSaturation(saturation_value);
                    video.Video_Effects_Add(saturation);
                }
                IVFVideoEffectDarkness darkness;
                var effect_four = video.Video_Effects_Get("Darkness");
                if (effect_four == null)
                {
                    darkness = new VFVideoEffectDarkness(true, darkness_value);
                    video.Video_Effects_Add(darkness);
                }
                IVFVideoEffectGrayscale grayscale;
                var effect_five = video.Video_Effects_Get("Grayscale");
                if (effect_five == null)
                {
                    grayscale = new VFVideoEffectGrayscale(grayscale_value);
                    video.Video_Effects_Add(grayscale);
                }
                IVFVideoEffectInvert invert;
                var effect_six = video.Video_Effects_Get("Invert");
                if (effect_six == null)
                {
                    invert = new VFVideoEffectInvert(invert_value);
                    video.Video_Effects_Add(invert);
                }
                IVFVideoEffectFlipRight flipX;
                var effect_seven = video.Video_Effects_Get("FlipRight");
                if (effect_seven == null)
                {
                    flipX = new VFVideoEffectFlipRight(flipX_value);
                    video.Video_Effects_Add(flipX);
                }
                IVFVideoEffectFlipDown flipY;
                var effect_vosem = VideoCapture1.Video_Effects_Get("FlipDown");
                if (effect_vosem == null)
                {
                    flipY = new VFVideoEffectFlipDown(flipY_value);
                    video.Video_Effects_Add(flipY);
                }
            }
        }

        private void LoadFirstSettingsOnMainScreen(Camera cam, VideoCapture videocam)
        {
            videocam.Video_Effects_Enabled = true;
            videocam.Video_Effects_Clear();
            videocam.IP_Camera_Source = new IPCameraSourceSettings
            {
                URL = cam.ConnectString,
                Login = cam.Login,
                Password = cam.Password, 
            };
            switch (cam.TypeCam)
            {
                case "Auto (VLC engine)":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.Auto_VLC;
                    }
                    break;
                case "Auto (FFMPEG engine)":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.Auto_FFMPEG;
                    }
                    break;
                case "Auto (LAV engine)":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.Auto_LAV;
                    }
                    break;
                case "RTSP (Live555 engine)":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.RTSP_HTTP_FFMPEG;
                    }
                    break;
                case "HTTP (FFMPEG engine)":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.HTTP_FFMPEG;
                    }
                    break;
                case "MMS - WMV":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.MMS_WMV;
                    }
                    break;
                case "RTSP - UDP (FFMPEG engine)":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.RTSP_UDP_FFMPEG;
                    }
                    break;
                case "RTSP - TCP (FFMPEG engine)":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.RTSP_TCP_FFMPEG;
                    }
                    break;
                case "RTSP over HTTP (FFMPEG engine)":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.RTSP_HTTP_FFMPEG;
                    }
                    break;
                case "HTTP MJPEG Low Latency":
                    {
                        videocam.IP_Camera_Source.Type = VFIPSource.HTTP_MJPEG_LowLatency;
                    }
                    break;
            }
            if (CheckConnectCam(cam.Ip, cam.Port))
            {
                SettEffectsAllIpCameras(videocam, (int)cam.Contrast, (int)cam.Lightness, (int)cam.Saturation, (int)cam.Darkness, cam.Grayscale, cam.Invert, cam.FlipX, cam.FlipY);
                LoadLastSettingsOnMainScreen(cam,videocam);
            }
        }

        private void LoadLastSettingsOnMainScreen(Camera cam, VideoCapture videocam)
        {
            if (cam.AudioEx== true)
            {
                videocam.Audio_PlayAudio = true;
                videocam.IP_Camera_Source.AudioCapture = true;
            }
            else
            {
                videocam.Audio_RecordAudio = false;
                videocam.Audio_PlayAudio = false;
                videocam.IP_Camera_Source.AudioCapture = false;
            }
            if(videocam.Name!= "VideoCamMain")
            {
                SetOutPutFormatVideoAndSetCapture(videocam, cam.OutPutType, cam.OutPutPath, cam.AudioEx,cam.Name);
            }
            else
            {
                videocam.Mode = VFVideoCaptureMode.IPPreview;
            }
            if (cam.MotionDecEnabled == true)
            {
                MotionDetection(videocam, true, cam.MotionCompare_Red, cam.MotionCompare_Green, cam.MotionCompare_Blue, cam.MotionCompare_GrayScale, cam.MotionHightLightEnabled, Convert.ToInt32(cam.MotionHightLightThres), cam.MotionFrameLimit, cam.MotionHightLightColor);
            }
            else if (cam.MotionDetectionExEnabled == true)
            {
                MotionDetectionEx(videocam, cam.MotionDetetionExDetector,cam.MotionDetetionExCore);
                videocam.Motion_Detection = null;
            }
            else
            {
                videocam.Motion_Detection = null;
                videocam.Motion_DetectionEx = null;
            }
            if (cam.Stretch)
            {
                videocam.Video_Renderer.StretchMode = VFVideoRendererStretchMode.Stretch;
            }
            videocam.Video_Renderer.Video_Renderer = VFVideoRendererWPF.WPF;
            videocam.Start();
        }

        private void SoundOnSetter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoCapture1.Audio_OutputDevice_Volume_Set((int)SoundOnSetter.Value);
        }

        private void OnloadCamsOnMainScreenBut_Click(object sender, RoutedEventArgs e)
        {
            cam1 = cam2 = cam3 = cam4 = cam5 = cam6 = cam7 = cam8 = cam9 = null;
            Cam1_Cap.Stop();
            Cam2_Cap.Stop();
            Cam3_Cap.Stop();
            Cam4_Cap.Stop();
            Cam5_Cap.Stop();
            Cam6_Cap.Stop();
            Cam7_Cap.Stop();
            Cam8_Cap.Stop();
            Cam9_Cap.Stop();
            VideoCamMain.Stop();
            timer_cam1.Stop();
            timer_cam2.Stop();
            timer_cam3.Stop();
            timer_cam4.Stop();
            timer_cam5.Stop();
            timer_cam6.Stop();
            timer_cam7.Stop();
            timer_cam8.Stop();
            timer_cam9.Stop();
            Cam1Load = Cam2Load = Cam3Load = Cam4Load = Cam5Load = Cam6Load = Cam7Load = Cam8Load = Cam9Load = false;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GetFiles();
            BlockAndUnblockComboCams();
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            LineMedia.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void PlayerTimer_Tick(object sender, EventArgs e)
        {
            LineMedia.Value = VideoPlayer.Position.TotalSeconds;
            DurationLabel.Content = VideoPlayer.Position.Duration();
        }

        private void StartMedia_Click(object sender, RoutedEventArgs e)
        {
            PlayVideo();
        }

        private void PlayVideo()
        {
            VideoPlayer.Play();
            LineMedia.Value = 0;
            if (VideoPlayer.Source != null)
            {
                PlayerTimer.Interval = TimeSpan.FromMilliseconds(20);
                PlayerTimer.Start();
                if (VideoPlayer.HasAudio)
                {
                    VideoPlayer.Volume = VolumeSliderVideoPl.Value;
                }
                else
                {
                    VolumeSliderVideoPl.Value = 0;
                }
                PlayerTimer.Tick += PlayerTimer_Tick;
            }
        }

        private void LineMedia_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoPlayer.Position = TimeSpan.FromSeconds(LineMedia.Value);
        }

        private void ResumeMedia_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Play();
            PlayerTimer.Start();
        }

        private void PauseMedia_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Pause();
            PlayerTimer.Stop();
            VideoPlayer.Position = TimeSpan.FromSeconds(LineMedia.Value);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Serialize(ListBoxCameras);
            SaveSettingProgramm();
        }

        private void CheckBeforeLoadMainWindowSettings()
        {
            ItemCollection items = Cam1Combo.Items;
            if (items != null)
            {
                for(int i=0; i < items.Count; i++)
                {
                    if (Settings.Default.Cam1.ToString() == items[i].ToString())
                    {
                        Cam1Combo.Text = items[i].ToString();
                    }
                    if (Settings.Default.Cam2.ToString() == items[i].ToString())
                    {
                        Cam2Combo.Text = items[i].ToString();
                    }
                    if (Settings.Default.Cam3.ToString() == items[i].ToString())
                    {
                        Cam3Combo.Text = items[i].ToString();
                    }
                    if (Settings.Default.Cam4.ToString() == items[i].ToString())
                    {
                        Cam4Combo.Text = items[i].ToString();
                    }
                    if (Settings.Default.Cam5.ToString() == items[i].ToString())
                    {
                        Cam5Combo.Text = items[i].ToString();
                    }
                    if (Settings.Default.Cam6.ToString() == items[i].ToString())
                    {
                        Cam6Combo.Text = items[i].ToString();
                    }
                    if (Settings.Default.Cam7.ToString() == items[i].ToString())
                    {
                        Cam7Combo.Text = items[i].ToString();
                    }
                    if (Settings.Default.Cam8.ToString() == items[i].ToString())
                    {
                        Cam8Combo.Text = items[i].ToString();
                    }
                    if (Settings.Default.Cam9.ToString() == items[i].ToString())
                    {
                        Cam9Combo.Text = items[i].ToString();
                    }
                }
            }
            ViewOne.IsChecked = Settings.Default.View_One;
            ViewTwo.IsChecked = Settings.Default.ViewTwo;
            if (ViewOne.IsChecked.Value == true)
            {
                Grid.SetColumn(CamersScreen, 0);
                Grid.SetColumn(MainCam, 1);
            }
            else if(ViewTwo.IsChecked.Value == true)
            {
                Grid.SetColumn(CamersScreen, 1);
                Grid.SetColumn(MainCam, 0);
            }
            else
            {
                ViewOne.IsChecked = true;
                Grid.SetColumn(CamersScreen, 0);
                Grid.SetColumn(MainCam, 1);
            }

        }

        private void SaveSettingProgramm()
        {
            Settings.Default.Cam1 = Cam1Combo.Text;
            Settings.Default.Cam2 = Cam2Combo.Text;
            Settings.Default.Cam3 = Cam3Combo.Text;
            Settings.Default.Cam4 = Cam4Combo.Text;
            Settings.Default.Cam5 = Cam5Combo.Text;
            Settings.Default.Cam6 = Cam6Combo.Text;
            Settings.Default.Cam7 = Cam7Combo.Text;
            Settings.Default.Cam8 = Cam8Combo.Text;
            Settings.Default.Cam9 = Cam9Combo.Text;
            Settings.Default.View_One = ViewOne.IsChecked.Value;
            Settings.Default.ViewTwo = ViewTwo.IsChecked.Value;
            Settings.Default.Save();
        }

        private void DateSelectFiles_CalendarClosed(object sender, RoutedEventArgs e)
        {
            GetFiles();
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("Не удалось загрузить видео");
        }

        private void VolumeSliderVideoPl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoPlayer.HasAudio)
            {
                VideoPlayer.Volume = VolumeSliderVideoPl.Value;
            }
            else
            {
                if (VolumeSliderVideoPl.Value != 0)
                {
                    MessageBox.Show("Видео не содержит аудио");
                    VolumeSliderVideoPl.Value = 0;
                }
            }
        }

        private void HelpCombo1_Click(object sender, RoutedEventArgs e)
        {
            switch (StretchModePlayerCombo.SelectedIndex)
            {
                case 0:
                    {
                        MessageBox.Show("Исходный размер содержимого видеопотока сохраняется","Информация",MessageBoxButton.OK,MessageBoxImage.Information);
                    }
                    break;
                case 1:
                    {
                        MessageBox.Show("Размер содержимого видеопотока изменится для заполнения области видео (прямоугольника) без сохранения пропорций", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case 2:
                    {
                        MessageBox.Show("Размер содержимого видеопотока изменится в соответствии с размерами области видео (прямоугольника) с сохранением исходных пропорций", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case 3:
                    {
                        MessageBox.Show("Размер содержимого видеопотока изменится в соответствии с размерами области видео (прямоугольника) с сохранением исходных пропорций\nЕсли пропорции целевого прямоугольника отличаются от пропорций источника, исходное содержимое обрезается в соответствии с размерами прямоугольника (объекта назначения)", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
            }
        }

        private void HelpCombo2_Click(object sender, RoutedEventArgs e)
        {
            switch (StretchDirectPlayerCombo.SelectedIndex)
            {
                case 0:
                    {
                        MessageBox.Show("Содержимое увелиивается до размеров родительского объекта в соответствии с требованиями режима", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case 1:
                    {
                        MessageBox.Show("Содержимое масштабируется вниз только в том случае, если оно больше родительского содержимого. Если содержимое меньше, выполняется масштабирование вверх", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case 2:
                    {
                        MessageBox.Show("Содержимое масштабируется вверх только в том случае, если оно меньше родительского содержимого. Если содержимое больше, выполняется масштабирование вниз", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
            }
        }

        private void StretchDirectPlayerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (StretchDirectPlayerCombo.SelectedIndex)
            {
                case 0:
                    {
                        VideoPlayer.StretchDirection = StretchDirection.Both;
                    }
                    break;
                case 1:
                    {
                        VideoPlayer.StretchDirection = StretchDirection.DownOnly;
                    }
                    break;
                case 2:
                    {
                        VideoPlayer.StretchDirection = StretchDirection.UpOnly;
                    }
                    break;
            }
        }

        private void StretchModePlayerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (StretchModePlayerCombo.SelectedIndex)
            {
                case 0:
                    {
                        VideoPlayer.Stretch = System.Windows.Media.Stretch.None;
                    }
                    break;
                case 1:
                    {
                        VideoPlayer.Stretch = System.Windows.Media.Stretch.Fill;
                    }
                    break;
                case 2:
                    {
                        VideoPlayer.Stretch = System.Windows.Media.Stretch.Uniform;
                    }
                    break;
                case 3:
                    {
                        VideoPlayer.Stretch = System.Windows.Media.Stretch.UniformToFill;
                    }
                    break;
            }
        }

        private void SpeedPlayer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoPlayer.SpeedRatio = SpeedPlayer.Value;
        }

        private void StopMedia_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            PlayerTimer.Stop();
            DurationLabel.Content = 0;
            LineMedia.Value = 0;
            VideoPlayer.Position = TimeSpan.FromSeconds(0);
        }

        private void VideoPanelOpenButton_Click(object sender, RoutedEventArgs e)
        {
            AnimatorVideoPanel();
        }

        private void ClosePlayerPanel_Click(object sender, RoutedEventArgs e)
        {
            AnimatorVideoPanel();
        }

        private void DeleteVideoFile_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = new MessageBoxResult();
            if (ListBoxVideo.SelectedItem != null)
            {
                VideoFile file = (VideoFile)ListBoxVideo.SelectedItem;
                if (File.Exists(file.FullName))
                {
                    Uri uri = new Uri(file.FullName);
                    if (VideoPlayer.Source == uri)
                    {
                        result = MessageBox.Show("Вы действительно хотите удалить файл? Данный файл будет удалён из ресурса плеера","Удаление файла", MessageBoxButton.YesNo,MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            VideoPlayer.Source = null;
                            File.Delete(file.FullName);
                            ListBoxVideo.Items.Remove(ListBoxVideo.SelectedItem);
                        }
                    }
                    else
                    {
                        result = MessageBox.Show("Вы действительно хотите удалить файл?", "Удаление файла", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            VideoPlayer.Source = null;
                            File.Delete(file.FullName);
                            ListBoxVideo.Items.Remove(ListBoxVideo.SelectedItem);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Файл из списка был удалён ранее");
                    ListBoxVideo.Items.Remove(ListBoxVideo.SelectedItem);
                    GetFiles();
                }
            }
        }

        private bool CancelIfCamLoad(Camera cam)
        {
            bool Cancel = false;
            if (cam.Name == Cam1Combo.Text && Cam1Load)
            {
                Cancel = true;
            }
            else if (cam.Name == Cam2Combo.Text && Cam2Load)
            {
                Cancel = true;
            }
            else if (cam.Name == Cam3Combo.Text && Cam3Load)
            {
                Cancel = true;
            }
            else if (cam.Name == Cam4Combo.Text && Cam4Load)
            {
                Cancel = true;
            }
            else if (cam.Name == Cam5Combo.Text && Cam5Load)
            {
                Cancel = true;
            }
            else if (cam.Name == Cam6Combo.Text && Cam6Load)
            {
                Cancel = true;
            }
            else if (cam.Name == Cam7Combo.Text && Cam7Load)
            {
                Cancel = true;
            }
            else if (cam.Name == Cam8Combo.Text && Cam8Load)
            {
                Cancel = true;
            }
            else if (cam.Name == Cam9Combo.Text && Cam9Load)
            {
                Cancel = true;
            }
            return Cancel;
        }

        private void OpenPanelSelectDateDeleteB_Click(object sender, RoutedEventArgs e)
        {
            GridSelectSettingDeleteFiles.Visibility = Visibility.Visible;
            AnimatorGrid(GridSelectSettingDeleteFiles);
            DaysScroll.Value=Settings.Default.Days;
            MainGrid.IsEnabled = false;
            ContentCams.IsEnabled = false;
            AdderAndEditorCamsGrid.IsEnabled = false;
            ContentVideoGrid.IsEnabled = false;
            ListMainMenu.IsEnabled = false;
            ButtonClose.IsEnabled = false;
            OpenPanelSelectDateDeleteB.IsEnabled = false;
        }

        private void LoadOnMainCamClickedCam(VideoCapture video,Camera cam)
        {
            if (video.IP_Camera_Source != null)
            {
                VideoCamMain.Stop();
                LoadFirstSettingsOnMainScreen(cam, VideoCamMain);
            }
        }

        private void ClosePaneSelectDaysToFileDelete_Click(object sender, RoutedEventArgs e)
        {
            GridSelectSettingDeleteFiles.Visibility = Visibility.Collapsed;
            MainGrid.IsEnabled = true;
            ContentCams.IsEnabled = true;
            AdderAndEditorCamsGrid.IsEnabled = true;
            ContentVideoGrid.IsEnabled = true;
            ListMainMenu.IsEnabled = true;
            ButtonClose.IsEnabled = true;
            OpenPanelSelectDateDeleteB.IsEnabled = true;
        }

        private void ApplyDaysBut_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Days = (int)DaysScroll.Value;
            Settings.Default.Save();
            MessageBox.Show("Изменения сохранены");
            GridSelectSettingDeleteFiles.Visibility = Visibility.Collapsed;
            MainGrid.IsEnabled = true;
            ContentCams.IsEnabled = true;
            AdderAndEditorCamsGrid.IsEnabled = true;
            ContentVideoGrid.IsEnabled = true;
            ListMainMenu.IsEnabled = true;
            ButtonClose.IsEnabled = true;
            OpenPanelSelectDateDeleteB.IsEnabled = true;
        }

        private void Cam2Combo_DropDownClosed(object sender, EventArgs e)
        {
            if ((Cam2Combo.SelectedItem == Cam1Combo.SelectedItem || Cam2Combo.SelectedItem == Cam3Combo.SelectedItem || Cam2Combo.SelectedItem == Cam4Combo.SelectedItem || Cam2Combo.SelectedItem == Cam5Combo.SelectedItem || Cam2Combo.SelectedItem == Cam6Combo.SelectedItem || Cam2Combo.SelectedItem == Cam7Combo.SelectedItem || Cam2Combo.SelectedItem == Cam8Combo.SelectedItem || Cam2Combo.SelectedItem == Cam9Combo.SelectedItem)&&Cam2Combo.Text!="")
            {
                Cam2Combo.SelectedItem = null;
                MessageBox.Show("Данная камера выбрана в другой секции");
            }
        }

        private void Cam4Combo_DropDownClosed(object sender, EventArgs e)
        {
            if ((Cam4Combo.SelectedItem == Cam1Combo.SelectedItem || Cam4Combo.SelectedItem == Cam2Combo.SelectedItem || Cam4Combo.SelectedItem == Cam3Combo.SelectedItem || Cam4Combo.SelectedItem == Cam5Combo.SelectedItem || Cam4Combo.SelectedItem == Cam6Combo.SelectedItem || Cam4Combo.SelectedItem == Cam7Combo.SelectedItem || Cam4Combo.SelectedItem == Cam8Combo.SelectedItem || Cam4Combo.SelectedItem == Cam9Combo.SelectedItem)&& Cam4Combo.Text!="")
            {
                Cam4Combo.SelectedItem = null;
                MessageBox.Show("Данная камера выбрана в другой секции");
            }
        }

        private void Cam5Combo_DropDownClosed(object sender, EventArgs e)
        {
            if ((Cam5Combo.SelectedItem == Cam1Combo.SelectedItem || Cam5Combo.SelectedItem == Cam2Combo.SelectedItem || Cam5Combo.SelectedItem == Cam3Combo.SelectedItem || Cam5Combo.SelectedItem == Cam4Combo.SelectedItem || Cam5Combo.SelectedItem == Cam6Combo.SelectedItem || Cam5Combo.SelectedItem == Cam7Combo.SelectedItem || Cam5Combo.SelectedItem == Cam8Combo.SelectedItem || Cam5Combo.SelectedItem == Cam9Combo.SelectedItem)&& Cam5Combo.Text!="")
            {
                Cam5Combo.SelectedItem = null;
                MessageBox.Show("Данная камера выбрана в другой секции");
            }
        }

        private void Cam6Combo_DropDownClosed(object sender, EventArgs e)
        {
            if ((Cam6Combo.SelectedItem == Cam1Combo.SelectedItem || Cam6Combo.SelectedItem == Cam2Combo.SelectedItem || Cam6Combo.SelectedItem == Cam3Combo.SelectedItem || Cam6Combo.SelectedItem == Cam4Combo.SelectedItem || Cam6Combo.SelectedItem == Cam5Combo.SelectedItem || Cam6Combo.SelectedItem == Cam7Combo.SelectedItem || Cam6Combo.SelectedItem == Cam8Combo.SelectedItem || Cam6Combo.SelectedItem == Cam9Combo.SelectedItem)&&Cam6Combo.Text!="")
            {
                Cam6Combo.SelectedItem = null;
                MessageBox.Show("Данная камера выбрана в другой секции");
            }
        }

        private void Cam7Combo_DropDownClosed(object sender, EventArgs e)
        {
            if ((Cam7Combo.SelectedItem == Cam1Combo.SelectedItem || Cam7Combo.SelectedItem == Cam2Combo.SelectedItem || Cam7Combo.SelectedItem == Cam3Combo.SelectedItem || Cam7Combo.SelectedItem == Cam4Combo.SelectedItem || Cam7Combo.SelectedItem == Cam5Combo.SelectedItem || Cam7Combo.SelectedItem == Cam6Combo.SelectedItem || Cam7Combo.SelectedItem == Cam8Combo.SelectedItem || Cam7Combo.SelectedItem == Cam9Combo.SelectedItem)&& Cam7Combo.Text!="")
            {
                Cam7Combo.SelectedItem = null;
                MessageBox.Show("Данная камера выбрана в другой секции");
            }
        }

        private void Cam8Combo_DropDownClosed(object sender, EventArgs e)
        {
            if ((Cam8Combo.SelectedItem == Cam1Combo.SelectedItem || Cam8Combo.SelectedItem == Cam2Combo.SelectedItem || Cam8Combo.SelectedItem == Cam3Combo.SelectedItem || Cam8Combo.SelectedItem == Cam4Combo.SelectedItem || Cam8Combo.SelectedItem == Cam5Combo.SelectedItem || Cam8Combo.SelectedItem == Cam6Combo.SelectedItem || Cam8Combo.SelectedItem == Cam7Combo.SelectedItem || Cam8Combo.SelectedItem == Cam9Combo.SelectedItem)&& Cam8Combo.Text!="")
            {
                Cam8Combo.SelectedItem = null;
                MessageBox.Show("Данная камера выбрана в другой секции");
            }
        }

        private void Cam9Combo_DropDownClosed(object sender, EventArgs e)
        {
            if ((Cam9Combo.SelectedItem == Cam1Combo.SelectedItem || Cam9Combo.SelectedItem == Cam2Combo.SelectedItem || Cam9Combo.SelectedItem == Cam3Combo.SelectedItem || Cam9Combo.SelectedItem == Cam4Combo.SelectedItem || Cam9Combo.SelectedItem == Cam5Combo.SelectedItem || Cam9Combo.SelectedItem == Cam6Combo.SelectedItem || Cam9Combo.SelectedItem == Cam7Combo.SelectedItem || Cam9Combo.SelectedItem == Cam8Combo.SelectedItem)&& Cam9Combo.Text!="")
            {
                Cam9Combo.SelectedItem = null;
                MessageBox.Show("Данная камера выбрана в другой секции");
            }
        }

        private void ListBoxVideo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState==e.LeftButton)
            {
                if (ListBoxVideo.SelectedItem != null)
                {
                    VideoFile video = (VideoFile)ListBoxVideo.SelectedItem;
                    if (File.Exists(video.FullName))
                    {
                        VideoPlayer.Source = new Uri(video.FullName);
                        VideoSection.SelectedIndex = 1;
                        PlayVideo();
                    }
                    else
                    {
                        MessageBox.Show("Видеофайл не найден. Элемент списка будет удалён");
                        ListBoxVideo.Items.Remove(ListBoxVideo.SelectedItem);
                    }
                }
            }
        }

        private void IpText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !(Char.IsDigit(e.Text, 0)||e.Text.StartsWith("."));
        }

        private void NameCamOnVideoPL_DropDownClosed(object sender, EventArgs e)
        {
            GetFiles();
        }

        private void BackMedia_Click(object sender, RoutedEventArgs e)
        {
            if ((ListBoxVideo.SelectedIndex - 1)>-1)
            {
                ListBoxVideo.SelectedIndex = ListBoxVideo.SelectedIndex - 1;
                VideoFile file= (VideoFile)ListBoxVideo.SelectedItem;
                if (File.Exists(file.FullName))
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    VideoPlayer.Source = new Uri(file.FullName);
                    LineMedia.Value = 0;
                    VideoPlayer.Position = TimeSpan.FromSeconds(0);
                    VideoPlayer.Play();
                    PlayerTimer.Interval = TimeSpan.FromMilliseconds(20);
                    PlayerTimer.Start();
                    PlayerTimer.Tick += PlayerTimer_Tick;
                }
            }
            else
            {
                MessageBox.Show("Невозможно перейти к предыдущему видео");
            }
        }

        private void NextMedia_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxVideo.SelectedIndex + 1 < ListBoxVideo.Items.Count)
            {
                ListBoxVideo.SelectedIndex = ListBoxVideo.SelectedIndex + 1;
                VideoFile file = (VideoFile)ListBoxVideo.SelectedItem;
                if (File.Exists(file.FullName))
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    VideoPlayer.Source = new Uri(file.FullName);
                    VideoPlayer.Position = TimeSpan.FromSeconds(0);
                    VideoPlayer.Play();
                    PlayerTimer.Interval = TimeSpan.FromMilliseconds(20);
                    PlayerTimer.Start();
                    PlayerTimer.Tick += PlayerTimer_Tick;
                }
                
            }
            else
            {
                MessageBox.Show("Невозможно перейти к следующему видео");
            }
        }

        private void MotionDetectEx_Click(object sender, RoutedEventArgs e)
        {
            if(MotionDetectEx.IsChecked==true)
            {
                MotionDetect.IsChecked = false;
                MotionDetectEx.IsChecked = true;
            }
        }

        private void edMotDetFrameInterval_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !(Char.IsDigit(e.Text, 0));
        }

        private void Cam1Combo_DropDownClosed(object sender, EventArgs e)
        {
            if ((Cam1Combo.SelectedItem == Cam2Combo.SelectedItem || Cam1Combo.SelectedItem == Cam3Combo.SelectedItem || Cam1Combo.SelectedItem == Cam4Combo.SelectedItem || Cam1Combo.SelectedItem == Cam5Combo.SelectedItem || Cam1Combo.SelectedItem == Cam6Combo.SelectedItem || Cam1Combo.SelectedItem == Cam7Combo.SelectedItem || Cam1Combo.SelectedItem == Cam8Combo.SelectedItem || Cam1Combo.SelectedItem == Cam9Combo.SelectedItem)&& Cam1Combo.Text!="")
            {
                Cam1Combo.SelectedItem = null;
                MessageBox.Show("Данная камера выбрана в другой секции");
            }
        }

        private void Cam3Combo_DropDownClosed(object sender, EventArgs e)
        {
            if ((Cam3Combo.SelectedItem == Cam1Combo.SelectedItem || Cam3Combo.SelectedItem == Cam2Combo.SelectedItem || Cam3Combo.SelectedItem == Cam4Combo.SelectedItem || Cam3Combo.SelectedItem == Cam5Combo.SelectedItem || Cam3Combo.SelectedItem == Cam6Combo.SelectedItem || Cam3Combo.SelectedItem == Cam7Combo.SelectedItem || Cam3Combo.SelectedItem == Cam8Combo.SelectedItem || Cam3Combo.SelectedItem == Cam9Combo.SelectedItem)&& Cam3Combo.Text!="")
            {
                Cam3Combo.SelectedItem = null;
                MessageBox.Show("Данная камера выбрана в другой секции");
            }
        }

        private void Cam1_Cap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadOnMainCamClickedCam(Cam1_Cap,cam1);
        }

        private void Cam3_Cap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadOnMainCamClickedCam(Cam3_Cap, cam3);
        }

        private void Cam4_Cap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadOnMainCamClickedCam(Cam4_Cap, cam4);
        }

        private void OnLoadMainVideoCap_Click(object sender, RoutedEventArgs e)
        {
            VideoCamMain.Stop();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void ScreenMainCam_Click(object sender, RoutedEventArgs e)
        {
            if (screenshotSaveDialog.ShowDialog() == true)
            {
                var filename = screenshotSaveDialog.FileName;
                var ext = Path.GetExtension(filename).ToLowerInvariant();
                switch (ext)
                {
                    case ".bmp":
                        VideoCamMain.Frame_Save(filename, VFImageFormat.BMP, 0);
                        break;
                    case ".jpg":
                        VideoCamMain.Frame_Save(filename, VFImageFormat.JPEG, 85);
                        break;
                    case ".gif":
                        VideoCamMain.Frame_Save(filename, VFImageFormat.GIF, 0);
                        break;
                    case ".png":
                        VideoCamMain.Frame_Save(filename, VFImageFormat.PNG, 0);
                        break;
                    case ".tiff":
                        VideoCamMain.Frame_Save(filename, VFImageFormat.TIFF, 0);
                        break;
                }
            }
        }

        private void Cam5_Cap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadOnMainCamClickedCam(Cam5_Cap, cam5);
        }

        private void Cam6_Cap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadOnMainCamClickedCam(Cam6_Cap, cam6);
        }

        private void Cam7_Cap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadOnMainCamClickedCam(Cam7_Cap, cam7);
        }

        private void Cam8_Cap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadOnMainCamClickedCam(Cam8_Cap, cam8);
        }

        private void Cam9_Cap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadOnMainCamClickedCam(Cam9_Cap, cam9);
        }

        private void Cam2_Cap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoadOnMainCamClickedCam(Cam2_Cap, cam2);
        }

        private void CamsComboAdder()
        {
            List<string> items = new List<string>();

            for (int i = 0; i < ListBoxCameras.Items.Count; i++)
            {
                Camera cam = (Camera)ListBoxCameras.Items[i];
                items.Add(cam.Name);
            }
            if (items != null)
            {
                items.Add("");
                Cam1Combo.ItemsSource = NameCamOnVideoPL.ItemsSource = Cam2Combo.ItemsSource = Cam3Combo.ItemsSource = Cam4Combo.ItemsSource = Cam5Combo.ItemsSource = Cam6Combo.ItemsSource = Cam7Combo.ItemsSource = Cam8Combo.ItemsSource = Cam1Combo.ItemsSource = items;
            }
        }

        private void AnimatorGrid(Grid grid)
        {
            DoubleAnimation da = new DoubleAnimation();
            da.From = 0;
            da.To = 1;
            da.Duration = new Duration(TimeSpan.FromSeconds(1));
            grid.BeginAnimation(Button.OpacityProperty, da);
        }

        private void AnimatorVideoPanel()
        {
            Thickness positionDefVideo = new Thickness(0, 0, 0, 30);
            Thickness positionOldVideo = new Thickness(0, 0, 0, 196);
            if (!OpenVideoPanel)
            {
                VideoPanelOpenButton.BeginAnimation(OpacityProperty,new DoubleAnimation(0,TimeSpan.FromSeconds(0.2)));
                VideoBorder.BeginAnimation(MarginProperty, new ThicknessAnimation(positionOldVideo, TimeSpan.FromSeconds(1)));
                DispatcherTimer dispatcherTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
                dispatcherTimer.Start();
                dispatcherTimer.Tick += new EventHandler((object c, EventArgs eventArgs) =>
                {
                    VideoPanelOpenButton.Visibility = Visibility.Collapsed;
                    PlayerPanel.Visibility = Visibility.Visible;
                    PlayerPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(1)));
                    ((DispatcherTimer)c).Stop();
                });
                OpenVideoPanel = true;
            }
            else
            {
                VideoBorder.BeginAnimation(MarginProperty, new ThicknessAnimation(positionDefVideo, TimeSpan.FromSeconds(1)));
                PlayerPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(0.2)));
                DispatcherTimer dispatcherTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(0.2) };
                dispatcherTimer.Start();
                dispatcherTimer.Tick += new EventHandler((object c, EventArgs eventArgs) =>
                {
                    VideoPanelOpenButton.Visibility = Visibility.Visible;
                    PlayerPanel.Visibility = Visibility.Visible;
                    ((DispatcherTimer)c).Stop();
                });
                VideoPanelOpenButton.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(1)));
                OpenVideoPanel = false;
            }
        }
     
        private void GetFiles()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            ListBoxVideo.Items.Clear();
            WindowsMediaPlayer wmp= new WindowsMediaPlayer();
            string name= NameCamOnVideoPL.Text, type=null, path=null;
            for (int i = 0; i < ListBoxCameras.Items.Count; i++)
            {
                Camera cam = (Camera)ListBoxCameras.Items[i];
                if (cam.Name == name)
                {
                    path = cam.OutPutPath;
                    type = cam.OutPutType;
                }
            }
            if (path != null)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                foreach (var dir in directoryInfo.GetDirectories())
                {
                    foreach (var file in dir.GetFiles())
                    {
                        if (Path.GetExtension(file.FullName) == ".avi" || Path.GetExtension(file.FullName) == ".mp4" || Path.GetExtension(file.FullName) == ".mov" || Path.GetExtension(file.FullName) == ".wmv" || Path.GetExtension(file.FullName) == ".mpeg")
                        {
                            if (file.CreationTime.Date == DateSelectFiles.SelectedDate)
                            {
                                if (file.Name.Contains(NameCamOnVideoPL.Text))
                                {
                                    IWMPMedia mediaInfo = wmp.newMedia(file.FullName);
                                    VideoFile videofile = new VideoFile(mediaInfo.name, type, mediaInfo.durationString, file.FullName);
                                    ListBoxVideo.Items.Add(videofile);
                                }
                            }
                        }
                    }
                }
            }            
        }

        private bool CheckNameCamera(bool change, string Name)
        {
            bool ok=true;
            Camera cam;
            if (ListBoxCameras.Items.Count > 0)
            {
                if (change)
                {
                    for (int i = 0; i < ListBoxCameras.Items.Count; i++)
                    {
                        cam = (Camera)ListBoxCameras.Items[i];
                        if (ListBoxCameras.Items[i] != ListBoxCameras.SelectedItem && Name == cam.Name)
                        {
                            ok = false;
                        }
                    }
                }
                else if(!change)
                {
                    for (int i = 0; i < ListBoxCameras.Items.Count; i++)
                    {
                        cam = (Camera)ListBoxCameras.Items[i];
                        if (Name == cam.Name)
                        {
                            MessageBox.Show(cam.Name);
                            ok = false;
                        }
                    }
                }
            }
            else
            {
                ok = true;
            }
            return ok;
        }

        private void DeleteFilesAfterLoad(int days)
        {
            Camers cams = new Camers();
            for (int i = 0; i < ListBoxCameras.Items.Count; i++)
            {
                cams.CamersList.Add((Camera)ListBoxCameras.Items[i]);
            }
            for (int i = 0; i < cams.CamersList.Count; i++)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(cams.CamersList[i].OutPutPath);
                //DirectoryInfo directoryInfo2 = new DirectoryInfo("");
                foreach (var dir in directoryInfo.GetDirectories())
                {
                    foreach (var file in dir.GetFiles())
                    {
                        if (Path.GetExtension(file.FullName) == ".avi" || Path.GetExtension(file.FullName) == ".mp4" || Path.GetExtension(file.FullName) == ".mov" || Path.GetExtension(file.FullName) == ".wmv" || Path.GetExtension(file.FullName) == ".mpeg")
                        {
                            if (file.CreationTime.Date <= DateTime.Now.Date.AddDays(-days))
                            {
                                if (file.Name.Contains(cams.CamersList[i].Name))
                                {
                                    File.Delete(file.FullName);
                                    if (dir.GetFiles("*.*").Length == 0)
                                    {
                                        Directory.Delete(dir.FullName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void BlockAndUnblockComboCams()
        {
            if(Cam1Load|| Cam2Load|| Cam3Load|| Cam4Load|| Cam5Load|| Cam6Load|| Cam7Load|| Cam8Load|| Cam9Load)
            {
                Cam1Combo.IsEnabled = false;
                Cam2Combo.IsEnabled = Cam3Combo.IsEnabled = Cam4Combo.IsEnabled = Cam5Combo.IsEnabled = Cam6Combo.IsEnabled = Cam7Combo.IsEnabled = Cam8Combo.IsEnabled = Cam9Combo.IsEnabled = Cam1Combo.IsEnabled;
            }
            else
            {
                Cam1Combo.IsEnabled = true;
                Cam2Combo.IsEnabled = Cam3Combo.IsEnabled = Cam4Combo.IsEnabled = Cam5Combo.IsEnabled = Cam6Combo.IsEnabled = Cam7Combo.IsEnabled = Cam8Combo.IsEnabled = Cam9Combo.IsEnabled = Cam1Combo.IsEnabled;
            }
        }
    }
}
