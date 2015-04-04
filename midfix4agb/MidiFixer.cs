using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileMIDI
{
    static class MidiFixer
    {

        static public void addAgbCompatibleEvents(ref FileMIDI midiFile, byte modType)
        {
            int[] channelNumber = new int[midiFile.MidiTracks.Count];
            for (int currentTrack = 0; currentTrack < midiFile.MidiTracks.Count; currentTrack++)
            {
                channelNumber[currentTrack] = getChannelNumberFromTrack(ref midiFile, currentTrack);
            }

            // add all MODT events

            Console.WriteLine("Adding MODT and LFOS Events...");

            for (int currentTrack = 0; currentTrack < midiFile.MidiTracks.Count; currentTrack++)
            {
                if (channelNumber[currentTrack] == -1) continue;    // skip track if there is no regular MIDI commands
                // add a MODT controller event in the beginning of the track and LFOS
                midiFile.MidiTracks[currentTrack].MidiEvents.Insert(0, new MidiEvent(0, (byte)channelNumber[currentTrack], 21, 44, NormalType.Controller));
                midiFile.MidiTracks[currentTrack].MidiEvents.Insert(0, new MidiEvent(0, (byte)channelNumber[currentTrack], 22, modType, NormalType.Controller));
            }

            Console.WriteLine("Adding BENDR Events...");

            for (int currentTrack = 0; currentTrack < midiFile.MidiTracks.Count; currentTrack++)
            {
                byte rpMSB = 0;
                byte rpLSB = 0;
                int newEventCount = midiFile.MidiTracks[currentTrack].MidiEvents.Count;
                for (int currentEvent = 0; currentEvent < newEventCount; currentEvent++)
                {
                    byte[] eventData = midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getEventData();
                    if (eventData[0] >> 4 == 0xB)
                    {
                        switch (eventData[1])
                        {
                            case 0x64:
                                rpLSB = eventData[2];
                                break;
                            case 0x65:
                                rpLSB = eventData[2];
                                break;
                            case 0x6:
                                if (rpLSB == 0 && rpMSB == 0)
                                {
                                    // insert new event if right parameter slots are selected
                                    midiFile.MidiTracks[currentTrack].MidiEvents.Insert(currentEvent, new MidiEvent(midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getAbsoluteTicks(), (byte)channelNumber[currentTrack], 20, eventData[2], NormalType.Controller));
                                    newEventCount++;    // extend the new event count because we added an event
                                    currentEvent++;     // extend the current event because we added one event and don't want to check this one again
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        static public void addModulationScale(ref FileMIDI midiFile, double modScale)
        {
            Console.WriteLine("Adding Modulation Scale...");

            for (int currentTrack = 0; currentTrack < midiFile.MidiTracks.Count; currentTrack++)
            {
                for (int currentEvent = 0; currentEvent < midiFile.MidiTracks[currentTrack].MidiEvents.Count; currentEvent++)
                {
                    // get current Event data
                    byte[] eventData = midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getEventData();
                    if (eventData[0] >> 4 == 0xB)
                    {
                        if (eventData[1] == 0x1)
                        {
                            midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent] = new MidiEvent(midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getAbsoluteTicks(), (byte)(eventData[0] & 0xF), eventData[1], Convert.ToByte(minMax(0, eventData[2] * modScale, 127)), NormalType.Controller);
                        }
                    }
                }
            }
        }

        static public void combineVolumeAndExpression(ref FileMIDI midiFile)
        {
            Console.WriteLine("Combining Volume and Expression Events...");

            for (int currentTrack = 0; currentTrack < midiFile.MidiTracks.Count; currentTrack++)
            {
                byte expressionLevel = 127;
                byte volumeLevel = 127;
                for (int currentEvent = 0; currentEvent < midiFile.MidiTracks[currentTrack].MidiEvents.Count; currentEvent++)
                {
                    // get current Event data
                    byte[] eventData = midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getEventData();
                    if (eventData[0] >> 4 == 0xB)
                    {
                        if (eventData[1] == 0x7)
                        {
                            volumeLevel = eventData[2];
                            byte newLevel = (byte)(volumeLevel * expressionLevel / 127);
                            midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent] = new MidiEvent(midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getAbsoluteTicks(), (byte)(eventData[0] & 0xF), 0x7, newLevel, NormalType.Controller);
                        }
                        else if (eventData[1] == 0xB)
                        {
                            expressionLevel = eventData[2];
                            byte newLevel = (byte)(volumeLevel * expressionLevel / 127);
                            midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent] = new MidiEvent(midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getAbsoluteTicks(), (byte)(eventData[0] & 0xF), 0x7, newLevel, NormalType.Controller);
                        }
                    }
                }
            }
        }

        static public void addExponentialScale(ref FileMIDI midiFile)
        {
            Console.WriteLine("Applying exponential Volume and Velocity Scale...");

            for (int currentTrack = 0; currentTrack < midiFile.MidiTracks.Count; currentTrack++)
            {
                for (int currentEvent = 0; currentEvent < midiFile.MidiTracks[currentTrack].MidiEvents.Count; currentEvent++)
                {
                    // get current Event data
                    byte[] eventData = midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getEventData();
                    if (eventData[0] >> 4 == 0xB)
                    {
                        if (eventData[1] == 0x7)
                        {
                            midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent] = new MidiEvent(midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getAbsoluteTicks(), (byte)(eventData[0] & 0xF), 0x7, expVol(eventData[2]), NormalType.Controller);
                        }
                    }
                    if (eventData[0] >> 4 == 0x9)
                    {
                        midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent] = new MidiEvent(midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getAbsoluteTicks(), (byte)(eventData[0] & 0xF), eventData[1], expVol(eventData[2]), NormalType.NoteON);
                    }
                }
            }
        }

        static public void removeRedundantMidiEvents(ref FileMIDI midiFile)
        {

        }

        static public void fixLoopCarryBack(ref FileMIDI midiFile)
        {
            Console.WriteLine("Fixing Loop Carryback Errors...");
            // first of all we need to check if the MIDI actually loops
            // we only have to check the first track for the [ ] brackets in the marker Events
            bool hasLoopStart = false;
            long loopStartTick = 0;
            bool hasLoopEnd = false;
            long loopEndTick = 0;

            for (int currentEvent = 0; currentEvent < midiFile.MidiTracks[0].MidiEvents.Count; currentEvent++)
            {
                byte[] eventData = midiFile.MidiTracks[0].MidiEvents[currentEvent].getEventData();
                if (eventData[0] == 0xFF)   // if event is META
                {
                    if (eventData[1] == 0x6)    // if event is Marker
                    {
                        if (eventData[2] == 0x1 && Encoding.ASCII.GetString(eventData, 3, 1) == "[")
                        {
                            hasLoopStart = true;
                            loopStartTick = midiFile.MidiTracks[0].MidiEvents[currentEvent].getAbsoluteTicks();
                        }
                        else if (eventData[2] == 0x1 && Encoding.ASCII.GetString(eventData, 3, 1) == "]")
                        {
                            hasLoopEnd = true;
                            loopEndTick = midiFile.MidiTracks[0].MidiEvents[currentEvent].getAbsoluteTicks();
                        }
                    }
                }
            }
            // we now got the loop points if there are any
            if (hasLoopStart == false || hasLoopEnd == false)
            {
                Console.WriteLine("MIDI is not looped!");
                return;
            }
            // now the carryback prevention is done because the program will return if there is no loop to fix
            for (int currentTrack = 0; currentTrack < midiFile.MidiTracks.Count; currentTrack++)
            {
                int midiChannel = getChannelNumberFromTrack(ref midiFile, currentTrack);    // first of all we need to get the midi channel on the current track

                agbControllerState loopStartState = new agbControllerState();
                agbControllerState loopEndState = new agbControllerState();

                if (midiFile.MidiTracks[currentTrack].MidiEvents.Count == 0) continue;  // skip the track if it has no events
                if (midiChannel == -1 && currentTrack != 0) continue;       // skip track if it has no midi events and we don't need to consider the tempo track

                int eventAtLoopStart = midiFile.MidiTracks[currentTrack].MidiEvents.Count - 1;

                #region recordStartState
                for (int currentEvent = 0; currentEvent < midiFile.MidiTracks[currentTrack].MidiEvents.Count; currentEvent++)
                {
                    // now all events get recorded on continously update the loop start state
                    if (midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getAbsoluteTicks() > loopStartTick)
                    {
                        eventAtLoopStart = currentEvent;
                        break;
                    }
                    byte[] eventData = midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getEventData();
                    if (eventData[0] == 0xFF)   // check if META tempo event occurs
                    {
                        if (eventData[1] == 0x51)   // ist META event a Tempo event?
                        {
                            loopStartState.Tempo[0] = eventData[3]; // set all tempo values
                            loopStartState.Tempo[1] = eventData[4];
                            loopStartState.Tempo[2] = eventData[5];
                        }
                    }
                    else if (eventData[0] >> 4 == 0xB)  // is event a controller event?
                    {
                        if (eventData[1] == 0x1)    // if MOD controller
                        {
                            loopStartState.Mod = eventData[2];  // save mod state
                        }
                        else if (eventData[1] == 0x7)
                        {
                            loopStartState.Volume = eventData[2];   // save volume state
                        }
                        else if (eventData[1] == 0xA)
                        {
                            loopStartState.Pan = eventData[2];  // save pan position
                        }
                        else if (eventData[1] == 0x14)
                        {
                            loopStartState.BendR = eventData[2];    // save pseudo AGB bendr
                        }
                    }
                    else if (eventData[0] >> 4 == 0xC)      // if voice change event
                    {
                        loopStartState.Voice = eventData[1];
                    }
                    else if (eventData[0] >> 4 == 0xE)      // if pitch bend
                    {
                        loopStartState.BendLSB = eventData[1];
                        loopStartState.BendMSB = eventData[2];
                    }
                }
                #endregion
                loopEndState = (agbControllerState)loopStartState.Clone();  // override all changes from the loop start state to the loop end state so we can continue on with checking the trackdata for carryback errors we need to correct     
                // recorded loop start state, now record until 

                int eventAtLoopEnd = midiFile.MidiTracks[currentTrack].MidiEvents.Count;

                #region recordLoopEndState
                for (int currentEvent = eventAtLoopStart; currentEvent < midiFile.MidiTracks[currentTrack].MidiEvents.Count; currentEvent++)
                {
                    // now all events get recorded on continously update the loop start state
                    if (midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getAbsoluteTicks() >= loopEndTick)
                    {
                        eventAtLoopEnd = currentEvent;      // if the loop end occurs before the end of the event data set it's value manually
                        break;
                    }
                    byte[] eventData = midiFile.MidiTracks[currentTrack].MidiEvents[currentEvent].getEventData();
                    if (eventData[0] == 0xFF)   // check if META tempo event occurs
                    {
                        if (eventData[1] == 0x51)   // ist META event a Tempo event?
                        {
                            loopEndState.Tempo[0] = eventData[3]; // set all tempo values
                            loopEndState.Tempo[1] = eventData[4];
                            loopEndState.Tempo[2] = eventData[5];
                        }
                    }
                    else if (eventData[0] >> 4 == 0xB)  // is event a controller event?
                    {
                        if (eventData[1] == 0x1)    // if MOD controller
                        {
                            loopEndState.Mod = eventData[2];  // save mod state
                        }
                        else if (eventData[1] == 0x7)
                        {
                            loopEndState.Volume = eventData[2];   // save volume state
                        }
                        else if (eventData[1] == 0xA)
                        {
                            loopEndState.Pan = eventData[2];  // save pan position
                        }
                        else if (eventData[1] == 0x14)
                        {
                            loopEndState.BendR = eventData[2];    // save pseudo AGB bendr
                        }
                    }
                    else if (eventData[0] >> 4 == 0xC)      // if voice change event
                    {
                        loopEndState.Voice = eventData[1];
                    }
                    else if (eventData[0] >> 4 == 0xE)      // if pitch bend
                    {
                        loopEndState.BendLSB = eventData[1];
                        loopEndState.BendMSB = eventData[2];
                    }
                }
                #endregion

                // now we need to fill in the events at the loop end event slot "eventAtLoopEnd"
                // check if the values vary and set them accordingly
                if (Enumerable.SequenceEqual(loopStartState.Tempo, loopEndState.Tempo) == false) // check if tempo is the same
                {
                    if (loopStartState.Tempo[0] != 0 && loopStartState.Tempo[1] != 0 && loopStartState.Tempo[2] != 0)   // don't fix it if the tempo is not set to a valid value
                    {
                        if (eventAtLoopStart >= midiFile.MidiTracks[currentTrack].MidiEvents.Count)
                        {
                            midiFile.MidiTracks[currentTrack].MidiEvents.Add(new MidiEvent(loopStartTick, 0x51, loopStartState.Tempo, true));
                        }
                        else
                        {
                            midiFile.MidiTracks[currentTrack].MidiEvents.Insert(eventAtLoopStart, new MidiEvent(loopStartTick, 0x51, loopStartState.Tempo, true));
                        }
                    }
                }
                if (midiChannel != -1)     // only do this fixing if the midi channel is actually defined (META only tracks are skipped here)
                {
                    if (loopStartState.Voice != loopEndState.Voice)
                    {
                        if (loopStartState.Voice != 0xFF)   // only fix if voice is a valid voice number (0-127)
                        {
                            if (eventAtLoopStart >= midiFile.MidiTracks[currentTrack].MidiEvents.Count)
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Add(new MidiEvent(loopStartTick, (byte)midiChannel, loopStartState.Voice, 0x0, NormalType.Program));
                            }
                            else
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Insert(eventAtLoopStart, new MidiEvent(loopStartTick, (byte)midiChannel, loopStartState.Voice, 0x0, NormalType.Program));
                            }
                        }
                    }
                    if (loopStartState.Volume != loopEndState.Volume)      // fix volume
                    {
                        if (loopStartState.Volume != 0xFF)
                        {
                            if (eventAtLoopStart >= midiFile.MidiTracks[currentTrack].MidiEvents.Count)
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Add(new MidiEvent(loopStartTick, (byte)midiChannel, 0x7, loopStartState.Volume, NormalType.Controller));
                            }
                            else
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Insert(eventAtLoopStart, new MidiEvent(loopStartTick, (byte)midiChannel, 0x7, loopStartState.Volume, NormalType.Controller));
                            }
                        }
                    }
                    if (loopStartState.Pan != loopEndState.Pan)      // fix PAN
                    {
                        if (loopStartState.Pan != 0xFF)
                        {
                            if (eventAtLoopStart >= midiFile.MidiTracks[currentTrack].MidiEvents.Count)
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Add(new MidiEvent(loopStartTick, (byte)midiChannel, 0xA, loopStartState.Pan, NormalType.Controller));
                            }
                            else
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Insert(eventAtLoopStart, new MidiEvent(loopStartTick, (byte)midiChannel, 0xA, loopStartState.Pan, NormalType.Controller));
                            }
                        }
                    }
                    if (loopStartState.BendR != loopEndState.BendR)      // fix BENDR
                    {
                        if (loopStartState.BendR != 0xFF)
                        {
                            if (eventAtLoopStart >= midiFile.MidiTracks[currentTrack].MidiEvents.Count)
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Add(new MidiEvent(loopStartTick, (byte)midiChannel, 20, loopStartState.BendR, NormalType.Controller));
                            }
                            else
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Insert(eventAtLoopStart, new MidiEvent(loopStartTick, (byte)midiChannel, 20, loopStartState.BendR, NormalType.Controller));
                            }
                        }
                    }
                    if (loopStartState.Mod != loopEndState.Mod)      // fix MOD
                    {
                        if (loopStartState.Mod != 0xFF)
                        {
                            if (eventAtLoopStart >= midiFile.MidiTracks[currentTrack].MidiEvents.Count)
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Add(new MidiEvent(loopStartTick, (byte)midiChannel, 0x1, loopStartState.Mod, NormalType.Controller));
                            }
                            else
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Insert(eventAtLoopStart, new MidiEvent(loopStartTick, (byte)midiChannel, 0x1, loopStartState.Mod, NormalType.Controller));
                            }
                        }
                    }
                    if (loopStartState.BendLSB != loopEndState.BendLSB || loopStartState.BendMSB != loopEndState.BendMSB)      // fix BEND
                    {
                        if (loopStartState.BendLSB != 0xFF) // we don't need to check MSB because if this one isn't 0xFF the other one won't be 0xFF either
                        {
                            if (eventAtLoopStart >= midiFile.MidiTracks[currentTrack].MidiEvents.Count)
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Add(new MidiEvent(loopStartTick, (byte)midiChannel, loopStartState.BendLSB, loopStartState.BendMSB, NormalType.PitchBend));
                            }
                            else
                            {
                                midiFile.MidiTracks[currentTrack].MidiEvents.Insert(eventAtLoopStart, new MidiEvent(loopStartTick, (byte)midiChannel, loopStartState.BendLSB, loopStartState.BendMSB, NormalType.PitchBend));
                            }
                        }
                    }
                }
            }
        }

        static int getChannelNumberFromTrack(ref FileMIDI midiFile, int trackNumber)
        {
            int channelNumber = -1;
            for (int currentEvent = 0; currentEvent < midiFile.MidiTracks[trackNumber].MidiEvents.Count; currentEvent++)
            {
                byte[] eventData = midiFile.MidiTracks[trackNumber].MidiEvents[currentEvent].getEventData();
                if ((eventData[0] >> 4) <= 0xE & (eventData[0] >> 4) >= 0x8)
                {
                    channelNumber = eventData[0] & 0xF;
                    break;
                }
            }
            return channelNumber;
        }

        static double minMax(double minVal, double val, double maxVal)
        {
            if (val < minVal) val = minVal;
            else if (val > maxVal) val = maxVal;
            return val;
        }

        static byte expVol(byte volume)
        {
            double returnValue = volume;
            if (returnValue == 0) return 0;
            returnValue /= 127;
            returnValue = Math.Pow(returnValue, 10.0 / 6.0);
            return Convert.ToByte(returnValue * 127);
        }
    }

    class agbControllerState : ICloneable
    {
        public agbControllerState()
        {
            Tempo = new byte[3];    // 0xFF means undefined, works since none of the values actually supports these values, except TEMPO which is 0x0
            Tempo[0] = 0x0;
            Tempo[1] = 0x0;
            Tempo[2] = 0x0;
            Voice = 0xFF;
            Volume = 0x7F;
            Pan = 0x40;
            BendR = 0xFF;
            BendMSB = 0xFF;
            BendLSB = 0xFF;
            Mod = 0x00;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public byte[] Tempo { get; set; }
        public byte Voice { get; set; }
        public byte Volume { get; set; }
        public byte Pan { get; set; }
        public byte BendR { get; set; }
        public byte BendMSB { get; set; }
        public byte BendLSB { get; set; }
        public byte Mod { get; set; }
    }
}
