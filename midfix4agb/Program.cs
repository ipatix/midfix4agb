using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace midfix4agb
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            string inputFile = "";
            string outputFile = "";
            double modScale = 0.5;
            byte modt = 0;
            bool fixAgbEvents = true;
            bool fixVolScale = true;
            bool fixLoop = true;

            // do an argument loop
            int isInputFile = 0;

            if (args.Length == 0)
            {
                showUsage();
            }

            for (int i = 0; i < args.Length; i++)
            {
                string[] splitArg = args[i].Split('=');
                if (splitArg.Length == 1)
                {
                    if (isInputFile == 0)
                    {
                        inputFile = splitArg[0];
                        isInputFile++;
                    }
                    else if (isInputFile == 1)
                    {
                        outputFile = splitArg[0];
                        isInputFile++;
                    }
                    else
                    {
                        showUsage();
                        return;
                    }
                }
                else if (splitArg.Length == 2)  // if regular argument
                {
                    switch (splitArg[0])
                    {
                        case "modscale":
                            modScale = Convert.ToDouble(splitArg[1]);
                            if (modScale < 0)
                            {
                                showUsage();
                                Console.WriteLine("The Mod Scale must not be negative!");
                                return;
                            }
                            break;
                        case "modt":
                            modt = Convert.ToByte(splitArg[1]);
                            if (modt > 2)
                            {
                                showUsage();
                                Console.WriteLine("The MODT must be 0, 1 or 2!");
                                return;
                            }
                            break;
                        case "fixloop":
                            fixLoop = Convert.ToBoolean(splitArg[1]);
                            break;
                        case "fixvolscale":
                            fixVolScale = Convert.ToBoolean(splitArg[1]);
                            break;
                        case "addagbevents":
                            fixAgbEvents = Convert.ToBoolean(splitArg[1]);
                            break;
                        default:
                            Console.WriteLine("Unknown argument: " + splitArg[0]);
                            showUsage();
                            break;
                    }
                }
            }

            // automatically name new outfile name if none is specified

            if (inputFile != "") inputFile = Path.GetFullPath(inputFile);

            if (inputFile != "" && outputFile == "")
            {
                outputFile = Path.GetFileNameWithoutExtension(inputFile) + "_FINAL.mid";
                outputFile = Path.Combine(Path.GetDirectoryName(inputFile), outputFile);
            }
            else
            {
                outputFile = Path.GetFullPath(outputFile);
            }

            Console.WriteLine("Inputfile:");
            Console.WriteLine(inputFile);
            Console.WriteLine("Outputfile:");
            Console.WriteLine(outputFile);

            csmidi.MidiFile midiFile = new csmidi.MidiFile();
            midiFile.loadMidiFromFile(inputFile);

            if (fixAgbEvents)
                MidiFixer.addAgbCompatibleEvents(midiFile, modt);
            if (fixVolScale)
                MidiFixer.combineVolumeAndExpression(midiFile);
            MidiFixer.addModulationScale(midiFile, modScale);
            if (fixVolScale)
                MidiFixer.addExponentialScale(midiFile);
            if (fixLoop)
                MidiFixer.fixLoopCarryBack(midiFile);

            midiFile.saveMidiToFile(outputFile);
            timer.Stop();

            Console.WriteLine("Fixed midi in : " + timer.ElapsedMilliseconds.ToString() + "ms");
        }

        static void showUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("$ midfix4agb.exe \"inputfile.mid\" [\"outputfile.mid\"] [optional arguments]");
            Console.WriteLine("Optional Arguments (examples):");
            Console.WriteLine();
            Console.WriteLine("modscale=1.0       default: 0.5");
            Console.WriteLine("modt=0             default: 0");
            Console.WriteLine("fixloop=true       default: true");
            Console.WriteLine("fixvolscale=true   default: true");
            Environment.Exit(1);
        }
    }
}
