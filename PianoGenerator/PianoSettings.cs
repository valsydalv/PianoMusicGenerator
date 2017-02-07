using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PianoGenerator
{
    sealed class PianoSettings
    {
        public enum NoteDuration { D1, D1_2, D1_4, D1_8, D1_16, D1_32 }

        public static PianoSettings Instance()
        {
            if(settings == null)
            {
                settings = new PianoSettings();
                settings.Load();
            }
            return settings;
        }

        public void Load()
        {
            var xml = new XmlDocument();
            xml.Load(@"D:\Projects\PianoGenerator\PianoGenerator\PianoSettings.xml");

            var piano = xml["piano"];

            var loadSettings = piano["load_settings"];

            fileNameFormat = loadSettings["file_name"].InnerText;
            pianoLoadPrefix = loadSettings["prefix"].InnerText;

            pianoLoadPressLevelPrefix[0] = loadSettings["press_level"]["l1"].InnerText;
            pianoLoadPressLevelPrefix[1] = loadSettings["press_level"]["l2"].InnerText;
            pianoLoadPressLevelPrefix[2] = loadSettings["press_level"]["l3"].InnerText;

            var playSettings = piano["play_settings"];

            noteDuration1 = double.Parse(playSettings["note_duration"]["d1"].InnerText);
            noteDuration1_2 = double.Parse(playSettings["note_duration"]["d1_2"].InnerText);
            noteDuration1_4 = double.Parse(playSettings["note_duration"]["d1_4"].InnerText);
            noteDuration1_8 = double.Parse(playSettings["note_duration"]["d1_8"].InnerText);
            noteDuration1_16 = double.Parse(playSettings["note_duration"]["d1_16"].InnerText);
            noteDuration1_32 = double.Parse(playSettings["note_duration"]["d1_32"].InnerText);

            releaseKeyK = double.Parse(playSettings["release_key_k"].InnerText);
        }

        public string GetNoteName(string note, int pressLevel)
        {
            return string.Format(fileNameFormat, pianoLoadPrefix, pianoLoadPressLevelPrefix[pressLevel - 1], note);
        }

        public double GetNoteDuration(NoteDuration duration)
        {
            switch(duration)
            {
                case NoteDuration.D1:
                    return noteDuration1;
                case NoteDuration.D1_2:
                    return noteDuration1_2;
                case NoteDuration.D1_4:
                    return noteDuration1_4;
                case NoteDuration.D1_8:
                    return noteDuration1_8;
                case NoteDuration.D1_16:
                    return noteDuration1_16;
                case NoteDuration.D1_32:
                    return noteDuration1_32;
                default:
                    throw new IndexOutOfRangeException($"Invalid note duration '{duration}'");
            }
        } 

        public double GetReleaseKeyK()
        {
            return releaseKeyK;
        }

        private PianoSettings() { }

        private static PianoSettings settings;

        private string fileNameFormat;
        private string pianoLoadPrefix;
        private string[] pianoLoadPressLevelPrefix = new string[3];

        private double noteDuration1;
        private double noteDuration1_2;
        private double noteDuration1_4;
        private double noteDuration1_8;
        private double noteDuration1_16;
        private double noteDuration1_32;

        private double releaseKeyK;
    }
}
