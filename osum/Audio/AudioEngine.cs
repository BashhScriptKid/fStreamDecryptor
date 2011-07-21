﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osum.Support;
using osum.Helpers;
using osum.GameplayElements.Beatmaps;

namespace osum.Audio
{
    enum OsuSamples
    {
        HitNormal,
        HitWhistle,
        HitClap,
        HitFinish,
        MenuHit,
        MenuClick,
        SliderTick,
        SliderSlide,
        MenuBack,
        SpinnerBonus,
        SpinnerSpin,
        stream_down,
        stream_up,
        count3,
        count2,
        count1,
        countgo,
        miss,
        PRELOAD_END,
        fail,
        menuwhoosh,
        MainMenu_Intro,
        RankingBam,
        RankingBam2
    }

    internal static class AudioEngine
    {
        static Dictionary<OsuSamples, int>[] loadedSamples = new Dictionary<OsuSamples, int>[]
        { 
            new Dictionary<OsuSamples, int>(),
            new Dictionary<OsuSamples, int>(),
            new Dictionary<OsuSamples, int>()
        };

        internal static SoundEffectPlayer Effect;
        internal static BackgroundAudioPlayer Music;


        /// <summary>
        /// Initializes the audio subsystem using specific implementations for sound effects and music modules.
        /// </summary>
        /// <param name="effect">The effect player.</param>
        /// <param name="music">The music player.</param>
        internal static void Initialize(SoundEffectPlayer effect, BackgroundAudioPlayer music)
        {
            Effect = effect;
            Music = music;

            foreach (SampleSet set in Enum.GetValues(typeof(SampleSet)))
            {
                if (set == SampleSet.None)
                    continue;
                foreach (OsuSamples s in Enum.GetValues(typeof(OsuSamples)))
                {
                    if (s == OsuSamples.PRELOAD_END)
                        break;
                    LoadSample(s, set);
                }
            }

            Effect.Volume = GameBase.Config.GetValue<int>("VolumeEffect", 90) / 100f;
            Music.MaxVolume = GameBase.Config.GetValue<int>("VolumeMusic", 90) / 100f;
        }

        static Dictionary<int, int> lastPlayedTimes = new Dictionary<int, int>();

        internal static Source PlaySample(OsuSamples sample, SampleSet set = SampleSet.Soft, float volume = 1)
        {
            int buffer = LoadSample(sample, set);
            if (buffer < 0) return null;

            if (AudioEngine.Effect == null)
                return null;

            int lastPlayed = -1;
            if (lastPlayedTimes.TryGetValue(buffer, out lastPlayed))
                if (Math.Abs(Clock.Time - lastPlayed) < 40)
                    return null;
            lastPlayedTimes[buffer] = Clock.Time;

            Source src = AudioEngine.Effect.PlayBuffer(buffer, volume);

            if (sample > OsuSamples.PRELOAD_END)
                src.Disposable = true;

            return src;
        }

        internal static void Reset()
        {
            lastPlayedTimes.Clear();
        }

        internal static int LoadSample(OsuSamples sample, SampleSet set = SampleSet.Soft)
        {
            int buffer;
            SampleSet ss = SampleSet.None;

            switch (sample)
            {
                case OsuSamples.HitClap:
                case OsuSamples.HitFinish:
                case OsuSamples.HitNormal:
                case OsuSamples.HitWhistle:
                case OsuSamples.SliderTick:
                case OsuSamples.SliderSlide:
                    ss = set;
                    break;
            }

            if (!loadedSamples[(int)ss].TryGetValue(sample, out buffer))
            {
                string sampleName = sample.ToString().ToLower();
                string setName = ss != SampleSet.None ? ss.ToString().ToLower() + "-" : string.Empty;
                
                bool oneShot = sample > OsuSamples.PRELOAD_END;

                if (AudioEngine.Effect != null)
                    buffer = AudioEngine.Effect.Load("Skins/Default/" + setName + sampleName + ".wav");
                if (!oneShot)
                    loadedSamples[(int)ss].Add(sample, buffer);

                return buffer;
            }

            return buffer;
        }

        internal static void Suspend()
        {
            if (Effect != null)
            {
                Effect.UnloadAll();
            }
        }
    }
}
