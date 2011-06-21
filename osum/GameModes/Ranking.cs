using System;
using osum.GameplayElements.Scoring;
using osum.Graphics.Sprites;
using osum.Graphics.Skins;
using osum.Helpers;
using OpenTK;
using OpenTK.Graphics;
using System.Collections;
using System.Collections.Generic;
using osum.Audio;
using osum.GameModes.SongSelect;
using osum.Graphics;
using osum.Online;
using osu_common.Helpers;
using osum.GameplayElements;
using System.IO;
namespace osum.GameModes
{
    public class Ranking : GameMode
    {
        internal static Score RankableScore;

        List<pDrawable> fillSprites = new List<pDrawable>();
        List<pDrawable> fallingSprites = new List<pDrawable>();
        List<pDrawable> resultSprites = new List<pDrawable>();

        bool wasBouncing = true;
        private BackButton s_ButtonBack;
        private pSprite background;
        private pSprite rankingBackground;
        private pSprite modeGraphic;
        private pSprite rankGraphic;

        const int colour_change_length = 500;
        const int end_bouncing = 600;
        const int time_between_fills = 600;

        const float fill_height = 5;
        float count_height = 80;
        private pSpriteText countTotalScore;
        private pSpriteText countScoreHit;
        private pSpriteText countScoreCombo;
        private pSpriteText countScoreAccuracy;
        private pSpriteText countScoreSpin;

        public override void Initialize()
        {
            background =
                new pSprite(TextureManager.Load(OsuTexture.songselect_background), FieldTypes.StandardSnapCentre, OriginTypes.Centre,
                            ClockTypes.Mode, Vector2.Zero, 0, true, Color4.White);
            spriteManager.Add(background);

            rankingBackground =
                new pSprite(TextureManager.Load(OsuTexture.ranking_background), FieldTypes.StandardSnapCentreLeft, OriginTypes.CentreLeft,
                            ClockTypes.Mode, Vector2.Zero, 0.4f, true, Color4.White);
            rankingBackground.Position = new Vector2(5, -20);
            rankingBackground.ScaleScalar = 0.85f;
            spriteManager.Add(rankingBackground);

            pText artist = new pText(Player.Beatmap.Artist, 30, new Vector2(10, fill_height + 5), 0.5f, true, Color4.OrangeRed) { TextShadow = true };
            spriteManager.Add(artist);
            pText title = new pText(Player.Beatmap.Title, 30, new Vector2(10 + artist.MeasureText().X / GameBase.BaseToNativeRatio, fill_height + 5), 0.5f, true, Color4.White) { TextShadow = true };
            spriteManager.Add(title);

            pTexture modeTex;
            switch (Player.Difficulty)
            {
                case Difficulty.Easy:
                    modeTex = TextureManager.Load(OsuTexture.songselect_mode_easy);
                    break;
                case Difficulty.Expert:
                    modeTex = TextureManager.Load(OsuTexture.songselect_mode_expert);
                    break;
                default:
                    modeTex = TextureManager.Load(OsuTexture.songselect_mode_stream);
                    break;
            }

            modeGraphic = new pSprite(modeTex, FieldTypes.StandardSnapRight, OriginTypes.TopRight, ClockTypes.Mode, new Vector2(5, 7), 0.45f, true, Color4.White) { ScaleScalar = 0.5f };
            spriteManager.Add(modeGraphic);

            pTexture rankLetter;
            switch (RankableScore.Ranking)
            {
                case Rank.X:
                    rankLetter = TextureManager.Load(OsuTexture.rank_x);
                    break;
                case Rank.S:
                    rankLetter = TextureManager.Load(OsuTexture.rank_s);
                    break;
                case Rank.A:
                    rankLetter = TextureManager.Load(OsuTexture.rank_a);
                    break;
                case Rank.B:
                    rankLetter = TextureManager.Load(OsuTexture.rank_b);
                    break;
                case Rank.C:
                    rankLetter = TextureManager.Load(OsuTexture.rank_c);
                    break;
                default:
                    rankLetter = TextureManager.Load(OsuTexture.rank_d);
                    break;
            }

            rankGraphic = new pSprite(rankLetter, FieldTypes.StandardSnapBottomRight, OriginTypes.BottomRight, ClockTypes.Mode, new Vector2(15, 15), 0.3f, true, Color4.White) { Alpha = 0 };

            spriteManager.Add(rankGraphic);

            initializeTransition();

            //Scoring
            {
                float v_offset = -165;

                pText heading = new pText("Score", 28, new Vector2(240, v_offset), 0.5f, true, Color4.White)
                {
                    Origin = OriginTypes.TopLeft,
                    Field = FieldTypes.StandardSnapCentreLeft,
                    Bold = true,
                };
                resultSprites.Add(heading);

                v_offset += 30;

                pSpriteText count = new pSpriteText("000,000", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.TopRight, ClockTypes.Mode, new Vector2(445, v_offset), 0.9f, true, new Color4(255, 166, 0, 255));
                count.TextConstantSpacing = true;
                countTotalScore = count;

                resultSprites.Add(count);

                v_offset += 40;

                //Spin Bonus
                heading = new pText("Hit", 20, new Vector2(240, v_offset + 2), 0.5f, true, Color4.Gray)
                {
                    Origin = OriginTypes.TopLeft,
                    Field = FieldTypes.StandardSnapCentreLeft,
                    Bold = true
                };
                resultSprites.Add(heading);

                count = new pSpriteText("000000", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.TopLeft, ClockTypes.Mode, new Vector2(330, v_offset), 0.9f, true, new Color4(255, 166, 0, 255));
                count.TextConstantSpacing = true;
                count.ZeroAlpha = 0.5f;
                count.ScaleScalar = 0.7f;
                resultSprites.Add(count);

                countScoreHit = count;

                v_offset += 25;


                heading = new pText("Combo", 20, new Vector2(240, v_offset + 2), 0.5f, true, Color4.Gray)
                {
                    Origin = OriginTypes.TopLeft,
                    Field = FieldTypes.StandardSnapCentreLeft,
                    Bold = true
                };
                resultSprites.Add(heading);

                count = new pSpriteText("000000", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.TopLeft, ClockTypes.Mode, new Vector2(330, v_offset), 0.9f, true, new Color4(255, 166, 0, 255));
                count.TextConstantSpacing = true;
                count.ZeroAlpha = 0.5f;
                count.ScaleScalar = 0.7f;
                resultSprites.Add(count);

                countScoreCombo = count;

                v_offset += 25;

                heading = new pText("Accuracy", 20, new Vector2(240, v_offset + 2), 0.5f, true, Color4.Gray)
                {
                    Origin = OriginTypes.TopLeft,
                    Field = FieldTypes.StandardSnapCentreLeft,
                    Bold = true
                };
                resultSprites.Add(heading);

                count = new pSpriteText("000000", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.TopLeft, ClockTypes.Mode, new Vector2(330, v_offset), 0.9f, true, new Color4(255, 166, 0, 255));
                count.TextConstantSpacing = true;
                count.ZeroAlpha = 0.5f;
                count.ScaleScalar = 0.7f;
                resultSprites.Add(count);

                countScoreAccuracy = count;

                v_offset += 25;

                heading = new pText("Spin", 20, new Vector2(240, v_offset + 2), 0.5f, true, Color4.Gray)
                {
                    Origin = OriginTypes.TopLeft,
                    Field = FieldTypes.StandardSnapCentreLeft,
                    Bold = true
                };
                resultSprites.Add(heading);

                count = new pSpriteText("000000", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.TopLeft, ClockTypes.Mode, new Vector2(330, v_offset), 0.9f, true, new Color4(255, 166, 0, 255));
                count.TextConstantSpacing = true;
                count.ZeroAlpha = 0.5f;
                count.ScaleScalar = 0.7f;
                resultSprites.Add(count);

                countScoreSpin = count;

                v_offset += 30;

                heading = new pText("Accuracy", 28, new Vector2(240, v_offset), 0.5f, true, Color4.White)
                {
                    Origin = OriginTypes.TopLeft,
                    Field = FieldTypes.StandardSnapCentreLeft,
                    Bold = true
                };
                resultSprites.Add(heading);

                v_offset += 34;

                count = new pSpriteText((RankableScore.accuracy * 100).ToString("00.00", GameBase.nfi) + "%", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.TopRight, ClockTypes.Mode, new Vector2(445, v_offset), 0.9f, true, new Color4(0, 180, 227, 255));
                count.ScaleScalar = 0.7f;
                resultSprites.Add(count);

                v_offset += 20;

                heading = new pText("Max Combo", 28, new Vector2(240, v_offset), 0.5f, true, Color4.White)
                {
                    Origin = OriginTypes.TopLeft,
                    Field = FieldTypes.StandardSnapCentreLeft,
                    Bold = true
                };
                resultSprites.Add(heading);

                v_offset += 34;

                count = new pSpriteText(RankableScore.maxCombo.ToString("#,0", GameBase.nfi) + "x", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.TopRight, ClockTypes.Mode, new Vector2(445, v_offset), 0.9f, true, new Color4(0, 180, 227, 255));
                count.ScaleScalar = 0.7f;
                resultSprites.Add(count);
            }

            {
                Vector2 pos = new Vector2(60, -130);
                Vector2 textOffset = new Vector2(150, 0);

                float spacing = 65;

                pSprite hitExplosion = new pSprite(TextureManager.Load(OsuTexture.hit300), pos) { Field = FieldTypes.StandardSnapCentreLeft, Origin = OriginTypes.Centre, ScaleScalar = 0.5f, DrawDepth = 0.9f };
                resultSprites.Add(hitExplosion);

                count300 = new pSpriteText("0x", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.CentreRight, ClockTypes.Mode, pos + textOffset, 0.9f, true, Color4.White) { SpacingOverlap = 3, TextConstantSpacing = true };
                resultSprites.Add(count300);

                pos.Y += spacing;

                hitExplosion = new pSprite(TextureManager.Load(OsuTexture.hit100), pos) { Field = FieldTypes.StandardSnapCentreLeft, Origin = OriginTypes.Centre, ScaleScalar = 0.5f, DrawDepth = 0.9f };
                resultSprites.Add(hitExplosion);

                count100 = new pSpriteText("0x", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.CentreRight, ClockTypes.Mode, pos + textOffset, 0.9f, true, Color4.White) { SpacingOverlap = 3, TextConstantSpacing = true };
                resultSprites.Add(count100);

                pos.Y += spacing;

                hitExplosion = new pSprite(TextureManager.Load(OsuTexture.hit50), pos) { Field = FieldTypes.StandardSnapCentreLeft, Origin = OriginTypes.Centre, ScaleScalar = 0.5f, DrawDepth = 0.9f };
                resultSprites.Add(hitExplosion);

                count50 = new pSpriteText("0x", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.CentreRight, ClockTypes.Mode, pos + textOffset, 0.9f, true, Color4.White) { SpacingOverlap = 3, TextConstantSpacing = true };
                resultSprites.Add(count50);

                pos.Y += spacing;

                hitExplosion = new pSprite(TextureManager.Load(OsuTexture.hit0), pos) { Field = FieldTypes.StandardSnapCentreLeft, Origin = OriginTypes.Centre, ScaleScalar = 0.5f, DrawDepth = 0.9f };
                resultSprites.Add(hitExplosion);

                count0 = new pSpriteText("0x", "score", 0, FieldTypes.StandardSnapCentreLeft, OriginTypes.CentreRight, ClockTypes.Mode, pos + textOffset, 0.9f, true, Color4.White) { SpacingOverlap = 3, TextConstantSpacing = true };
                resultSprites.Add(count0);
            }

            //Average Timing
            {
                float avg = (float)RankableScore.hitOffsetMilliseconds / Math.Max(1, RankableScore.hitOffsetCount);
                pText heading = new pText("Avg. Timing: " + avg + (RankableScore.hitOffsetMilliseconds > 0 ? "ms late" : "ms early"), 16, new Vector2(0, 20), 0.5f, true, Color4.White)
                {
                    Field = FieldTypes.StandardSnapBottomCentre,
                    Origin = OriginTypes.BottomCentre
                };
                resultSprites.Add(heading);
            }

            spriteManager.Add(resultSprites);

            //add a temporary button to allow returning to song select
            s_ButtonBack = new BackButton(returnToSelect);
            spriteManager.Add(s_ButtonBack);

            BeatmapInfo bmi = BeatmapDatabase.GetBeatmapInfo(Player.Beatmap, Player.Difficulty);
            if (RankableScore.totalScore > bmi.HighScore)
            {
                isPersonalBest = true;
                bmi.HighScore = RankableScore.totalScore;
                bmi.Ranking = RankableScore.Ranking;
            }

            Director.OnTransitionEnded += Director_OnTransitionEnded;
        }

        void Director_OnTransitionEnded()
        {
            //hit -> combo -> accuracy -> spin

            int time = 500;

            GameBase.Scheduler.Add(delegate
            {
                countScoreHit.ShowInt(RankableScore.hitScore, 6, false);
                pDrawable flash = countScoreHit.AdditiveFlash(500, 1);

                addedScore += RankableScore.hitScore;
                countTotalScore.ShowInt(addedScore, 6, true);
                countTotalScore.FlashColour(Color4.White, 1000);
            }, time);

            time += 500;

            GameBase.Scheduler.Add(delegate
            {
                countScoreCombo.ShowInt(RankableScore.comboBonusScore, 6, false);
                pDrawable flash = countScoreCombo.AdditiveFlash(500, 1);

                addedScore += RankableScore.comboBonusScore;
                countTotalScore.ShowInt(addedScore, 6, true);
                countTotalScore.FlashColour(Color4.White, 1000);
            }, time);

            time += 500;

            GameBase.Scheduler.Add(delegate
            {
                countScoreAccuracy.ShowInt(RankableScore.accuracyBonusScore, 6, false);
                pDrawable flash = countScoreAccuracy.AdditiveFlash(500, 1);

                addedScore += RankableScore.accuracyBonusScore;
                countTotalScore.ShowInt(addedScore, 6, true);
                countTotalScore.FlashColour(Color4.White, 1000);
            }, time);

            time += 500;

            GameBase.Scheduler.Add(delegate
            {
                countScoreSpin.ShowInt(RankableScore.spinnerBonusScore, 6, false);
                pDrawable flash = countScoreSpin.AdditiveFlash(500, 1);

                addedScore += RankableScore.spinnerBonusScore;
                countTotalScore.ShowInt(addedScore, 6, true);
                countTotalScore.FlashColour(Color4.White, 1000);
            }, time);

            time += 500;

            GameBase.Scheduler.Add(delegate
            {
                rankGraphic.Alpha = 1;
                rankGraphic.AdditiveFlash(1500, 1);

                if (Player.Beatmap.DifficultyInfo.Count == 0)
                {
                    GameBase.Notify("Please update your maps from the store!",null);
                }
                else
                {
                    //we should move this to happen earlier but delay the ranking dialog from displaying until after animations are done.
                    OnlineHelper.SubmitScore(CryptoHelper.GetMd5String(Path.GetFileName(Player.Beatmap.ContainerFilename) + "-" + Player.Difficulty.ToString()), RankableScore.totalScore);
                }
            }, time);

            time += 1000;


            GameBase.Scheduler.Add(delegate
            {
                if (isPersonalBest)
                {
                    pSprite personalBest = new pSprite(TextureManager.Load(OsuTexture.personalbest), FieldTypes.StandardSnapBottomRight, OriginTypes.Centre, ClockTypes.Mode, new Vector2(80, 250),
                            1, true, Color4.White);
                    personalBest.FadeInFromZero(500);
                    personalBest.ScaleScalar = 1.6f;
                    personalBest.RotateTo(0.2f, 500);
                    personalBest.ScaleTo(1, 500, EasingTypes.Out);
    
                    GameBase.Scheduler.Add(delegate { personalBest.AdditiveFlash(1000, 1).ScaleTo(1.05f, 1000); }, 500);
    
                    spriteManager.Add(personalBest);
                }
            }, time);

        }

        int addedScore;
        private pSpriteText count300;
        private pSpriteText count100;
        private pSpriteText count50;
        private pSpriteText count0;
        private bool isPersonalBest;

        private void initializeTransition()
        {
            pDrawable fill = pSprite.FullscreenWhitePixel;
            fill.AlignToSprites = true;
            fill.Scale.X *= (float)RankableScore.count300 / RankableScore.totalHits;
            fill.Scale.Y = fill_height;
            fill.DrawDepth = 0.9f;
            fill.Alpha = 1;
            fill.AlwaysDraw = true;
            fill.Colour = new Color4(1, 0.63f, 0.01f, 1);
            fillSprites.Add(fill);

            fill = pSprite.FullscreenWhitePixel;
            fill.AlignToSprites = true;
            fill.Position.X = fillSprites[fillSprites.Count - 1].Position.X + fillSprites[fillSprites.Count - 1].Scale.X;
            fill.Scale.X *= (float)RankableScore.count100 / RankableScore.totalHits;
            fill.Scale.Y = fill_height;
            fill.DrawDepth = 0.9f;
            fill.Alpha = 1;
            fill.AlwaysDraw = true;
            fill.Colour = new Color4(0.55f, 0.84f, 0, 1);
            fillSprites.Add(fill);

            fill = pSprite.FullscreenWhitePixel;
            fill.AlignToSprites = true;
            fill.Position.X = fillSprites[fillSprites.Count - 1].Position.X + fillSprites[fillSprites.Count - 1].Scale.X;
            fill.Scale.X *= (float)RankableScore.count50 / RankableScore.totalHits;
            fill.Scale.Y = fill_height;
            fill.DrawDepth = 0.9f;
            fill.Alpha = 1;
            fill.AlwaysDraw = true;
            fill.Colour = new Color4(0.50f, 0.29f, 0.635f, 1);
            fillSprites.Add(fill);

            fill = pSprite.FullscreenWhitePixel;
            fill.AlignToSprites = true;
            fill.Position.X = fillSprites[fillSprites.Count - 1].Position.X + fillSprites[fillSprites.Count - 1].Scale.X;
            fill.Scale.X *= (float)RankableScore.countMiss / RankableScore.totalHits;
            fill.Scale.Y = fill_height;
            fill.DrawDepth = 0.9f;
            fill.Alpha = 1;
            fill.AlwaysDraw = true;
            fill.Colour = new Color4(0.10f, 0.10f, 0.10f, 1);
            fillSprites.Add(fill);

            spriteManager.Add(fillSprites);
        }

        void returnToSelect(object sender, EventArgs args)
        {
            Director.ChangeMode(OsuMode.SongSelect);
        }

        public override void Dispose()
        {
            AudioEngine.Music.Unload();
            base.Dispose();
        }

        public Ranking()
        {
        }

        public override void Update()
        {
            base.Update();

            if (!Director.IsTransitioning)
            {
                float pos = (float)GameBase.Random.NextDouble() * GameBase.BaseSizeFixedWidth.Width;

                pTexture tex = null;
                if (pos < fillSprites[1].Position.X)
                    tex = TextureManager.Load(OsuTexture.hit300);
                else if (pos < fillSprites[2].Position.X)
                    tex = TextureManager.Load(OsuTexture.hit100);
                else if (pos < fillSprites[3].Position.X)
                    tex = TextureManager.Load(OsuTexture.hit50);

                fallingSprites.RemoveAll(p => p.Alpha == 0);
                foreach (pSprite p in fallingSprites)
                {
                    p.Position.Y += (p.Position.Y - p.StartPosition.Y + 1) * 0.05f;
                }

                if (tex != null)
                {
                    pSprite f = new pSprite(tex, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Mode, new Vector2(pos, fill_height - 30), 0.3f, false, Color4.White);
                    f.ScaleScalar = 0.2f;
                    f.Transform(new Transformation(TransformationType.Fade, 0, 1, Clock.ModeTime, Clock.ModeTime + 150));
                    f.Transform(new Transformation(TransformationType.Fade, 1, 0, Clock.ModeTime + 250, Clock.ModeTime + 1000 + (int)(GameBase.Random.NextDouble() * 1000)));
                    fallingSprites.Add(f);
                    spriteManager.Add(f);
                }
            }

            int increaseAmount = (int)(GameBase.ElapsedMilliseconds / 8);
            if (count300.LastInt < RankableScore.count300)
                count300.ShowInt(Math.Min(RankableScore.count300, count300.LastInt + increaseAmount), 0, false, 'x');
            if (count100.LastInt < RankableScore.count100)
                count100.ShowInt(Math.Min(RankableScore.count100, count100.LastInt + increaseAmount), 0, false, 'x');
            if (count50.LastInt < RankableScore.count50)
                count50.ShowInt(Math.Min(RankableScore.count50, count50.LastInt + increaseAmount), 0, false, 'x');
            if (count0.LastInt < RankableScore.countMiss)
                count0.ShowInt(Math.Min(RankableScore.countMiss, count0.LastInt + increaseAmount), 0, false, 'x');
        }
    }

    public class RankingTransition : Transition
    {
        float count_height = 80;
        const float fill_height = 5;
        const int end_bouncing = 600;
        const int colour_change_length = 500;
        const int time_between_fills = 300;

        List<pDrawable> fillSprites = new List<pDrawable>();
        List<pDrawable> countSprites = new List<pDrawable>();

        public override bool FadeOutDone
        {
            get
            {
                return Clock.Time - startTime > 3000;
            }
        }

        public override bool FadeInDone
        {
            get
            {
                return spriteManager.Transformations.Count == 0;
            }
        }

        int startTime;
        private pDrawable flash;

        pSprite background;

        public override void Initialize()
        {
            startTime = Clock.Time;

            background = new pSprite(TextureManager.Load(OsuTexture.cleared), FieldTypes.StandardSnapCentre, OriginTypes.CentreLeft,
                            ClockTypes.Mode, Vector2.Zero, 1, true, Color4.White);
            background.Position.X -= background.DrawWidth * GameBase.SpriteToBaseRatio / 2;

            background.Additive = true;
            spriteManager.Add(background);

            pDrawable fill = pSprite.FullscreenWhitePixel;
            fill.AlignToSprites = true;
            fill.Clocking = ClockTypes.Game;
            fill.Scale.X *= (float)Ranking.RankableScore.count300 / Ranking.RankableScore.totalHits;
            fill.Colour = new Color4(1, 0.63f, 0.01f, 1);
            fillSprites.Add(fill);

            pSpriteText count = new pSpriteText(Ranking.RankableScore.count300.ToString(), "default", 0, FieldTypes.Standard, OriginTypes.BottomRight, ClockTypes.Game, new Vector2(0, count_height), 1, false, Color4.White);
            countSprites.Add(count);

            count_height += 80;

            fill = pSprite.FullscreenWhitePixel;
            fill.AlignToSprites = true;
            fill.Clocking = ClockTypes.Game;
            fill.Scale.X *= (float)Ranking.RankableScore.count100 / Ranking.RankableScore.totalHits;
            fill.Colour = new Color4(0.55f, 0.84f, 0, 1);
            fillSprites.Add(fill);

            count = new pSpriteText(Ranking.RankableScore.count100.ToString(), "default", 0, FieldTypes.Standard, OriginTypes.BottomRight, ClockTypes.Game, new Vector2(0, count_height), 1, false, Color4.White);
            countSprites.Add(count);

            count_height += 80;

            fill = pSprite.FullscreenWhitePixel;
            fill.AlignToSprites = true;
            fill.Clocking = ClockTypes.Game;
            fill.Scale.X *= (float)Ranking.RankableScore.count50 / Ranking.RankableScore.totalHits;
            fill.Colour = new Color4(0.50f, 0.29f, 0.635f, 1);
            fillSprites.Add(fill);

            count = new pSpriteText(Ranking.RankableScore.count50.ToString(), "default", 0, FieldTypes.Standard, OriginTypes.BottomRight, ClockTypes.Game, new Vector2(0, count_height), 1, false, Color4.White);
            countSprites.Add(count);

            count_height += 80;

            fill = pSprite.FullscreenWhitePixel;
            fill.AlignToSprites = true;
            fill.Clocking = ClockTypes.Game;
            fill.Scale.X *= (float)Ranking.RankableScore.countMiss / Ranking.RankableScore.totalHits;
            fill.Colour = new Color4(0.10f, 0.10f, 0.10f, 1);
            fillSprites.Add(fill);

            count = new pSpriteText(Ranking.RankableScore.countMiss.ToString(), "default", 0, FieldTypes.Standard, OriginTypes.BottomRight, ClockTypes.Game, new Vector2(0, count_height), 1, false, Color4.White);
            countSprites.Add(count);

            int i = 0;

            foreach (pDrawable p in countSprites)
            {
                p.Alpha = 0;
                p.AlwaysDraw = true;
                p.Additive = true;

                int offset = Clock.Time + i++ * time_between_fills;
                //p.Transform(new Transformation(TransformationType.Fade, 0, 0.5f, offset - 50, offset + 200));
                //p.Transform(new Transformation(TransformationType.Fade, 1, 0, offset + 100, offset + 800));
            }


            i = 0;

            foreach (pDrawable p in fillSprites)
            {
                p.Alpha = 1;
                //p.Additive = true;
                p.DrawDepth = 0.98f;
                p.AlwaysDraw = true;

                int offset = Clock.Time + i++ * time_between_fills;

                p.Transform(new Transformation(new Color4(23, 51, 71, 255), new Color4(23, 51, 71, 255), Clock.Time, Clock.Time + 1400));
                p.Transform(new Transformation(Color4.White, p.Colour, Clock.Time + 1400, Clock.Time + 3000));
                //force the initial colour to be an ambiguous gray.

                p.Transform(new TransformationBounce(offset, offset + end_bouncing * 2, p.Scale.X, p.Scale.X, 5));
            }

            spriteManager.Add(fillSprites);
            spriteManager.Add(countSprites);

            base.Initialize();
        }

        internal override void FadeIn()
        {
            //spriteManager.Sprites.ForEach(s =>
            //{
            //    s.AlwaysDraw = false;
            //});

            //background.FadeOut(100);
            //fillSprites.ForEach(s => s.FadeOut(800));

            spriteManager.MoveTo(new Vector2(0, -GameBase.BaseSize.Height), 1000, EasingTypes.InOut);

            /*flash = pSprite.FullscreenWhitePixel;
            flash.Clocking = ClockTypes.Game;
            flash.FadeOutFromOne(800);
            spriteManager.Add(flash);*/
            base.FadeIn();
        }

        public override void Update()
        {
            base.Update();

            //set the x scale back to the default value (override the bounce transformation).
            float lastPos = 0;

            for (int i = 0; i < fillSprites.Count; i++)
            {
                pDrawable fill = fillSprites[i];
                pDrawable count = countSprites[i];

                fill.Scale.Y = GameBase.BaseSize.Height;

                if (lastPos != 0) fill.Position.X = lastPos;
                lastPos = fill.Position.X + fill.Scale.X;

                count.Position.X = lastPos - 3;
            }

            float widthOffset = -background.FieldPosition.X / GameBase.BaseToNativeRatio / GameBase.SpriteToBaseRatio;
            background.DrawWidth = (int)(widthOffset + (background.Texture.Width - widthOffset * 2) * (lastPos / GameBase.BaseSize.Width));
        }
    }
}

