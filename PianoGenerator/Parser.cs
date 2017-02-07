using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PianoGenerator
{
    public class CompilationException : InvalidOperationException
    {
        public CompilationException(string message) : base(message) { }
    }
    sealed class Parser
    {
        public class VariableElement
        {
            public enum Type { Note, Pause, Call };
            public virtual Type ElementType { get; }
        }
        public class NoteElement : VariableElement
        {
            public override Type ElementType { get; } = Type.Note;

            public string Note;
            public int PressLevel;
            public double Duration;
        }
        public class PauseElement : VariableElement
        {
            public override Type ElementType { get; } = Type.Pause;

            public double Duration;

            public string WaitForVariable;

            public void CalculateDuration(Variable target, Variable current)
            {
                PauseElement pause;
                CallElement call;

                foreach (var element in target.Elements)
                {
                    if (element.ElementType == VariableElement.Type.Pause)
                    {
                        pause = (PauseElement)element;
                        //TODO: pause with WaitForVariable
                        Duration += pause.Duration;
                    }
                }

                int callPosition = -1;
                int pausePosition = current.Elements.IndexOf(this);

                for (int i = 0; i < pausePosition; ++i)
                {
                    if(current.Elements[i].ElementType == Type.Call)
                    {
                        call = (CallElement)current.Elements[i];

                        if (call.VariableName == WaitForVariable)
                        {
                            callPosition = i;
                        }
                        continue;
                    }
                    if(current.Elements[i].ElementType == Type.Pause)
                    {
                        pause = (PauseElement)current.Elements[i];

                        if(pause.WaitForVariable == WaitForVariable)
                        {
                            callPosition = -1;
                        }
                        continue;
                    }
                }

                if(callPosition == -1)
                {
                    throw new CompilationException($"Can't find the variable '{WaitForVariable}' to wait in the '{current.Name}'");
                }
            
                for(int i = callPosition; i < pausePosition; ++i)
                {
                    if(current.Elements[i].ElementType == Type.Pause)
                    {
                        pause = (PauseElement)current.Elements[i];
                        Duration -= pause.Duration;
                    }
                }
            }
        }
        public class CallElement : VariableElement
        {
            public override Type ElementType { get; } = Type.Call;

            public string VariableName;
        }

        public class Variable
        {
            public string Name;
            public List<VariableElement> Elements = new List<VariableElement>();
        }

        public class Source
        {
            public List<Variable> Variables = new List<Variable>();

            public Variable FindVariable(string name)
            {
                return Variables.Find(v => v.Name == name);
            }
        }


        public void Parse(string data)
        {
            var lines = data.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);

            currentLine = 0;
            foreach (var line in lines)
            {
                ParseLine(line);
                ++currentLine;
                currentIndex = 0;
            }

            foreach (var variable in source.Variables)
            {
                foreach (var element in variable.Elements)
                {
                    if (element.ElementType == VariableElement.Type.Pause)
                    {
                        var pause = (PauseElement)element;
                        if (!string.IsNullOrEmpty(pause.WaitForVariable))
                        {
                            pause.CalculateDuration(source.FindVariable(pause.WaitForVariable), variable);
                        }
                    }
                }
            }
        }

        public Source GetSource()
        {
            return source;
        }

        private void ParseLine(string line)
        {
            currentIndex = ParserHelper.SkipSpaces(line, currentIndex);

            if (currentIndex >= line.Length)
                return;

            if(line[currentIndex] == '@')
            {
                ParseVariable(line);
            }

            currentIndex = ParserHelper.SkipSpaces(line, currentIndex);
        }

        private void ParseVariable(string line)
        {
            Variable variable = new Variable();

            ++currentIndex;

            currentIndex = ParserHelper.ReadWord(line, currentIndex, out variable.Name);

            if(string.IsNullOrEmpty(variable.Name))
            {
                throw new CompilationException($"Invaild variable name at line {currentLine}");
            }

            currentIndex = ParserHelper.SkipSpaces(line, currentIndex);

            if (line[currentIndex] != '=')
            {
                throw new CompilationException($"Expected '=' at {currentLine}:{currentIndex}");
            }

            ++currentIndex;

            while(true)
            {
                currentIndex = ParserHelper.SkipSpaces(line, currentIndex);

                ParseVariableElement(line, variable);

                currentIndex = ParserHelper.SkipSpaces(line, currentIndex);

                if (currentIndex >= line.Length)
                    break;
                if(line[currentIndex] != ',')
                {
                    throw new CompilationException($"Expected ',' at {currentLine}:{currentIndex}");
                }
                ++currentIndex;
            }

            source.Variables.Add(variable);
        }

        private void ParseVariableElement(string line, Variable variable)
        {
            switch(line[currentIndex])
            {
                case '@':
                    ParseVariableCall(line, variable);
                    break;
                case '*':
                    ParseVariablePause(line, variable);
                    break;
                default:
                    ParseVariableNote(line, variable);
                    break;
            }
        }

        private void ParseVariableCall(string line, Variable variable)
        {
            ++currentIndex;

            var callElement = new CallElement();

            currentIndex = ParserHelper.ReadWord(line, currentIndex, out callElement.VariableName);

            if(string.IsNullOrEmpty(callElement.VariableName))
            {
                throw new CompilationException($"Invalid variable name at {currentLine}:{currentIndex}");
            }

            variable.Elements.Add(callElement);
        }
        private void ParseVariableNote(string line, Variable variable)
        {
            var noteElement = new NoteElement();

            if (line[currentIndex] == '~')
            {
                noteElement.Note = ParseRelativeVariable(line, variable);
            }
            else
            {
                StringBuilder note = new StringBuilder(line.Substring(currentIndex, 2), 3);

                if (line[currentIndex] < 'A' || line[currentIndex] > 'G')
                {
                    throw new CompilationException($"Expected a value between 'A' and 'G' not '{line[currentIndex]}' at {currentLine}:{currentIndex}");
                }
                if (line[currentIndex + 1] < '1' || line[currentIndex + 1] > '6')
                {
                    throw new CompilationException($"Expected a value between '1' and '6' not '{line[currentIndex + 1]}' at {currentLine}:{currentIndex}");
                }

                currentIndex += 2;

                if (line[currentIndex] == '#' || line[currentIndex] == 'b')
                {
                    if (line[currentIndex] == '#')
                    {
                        HigherNote(note);
                    }

                    if (!HasBemolle(note))
                    {
                        throw new CompilationException($"'{note}' note hasn't a bemolle! (at {currentLine}:{currentIndex})");
                    }

                    note.Append(note[1]);//move a number to the 3rd position
                    note[1] = 'b';

                    ++currentIndex;
                }

                noteElement.Note = note.ToString();
            }

            if (line[currentIndex] != '!')
            {
                throw new CompilationException($"Expected '!' at {currentLine}:{currentIndex}");
            }

            ++currentIndex;

            if(!char.IsDigit(line[currentIndex]))
            {
                throw new CompilationException($"Expected digit at {currentLine}:{currentIndex}");
            }

            noteElement.PressLevel = line[currentIndex] - '0';

            if(noteElement.PressLevel > 3 || noteElement.PressLevel < 1)
            {
                throw new CompilationException($"Invalid press value '{noteElement.PressLevel}' at {currentLine}:{currentIndex}");
            }

            ++currentIndex;

            if (line[currentIndex] == '_')
            {
                ++currentIndex;

                if (!char.IsDigit(line[currentIndex]))
                {
                    throw new CompilationException($"Expected a digit at {currentLine}:{currentIndex}");
                }

                string numberStr;
                currentIndex = ParserHelper.ReadNumber(line, currentIndex, true, out numberStr);

                if (string.IsNullOrEmpty(numberStr))
                {
                    throw new CompilationException($"Expected a number at {currentLine}:{currentIndex}");
                }

                noteElement.Duration = PianoSettings.Instance().GetNoteDuration(GetDuration(numberStr));
            }
            else if(line[currentIndex] == '=')
            {
                ++currentIndex;

                string numberStr;
                currentIndex = ParserHelper.ReadNumber(line, currentIndex, false, out numberStr);

                if (string.IsNullOrEmpty(numberStr))
                {
                    throw new CompilationException($"Expected a number at {currentLine}:{currentIndex}");
                }

                double duration;
                bool ok = double.TryParse(numberStr, out duration);

                if(!ok)
                {
                    throw new CompilationException($"Expected a number at {currentLine}:{currentIndex}");
                }

                noteElement.Duration = duration;
            }
            else
            {
                throw new CompilationException($"Expected '_' or '=' at {currentLine}:{currentIndex}");
            }

            

            variable.Elements.Add(noteElement);
        }
        private string ParseRelativeVariable(string line, Variable variable)
        {
            ++currentIndex;

            string numStr;
            currentIndex = ParserHelper.ReadNumber(line, currentIndex, true, out numStr);

            if(string.IsNullOrEmpty(numStr))
            {
                throw new CompilationException($"Expected number at {currentLine}:{currentIndex}");
            }

            int offset;

            bool ok = int.TryParse(numStr, out offset);

            if (!ok)
            {
                throw new CompilationException($"'{numStr}' isn't a number at {currentLine}:{currentIndex}");
            }

            if (line[currentIndex] != '+' && line[currentIndex] != '-')
            {
                throw new CompilationException($"Expected '+' or '-' at {currentLine}:{currentIndex}");
            }

            bool isAdding = line[currentIndex] == '+';

            ++currentIndex;

            currentIndex = ParserHelper.ReadNumber(line, currentIndex, true, out numStr);

            if (string.IsNullOrEmpty(numStr))
            {
                throw new CompilationException($"Expected number at {currentLine}:{currentIndex}");
            }

            int changeValueOn;

            ok = int.TryParse(numStr, out changeValueOn);

            if (!ok)
            {
                throw new CompilationException($"'{numStr}' isn't a number at {currentLine}:{currentIndex}");
            }

            bool changeOnHalf = false;

            if(line[currentIndex] == '.')
            {
                changeOnHalf = true;
                ++currentIndex;
            }

            var builder = new StringBuilder();

            int targetNoteIndex = variable.Elements.Count - 1;

            while(true)
            {
                if (variable.Elements[targetNoteIndex].ElementType == VariableElement.Type.Note)
                    --offset;
                if (offset == 0)
                    break;

                --targetNoteIndex;
            }

            builder.Append(((NoteElement)variable.Elements[targetNoteIndex]).Note);

            LowerOrHigherNote(builder, changeValueOn  * (isAdding ? 1 : -1));

            if (changeOnHalf)
            {
                if(isAdding)
                {
                    HigherNote(builder);
                }

                builder.Append(builder[1]);//move a number to the 3rd position
                builder[1] = 'b';

                if (!HasBemolle(builder))
                {
                    throw new CompilationException($"'{builder.ToString()}' note hasn't a bemolle! (at {currentLine}:{currentIndex})");
                }
            }

            return builder.ToString();
        }

        private void ParseVariablePause(string line, Variable variable)
        {
            ++currentIndex;

            var pauseElement = new PauseElement();

            if (line[currentIndex] == '_')
            {
                ++currentIndex;

                string numStr;

                currentIndex = ParserHelper.ReadNumber(line, currentIndex, true, out numStr);

                if (string.IsNullOrEmpty(numStr))
                {
                    throw new CompilationException($"'{line[currentIndex]}' isn't a number at {currentLine}:{currentIndex}");
                }

                pauseElement.Duration = PianoSettings.Instance().GetNoteDuration(GetDuration(numStr));
            }
            else if(line[currentIndex] == '@')
            {
                ++currentIndex;

                string varName;

                currentIndex = ParserHelper.ReadWord(line, currentIndex, out varName);

                if (string.IsNullOrEmpty(varName))
                {
                    throw new CompilationException($"Invalid variable name at {currentLine}:{currentIndex}");
                }

                pauseElement.WaitForVariable = varName;
            }
            else
            {
                string numStr;

                currentIndex = ParserHelper.ReadNumber(line, currentIndex, false, out numStr);

                if (string.IsNullOrEmpty(numStr))
                {
                    throw new CompilationException($"'{line[currentIndex]}' isn't a number at {currentLine}:{currentIndex}");
                }

                bool ok = double.TryParse(numStr, out pauseElement.Duration);

                if (!ok)
                {
                    throw new CompilationException($"'{numStr}' isn't a number at {currentLine}:{currentIndex}");
                }
            }

            variable.Elements.Add(pauseElement);
        }
        
        private bool HasBemolle(StringBuilder note)
        {
            switch(note[0])
            {
                case 'C':
                    return false;
                case 'D':
                    return true;
                case 'E':
                    return true;
                case 'F':
                    return false;
                case 'G':
                    return true;
                case 'A':
                    return true;
                case 'B':
                    return true;
                default:
                    throw new CompilationException($"Invalid note '{note}' at {currentLine}:{currentIndex}");
            }
        }

        private void LowerOrHigherNote(StringBuilder note, int changeOn)
        {
            if(changeOn > 0)
            {
                while(changeOn > 0)
                {
                    HigherNote(note);
                    --changeOn;
                }
            }
            else if(changeOn < 0)
            {
                while(changeOn < 0)
                {
                    LowerNote(note);
                    ++changeOn;
                }
            }
        }
        private void LowerNote(StringBuilder note)
        {
            switch (note[0])
            {
                case 'C':
                    note[0] = 'B';
                    note[note.Length - 1] = (char)((int)note[note.Length - 1] - 1);
                    break;
                case 'D':
                    note[0] = 'C';
                    break;
                case 'E':
                    note[0] = 'D';
                    break;
                case 'F':
                    note[0] = 'E';
                    break;
                case 'G':
                    note[0] = 'F';
                    break;
                case 'A':
                    note[0] = 'G';
                    break;
                case 'B':
                    note[0] = 'A';
                    break;
            }
        }
        private void HigherNote(StringBuilder note)
        {
            switch(note[0])
            {
                case 'C':
                    note[0] = 'D';
                    break;
                case 'D':
                    note[0] = 'E';
                    break;
                case 'E':
                    note[0] = 'F';
                    break;
                case 'F':
                    note[0] = 'G';
                    break;
                case 'G':
                    note[0] = 'A';
                    break;
                case 'A':
                    note[0] = 'B';
                    break;
                case 'B':
                    note[0] = 'C';
                    note[note.Length - 1] = (char)((int)note[note.Length - 1] + 1);
                    break;
            }
        }

        private PianoSettings.NoteDuration GetDuration(string number)
        {
            switch(number)
            {
                case "1":
                    return PianoSettings.NoteDuration.D1;
                case "2":
                    return PianoSettings.NoteDuration.D1_2;
                case "4":
                    return PianoSettings.NoteDuration.D1_4;
                case "8":
                    return PianoSettings.NoteDuration.D1_8;
                case "16":
                    return PianoSettings.NoteDuration.D1_16;
                case "32":
                    return PianoSettings.NoteDuration.D1_32;
                default:
                    throw new CompilationException($"Unknown duration at {currentLine}:{currentIndex}");
            }
        }

        private Source source = new Source();
        private int currentLine;
        int currentIndex;
    }
}
