# midfix4agb

"midfix4agb" is a utility program in C# I wrote to make inserting MIDI files into GBA games with the mp2k driver easier. This program is made to "post-process" a MIDI before isnertion. 

Features:
- apply a natural volume scale (x^(10/6)) to volume, expression and velocity events (mp2k only supports linear volume levels)
- combine expression and volume events to volume events only (mp2k only supports volume)
- apply a custom scale onto all pitch modulation
- prevent loop carry-back errors (by default mid2agb won't automatically insert events to revert changes that need to occur when the song jumps back (however, this feature doesn't completely work yet)
- for programmers: an easy to use MIDI interface for loading MIDI files into event objects and saving these. Loading works for type 0 and type 1 but they will internally get converted to type 1 and saved accordingly

Remember, this is some code I originally wrote when I didn't have a lot of knowledge about programming and the code quality therefore isn't "great". I just put the code on GitHub for peeps who bight be interested in it (if there is 1 or 2 in the world)
