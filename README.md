This program converts source music files to wav files.

--The source file structure--

Note: [note: C|D|E|F|G|A|B][octave: 1-6][optional bemolle: b or sharp: #]![force: 1-3]=[duration seconds: ] A4b!2=0.5

Pause:

Wait for a variable *@[variable name]

Wait *[seconds]

Use variables's notes @[variable name]

Variable:

@[variable name] = [Note or Pause], ..., [Note or Pause];

@main = A4b!2=0.5, *0.4, @var, *@var, C3!3=2

The program requires the main variable as an entry point.

To generate a music use PianoGenerator.Generate and WavIO.Save functions.
