using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace PianoGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            var generator = new PianoGenerator();
            generator.Generate(@"D:\Projects\PianoGenerator\PianoGenerator\source2.txt");
            var wav = generator.GetWav();

            WavIO.Save("D:\\Projects\\PianoGenerator\\PianoGenerator\\bin\\Debug\\Resources\\1.wav", wav);
        }

        static void CutNotes()
        {
            var files = Directory.EnumerateFiles(Directory.GetCurrentDirectory() + @"\\Resources\\Notes");

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);

                var wav = WavIO.Load(filePath);

                var nwav = WavEditor.Modify(wav).Cut(val => Math.Abs((int)val) > 20000,
                                                     val => Math.Abs((int)val) > 20000,
                                                     0.3f, 2f);
                WavIO.Save("D:\\Projects\\PianoGenerator\\PianoGenerator\\bin\\Debug\\Resources\\Notes\\Cut\\" + fileName, nwav.GetWav());

                Console.WriteLine($"'{fileName}' done.");
            }
        }
    }
}
