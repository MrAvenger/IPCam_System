using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCam_System
{
    [Serializable]
    public class Camers
    {
        public List<Camera> CamersList { get; set; } = new List<Camera>();
    }
    [Serializable]
    public class Camera
    {
        public string Name { get; set; }
        public string ConnectString { get; set; }
        public string TypeCam { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string OutPutType { get; set; }
        public string OutPutPath { get; set; }
        public string OutPutTime { get; set; }
        public bool AudioEx { get; set; }
        public double Lightness { get; set; }
        public double Saturation { get; set; }
        public double Contrast { get; set; }
        public double Darkness { get; set; }
        public bool Invert { get; set; }
        public bool Grayscale { get; set; }
        public bool FlipX { get; set; }
        public bool FlipY { get; set; }
        public double Sound { get; set; }
        public bool Stretch { get; set; }
        public bool Recordtest { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public bool MotionDecEnabled { get; set; }
        public bool MotionHightLightEnabled { get; set; }
        public double MotionHightLightThres { get; set; }
        public int MotionFrameLimit { get; set; }
        public string MotionHightLightColor { get; set; }
        public bool MotionCompare_Red { get; set; }
        public bool MotionCompare_Green { get; set; }
        public bool MotionCompare_Blue { get; set; }
        public bool MotionCompare_GrayScale { get; set; }
        public bool MotionDetectionExEnabled { get; set; }
        public string MotionDetetionExDetector { get; set; }
        public string MotionDetetionExCore { get; set; }

        public Camera(){}

        public Camera(string Name,string ConnectString, string TypeCam, string Login, string Password,string OutPutType,string OutPutPath, string OutPutTime,bool AudioEx,
            double Lightness, double Saturation, double Contrast, double Darkness, bool Invert, bool Grayscale, bool FlipX, bool FlipY, double Sound,bool Stretch, bool Recordtest, string Ip, int Port,
            bool MotionDecEnabled,bool MotionHightLightEnabled,double MotionHightLightThres,int MotionFrameLimit,string MotionHightLightColor,bool MotionCompare_Red,bool MotionCompare_Green,
            bool MotionCompare_Blue,bool MotionCompare_GrayScale,bool MotionDetectionExEnabled,string MotionDetetionExDetector,string MotionDetetionExCore)
        {
            this.Name = Name;
            this.ConnectString = ConnectString;
            this.TypeCam = TypeCam;
            this.Login = Login;
            this.Password = Password;
            this.OutPutType = OutPutType;
            this.OutPutPath = OutPutPath;
            this.OutPutTime = OutPutTime;
            this.AudioEx = AudioEx;
            this.Lightness = Lightness;
            this.Saturation = Saturation;
            this.Contrast = Contrast;
            this.Darkness = Darkness;
            this.Invert = Invert;
            this.Grayscale = Grayscale;
            this.FlipX = FlipX;
            this.FlipY = FlipY;
            this.Recordtest = Recordtest;
            this.Ip = Ip;
            this.Port = Port;
            this.Sound = Sound;
            this.Stretch = Stretch;
            this.MotionDecEnabled = MotionDecEnabled;
            this.MotionHightLightEnabled = MotionHightLightEnabled;
            this.MotionHightLightThres = MotionHightLightThres;
            this.MotionFrameLimit = MotionFrameLimit;
            this.MotionHightLightColor = MotionHightLightColor;
            this.MotionCompare_Red = MotionCompare_Red;
            this.MotionCompare_Green = MotionCompare_Green;
            this.MotionCompare_Blue = MotionCompare_Blue;
            this.MotionCompare_GrayScale = MotionCompare_GrayScale;
            this.MotionDetectionExEnabled = MotionDetectionExEnabled;
            this.MotionDetetionExDetector = MotionDetetionExDetector;
            this.MotionDetetionExCore = MotionDetetionExCore;

        }
    }
}
