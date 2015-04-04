using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileMIDI
{
    public class FileMIDI
    {
        public ushort timeDivision;

        internal List<MidiTrack> MidiTracks;

        public FileMIDI()
        {
            MidiTracks = new List<MidiTrack>();
            timeDivision = 0;
        }

        public void loadMidiFromFile(string filePath)
        {
            // first clear the currently loaded Midi by clearing the List
            bool succeeded = MidiLoader.loadFromFile(filePath, ref MidiTracks, ref timeDivision);      // load the Midi using the MidiLoader class
        }

        public void saveMidiToFile(string filePath)
        {
            MidiSaver.saveToFile(filePath, ref MidiTracks, timeDivision);         // save the Midi to a file
        }

        internal void sortTrackEvents(ref List<MidiTrack> sortingTracks)
        {
            for (int currentTrack = 0; currentTrack < sortingTracks.Count; currentTrack++)
            {
                sortingTracks[currentTrack].MidiEvents = sortingTracks[currentTrack].MidiEvents.OrderBy(item => item.getAbsoluteTicks()).ToList();
            }
        }
    }

    internal class MidiEvent
    {
        private long absoluteTicks;

        public void setAbsoluteTicks(long ticks)
        {
            absoluteTicks = ticks;
        }

        public long getAbsoluteTicks()
        {
            return absoluteTicks;
        }

        public EventType type;

        private object eventData;

        public byte[] getEventData()
        {
            byte[] returnData;
            switch (type)
            {
                case EventType.Meta:
                    returnData = ((MetaMessage)eventData).getData();
                    break;
                case EventType.Normal:
                    returnData = ((MidiMessage)eventData).getData();
                    break;
                case EventType.SysEx:
                    returnData = ((SysExMessage)eventData).getData();
                    break;
                default:
                    returnData = new byte[0];
                    break;
            }
            return returnData;
        }

        public MidiEvent(long ticks, byte midiChannel, byte par1, byte par2, NormalType _type)
        {
            absoluteTicks = ticks;
            eventData = new MidiMessage(midiChannel, _type, par1, par2);
            type = EventType.Normal;
        }

        public MidiEvent(long ticks, byte metaSysExType, byte[] _eventData, bool isMetaEvent)
        {
            absoluteTicks = ticks;
            if (isMetaEvent)
            {
                eventData = new MetaMessage(metaSysExType, _eventData);
                type = EventType.Meta;
            }
            else
            {
                eventData = new SysExMessage(metaSysExType, _eventData);
                type = EventType.SysEx;
            }
        }
    }

    internal class MidiTrack
    {
        internal List<MidiEvent> MidiEvents;

        internal MidiTrack()
        {
            MidiEvents = new List<MidiEvent>();
        }
    }

    internal class MidiMessage
    {
        private byte midiChannel;
        private byte parameter1;
        private byte parameter2;
        private NormalType type;

        internal byte[] getData()
        {
            byte[] returnData = new byte[3];
            switch (type)
            {
                case NormalType.NoteOFF:                // #0x8
                    returnData[0] = (byte)(midiChannel | (0x8 << 4));
                    returnData[1] = parameter1;     // note number
                    returnData[2] = parameter2;     // velocity
                    break;
                case NormalType.NoteON:                 // #0x9
                    returnData[0] = (byte)(midiChannel | (0x9 << 4));
                    returnData[1] = parameter1;     // note number
                    returnData[2] = parameter2;     // velocity
                    break;
                case NormalType.NoteAftertouch:         // #0xA
                    returnData[0] = (byte)(midiChannel | (0xA << 4));
                    returnData[1] = parameter1;     // note number
                    returnData[2] = parameter2;     // aftertouch value
                    break;
                case NormalType.Controller:             // #0xB
                    returnData[0] = (byte)(midiChannel | (0xB << 4));
                    returnData[1] = parameter1;     // controller number
                    returnData[2] = parameter2;     // controller value
                    break;
                case NormalType.Program:                // #0xC
                    returnData = new byte[2];           // this event doesn't have a 2nd parameter
                    returnData[0] = (byte)(midiChannel | (0xC << 4));
                    returnData[1] = parameter1;     // program number
                    break;
                case NormalType.ChannelAftertouch:      // #0xD
                    returnData = new byte[2];           // this event doesn't have a 2nd parameter
                    returnData[0] = (byte)(midiChannel | (0xD << 4));
                    returnData[1] = parameter1;     // aftertouch value
                    break;
                case NormalType.PitchBend:              // #0xE
                    returnData[0] = (byte)(midiChannel | (0xE << 4));
                    returnData[1] = parameter1;     // pitch LSB
                    returnData[2] = parameter2;     // pitch MSB
                    break;
            }
            return returnData;
        }

        internal MidiMessage(byte _channel, NormalType _type, byte _par1, byte _par2)
        {
            midiChannel = _channel;
            type = _type;
            parameter1 = _par1;
            parameter2 = _par2;
        }
    }

    internal class MetaMessage
    {
        private byte[] data;
        private byte metaType;

        internal byte[] getData()     // returns a raw byte array of this META Event in the MIDI file
        {
            byte[] dataLength = VariableLength.ConvertToVariableLength(data.Length);
            byte[] returnData = new byte[data.Length + 2 + dataLength.Length];
            returnData[0] = 0xFF;
            returnData[1] = metaType;
            Array.Copy(dataLength, 0, returnData, 2, dataLength.Length);
            Array.Copy(data, 0, returnData, 2 + dataLength.Length, data.Length);
            return returnData;
        }

        internal MetaMessage(byte _metaType, byte[] _data)
        {
            metaType = _metaType;
            data = _data;
        }
    }

    internal class SysExMessage
    {
        private byte[] data;
        private byte sysexType;

        internal byte[] getData()     // returns a raw byte array of this SysEx Event in the MIDI file
        {
            byte[] dataLength = VariableLength.ConvertToVariableLength(data.Length);
            byte[] returnData = new byte[data.Length + 1 + dataLength.Length];
            returnData[0] = sysexType;
            Array.Copy(dataLength, 0, returnData, 1, dataLength.Length);
            Array.Copy(data, 0, returnData, 1 + dataLength.Length, data.Length);
            return returnData;
        }

        internal SysExMessage(byte _sysexType, byte[] _data)
        {
            sysexType = _sysexType;
            data = _data;
        }
    }

    internal enum EventType
    {
        Normal,
        Meta, 
        SysEx
    }

    internal enum NormalType
    {
        NoteON, 
        NoteOFF, 
        NoteAftertouch, 
        Controller, 
        Program, 
        ChannelAftertouch, 
        PitchBend
    }

    internal static class VariableLength
    {
        internal static byte[] ConvertToVariableLength(long value)
        {
            int i = 0;
            byte[] returnData = new byte[i + 1];
            returnData[i] = (byte)(value & 0x7F);
            i++;

            value = value >> 7;

            while (value != 0)
            {
                Array.Resize(ref returnData, i + 1);
                returnData[i] = (byte)((value & 0x7F) | 0x80);
                value = value >> 7;
                i++;
            }

            Array.Reverse(returnData);
            return returnData;
        }

        internal static long ConvertToInt(byte[] values)
        {
            long value = 0;
            for (int i = 0; i < values.Length; i++)
            {
                value = value << 7;     // doesn't matter on first loop anyway, if it's one of the next loops it shifts the latest value up 7 bits
                value = value | (byte)(values[i] & 0x7F);
            }
            return value;
        }
    }
}
