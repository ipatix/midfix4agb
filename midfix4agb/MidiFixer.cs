using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using csmidi;

namespace midfix4agb
{
    static class MidiFixer
    {

        static public void addAgbCompatibleEvents(MidiFile midiFile, byte modType)
        {
            int[] channelNumber = new int[midiFile.midiTracks.Count];
            for (int i = 0; i < midiFile.midiTracks.Count; i++)
                channelNumber[i] = getChannelNumberFromTrack(midiFile.midiTracks[i]);

            // add all MODT events

            Debug.WriteLine("Adding MODT and LFOS Events...");

            for (int i = 0; i < midiFile.midiTracks.Count; i++)
            {
                if (channelNumber[i] == -1) continue;    // skip track if there is no regular MIDI commands
                // add a MODT controller event in the beginning of the track and LFOS
                midiFile.midiTracks[i].midiEvents.Insert(
                    0, new MessageMidiEvent(0, (byte)channelNumber[i], NormalType.Controller, 21, 44));
                midiFile.midiTracks[i].midiEvents.Insert(
                    0, new MessageMidiEvent(0, (byte)channelNumber[i], NormalType.Controller, 22, modType));
            }

            Debug.WriteLine("Adding BENDR Events...");

            for (int currentTrack = 0; currentTrack < midiFile.midiTracks.Count; currentTrack++)
            {
                MidiTrack trk = midiFile.midiTracks[currentTrack];
                byte rpMSB = 0;
                byte rpLSB = 0;
                for (int currentEvent = 0; currentEvent < trk.midiEvents.Count; currentEvent++)
                {
                    MessageMidiEvent ev;
                    if (trk.midiEvents[currentEvent] is MessageMidiEvent)
                        ev = trk.midiEvents[currentEvent] as MessageMidiEvent;
                    else
                        continue;

                    if (ev.type != NormalType.Controller)
                        continue;

                    switch (ev.parameter1)
                    {
                        case 0x64: // midi RP
                            rpLSB = ev.parameter2;
                            break;
                        case 0x65: // midi RP
                            rpLSB = ev.parameter2;
                            break;
                        case 0x6:  // midi data entry
                            if (rpLSB == 0 && rpMSB == 0)
                            {
                                // insert new event if right parameter slots are selected
                                long cTicks = ev.absoluteTicks;
                                trk.midiEvents.Insert(currentEvent, new MessageMidiEvent(
                                    cTicks, (byte)channelNumber[currentTrack], NormalType.Controller, 20, ev.parameter2));
                                // extend the current event because we added one event and don't want to check this one again
                                currentEvent++;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        static public void addModulationScale(MidiFile midiFile, double modScale)
        {
            Debug.WriteLine("Adding Modulation Scale...");

            for (int currentTrack = 0; currentTrack < midiFile.midiTracks.Count; currentTrack++)
            {
                MidiTrack trk = midiFile.midiTracks[currentTrack];
                for (int currentEvent = 0; currentEvent < trk.midiEvents.Count; currentEvent++)
                {
                    // get current Event data
                    MessageMidiEvent ev;
                    if (trk.midiEvents[currentEvent] is MessageMidiEvent)
                        ev = trk.midiEvents[currentEvent] as MessageMidiEvent;
                    else
                        continue;

                    if (ev.type != NormalType.Controller)
                        continue;
                    if (ev.parameter1 != 0x1) // midi mod controller event
                        continue;

                    long cTicks = ev.absoluteTicks;
                    trk.midiEvents[currentEvent] = new MessageMidiEvent(
                        cTicks, ev.midiChannel, NormalType.Controller, ev.parameter1, (byte)(minMax(0, ev.parameter2 * modScale, 127)));
                }
            }
        }

        static public void combineVolumeAndExpression(MidiFile midiFile)
        {
            Debug.WriteLine("Combining Volume and Expression Events...");

            for (int currentTrack = 0; currentTrack < midiFile.midiTracks.Count; currentTrack++)
            {
                MidiTrack trk = midiFile.midiTracks[currentTrack];
                byte expressionLevel = 127;
                byte volumeLevel = 127;
                for (int currentEvent = 0; currentEvent < trk.midiEvents.Count; currentEvent++)
                {
                    // get current Event data
                    MessageMidiEvent ev;
                    if (trk.midiEvents[currentEvent] is MessageMidiEvent)
                        ev = trk.midiEvents[currentEvent] as MessageMidiEvent;
                    else
                        continue;

                    if (ev.type != NormalType.Controller)
                        continue;

                    if (ev.parameter1 == 0x7)
                    {
                        volumeLevel = ev.parameter2;
                        byte newLevel = (byte)(volumeLevel * expressionLevel / 127);
                        long cTicks = trk.midiEvents[currentEvent].absoluteTicks;
                        trk.midiEvents[currentEvent] = new MessageMidiEvent(cTicks, ev.midiChannel, NormalType.Controller, 0x7, newLevel);
                    }
                    else if (ev.parameter1 == 0xB)
                    {
                        expressionLevel = ev.parameter2;
                        byte newLevel = (byte)(volumeLevel * expressionLevel / 127);
                        long cTicks = trk.midiEvents[currentEvent].absoluteTicks;
                        trk.midiEvents[currentEvent] = new MessageMidiEvent(cTicks, ev.midiChannel, NormalType.Controller, 0x7, newLevel);
                    }
                }
            }
        }

        static public void addExponentialScale(MidiFile midiFile)
        {
            Debug.WriteLine("Applying exponential Volume and Velocity Scale...");

            for (int currentTrack = 0; currentTrack < midiFile.midiTracks.Count; currentTrack++)
            {
                MidiTrack trk = midiFile.midiTracks[currentTrack];
                for (int currentEvent = 0; currentEvent < midiFile.midiTracks[currentTrack].midiEvents.Count; currentEvent++)
                {
                    // get current Event data
                    MessageMidiEvent ev;
                    if (trk.midiEvents[currentEvent] is MessageMidiEvent)
                        ev = trk.midiEvents[currentEvent] as MessageMidiEvent;
                    else
                        continue;

                    if (ev.type == NormalType.Controller && ev.parameter1 == 0x7)
                    {
                        long cTicks = trk.midiEvents[currentEvent].absoluteTicks;
                        trk.midiEvents[currentEvent] = new MessageMidiEvent(
                            cTicks, ev.midiChannel, NormalType.Controller, 0x7, expVol(ev.parameter2));
                    }
                    if (ev.type == NormalType.NoteON)
                    {
                        long cTicks = trk.midiEvents[currentEvent].absoluteTicks;
                        trk.midiEvents[currentEvent] = new MessageMidiEvent(
                            cTicks, ev.midiChannel, NormalType.NoteON, ev.parameter1, expVol(ev.parameter2));
                    }
                }
            }
        }

        static public void removeRedundantMidiEvents(MidiFile midiFile)
        {

        }

        static public void fixLoopCarryBack(MidiFile midiFile)
        {
            Debug.WriteLine("Fixing Loop Carryback Errors...");
            // first of all we need to check if the MIDI actually loops
            // we only have to check the first track for the [ ] brackets in the marker Events
            bool hasLoopStart = false;
            long loopStartTick = 0;
            bool hasLoopEnd = false;
            long loopEndTick = 0;

            if (midiFile.midiTracks.Count == 0)
                return;
            MidiTrack metaTrack = midiFile.midiTracks[0];

            for (int currentEvent = 0; currentEvent < metaTrack.midiEvents.Count; currentEvent++)
            {
                byte[] eventData = metaTrack.midiEvents[currentEvent].getEventData();
                if (eventData[0] != 0xFF || eventData[1] != 0x6)   // if event is META and marker type
                    continue;

                if (eventData[2] == 0x1 && Encoding.ASCII.GetString(eventData, 3, 1) == "[")
                {
                    hasLoopStart = true;
                    loopStartTick = metaTrack.midiEvents[currentEvent].absoluteTicks;
                }
                else if (eventData[2] == 0x1 && Encoding.ASCII.GetString(eventData, 3, 1) == "]")
                {
                    hasLoopEnd = true;
                    loopEndTick = metaTrack.midiEvents[currentEvent].absoluteTicks;
                }
            }

            // we now got the loop points if there are any
            if (hasLoopStart == false || hasLoopEnd == false)
            {
                Debug.WriteLine("MIDI is not looped!");
                return;
            }
            // now the carryback prevention is done
            for (int currentTrack = 0; currentTrack < midiFile.midiTracks.Count; currentTrack++)
            {
                MidiTrack trk = midiFile.midiTracks[currentTrack];
                int midiChannel = getChannelNumberFromTrack(trk);    // first of all we need to get the midi channel on the current track

                agbControllerState loopStartState = new agbControllerState();

                if (trk.midiEvents.Count == 0)
                    continue;  // skip the track if it has no events
                if (midiChannel == -1 && currentTrack != 0)
                    continue;       // skip track if it has no midi events and we don't need to consider the tempo track

                int eventAtLoopStart = trk.midiEvents.Count - 1;

                #region recordStartState
                for (int currentEvent = 0; currentEvent < trk.midiEvents.Count; currentEvent++)
                {
                    // now all events get recorded on continously update the loop start state
                    if (trk.midiEvents[currentEvent].absoluteTicks > loopStartTick)
                    {
                        eventAtLoopStart = currentEvent;
                        break;
                    }
                    byte[] eventData = trk.midiEvents[currentEvent].getEventData();
                    if (eventData[0] == 0xFF && eventData[1] == 0x51)   // check if META tempo event occurs
                    {
                        loopStartState.Tempo[0] = eventData[3]; // set all tempo values
                        loopStartState.Tempo[1] = eventData[4];
                        loopStartState.Tempo[2] = eventData[5];
                    }
                    else if (eventData[0] >> 4 == 0xB)  // is event a controller event?
                    {
                        if (eventData[1] == 0x1)    // if MOD controller
                            loopStartState.Mod = eventData[2];  // save mod state
                        else if (eventData[1] == 0x7)
                            loopStartState.Volume = eventData[2];   // save volume state
                        else if (eventData[1] == 0xA)
                            loopStartState.Pan = eventData[2];  // save pan position
                        else if (eventData[1] == 0x14)
                            loopStartState.BendR = eventData[2];    // save pseudo AGB bendr
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
                /* override all changes from the loop start state to the loop end state so we can
                 * continue on with checking the trackdata for carryback errors we need to correct
                 */
                agbControllerState loopEndState = (agbControllerState)loopStartState.Clone();
                // recorded loop start state, now record until 

                int eventAtLoopEnd = trk.midiEvents.Count;

                #region recordLoopEndState
                for (int currentEvent = eventAtLoopStart; currentEvent < trk.midiEvents.Count; currentEvent++)
                {
                    // now all events get recorded on continously update the loop start state
                    if (trk.midiEvents[currentEvent].absoluteTicks >= loopEndTick)
                    {
                        // if the loop end occurs before the end of the event data set it's value manually
                        eventAtLoopEnd = currentEvent;
                        break;
                    }
                    byte[] eventData = trk.midiEvents[currentEvent].getEventData();
                    if (eventData[0] == 0xFF && eventData[1] == 0x51)   // check if META tempo event occurs
                    {
                        loopEndState.Tempo[0] = eventData[3]; // set all tempo values
                        loopEndState.Tempo[1] = eventData[4];
                        loopEndState.Tempo[2] = eventData[5];
                    }
                    else if (eventData[0] >> 4 == 0xB)  // is event a controller event?
                    {
                        if (eventData[1] == 0x1)
                            loopEndState.Mod = eventData[2];      // save mod state
                        else if (eventData[1] == 0x7)
                            loopEndState.Volume = eventData[2];   // save volume state
                        else if (eventData[1] == 0xA)
                            loopEndState.Pan = eventData[2];      // save pan position
                        else if (eventData[1] == 0x14)
                            loopEndState.BendR = eventData[2];    // save pseudo AGB bendr
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
                if (!Enumerable.SequenceEqual(loopStartState.Tempo, loopEndState.Tempo) &&
                    loopStartState.Tempo[0] != 0 && loopStartState.Tempo[1] != 0 && loopStartState.Tempo[2] != 0)
                {
                    if (eventAtLoopStart >= trk.midiEvents.Count)
                        trk.midiEvents.Add(new MetaMidiEvent(loopStartTick, 0x51, loopStartState.Tempo));
                    else
                        trk.midiEvents.Insert(eventAtLoopStart, new MetaMidiEvent(loopStartTick, 0x51, loopStartState.Tempo));
                }
                // only do this fixing if the midi channel is actually defined (META only tracks are skipped here)
                if (midiChannel == -1)
                    continue;

                // only fix if voice is a valid voice number (0-127)
                if (loopStartState.Voice != loopEndState.Voice && loopStartState.Voice != 0xFF)
                {
                    if (eventAtLoopStart >= trk.midiEvents.Count)
                        trk.midiEvents.Add(new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Program, loopStartState.Voice, 0x0));
                    else
                        trk.midiEvents.Insert(eventAtLoopStart, new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Program, loopStartState.Voice, 0x0));
                }
                // fix volume
                if (loopStartState.Volume != loopEndState.Volume && loopStartState.Volume != 0xFF)
                {
                    if (eventAtLoopStart >= trk.midiEvents.Count)
                        trk.midiEvents.Add(new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Controller, 0x7, loopStartState.Volume));
                    else
                        trk.midiEvents.Insert(eventAtLoopStart, new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Controller, 0x7, loopStartState.Volume));
                }
                // fix PAN
                if (loopStartState.Pan != loopEndState.Pan && loopStartState.Pan != 0xFF)
                {
                    if (eventAtLoopStart >= trk.midiEvents.Count)
                        trk.midiEvents.Add(new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Controller, 0xA, loopStartState.Pan));
                    else
                        trk.midiEvents.Insert(eventAtLoopStart, new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Controller, 0xA, loopStartState.Pan));
                }
                // fix BENDR
                if (loopStartState.BendR != loopEndState.BendR && loopStartState.BendR != 0xFF)
                {
                    if (eventAtLoopStart >= trk.midiEvents.Count)
                        trk.midiEvents.Add(new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Controller, 20, loopStartState.BendR));
                    else
                        trk.midiEvents.Insert(eventAtLoopStart, new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Controller, 20, loopStartState.BendR));
                }
                // fix MOD
                if (loopStartState.Mod != loopEndState.Mod && loopStartState.Mod != 0xFF)
                {
                    if (eventAtLoopStart >= trk.midiEvents.Count)
                        trk.midiEvents.Add(new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Controller, 0x1, loopStartState.Mod));
                    else
                        trk.midiEvents.Insert(eventAtLoopStart, new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.Controller, 0x1, loopStartState.Mod));
                }
                // fix BEND
                if ((loopStartState.BendLSB != loopEndState.BendLSB ||
                    loopStartState.BendMSB != loopEndState.BendMSB) &&
                    loopStartState.BendLSB != 0xFF)
                {
                    if (eventAtLoopStart >= trk.midiEvents.Count)
                        trk.midiEvents.Add(new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.PitchBend, loopStartState.BendLSB, loopStartState.BendMSB));
                    else
                        trk.midiEvents.Insert(eventAtLoopStart, new MessageMidiEvent(
                            loopStartTick, (byte)midiChannel, NormalType.PitchBend, loopStartState.BendLSB, loopStartState.BendMSB));
                }
            }
        }

        static int getChannelNumberFromTrack(MidiTrack trk)
        {
            int channelNumber = -1;
            foreach (MidiEvent ev in trk.midiEvents)
            {
                byte[] eventData = ev.getEventData();
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
            if (val < minVal)
                val = minVal;
            else if (val > maxVal)
                val = maxVal;
            return val;
        }

        static byte expVol(byte volume)
        {
            double returnValue = volume;
            if (returnValue == 0)
                return 0;
            returnValue /= 127.0;
            returnValue = Math.Pow(returnValue, 10.0 / 6.0);
            returnValue *= 127.0;
            returnValue = Math.Round(returnValue, MidpointRounding.AwayFromZero);
            return Convert.ToByte(returnValue);
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
            Volume = 0xFF;
            Pan = 0xFF;
            BendR = 0xFF;
            BendMSB = 0xFF;
            BendLSB = 0xFF;
            Mod = 0xFF;
        }

        public object Clone()
        {
            return MemberwiseClone();
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
