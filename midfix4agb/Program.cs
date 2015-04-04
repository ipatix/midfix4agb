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
            Console.WriteLine("##################################################");
            Console.WriteLine("midfix4agb (c) 2014 by ipatix");
            Console.WriteLine();
            string inputFile = "";
            string outputFile = "";
            double modScale = 0.5;
            byte modt = 0;
            bool fixAgbEvents = true;
            bool fixVolScale = true;
            bool fixLoop = true;
            bool debugMode = false;

            // debug setting
            //inputFile = @"C:\Users\michael\Projects\SotS\Music\MID\CC_Grasslands_of_Time.mid";
            //outputFile = @"C:\Users\michael\Projects\SotS\Music\MID_FINAL\CC_Grasslands_of_Time_FINAL.mid";

            // do an argument loop

            // new debug settings
            /*args = new string[6];
            args[0] = @"C:\Users\michael\Desktop\AGroover.mid";
            args[1] = @"C:\Users\michael\Desktop\AGrooverNew.mid";
            args[2] = "modscale=0,15";
            args[3] = "fixloop=false";
            args[4] = "fixvolscale=false";
            args[5] = "addagbevents=false";*/

            int isInputFile = 0;

            if (debugMode == false)
            {
                if (args.Length == 0)
                {
                    showUsage();
                    return;
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
            }

            // automatically name new outfile name if none is specified

            if (inputFile != "") inputFile = Path.GetFullPath(inputFile);

            if (inputFile != "" && outputFile == "")
            {
                outputFile = Path.GetFileNameWithoutExtension(inputFile) + "_FINAL.mid";
                outputFile = Path.GetDirectoryName(inputFile) + "\\" + outputFile;
            }
            else
            {
                outputFile = Path.GetFullPath(outputFile);
            }

            Console.WriteLine("Inputfile:");
            Console.WriteLine(inputFile);
            Console.WriteLine("Outputfile:");
            Console.WriteLine(outputFile);

            FileMIDI.FileMIDI midiFile = new FileMIDI.FileMIDI();
            midiFile.loadMidiFromFile(inputFile);

            if (fixAgbEvents) FileMIDI.MidiFixer.addAgbCompatibleEvents(ref midiFile, modt);
            if (fixVolScale) FileMIDI.MidiFixer.combineVolumeAndExpression(ref midiFile);
            FileMIDI.MidiFixer.addModulationScale(ref midiFile, modScale);
            if (fixVolScale) FileMIDI.MidiFixer.addExponentialScale(ref midiFile);
            if (fixLoop) FileMIDI.MidiFixer.fixLoopCarryBack(ref midiFile);

            midiFile.saveMidiToFile(outputFile);
            timer.Stop();

            Console.WriteLine("Time elapsed: " + timer.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine("##################################################");
            Console.WriteLine();
        }

        static void showUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("midfix4agb.exe \"inputfile.mid\" [\"outputfile.mid\"] [optional arguments]");
            Console.WriteLine("Optional Arguments (examples):");
            Console.WriteLine();
            Console.WriteLine("modscale=1.0       default: 0.5");
            Console.WriteLine("modt=0             default: 0");
            Console.WriteLine("fixloop=true       default: true");
            Console.WriteLine("fixvolscale=true   default: true");
            Console.WriteLine();
            Console.WriteLine("##################################################");
            Console.ReadKey();
        }
    }
}
