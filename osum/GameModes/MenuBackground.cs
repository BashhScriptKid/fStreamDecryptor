﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osum.Graphics.Sprites;
using osum.Graphics.Drawables;
using OpenTK;
using OpenTK.Graphics;
using osum.Graphics.Primitives;
using osum.Helpers;
using osum.Audio;
using osum.Graphics.Skins;
using osum.Graphics;

namespace osum.GameModes
{
    class MenuBackground : SpriteManager
    {
        Vector2 centre = new Vector2(320, 200);
        private pQuad yellow;
        private pQuad orange;
        private pQuad blue;
        private pQuad pink;

        public MenuBackground()
        {
            rectangleLineLeft = new Line(new Vector2(114, 55) - centre, new Vector2(169, 362) - centre);
            rectangleLineRight = new Line(new Vector2(-100, -855) - centre, new Vector2(1200, 250) - centre);

            rectBorder = new pQuad(
                rectangleLineLeft.p1 + new Vector2(-2, -2),
                new Vector2(444 + 2, 172 - 2) - centre,
                rectangleLineLeft.p2 + new Vector2(-2, 2),
                new Vector2(528 + 3, 297 + 2) - centre,
                true, 0.4f, new Color4(13, 13, 13, 255));
            rectBorder.Field = FieldTypes.StandardSnapCentre;
            rectBorder.Origin = OriginTypes.Centre;
            Add(rectBorder);

            rect = new pQuad(
                rectangleLineLeft.p1,
                new Vector2(444, 172) - centre,
                rectangleLineLeft.p2,
                new Vector2(528, 297) - centre,
                true, 0.42f, new Color4(33, 35, 42, 255));
            rect.Field = FieldTypes.StandardSnapCentre;
            rect.Origin = OriginTypes.Centre;
            rect.colours = new Color4[] {
                new Color4(28,29,35,255),
                new Color4(27,29,33,255),
                new Color4(18,19,21,255),
                new Color4(18,19,21,255)
            };
            Add(rect);

            pTexture specialTexture = TextureManager.Load(OsuTexture.menu_item_background);
            specialTexture.X++;
            specialTexture.Width -= 2;

            yellow = new pQuad(
                Vector2.Zero,
                Vector2.Zero,
                Vector2.Zero,
                Vector2.Zero,
                true, 0.5f, new Color4(254, 242, 0, 255));
            yellow.Tag = yellow.Colour;
            yellow.HandleClickOnUp = true;
            yellow.Texture = specialTexture;
            yellow.Field = FieldTypes.StandardSnapCentre;
            yellow.Origin = OriginTypes.Centre;
            yellow.OnClick += Option_OnClick;
            yellow.OnHover += Option_OnHover;
            yellow.OnHoverLost += Option_OnHoverLost;
            Add(yellow);

            orange = new pQuad(
                Vector2.Zero,
                Vector2.Zero,
                Vector2.Zero,
                Vector2.Zero,
                true, 0.5f, new Color4(255, 102, 0, 255));
            orange.Tag = orange.Colour;
            orange.HandleClickOnUp = true;
            orange.Texture = specialTexture;
            orange.Field = FieldTypes.StandardSnapCentre;
            orange.Origin = OriginTypes.Centre;
            orange.OnClick += Option_OnClick;
            orange.OnHover += Option_OnHover;
            orange.OnHoverLost += Option_OnHoverLost;

            Add(orange);

            blue = new pQuad(
                Vector2.Zero,
                Vector2.Zero,
                Vector2.Zero,
                Vector2.Zero,
                true, 0.5f, new Color4(0, 192, 245, 255));
            blue.Tag = blue.Colour;
            blue.HandleClickOnUp = true;
            blue.Texture = specialTexture;
            blue.Field = FieldTypes.StandardSnapCentre;
            blue.Origin = OriginTypes.Centre;
            blue.OnClick += Option_OnClick;
            blue.OnClick += Option_OnClick;
            blue.OnHover += Option_OnHover;
            blue.OnHoverLost += Option_OnHoverLost;
            Add(blue);

            pink = new pQuad(
                Vector2.Zero,
                Vector2.Zero,
                Vector2.Zero,
                Vector2.Zero,
                true, 0.5f, new Color4(237, 0, 140, 255));
            pink.Texture = specialTexture;
            pink.Tag = pink.Colour;
            pink.HandleClickOnUp = true;
            pink.Field = FieldTypes.StandardSnapCentre;
            pink.Origin = OriginTypes.Centre;
            pink.OnClick += Option_OnClick;
            pink.OnClick += Option_OnClick;
            pink.OnHover += Option_OnHover;
            pink.OnHoverLost += Option_OnHoverLost;
            Add(pink);

            ScaleScalar = 1.4f;

            pSprite text = new pSprite(TextureManager.Load(OsuTexture.menu_tutorial), new Vector2(-66, 3));
            text.Field = FieldTypes.StandardSnapCentre;
            text.Origin = OriginTypes.Centre;
            text.Rotation = -rotation_offset;
            text.ScaleScalar = 1/scale_offset;
            text.Alpha = 0;
            Add(text);
            textSprites.Add(text);

            text = new pSprite(TextureManager.Load(OsuTexture.menu_play), new Vector2(-48, 22));
            text.Field = FieldTypes.StandardSnapCentre;
            text.Origin = OriginTypes.Centre;
            text.Rotation = -rotation_offset;
            text.ScaleScalar = 1 / scale_offset;
            text.Alpha = 0;
            Add(text);
            textSprites.Add(text);

            text = new pSprite(TextureManager.Load(OsuTexture.menu_store), new Vector2(-43, 48));
            text.Field = FieldTypes.StandardSnapCentre;
            text.Origin = OriginTypes.Centre;
            text.Rotation = -rotation_offset;
            text.ScaleScalar = 1 / scale_offset;
            text.Alpha = 0;
            Add(text);
            textSprites.Add(text);

            text = new pSprite(TextureManager.Load(OsuTexture.menu_options), new Vector2(-44, 74));
            text.Field = FieldTypes.StandardSnapCentre;
            text.Origin = OriginTypes.Centre;
            text.Rotation = -rotation_offset;
            text.ScaleScalar = 1 / scale_offset;
            text.Alpha = 0;
            Add(text);
            textSprites.Add(text);
        }

        List<pSprite> textSprites = new List<pSprite>();

        void Option_OnHoverLost(object sender, EventArgs e)
        {
            pDrawable d = sender as pDrawable;
            d.FadeColour((Color4)d.Tag, 600);
            //d.FadeColour(ColourHelper.Darken(d.Colour, 0.5f), 50);
        }

        void Option_OnHover(object sender, EventArgs e)
        {
            pDrawable d = sender as pDrawable;

            d.FadeColour(Color4.White, 100);
            //d.FadeColour(ColourHelper.Lighten(d.Colour, 0.5f),50);
        }

        void Option_OnClick(object sender, EventArgs e)
        {
            if (!IsAwesome)
                return;

            pDrawable d = sender as pDrawable;

            AudioEngine.PlaySample(OsuSamples.MenuHit);

            if (sender == yellow)
                Director.ChangeMode(OsuMode.Tutorial);
            else if (sender == orange)
                Director.ChangeMode(OsuMode.SongSelect);
            else if (sender == blue)
                Director.ChangeMode(OsuMode.Store);
            else
            { }
        }

        int awesomeStartTime = -1;
        private Line rectangleLineLeft;
        private Transformation awesomeTransformation;
        private Line rectangleLineRight;
        const int duration = 4000;

        const float rotation_offset = 0.35f;
        const float scale_offset = 4.2f;

        internal void BeAwesome()
        {
            GameBase.Scheduler.Add(delegate
            {
                ScaleTo(scale_offset, duration, EasingTypes.InDouble);
                MoveTo(new Vector2(75, -44), duration, EasingTypes.InDouble);
                RotateTo(rotation_offset, duration, EasingTypes.InDouble);

                rect.FadeOut(duration);
                rectBorder.FadeOut(duration);
            }, 1000);

            awesomeStartTime = Clock.ModeTime;
            awesomeTransformation = new TransformationBounce(Clock.ModeTime, Clock.ModeTime + duration / 4, 1, 0.6f, 6);
                //new Transformation(TransformationType.Fade, 0, 1, Clock.ModeTime, Clock.ModeTime + duration/4, EasingTypes.InDouble);
            awesomeTransformation.Clocking = ClockTypes.Mode;

            textSprites.ForEach(s => s.FadeIn(500));

        }

        bool first = true;
        private pQuad rectBorder;
        private pQuad rect;
        public override void Update()
        {
            if (awesomeTransformation != null || first)
            {
                float progress = awesomeTransformation == null ? 0 : awesomeTransformation.CurrentFloat;
                yellow.p1 = rectangleLineLeft.PositionAt(0.575f + 0.08f * progress);
                yellow.p2 = rectangleLineRight.PositionAt(0.575f + 0.08f * progress);
                yellow.p3 = rectangleLineLeft.PositionAt(0.58f + 0.12f * progress);
                yellow.p4 = rectangleLineRight.PositionAt(0.58f + 0.12f * progress);

                orange.p1 = rectangleLineLeft.PositionAt(0.69f + 0.02f * progress);
                orange.p2 = rectangleLineRight.PositionAt(0.69f + 0.02f * progress);
                orange.p3 = rectangleLineLeft.PositionAt(0.73f + 0.02f * progress);
                orange.p4 = rectangleLineRight.PositionAt(0.73f + 0.02f * progress);

                blue.p1 = rectangleLineLeft.PositionAt(0.785f - 0.025f * progress);
                blue.p2 = rectangleLineRight.PositionAt(0.785f - 0.025f * progress);
                blue.p3 = rectangleLineLeft.PositionAt(0.79f + 0.01f * progress);
                blue.p4 = rectangleLineRight.PositionAt(0.79f + 0.01f * progress);

                pink.p1 = rectangleLineLeft.PositionAt(0.82f - 0.01f * progress);
                pink.p2 = rectangleLineRight.PositionAt(0.82f - 0.01f * progress);
                pink.p3 = rectangleLineLeft.PositionAt(0.825f + 0.03f * progress);
                pink.p4 = rectangleLineRight.PositionAt(0.825f + 0.03f * progress);

                Rotation *= 0.9f;

                if (awesomeTransformation != null && awesomeTransformation.Terminated)
                    awesomeTransformation = null;
                first = false;
            }

            base.Update();
        }

        public bool IsBeingAwesome { get { return awesomeTransformation != null; } }
        public bool IsAwesome { get { return awesomeStartTime >= 0 && Clock.ModeTime - awesomeStartTime > 100; } }
    }
}
