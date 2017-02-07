using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PianoGenerator
{
    sealed class PianoGenerator
    {
        
        public static WavInfo GetNote(string note, int pressLevel, PianoSettings.NoteDuration duration)
        {
            return GetNote(note, pressLevel, PianoSettings.Instance().GetNoteDuration(duration));
        }
        public static WavInfo GetNote(string note, int pressLevel, double duration)
        {
            var settings = PianoSettings.Instance();

            return WavEditor.Modify(
                                        WavIO.Load($"D:\\Projects\\PianoGenerator\\PianoGenerator\\bin\\Debug\\Resources\\Notes\\{settings.GetNoteName(note, pressLevel)}")
                                   )
                            .FreezeRegion(0, duration)
                            .MultiplyWith((double x, double i) => Math.Max(0, 1 - settings.GetReleaseKeyK() * i), true)
                            .GetWav();
        }

        public void Generate(string pathToFile)
        {
            var parser = new Parser();
            parser.Parse(File.ReadAllText(pathToFile));

            source = parser.GetSource();

            var main = source.FindVariable("main");

            if(main == null)
            {
                throw new CompilationException("No 'main' variable found");
            }

            mainEditor = WavEditor.CreateWav(GetNote("C4", 3, PianoSettings.NoteDuration.D1));

            GenerateVariable(main, 0);
        }

        public WavInfo GetWav()
        {
            return mainEditor.GetWav();
        }

        private void GenerateVariable(Parser.Variable variable, double initOffset)
        {
            double currentOffset = initOffset;

            foreach (var element in variable.Elements)
            {
                switch (element.ElementType)
                {
                    case Parser.VariableElement.Type.Note:
                        GenerateNote((Parser.NoteElement)element, currentOffset);
                        break;
                    case Parser.VariableElement.Type.Pause:
                        GeneratePause((Parser.PauseElement)element, ref currentOffset);
                        break;
                    case Parser.VariableElement.Type.Call:
                        GenerateCall((Parser.CallElement)element, currentOffset);
                        break;
                }
            }
        }

        private void GenerateNote(Parser.NoteElement element, double currentOffset)
        {
            mainEditor.MixWith(
                                PianoGenerator.GetNote(element.Note, element.PressLevel, element.Duration),
                                currentOffset
                              );
        }
        private void GeneratePause(Parser.PauseElement element, ref double currentOffset)
        {
            currentOffset += element.Duration;
        }
        private void GenerateCall(Parser.CallElement element, double currentOffset)
        {
            var variable = source.FindVariable(element.VariableName);

            if (variable == null)
            {
                throw new CompilationException($"No '{element.VariableName}' variable found");
            }

            GenerateVariable(variable, currentOffset);
        }

        private Parser.Source source;
        private WavEditor mainEditor;
    }
}
