﻿using System;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Note = Melanchall.DryWetMidi.MusicTheory.Note;

namespace Djenerative;

public class Patterns
{
    private int OctaveCache { get; set; }
    private Interval? IntervalCache { get; set; }
    public Scales.Intervals Scale { get; }
    public Note RootNote { get; }
    public Probability.Scale ProbScaleRhythm { get; }
    public Probability.Scale ProbScaleLead { get; }
    public Probability.Timing ProbTiming { get; }
    public Ranges.Settings Settings { get; }

    public Patterns(Scales.Intervals scale, Note rootNote, Probability.Scale probScaleRhythm, Probability.Scale probScaleLead, Probability.Timing probTiming, Ranges.Settings settings)
    {
        Settings = settings;
        Scale = scale;
        RootNote = rootNote;
        ProbScaleRhythm = probScaleRhythm;
        ProbScaleLead = probScaleLead;
        ProbTiming = probTiming;

        if (settings.LeadOctMin <= settings.LeadOctMax)
        {
            Settings = new Ranges.Settings
            {
                LeadOctMin = settings.LeadOctMin,
                LeadOctMax = settings.LeadOctMax
            };
        }
        else
        {
            Settings = new Ranges.Settings
            {
                LeadOctMin = settings.LeadOctMax,
                LeadOctMax = settings.LeadOctMin
            };
        }
    }

    public class NoteGroup
    {
        public Pattern? Guitar1 { get; set; }
        public Pattern? Guitar2 { get; set; }
        public Pattern? Bass { get; set; }
        public Pattern? Drums { get; set; }
    }

    public NoteGroup GenerateNote(Enums.NoteRequest request, bool harmony = false)
    {
        Pattern guitar1;
        Pattern guitar2;
        Pattern bass;
        Pattern drums;

        var timeSpan = RandomNoteTime();

        switch (request)
        {
            case Enums.NoteRequest.Gap:
                guitar1 = guitar2 = bass = drums = Gap(timeSpan);
                break;
            case Enums.NoteRequest.RhythmOpen:
                guitar1 = guitar2 = Rhythm(Enums.NoteVelocity.Open, timeSpan);
                bass = Bass(timeSpan);
                drums = Drums(timeSpan);
                break;
            case Enums.NoteRequest.RhythmMute:
                guitar1 = guitar2 = Rhythm(Enums.NoteVelocity.Mute, timeSpan);
                bass = Bass(timeSpan);
                drums = Drums(timeSpan);
                break;
            case Enums.NoteRequest.Lead:
                if (harmony)
                {
                    guitar1 = Lead(timeSpan);
                    guitar2 = Lead(timeSpan, harmony);
                }
                else
                {
                    guitar1 = guitar2 = Lead(timeSpan);
                }
                bass = drums = Gap(timeSpan);
                break;
            case Enums.NoteRequest.Harmonic:
                if (harmony)
                {
                    guitar1 = Harmonic(timeSpan);
                    guitar2 = Harmonic(timeSpan, harmony);
                }
                else
                {
                    guitar1 = guitar2 = Harmonic(timeSpan);
                }
                bass = drums = Gap(timeSpan);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), request, null);
        }

        return new NoteGroup
        {
            Guitar1 = guitar1,
            Guitar2 = guitar2,
            Bass = bass,
            Drums = drums
        };
    }

    private Interval GetInterval(Probability.Scale scale)
    {
        int seed = 0;

        Weighted.ChanceExecutor chanceExecutor = new();

        if (scale.Degree1 > 0)
        {
            chanceExecutor.Add(new Weighted.ChanceParam(() =>
            {
                seed = 0;
            }, scale.Degree1));
        }
        if (scale.Degree2 > 0)
        {
            chanceExecutor.Add(new Weighted.ChanceParam(() =>
            {
                seed = 1;
            }, scale.Degree2));
        }
        if (scale.Degree3 > 0)
        {
            chanceExecutor.Add(new Weighted.ChanceParam(() =>
            {
                seed = 2;
            }, scale.Degree3));
        }
        if (scale.Degree4 > 0)
        {
            chanceExecutor.Add(new Weighted.ChanceParam(() =>
            {
                seed = 3;
            }, scale.Degree4));
        }
        if (scale.Degree5 > 0)
        {
            chanceExecutor.Add(new Weighted.ChanceParam(() =>
            {
                seed = 4;
            }, scale.Degree5));
        }
        if (scale.Degree6 > 0)
        {
            chanceExecutor.Add(new Weighted.ChanceParam(() =>
            {
                seed = 5;
            }, scale.Degree6));
        }
        if (scale.Degree7 > 0)
        {
            chanceExecutor.Add(new Weighted.ChanceParam(() =>
            {
                seed = 6;
            }, scale.Degree7));
        }

        chanceExecutor.Execute();

        return seed switch
        {
            0 => Interval.FromHalfSteps(Scale.Interval1),
            1 => Interval.FromHalfSteps(Scale.Interval2),
            2 => Interval.FromHalfSteps(Scale.Interval3),
            3 => Interval.FromHalfSteps(Scale.Interval4),
            4 => Interval.FromHalfSteps(Scale.Interval5),
            5 => Interval.FromHalfSteps(Scale.Interval6),
            6 => Interval.FromHalfSteps(Scale.Interval7),
            _ => Interval.Zero
        };
    }

    public Pattern Rhythm(Enums.NoteVelocity velocity, MusicalTimeSpan timeSpan, bool harmony = false)
    {
        if (harmony)
        {
            CreateHarmony();
        }
        else
        {
            int octave = RootNote.Octave;
            OctaveCache = octave;
            IntervalCache = GetInterval(ProbScaleRhythm);
        }

        Note root = Note.Get(RootNote.NoteName, OctaveCache);

        return new PatternBuilder()
            .SetNoteLength(timeSpan)
            .SetRootNote(root)
            .Note(IntervalCache, new SevenBitNumber((byte) velocity))
            .Build();
    }

    public MusicalTimeSpan RandomNoteTime()
    {
        int seed = 0;

        Weighted.ChanceExecutor chanceExecutor = new();

        chanceExecutor.Add(new Weighted.ChanceParam(() =>
        {
            seed = 64;
        }, ProbTiming.SixtyFourth));

        chanceExecutor.Add(new Weighted.ChanceParam(() =>
        {
            seed = 32;
        }, ProbTiming.ThirtySecond));

        chanceExecutor.Add(new Weighted.ChanceParam(() =>
        {
            seed = 16;
        }, ProbTiming.Sixteenth));

        chanceExecutor.Add(new Weighted.ChanceParam(() =>
        {
            seed = 8;
        }, ProbTiming.Eighth));

        chanceExecutor.Add(new Weighted.ChanceParam(() =>
        {
            seed = 4;
        }, ProbTiming.Quarter));

        chanceExecutor.Add(new Weighted.ChanceParam(() =>
        {
            seed = 2;
        }, ProbTiming.Half));

        chanceExecutor.Add(new Weighted.ChanceParam(() =>
        {
            seed = 1;
        }, ProbTiming.Whole));

        chanceExecutor.Execute();

        return seed switch
        {
            64 => MusicalTimeSpan.SixtyFourth,
            32 => MusicalTimeSpan.ThirtySecond,
            16 => MusicalTimeSpan.Sixteenth,
            8 => MusicalTimeSpan.Eighth,
            4 => MusicalTimeSpan.Quarter,
            2 => MusicalTimeSpan.Half,
            1 => MusicalTimeSpan.Whole,
            _ => MusicalTimeSpan.Whole
        };
    }

    public Pattern Bass(MusicalTimeSpan timeSpan)
    {
        Note root = Note.Get(RootNote.NoteName, OctaveCache - 1);

        return new PatternBuilder()
            .SetNoteLength(timeSpan)
            .SetRootNote(root)
            .Note(IntervalCache, new SevenBitNumber((byte) Enums.NoteVelocity.Full))
            .Build();
    }

    public static Pattern Drums(MusicalTimeSpan timeSpan)
    {
        /*.Chord(new[]
        {
            Note.Get(NoteName.B, 3), // B3
            Note.Get(NoteName.C, 4), // C4
        })*/


        Note kick = Note.Get(NoteName.C, 2);

        return new PatternBuilder()
            .SetNoteLength(timeSpan)
            .Note(kick, new SevenBitNumber((byte) Enums.NoteVelocity.Full))
            .Build();
    }

    public Pattern Lead(MusicalTimeSpan timeSpan, bool harmony = false)
    {
        if (harmony)
        {
            CreateHarmony();
        }
        else
        {
            int octave = RootNote.Octave;
            OctaveCache = Randomise.Run(octave + Settings.LeadOctMin, octave + Settings.LeadOctMax);
            IntervalCache = GetInterval(ProbScaleLead);
        }

        Note root = Note.Get(RootNote.NoteName, OctaveCache);

        return new PatternBuilder()
            .SetNoteLength(timeSpan)
            .SetRootNote(root)
            .Note(IntervalCache, new SevenBitNumber((byte) Enums.NoteVelocity.Open))
            .Build();
    }

    public Pattern Harmonic(MusicalTimeSpan timeSpan, bool harmony = false)
    {
        if (harmony)
        {
            CreateHarmony();
        }
        else
        {
            int octave = RootNote.Octave;
            OctaveCache = octave + 2;
            IntervalCache = GetInterval(ProbScaleLead);
        }

        Note root = Note.Get(RootNote.NoteName, OctaveCache);

        return new PatternBuilder()
            .SetNoteLength(timeSpan)
            .SetRootNote(root)
            .Note(IntervalCache, new SevenBitNumber((byte) Enums.NoteVelocity.Harmonic))
            .Build();
    }

    public static Pattern Gap(MusicalTimeSpan timeSpan)
    {
        return new PatternBuilder()
            .StepForward(timeSpan)
            .Build();
    }

    private void CreateHarmony()
    {
        var dictionary = new Dictionary<int, Interval>
        {
            { 0, Interval.FromHalfSteps(Scale.Interval1) },
            { 1, Interval.FromHalfSteps(Scale.Interval2) },
            { 2, Interval.FromHalfSteps(Scale.Interval3) },
            { 3, Interval.FromHalfSteps(Scale.Interval4) },
            { 4, Interval.FromHalfSteps(Scale.Interval5) },
            { 5, Interval.FromHalfSteps(Scale.Interval6) },
            { 6, Interval.FromHalfSteps(Scale.Interval7) }
        };

        int position = dictionary.FirstOrDefault(x => x.Value == IntervalCache).Key;

        var positionNew = (position + 2) % 7; // TODO Replace 7 with dictionary length (using a minus 1 due to 0-index)
        IntervalCache = Interval.FromHalfSteps(dictionary[positionNew]);

        if (positionNew < position)
        {
            OctaveCache++;
        }
    }

    // Insert a pause with length of 2 bars
    //.StepForward(new BarBeatTicksTimeSpan(2, 0))

    // Insert a chord defined by intervals relative to the root note specified
    // as an argument of Chord method (B2)

    /*
        .Chord(new[]
            {
                Interval.Two, // B2 + 2
                Interval.Three, // B2 + 3
                -Interval.Twelve, // B2 - 12
            },
            Octave.Get(2).B)

        // Insert a pause of single dotted triplet half length

        .StepForward(MusicalTimeSpan.Half.Triplet().SingleDotted())

        // Insert a chord defined by the specified notes

        .Chord(new[]
        {
            Melanchall.DryWetMidi.MusicTheory.Note.Get(NoteName.B, 3), // B3
            Melanchall.DryWetMidi.MusicTheory.Note.Get(NoteName.C, 4), // C4
        })
        */
}