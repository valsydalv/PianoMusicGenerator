using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace PianoGenerator
{
    static class WavIO
    {
        public static void Save(string fileName, WavInfo info)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write((Int32)0x46464952);//"RIFF"
                    writer.Write((Int32)(4 + 4 + 4 + 2 + 2 + 4 + 4 + 2 + 2 + 4 + 4 + info.Amplitudes.Length * sizeof(Int16)));//Length of file - 8
                    writer.Write((Int32)0x45564157);//"WAVE"
                    writer.Write((Int32)0x20746d66);//"fmt"
                    writer.Write((Int32)16);//Length of the fmt block
                    writer.Write((Int16)1);//PCM format
                    writer.Write((Int16)1);//Number of channels
                    writer.Write((Int32)info.SampleRate);
                    writer.Write((Int32)info.ByteRate);
                    writer.Write((Int16)info.BlockAlign);
                    writer.Write((Int16)info.BitsPerSample);
                    writer.Write((Int32)0x61746164);//"data"
                    writer.Write((Int32)(info.Amplitudes.Length * sizeof(Int16)));

                    writer.Write(info.Amplitudes.Select(v => (short)(v * short.MaxValue)).SelectMany(a => BitConverter.GetBytes(a)).ToArray());
                }
            }
        }
        public static WavInfo Load(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    WavInfo info = new WavInfo();

                    var chunkId = reader.ReadInt32();
                    Trace.Assert(chunkId == 0x46464952);

                    var chunkSize = reader.ReadInt32();

                    var format = reader.ReadInt32();
                    Trace.Assert(format == 0x45564157);

                    var subChunk1Id = reader.ReadInt32();
                    Trace.Assert(subChunk1Id == 0x20746d66);

                    var subChunk1Size = reader.ReadInt32();

                    var audioFormat = reader.ReadInt16();
                    Trace.Assert(audioFormat == 1);

                    var numChannels = reader.ReadInt16();

                    info.SampleRate = reader.ReadInt32();
                    info.ByteRate = reader.ReadInt32();
                    info.BlockAlign = reader.ReadInt16();
                    info.BitsPerSample = reader.ReadInt16();

                    if (subChunk1Size == 18)
                    {
                        // Read any extra values
                        int fmtExtraSize = reader.ReadInt16();
                        reader.ReadBytes(fmtExtraSize);
                    }

                    var subChunk2Id = reader.ReadInt32();
                    Trace.Assert(subChunk2Id == 0x61746164);

                    var subChunk2Size = reader.ReadInt32();

                    ReadDataSection(reader, info, subChunk2Size, numChannels);

                    info.BlockAlign = (short)(info.BitsPerSample / 8);
                    info.ByteRate /= numChannels;

                    return info;
                }
            }
        }

        private static void ReadDataSection(BinaryReader reader, WavInfo info, int subChunk2Size, int numChannels)
        {
            int samples = subChunk2Size / info.BlockAlign;
            List<double> bytes = new List<double>(samples);

            for (int i = 0, channel = 0; i < samples; channel = (channel == numChannels - 1) ? 0 : channel + 1)
            {
                Int16 ampl = 0;
                switch(info.BitsPerSample)
                {
                    case 8:
                        ampl = (Int16)reader.ReadByte();
                        break;
                    case 16:
                        {
                            ampl = reader.ReadInt16();
                        }
                        break;
                    default:
                        Trace.Fail("Bits per second: expected 8 or 16");
                        break;
                }

                if (channel == 0)
                {
                    bytes.Add(ampl / (float)(short.MaxValue));
                    ++i;
                }
            }

            info.Amplitudes = bytes.ToArray();
        }

        private static Int16 SwapBytes(Int16 val)
        {
            UInt16 t = (UInt16)val;

            return (Int16)(((t & 0x00FF) << 8) | ((t & 0xFF00) >> 8));
        }
    }
}
