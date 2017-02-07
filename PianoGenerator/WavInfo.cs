using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PianoGenerator
{
    sealed class WavInfo
    {
        /// <summary>
        /// Частота дискретизации. Количество сэмплов в секунду.
        /// </summary>
        public Int32 SampleRate;
        /// <summary>
        /// Количество байт, переданных за секунду воспроизведения.
        /// </summary>
        public Int32 ByteRate;
        /// <summary>
        /// Количество байт для одного сэмпла, включая все каналы.
        /// </summary>
        public Int16 BlockAlign;
        /// <summary>
        /// Количество бит в сэмпле.
        /// </summary>
        public Int16 BitsPerSample;

        /// <summary>
        /// Значения амплитуд волны 
        /// </summary>
        public double[] Amplitudes;
    }

    sealed class WavEditor
    {
        public static WavEditor Modify(WavInfo wavInfo)
        {
            var editor = new WavEditor();
            editor.wav = CloneWav(wavInfo);

            return editor;
        }

        public static WavEditor CreateWav(WavInfo settings)
        {
            var editor = new WavEditor();
            editor.wav = new WavInfo();
            editor.wav.Amplitudes = new double[0];
            editor.wav.BitsPerSample = settings.BitsPerSample;
            editor.wav.BlockAlign = settings.BlockAlign;
            editor.wav.ByteRate = settings.ByteRate;
            editor.wav.SampleRate = settings.SampleRate;

            return editor;
        }

        public WavInfo GetWav()
        {
            return wav;
        }
        
        public WavEditor Where(Func<double, bool> condition)
        {
            wav.Amplitudes = wav.Amplitudes.Where((val, index) =>
                        {
                            CheckRegionIndexBeforeRemoving(index);

                            return !IsModificationAllowed(index) || condition(val);
                        }).ToArray();

            UpdateRegion();
            return this;
        }

        public WavEditor CutFromTo(int from, int to)
        {
            wav.Amplitudes = wav.Amplitudes.Where((val, index) =>
                                {
                                    CheckRegionIndexBeforeRemoving(index);

                                    return !IsModificationAllowed(index) || (index >= from && index < to);
                                }).ToArray();

            return this;
        }

        public WavEditor Cut(Func<double, bool> fromCondition, Func<double, bool> whileCondition, float waitTimeUntilBreakMs = 0, float addExtraTime = 0)
        {

            int from = 0, to = 0;

            from = RunCycleAndFind(wav, fromCondition, true, 0, true);

            if (from == -1)
            {
                Console.WriteLine("Empty wav file in Cut function");

                wav.Amplitudes = new double[0];
                return this;
            }

            to = RunCycleAndFind(wav, whileCondition, true, from, false);
            while (true)
            {
                if (to == -1)
                {
                    to = wav.Amplitudes.Length;
                    break;
                }

                var breakEnd = RunCycleAndFind(wav, whileCondition, true, to, true);
                if (breakEnd == -1)
                {
                    breakEnd = wav.Amplitudes.Length;
                }

                var breakLength = breakEnd - to;

                if ((breakLength / (float)wav.SampleRate) >= waitTimeUntilBreakMs || breakEnd == wav.Amplitudes.Length)
                    break;

                to = RunCycleAndFind(wav, whileCondition, true, to + breakLength, false);
            }

            to += (int)(addExtraTime * wav.SampleRate);
            if (to > wav.Amplitudes.Length)
                to = wav.Amplitudes.Length;

            return CutFromTo(from, to);
        }

        public WavEditor MultiplyWith(Func<double, double, double> func, bool ignoreFreezed = false)
        {
            var totalSize = (double)(ignoreFreezed ? GetSizeExceptFreezed() : wav.Amplitudes.Length);
            var i = 0.0;

            for (int from = GetFirstAllowedIndex(); from != -1; from = GetNextAllowedIndex(from))
            {
                i = ignoreFreezed ? i + 1 : from;
                wav.Amplitudes[from] *= func(wav.Amplitudes[from], i / totalSize);
            }

            return this;
        }
        public WavEditor MultiplyWith(Func<double, int, double> func)
        {
            for (int from = GetFirstAllowedIndex(); from != -1; from = GetNextAllowedIndex(from))
            {
                wav.Amplitudes[from] *= func(wav.Amplitudes[from], from);
            }

            return this;
        }

        public WavEditor Add(Func<double, double, double> func)
        {
            for (int from = GetFirstAllowedIndex(); from != -1; from = GetNextAllowedIndex(from))
            {
                wav.Amplitudes[from] += func(wav.Amplitudes[from], from / (double)wav.Amplitudes.Length);
            }

            return this;
        }
        public WavEditor Add(Func<double, int, double> func)
        {
            for (int from = GetFirstAllowedIndex(); from != -1; from = GetNextAllowedIndex(from))
            {
                wav.Amplitudes[from] += func(wav.Amplitudes[from], from);
            }

            return this;
        }

        public WavEditor ChangePlayingSpeed(double speedK)
        {
            var l = (float)wav.Amplitudes.Length;
            wav.Amplitudes = wav.Amplitudes.Where((v, i) =>
                    {
                        CheckRegionIndexBeforeRemoving(i);

                        return !IsModificationAllowed(i) || ( (i % speedK).AlmostEqual(0) || i == 0 );
                    }).ToArray();

            l /= wav.Amplitudes.Length;

            UpdateRegion();

            return this;
        }

        public WavEditor MixWith(WavInfo other, int offset = 0, double k1 = 1, double k2 = 1)
        {
            var thisWav = wav;

            wav = CloneWav(wav, false);

            int newLength = thisWav.Amplitudes.Length + other.Amplitudes.Length;

            int intersectionFrom = Math.Max(0, offset);
            int intersectionTo = Math.Min(thisWav.Amplitudes.Length, other.Amplitudes.Length + offset);

            if(intersectionFrom < intersectionTo)
            {
                newLength -= intersectionTo - intersectionFrom;
            }
            else
            {
                newLength += intersectionFrom - intersectionTo;
            }

            wav.Amplitudes = new double[newLength];
            if(offset < 0)
            {
                foreach(var region in freezedRegions)
                {
                    region.From -= offset;
                    region.To -= offset;
                }
            }

            for(int i = 0, d = Math.Max(-offset, 0); i < thisWav.Amplitudes.Length; ++i)
            {
                wav.Amplitudes[i + d] = thisWav.Amplitudes[i] * k1;
            }

            Add((double v, int i) =>
                {
                    if(offset < 0)
                    {
                        if (i >= other.Amplitudes.Length)
                            return 0;

                        return other.Amplitudes[i] * k2;
                    }
                    else
                    {
                        if ((i - offset) < 0)
                            return 0;
                        if ((i - offset) >= other.Amplitudes.Length)
                            return 0;

                        return other.Amplitudes[i - offset] * k2;
                    }
                });

            return this;
        }
        public WavEditor MixWith(WavInfo other, double timeOffset = 0, double k1 = 1, double k2 = 1)
        {
            return MixWith(other, (int)(timeOffset * wav.SampleRate), k1, k2);
        }

        public WavEditor ClipBoundaries()
        {
            wav.Amplitudes = wav.Amplitudes.Select(v => Math.Max(0, Math.Min(1, v))).ToArray();

            return this;
        }

        public WavEditor FreezeRegion(int from, int to = -1)
        {
            if(to == -1)
            {
                to = wav.Amplitudes.Length;
            }

            if (from < 0 || from >= wav.Amplitudes.Length)
            {
                Console.WriteLine($"Invalid modify region: [{from};{to})");
                throw new IndexOutOfRangeException();
            }
            if(to < from || to > wav.Amplitudes.Length)
            {
                Console.WriteLine($"Invalid modify region: [{from};{to})");
                throw new IndexOutOfRangeException();
            }

            freezedRegions.Add(new Region { From = from, To = to, RemovedBefore = 0 });

            return this;
        }
        public WavEditor FreezeRegion(double fromSecond, double time = -1)
        {
            return FreezeRegion((int)(fromSecond * wav.SampleRate), (int)(time.AlmostEqual(-1) ? -1 : (fromSecond + time) * wav.SampleRate));
        }

        public WavEditor UnfreezeRegion(int i = -1)
        {
            if(i == -1)
            {
                freezedRegions.Clear();
            }

            freezedRegions.RemoveAt(i);

            return this;
        }

        private void UpdateRegion()
        {
            foreach(var region in freezedRegions)
            {
                region.From -= region.RemovedBefore;
                region.To -= region.RemovedBefore;
                region.RemovedBefore = 0;
            }
        }
        private void CheckRegionIndexBeforeRemoving(int index)
        {
            foreach (var region in freezedRegions)
            {
                if (index < region.From)
                {
                    region.RemovedBefore += 1;
                }
            }
        }

        private bool IsModificationAllowed(int index)
        {
            foreach (var region in freezedRegions)
            {
                if(index >= region.From && index < region.To)
                {
                    return false;
                }
            }
            return true;
        }

        private int GetFirstAllowedIndex()
        {
            if(freezedRegions.All(v => v.From > 0))//or empty
            {
                return 0;
            }

            return GetNextAllowedIndex(0);
        }
        private int GetNextAllowedIndex(int index)
        {
            ++index;

            foreach (var region in freezedRegions)
            {
                if (index >= region.To || index < region.From)
                {
                    if (index >= wav.Amplitudes.Length)
                        return -1;
                    return index;
                }
            }

            for(; index < wav.Amplitudes.Length;)
            {
                foreach (var region in freezedRegions)
                {
                    if (index >= region.From && index < region.To)
                    {
                        index = region.To;
                        continue;
                    }
                }
                return index;
            }

            return -1;
        }

        private int GetSizeExceptFreezed()
        {
            int total = 0;

            for (int index = 0; index < wav.Amplitudes.Length;)
            {
                foreach (var region in freezedRegions)
                {
                    if (index >= region.From && index < region.To)
                    {
                        index = region.To;
                        continue;
                    }
                }
                ++index;
                ++total;
            }

            return total;
        }

        private static WavInfo CloneWav(WavInfo wavInfo, bool copyAmplitude = true)
        {
            var o = new WavInfo();
            o.SampleRate = wavInfo.SampleRate;
            o.ByteRate = wavInfo.ByteRate;
            o.BlockAlign = wavInfo.BlockAlign;
            o.BitsPerSample = wavInfo.BitsPerSample;

            if (copyAmplitude)
            {
                o.Amplitudes = (double[])wavInfo.Amplitudes.Clone();
            }
            else
            {
                o.Amplitudes = null;
            }

            return o;
        }
        private int RunCycleAndFind(WavInfo wav, Func<double, bool> condition, bool isLeftToRight, int startPos = -1, bool breakAtValue = true)
        {
            if (isLeftToRight)
            {
                if (startPos == -1)
                    startPos = 0;

                for (int i = startPos; i < wav.Amplitudes.Length; ++i)
                {
                    if (condition(wav.Amplitudes[i]) == breakAtValue)
                    {
                        return i;
                    }
                }
            }
            else
            {
                if (startPos == -1)
                    startPos = wav.Amplitudes.Length - 1;

                for (int i = startPos; i >= 0; --i)
                {
                    if (condition(wav.Amplitudes[i]) == breakAtValue)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private class Region
        {
            public int From;
            public int To;

            public int RemovedBefore;
        }

        private WavInfo wav;

        private List<Region> freezedRegions = new List<Region>();
    }
}
