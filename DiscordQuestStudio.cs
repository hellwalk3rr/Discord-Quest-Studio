using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using Microsoft.Win32;

[assembly: AssemblyTitle("Discord Quest Studio")]
[assembly: AssemblyDescription("Discord Quest Studio")]
[assembly: AssemblyCompany("hellwalk3rr")]
[assembly: AssemblyProduct("Discord Quest Studio")]
[assembly: AssemblyCopyright("Copyright (C) 2026 hellwalk3rr. All Rights Reserved.")]
[assembly: AssemblyVersion("7.1.0.0")]
[assembly: AssemblyFileVersion("7.1.0.0")]

namespace DiscordQuestStudioNative
{
    internal static class Dpi
    {
        private static float factor = 0f;
        private static float rawDpi = 0f;

        private static void EnsureInitialized()
        {
            if (factor > 0.1f) return;
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    rawDpi = g.DpiX / 96.0f;
                }
            }
            catch
            {
                rawDpi = 1.0f;
            }
            if (rawDpi < 1.0f) rawDpi = 1.0f;

            try
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                float maxFitW = (wa.Width * 0.94f) / 1380.0f;
                float maxFitH = (wa.Height * 0.92f) / 860.0f;
                float maxFit = Math.Min(maxFitW, maxFitH);
                factor = Math.Max(0.72f, Math.Min(rawDpi, maxFit));
            }
            catch
            {
                factor = rawDpi;
            }
        }

        public static float Factor
        {
            get
            {
                EnsureInitialized();
                return factor;
            }
        }

        public static float RawDpi
        {
            get
            {
                EnsureInitialized();
                return rawDpi;
            }
        }

        public static int S(int px)
        {
            return (int)Math.Round(px * Factor);
        }

        public static float F(float basePt)
        {
            EnsureInitialized();
            float ratio = Factor / RawDpi;
            return Math.Max(6.5f, basePt * ratio);
        }

        public static Padding Pad(int l, int t, int r, int b)
        {
            return new Padding(S(l), S(t), S(r), S(b));
        }
    }

    internal static class Loc
    {
        public static string Lang = "ru";
        public static bool IsEn { get { return Lang == "en"; } }
        public static string T(string ru, string en) { return IsEn ? en : ru; }
    }

    internal static class GdiHelper
    {
        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return path;
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 2)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color LerpColor(Color a, Color b, float t)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            int r = (int)Math.Round(a.R + (b.R - a.R) * t);
            int g = (int)Math.Round(a.G + (b.G - a.G) * t);
            int bl = (int)Math.Round(a.B + (b.B - a.B) * t);
            int al = (int)Math.Round(a.A + (b.A - a.A) * t);
            return Color.FromArgb(
                Math.Max(0, Math.Min(255, al)),
                Math.Max(0, Math.Min(255, r)),
                Math.Max(0, Math.Min(255, g)),
                Math.Max(0, Math.Min(255, bl))
            );
        }

        public static void DrawVectorEmoji(Graphics g, string kind, Rectangle box)
        {
            if (string.IsNullOrEmpty(kind) || box.Width <= 4 || box.Height <= 4) return;
            SmoothingMode oldSm = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float x = box.X;
            float y = box.Y;
            float w = box.Width;
            float h = box.Height;
            float cx = x + w * 0.5f;
            float cy = y + h * 0.5f;

            if (kind == "gem")
            {
                PointF[] topPoly = new PointF[] {
                    new PointF(x + w * 0.22f, y + h * 0.16f),
                    new PointF(x + w * 0.78f, y + h * 0.16f),
                    new PointF(x + w * 0.94f, y + h * 0.40f),
                    new PointF(x + w * 0.50f, y + h * 0.88f),
                    new PointF(x + w * 0.06f, y + h * 0.40f)
                };
                using (SolidBrush bMain = new SolidBrush(Color.FromArgb(56, 198, 255)))
                {
                    g.FillPolygon(bMain, topPoly);
                }
                PointF[] crownPoly = new PointF[] {
                    new PointF(x + w * 0.22f, y + h * 0.16f),
                    new PointF(x + w * 0.78f, y + h * 0.16f),
                    new PointF(x + w * 0.94f, y + h * 0.40f),
                    new PointF(x + w * 0.06f, y + h * 0.40f)
                };
                using (SolidBrush bCrown = new SolidBrush(Color.FromArgb(130, 228, 255)))
                {
                    g.FillPolygon(bCrown, crownPoly);
                }
                PointF[] centerFacet = new PointF[] {
                    new PointF(x + w * 0.34f, y + h * 0.40f),
                    new PointF(x + w * 0.66f, y + h * 0.40f),
                    new PointF(x + w * 0.50f, y + h * 0.88f)
                };
                using (SolidBrush bCenter = new SolidBrush(Color.FromArgb(24, 152, 235)))
                {
                    g.FillPolygon(bCenter, centerFacet);
                }
            }
            else if (kind == "fire")
            {
                PointF[] outerFlame = new PointF[] {
                    new PointF(cx, y + h * 0.08f),
                    new PointF(x + w * 0.84f, y + h * 0.46f),
                    new PointF(x + w * 0.78f, y + h * 0.82f),
                    new PointF(cx, y + h * 0.94f),
                    new PointF(x + w * 0.22f, y + h * 0.82f),
                    new PointF(x + w * 0.16f, y + h * 0.46f)
                };
                using (SolidBrush bOuter = new SolidBrush(Color.FromArgb(255, 108, 34)))
                {
                    g.FillClosedCurve(bOuter, outerFlame);
                }
                PointF[] innerFlame = new PointF[] {
                    new PointF(cx, y + h * 0.34f),
                    new PointF(x + w * 0.68f, y + h * 0.62f),
                    new PointF(cx, y + h * 0.90f),
                    new PointF(x + w * 0.32f, y + h * 0.62f)
                };
                using (SolidBrush bInner = new SolidBrush(Color.FromArgb(255, 212, 52)))
                {
                    g.FillClosedCurve(bInner, innerFlame);
                }
            }
            else if (kind == "gear")
            {
                using (Pen pTeeth = new Pen(Color.FromArgb(174, 186, 206), Math.Max(2f, w * 0.16f)))
                {
                    pTeeth.StartCap = LineCap.Round;
                    pTeeth.EndCap = LineCap.Round;
                    g.DrawLine(pTeeth, cx, y + h * 0.10f, cx, y + h * 0.90f);
                    g.DrawLine(pTeeth, x + w * 0.10f, cy, x + w * 0.90f, cy);
                    g.DrawLine(pTeeth, x + w * 0.22f, y + h * 0.22f, x + w * 0.78f, y + h * 0.78f);
                    g.DrawLine(pTeeth, x + w * 0.78f, y + h * 0.22f, x + w * 0.22f, y + h * 0.78f);
                }
                using (SolidBrush bRing = new SolidBrush(Color.FromArgb(196, 206, 224)))
                {
                    g.FillEllipse(bRing, x + w * 0.20f, y + h * 0.20f, w * 0.60f, h * 0.60f);
                }
                using (SolidBrush bHole = new SolidBrush(Color.FromArgb(24, 28, 36)))
                {
                    g.FillEllipse(bHole, x + w * 0.36f, y + h * 0.36f, w * 0.28f, h * 0.28f);
                }
            }
            else if (kind == "pencil")
            {
                PointF[] body = new PointF[] {
                    new PointF(x + w * 0.66f, y + h * 0.14f),
                    new PointF(x + w * 0.86f, y + h * 0.34f),
                    new PointF(x + w * 0.38f, y + h * 0.82f),
                    new PointF(x + w * 0.18f, y + h * 0.62f)
                };
                using (SolidBrush bWood = new SolidBrush(Color.FromArgb(245, 182, 42)))
                {
                    g.FillPolygon(bWood, body);
                }
                PointF[] tip = new PointF[] {
                    new PointF(x + w * 0.18f, y + h * 0.62f),
                    new PointF(x + w * 0.38f, y + h * 0.82f),
                    new PointF(x + w * 0.12f, y + h * 0.88f)
                };
                using (SolidBrush bTip = new SolidBrush(Color.FromArgb(255, 224, 178)))
                {
                    g.FillPolygon(bTip, tip);
                }
                PointF[] eraser = new PointF[] {
                    new PointF(x + w * 0.66f, y + h * 0.14f),
                    new PointF(x + w * 0.76f, y + h * 0.04f),
                    new PointF(x + w * 0.96f, y + h * 0.24f),
                    new PointF(x + w * 0.86f, y + h * 0.34f)
                };
                using (SolidBrush bEr = new SolidBrush(Color.FromArgb(255, 118, 138)))
                {
                    g.FillPolygon(bEr, eraser);
                }
            }
            else if (kind == "check")
            {
                Rectangle rBox = new Rectangle((int)(x + w * 0.08f), (int)(y + h * 0.08f), (int)(w * 0.84f), (int)(h * 0.84f));
                using (GraphicsPath gp = RoundRect(rBox, Math.Max(3, (int)(w * 0.22f))))
                using (SolidBrush bGreen = new SolidBrush(Color.FromArgb(46, 204, 113)))
                {
                    g.FillPath(bGreen, gp);
                }
                using (Pen pChk = new Pen(Color.White, Math.Max(1.8f, w * 0.13f)))
                {
                    pChk.StartCap = LineCap.Round;
                    pChk.EndCap = LineCap.Round;
                    pChk.LineJoin = LineJoin.Round;
                    g.DrawLines(pChk, new PointF[] {
                        new PointF(x + w * 0.26f, y + h * 0.52f),
                        new PointF(x + w * 0.44f, y + h * 0.70f),
                        new PointF(x + w * 0.76f, y + h * 0.32f)
                    });
                }
            }
            else if (kind == "bug")
            {
                using (Pen pLegs = new Pen(Color.FromArgb(72, 219, 132), Math.Max(1.4f, w * 0.09f)))
                {
                    g.DrawLine(pLegs, x + w * 0.14f, y + h * 0.38f, x + w * 0.86f, y + h * 0.38f);
                    g.DrawLine(pLegs, x + w * 0.10f, y + h * 0.56f, x + w * 0.90f, y + h * 0.56f);
                    g.DrawLine(pLegs, x + w * 0.16f, y + h * 0.74f, x + w * 0.84f, y + h * 0.74f);
                }
                using (SolidBrush bShell = new SolidBrush(Color.FromArgb(46, 204, 113)))
                {
                    g.FillEllipse(bShell, x + w * 0.24f, y + h * 0.26f, w * 0.52f, h * 0.62f);
                }
                using (SolidBrush bHead = new SolidBrush(Color.FromArgb(245, 197, 24)))
                {
                    g.FillEllipse(bHead, x + w * 0.34f, y + h * 0.12f, w * 0.32f, h * 0.24f);
                }
            }
            else if (kind == "siren")
            {
                using (SolidBrush bDome = new SolidBrush(Color.FromArgb(255, 76, 92)))
                {
                    g.FillPie(bDome, x + w * 0.20f, y + h * 0.18f, w * 0.60f, h * 0.80f, 180, 180);
                    g.FillRectangle(bDome, x + w * 0.20f, y + h * 0.48f, w * 0.60f, h * 0.26f);
                }
                using (SolidBrush bHighlight = new SolidBrush(Color.FromArgb(255, 180, 188)))
                {
                    g.FillEllipse(bHighlight, x + w * 0.32f, y + h * 0.30f, w * 0.16f, h * 0.24f);
                }
                using (SolidBrush bBase = new SolidBrush(Color.FromArgb(168, 178, 196)))
                {
                    g.FillRectangle(bBase, x + w * 0.14f, y + h * 0.74f, w * 0.72f, h * 0.16f);
                }
            }
            else if (kind == "bolt")
            {
                PointF[] bolt = new PointF[] {
                    new PointF(x + w * 0.58f, y + h * 0.06f),
                    new PointF(x + w * 0.20f, y + h * 0.54f),
                    new PointF(x + w * 0.48f, y + h * 0.54f),
                    new PointF(x + w * 0.36f, y + h * 0.94f),
                    new PointF(x + w * 0.82f, y + h * 0.44f),
                    new PointF(x + w * 0.54f, y + h * 0.44f)
                };
                using (SolidBrush bBolt = new SolidBrush(Color.FromArgb(18, 20, 26)))
                {
                    g.FillPolygon(bBolt, bolt);
                }
            }
            else if (kind == "rocket")
            {
                PointF[] finL = new PointF[] {
                    new PointF(x + w * 0.28f, y + h * 0.54f),
                    new PointF(x + w * 0.10f, y + h * 0.82f),
                    new PointF(x + w * 0.36f, y + h * 0.76f)
                };
                PointF[] finR = new PointF[] {
                    new PointF(x + w * 0.72f, y + h * 0.54f),
                    new PointF(x + w * 0.90f, y + h * 0.82f),
                    new PointF(x + w * 0.64f, y + h * 0.76f)
                };
                using (SolidBrush bFin = new SolidBrush(Color.FromArgb(214, 48, 49)))
                {
                    g.FillPolygon(bFin, finL);
                    g.FillPolygon(bFin, finR);
                }
                PointF[] hull = new PointF[] {
                    new PointF(cx, y + h * 0.06f),
                    new PointF(x + w * 0.72f, y + h * 0.38f),
                    new PointF(x + w * 0.66f, y + h * 0.76f),
                    new PointF(x + w * 0.34f, y + h * 0.76f),
                    new PointF(x + w * 0.28f, y + h * 0.38f)
                };
                using (SolidBrush bHull = new SolidBrush(Color.FromArgb(18, 20, 26)))
                {
                    g.FillPolygon(bHull, hull);
                }
                using (SolidBrush bWin = new SolidBrush(Color.FromArgb(72, 202, 255)))
                {
                    g.FillEllipse(bWin, x + w * 0.39f, y + h * 0.34f, w * 0.22f, h * 0.22f);
                }
            }
            else if (kind == "stop")
            {
                PointF[] oct = new PointF[] {
                    new PointF(x + w * 0.32f, y + h * 0.10f),
                    new PointF(x + w * 0.68f, y + h * 0.10f),
                    new PointF(x + w * 0.90f, y + h * 0.32f),
                    new PointF(x + w * 0.90f, y + h * 0.68f),
                    new PointF(x + w * 0.68f, y + h * 0.90f),
                    new PointF(x + w * 0.32f, y + h * 0.90f),
                    new PointF(x + w * 0.10f, y + h * 0.68f),
                    new PointF(x + w * 0.10f, y + h * 0.32f)
                };
                using (SolidBrush bOct = new SolidBrush(Color.FromArgb(255, 82, 96)))
                {
                    g.FillPolygon(bOct, oct);
                }
                using (SolidBrush bSq = new SolidBrush(Color.White))
                {
                    g.FillRectangle(bSq, x + w * 0.34f, y + h * 0.34f, w * 0.32f, h * 0.32f);
                }
            }
            else if (kind == "book")
            {
                Rectangle rCover = new Rectangle((int)(x + w * 0.16f), (int)(y + h * 0.12f), (int)(w * 0.68f), (int)(h * 0.76f));
                using (GraphicsPath gp = RoundRect(rCover, 3))
                using (SolidBrush bBook = new SolidBrush(Color.FromArgb(88, 166, 255)))
                {
                    g.FillPath(bBook, gp);
                }
                using (Pen pLines = new Pen(Color.White, 1.4f))
                {
                    g.DrawLine(pLines, x + w * 0.30f, y + h * 0.34f, x + w * 0.70f, y + h * 0.34f);
                    g.DrawLine(pLines, x + w * 0.30f, y + h * 0.50f, x + w * 0.70f, y + h * 0.50f);
                    g.DrawLine(pLines, x + w * 0.30f, y + h * 0.66f, x + w * 0.56f, y + h * 0.66f);
                }
            }
            else if (kind == "scales")
            {
                using (Pen pGold = new Pen(Color.FromArgb(245, 197, 24), Math.Max(1.6f, w * 0.10f)))
                {
                    g.DrawLine(pGold, cx, y + h * 0.14f, cx, y + h * 0.86f);
                    g.DrawLine(pGold, x + w * 0.26f, y + h * 0.86f, x + w * 0.74f, y + h * 0.86f);
                    g.DrawLine(pGold, x + w * 0.16f, y + h * 0.32f, x + w * 0.84f, y + h * 0.32f);
                }
                using (SolidBrush bPan = new SolidBrush(Color.FromArgb(245, 197, 24)))
                {
                    g.FillPie(bPan, x + w * 0.06f, y + h * 0.44f, w * 0.32f, h * 0.28f, 0, 180);
                    g.FillPie(bPan, x + w * 0.62f, y + h * 0.44f, w * 0.32f, h * 0.28f, 0, 180);
                }
            }
            else if (kind == "gamepad")
            {
                Rectangle rPad = new Rectangle((int)(x + w * 0.08f), (int)(y + h * 0.24f), (int)(w * 0.84f), (int)(h * 0.54f));
                using (GraphicsPath gp = RoundRect(rPad, Math.Max(4, (int)(h * 0.24f))))
                using (SolidBrush bPad = new SolidBrush(Color.FromArgb(138, 116, 249)))
                {
                    g.FillPath(bPad, gp);
                }
                using (Pen pCross = new Pen(Color.White, 1.6f))
                {
                    g.DrawLine(pCross, x + w * 0.24f, cy, x + w * 0.40f, cy);
                    g.DrawLine(pCross, x + w * 0.32f, y + h * 0.40f, x + w * 0.32f, y + h * 0.60f);
                }
                using (SolidBrush bBtn = new SolidBrush(Color.FromArgb(245, 197, 24)))
                {
                    g.FillEllipse(bBtn, x + w * 0.62f, y + h * 0.38f, w * 0.10f, h * 0.10f);
                    g.FillEllipse(bBtn, x + w * 0.74f, y + h * 0.50f, w * 0.10f, h * 0.10f);
                }
            }
            else if (kind == "sync")
            {
                using (Pen pSync = new Pen(Color.FromArgb(18, 20, 26), Math.Max(1.8f, w * 0.12f)))
                {
                    pSync.StartCap = LineCap.Round;
                    pSync.EndCap = LineCap.ArrowAnchor;
                    g.DrawArc(pSync, x + w * 0.16f, y + h * 0.16f, w * 0.68f, h * 0.68f, 200, 140);
                    g.DrawArc(pSync, x + w * 0.16f, y + h * 0.16f, w * 0.68f, h * 0.68f, 20, 140);
                }
            }

            g.SmoothingMode = oldSm;
        }
    }

    public class QuestItem
    {
        public string Id = "";
        public string AppId = "";
        public string QuestName = "";
        public string GameName = "";
        public string Expires = "—";
        public double ExpiresTs = 0;
        public string TaskType = "PLAY_ON_DESKTOP";
        public string TaskLabel = "Играть 15 мин.";
        public int BaseOrbs = 0;
        public string RewardText = "Награда Discord";
        public string ExeRelPath = "";
        public string SteamId = "";
        public int DurationMin = 15;
        public int RemainingMin = 15;
        public int TargetSec = 900;
        public int DoneSec = 0;
        public int ServerProgressPct = 0;
        public bool IsEnrolled = false;
        public bool IsClaimed = false;
        public string TrafficMetadataSealed = "";
        public string MetadataSealed = "";

        public bool IsVideoQuest()
        {
            return !string.IsNullOrEmpty(TaskType) &&
                   TaskType.IndexOf("WATCH_VIDEO", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool IsHeartbeatQuest()
        {
            if (string.IsNullOrEmpty(TaskType)) return false;
            return TaskType.IndexOf("PLAY_ACTIVITY", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   TaskType.IndexOf("STREAM_ON_DESKTOP", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public class DetectableGameItem
    {
        public string Id = "";
        public string Name = "";
        public string ExePath = "";
        public string SteamId = "";
    }

    public class ActiveSpoofItem
    {
        public int Pid = 0;
        public string GameName = "";
        public string ExeFullPath = "";
        public List<string> CreatedDirs = new List<string>();
        public string AcfPath = "";
        public int DurationSec = 900;
        public string QuestId = "";
        public int OrbsReward = 0;
        public DateTime StartedAtUtc = DateTime.UtcNow;
        public Process ProcObj = null;
        public bool IsVideoQuest = false;
        public bool IsHeartbeatQuest = false;
        public volatile bool VideoStopRequested = false;
        public volatile bool VideoFinished = false;

        public int ElapsedSeconds()
        {
            return Math.Max(0, (int)(DateTime.UtcNow - StartedAtUtc).TotalSeconds);
        }

        public int RemainingSeconds()
        {
            if ((IsVideoQuest || IsHeartbeatQuest) && VideoFinished) return 0;
            return Math.Max(0, DurationSec - ElapsedSeconds());
        }

        public int ProgressPct()
        {
            if ((IsVideoQuest || IsHeartbeatQuest) && VideoFinished) return 100;
            if (DurationSec <= 0) return 100;
            return Math.Min(100, (int)((ElapsedSeconds() * 100.0) / DurationSec));
        }

        public bool IsRunning()
        {
            if (IsVideoQuest || IsHeartbeatQuest)
            {
                return !VideoStopRequested && !VideoFinished;
            }
            if (ProcObj != null)
            {
                try { return !ProcObj.HasExited; } catch { }
            }
            if (Pid <= 0) return false;
            try
            {
                Process p = Process.GetProcessById(Pid);
                return !p.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public void StopAndCleanup()
        {
            if (IsVideoQuest || IsHeartbeatQuest)
            {
                VideoStopRequested = true;
                return;
            }
            if (ProcObj != null)
            {
                try { if (!ProcObj.HasExited) ProcObj.Kill(); } catch { }
            }
            if (Pid > 0)
            {
                try
                {
                    Process p = Process.GetProcessById(Pid);
                    if (!p.HasExited) p.Kill();
                }
                catch { }
            }
            if (!string.IsNullOrEmpty(ExeFullPath))
            {
                try { if (File.Exists(ExeFullPath)) File.Delete(ExeFullPath); } catch { }
            }
            if (!string.IsNullOrEmpty(AcfPath))
            {
                try { if (File.Exists(AcfPath)) File.Delete(AcfPath); } catch { }
            }
            CreatedDirs.Sort((a, b) => b.Length.CompareTo(a.Length));
            foreach (string d in CreatedDirs)
            {
                try
                {
                    if (Directory.Exists(d) && Directory.GetFileSystemEntries(d).Length == 0)
                    {
                        Directory.Delete(d);
                    }
                }
                catch { }
            }
        }
    }

    public class DarkGoldTitleBar : Control
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private Form targetForm;
        private Image logoImg;
        private bool allowMaximize;
        private int hoveredBtn = -1; // 0 = min, 1 = max, 2 = close, 3 = tray
        private float[] btnAnim = new float[] { 0f, 0f, 0f, 0f };
        public event Action TrayRequested;
        private System.Windows.Forms.Timer animTimer;
        private Font titleFont;

        public DarkGoldTitleBar(Form form, Image logo, string titleText, bool canMaximize = true, bool canMinimize = true)
        {
            this.targetForm = form;
            this.logoImg = logo;
            this.Text = titleText;
            this.allowMaximize = canMaximize;
            this.ShowMinimize = canMinimize;
            this.Dock = DockStyle.Top;
            this.Height = Dpi.S(34);
            this.BackColor = Color.FromArgb(9, 10, 13);
            this.ForeColor = Color.FromArgb(243, 245, 248);
            this.titleFont = new Font("Segoe UI", Dpi.F(9.0f), FontStyle.Bold);

            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 16;
            animTimer.Tick += (s, e) =>
            {
                bool active = false;
                for (int i = 0; i < 4; i++)
                {
                    float target = (hoveredBtn == i) ? 1.0f : 0.0f;
                    if (Math.Abs(btnAnim[i] - target) < 0.05f)
                    {
                        btnAnim[i] = target;
                    }
                    else
                    {
                        btnAnim[i] += (target - btnAnim[i]) * 0.32f;
                        active = true;
                    }
                }
                if (!active) animTimer.Stop();
                this.Invalidate();
            };
        }

        public bool ShowMinimize = true;

        private Rectangle GetCloseRect()
        {
            int bw = Dpi.S(46);
            return new Rectangle(this.ClientSize.Width - bw, 0, bw, this.ClientSize.Height - 1);
        }

        private Rectangle GetMaxRect()
        {
            if (!allowMaximize) return Rectangle.Empty;
            int bw = Dpi.S(46);
            return new Rectangle(this.ClientSize.Width - bw * 2, 0, bw, this.ClientSize.Height - 1);
        }

        private Rectangle GetMinRect()
        {
            if (!ShowMinimize) return Rectangle.Empty;
            int bw = Dpi.S(46);
            int mult = allowMaximize ? 3 : 2;
            return new Rectangle(this.ClientSize.Width - bw * mult, 0, bw, this.ClientSize.Height - 1);
        }

        private Rectangle GetTrayRect()
        {
            if (!ShowMinimize || !allowMaximize) return Rectangle.Empty;
            int bw = Dpi.S(46);
            return new Rectangle(this.ClientSize.Width - bw * 4, 0, bw, this.ClientSize.Height - 1);
        }

        private int HitTestButton(Point pt)
        {
            if (GetCloseRect().Contains(pt)) return 2;
            if (allowMaximize && GetMaxRect().Contains(pt)) return 1;
            if (ShowMinimize && GetMinRect().Contains(pt)) return 0;
            if (ShowMinimize && allowMaximize && GetTrayRect().Contains(pt)) return 3;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hit = HitTestButton(e.Location);
            Cursor c = (hit >= 0) ? Cursors.Hand : Cursors.Default;
            if (this.Cursor != c) this.Cursor = c;
            if (hit != hoveredBtn)
            {
                hoveredBtn = hit;
                animTimer.Start();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoveredBtn != -1)
            {
                hoveredBtn = -1;
                animTimer.Start();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int hit = HitTestButton(e.Location);
            if (hit >= 0) return;

            if (targetForm != null)
            {
                if (e.Clicks == 2 && allowMaximize)
                {
                    ToggleMaximize();
                    return;
                }
                try
                {
                    ReleaseCapture();
                    SendMessage(targetForm.Handle, 0x00A1, 0x0002, 0);
                }
                catch { }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || targetForm == null) return;
            int hit = HitTestButton(e.Location);
            if (hit == 2)
            {
                targetForm.Close();
            }
            else if (hit == 1 && allowMaximize)
            {
                ToggleMaximize();
            }
            else if (hit == 0 && ShowMinimize)
            {
                targetForm.WindowState = FormWindowState.Minimized;
            }
            else if (hit == 3 && ShowMinimize && allowMaximize)
            {
                if (TrayRequested != null) TrayRequested();
            }
        }

        private void ToggleMaximize()
        {
            if (targetForm == null) return;
            if (targetForm.WindowState == FormWindowState.Maximized)
            {
                targetForm.WindowState = FormWindowState.Normal;
            }
            else
            {
                targetForm.WindowState = FormWindowState.Maximized;
            }
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush bgBr = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(bgBr, this.ClientRectangle);
            }

            // Subtle bottom divider + Cyber Gold accent bar on the left
            using (Pen divPen = new Pen(Color.FromArgb(28, 33, 44), 1f))
            {
                e.Graphics.DrawLine(divPen, 0, this.ClientSize.Height - 1, this.ClientSize.Width, this.ClientSize.Height - 1);
            }
            using (Pen goldAccent = new Pen(Color.FromArgb(180, 245, 197, 24), 1.5f))
            {
                e.Graphics.DrawLine(goldAccent, Dpi.S(14), this.ClientSize.Height - 1, Dpi.S(118), this.ClientSize.Height - 1);
            }

            int x = Dpi.S(14);
            if (logoImg != null)
            {
                int isz = Dpi.S(18);
                int iy = (this.ClientSize.Height - isz) / 2;
                e.Graphics.DrawImage(logoImg, new Rectangle(x, iy, isz, isz));
                x += isz + Dpi.S(8);
            }

            // Small gold accent dot + title
            using (SolidBrush dotBr = new SolidBrush(Color.FromArgb(245, 197, 24)))
            {
                int dsz = Dpi.S(5);
                e.Graphics.FillEllipse(dotBr, x, (this.ClientSize.Height - dsz) / 2, dsz, dsz);
                x += dsz + Dpi.S(7);
            }

            int rightButtonsStart = (ShowMinimize && allowMaximize) ? GetTrayRect().Left : (ShowMinimize ? GetMinRect().Left : (allowMaximize ? GetMaxRect().Left : GetCloseRect().Left));
            Rectangle titleRect = new Rectangle(x, 0, Math.Max(20, rightButtonsStart - x - Dpi.S(8)), this.ClientSize.Height);
            TextRenderer.DrawText(
                e.Graphics,
                this.Text,
                titleFont,
                titleRect,
                Color.FromArgb(232, 236, 244),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
             | TextFormatFlags.NoPrefix);

            // Minimize to System Tray button
            if (ShowMinimize && allowMaximize)
            {
                Rectangle rTray = GetTrayRect();
                if (btnAnim[3] > 0.01f)
                {
                    using (SolidBrush hBr = new SolidBrush(Color.FromArgb((int)(btnAnim[3] * 48), 245, 197, 24)))
                    {
                        e.Graphics.FillRectangle(hBr, rTray);
                    }
                    using (Pen hPen = new Pen(Color.FromArgb((int)(btnAnim[3] * 220), 245, 197, 24), 2f))
                    {
                        e.Graphics.DrawLine(hPen, rTray.Left + Dpi.S(8), rTray.Bottom, rTray.Right - Dpi.S(8), rTray.Bottom);
                    }
                }
                Color cTray = GdiHelper.LerpColor(Color.FromArgb(168, 176, 190), Color.FromArgb(245, 197, 24), btnAnim[3]);
                using (Pen pTray = new Pen(cTray, 1.6f))
                {
                    pTray.StartCap = LineCap.Round;
                    pTray.EndCap = LineCap.Round;
                    pTray.LineJoin = LineJoin.Round;
                    int cx = rTray.X + rTray.Width / 2;
                    int cy = rTray.Y + rTray.Height / 2;
                    int d = Dpi.S(4);
                    e.Graphics.DrawLines(pTray, new Point[] {
                        new Point(cx - d, cy - d / 2),
                        new Point(cx, cy + d / 2),
                        new Point(cx + d, cy - d / 2)
                    });
                    e.Graphics.DrawLine(pTray, cx - d, cy + d + 1, cx + d, cy + d + 1);
                }
            }

            // Minimize button
            if (ShowMinimize)
            {
                Rectangle rMin = GetMinRect();
                if (btnAnim[0] > 0.01f)
                {
                    using (SolidBrush hBr = new SolidBrush(Color.FromArgb((int)(btnAnim[0] * 48), 245, 197, 24)))
                    {
                        e.Graphics.FillRectangle(hBr, rMin);
                    }
                    using (Pen hPen = new Pen(Color.FromArgb((int)(btnAnim[0] * 220), 245, 197, 24), 2f))
                    {
                        e.Graphics.DrawLine(hPen, rMin.Left + Dpi.S(8), rMin.Bottom, rMin.Right - Dpi.S(8), rMin.Bottom);
                    }
                }
                Color cMin = GdiHelper.LerpColor(Color.FromArgb(168, 176, 190), Color.FromArgb(245, 197, 24), btnAnim[0]);
                using (Pen pMin = new Pen(cMin, 1.6f))
                {
                    int mw = Dpi.S(11);
                    int mx = rMin.X + (rMin.Width - mw) / 2;
                    int my = rMin.Y + rMin.Height / 2;
                    e.Graphics.DrawLine(pMin, mx, my, mx + mw, my);
                }
            }

            // Maximize / Restore button
            if (allowMaximize)
            {
                Rectangle rMax = GetMaxRect();
                if (btnAnim[1] > 0.01f)
                {
                    using (SolidBrush hBr = new SolidBrush(Color.FromArgb((int)(btnAnim[1] * 48), 245, 197, 24)))
                    {
                        e.Graphics.FillRectangle(hBr, rMax);
                    }
                    using (Pen hPen = new Pen(Color.FromArgb((int)(btnAnim[1] * 220), 245, 197, 24), 2f))
                    {
                        e.Graphics.DrawLine(hPen, rMax.Left + Dpi.S(8), rMax.Bottom, rMax.Right - Dpi.S(8), rMax.Bottom);
                    }
                }
                Color cMax = GdiHelper.LerpColor(Color.FromArgb(168, 176, 190), Color.FromArgb(245, 197, 24), btnAnim[1]);
                using (Pen pMax = new Pen(cMax, 1.5f))
                {
                    int bx = Dpi.S(10);
                    Rectangle sq = new Rectangle(rMax.X + (rMax.Width - bx) / 2, rMax.Y + (rMax.Height - bx) / 2, bx, bx);
                    e.Graphics.DrawRectangle(pMax, sq);
                }
            }

            // Close button
            Rectangle rClose = GetCloseRect();
            if (btnAnim[2] > 0.01f)
            {
                Color closeBg = GdiHelper.LerpColor(Color.FromArgb(46, 38, 18), Color.FromArgb(214, 48, 49), btnAnim[2]);
                using (SolidBrush hBr = new SolidBrush(Color.FromArgb((int)(btnAnim[2] * 225), closeBg)))
                {
                    e.Graphics.FillRectangle(hBr, rClose);
                }
            }
            Color cClose = GdiHelper.LerpColor(Color.FromArgb(245, 197, 24), Color.White, btnAnim[2]);
            using (Pen pClose = new Pen(cClose, 1.7f))
            {
                pClose.StartCap = LineCap.Round;
                pClose.EndCap = LineCap.Round;
                int xw = Dpi.S(10);
                int xx = rClose.X + (rClose.Width - xw) / 2;
                int xy = rClose.Y + (rClose.Height - xw) / 2;
                e.Graphics.DrawLine(pClose, xx, xy, xx + xw, xy + xw);
                e.Graphics.DrawLine(pClose, xx + xw, xy, xx, xy + xw);
            }
        }
    }

    public class HighDpiLogoControl : Control
    {
        public Image LogoImage;
        private float hoverT = 0f;
        private bool isHovered = false;
        private System.Windows.Forms.Timer animTimer;

        public HighDpiLogoControl(Image img)
        {
            this.LogoImage = img;
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true
            );
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 16;
            animTimer.Tick += (s, e) =>
            {
                float target = isHovered ? 1.0f : 0.0f;
                if (Math.Abs(hoverT - target) < 0.04f)
                {
                    hoverT = target;
                    animTimer.Stop();
                }
                else
                {
                    hoverT += (target - hoverT) * 0.28f;
                }
                this.Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            animTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            animTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (hoverT > 0.01f)
            {
                Rectangle pillGlow = new Rectangle(1, 1, this.ClientSize.Width - 3, this.ClientSize.Height - 3);
                using (GraphicsPath gp = GdiHelper.RoundRect(pillGlow, Dpi.S(12)))
                {
                    using (SolidBrush brGlow = new SolidBrush(Color.FromArgb((int)(hoverT * 36), 245, 197, 24)))
                    {
                        e.Graphics.FillPath(brGlow, gp);
                    }
                    using (Pen penGlow = new Pen(Color.FromArgb((int)(hoverT * 140), 245, 197, 24), 1.3f))
                    {
                        e.Graphics.DrawPath(penGlow, gp);
                    }
                }
            }

            int baseIco = Math.Max(Dpi.S(22), this.ClientSize.Height - Dpi.S(14));
            int icoSz = baseIco + (int)Math.Round(hoverT * Dpi.S(2));
            int icoY = (this.ClientSize.Height - icoSz) / 2;

            Color topCol = GdiHelper.LerpColor(Color.White, Color.FromArgb(255, 236, 150), hoverT);
            Color botCol = GdiHelper.LerpColor(Color.FromArgb(245, 197, 24), Color.FromArgb(255, 224, 82), hoverT);

            using (Font fTop = new Font("Segoe UI", Dpi.F(9.0f), FontStyle.Bold | FontStyle.Italic))
            using (Font fBot = new Font("Segoe UI", Dpi.F(9.5f), FontStyle.Bold | FontStyle.Italic))
            {
                Size szTop = TextRenderer.MeasureText("QUEST", fTop);
                Size szBot = TextRenderer.MeasureText("STUDIO", fBot);
                int textW = Math.Max(szTop.Width, szBot.Width);
                int gap = Dpi.S(8);
                int totalW = icoSz + gap + textW;
                int icoX = Math.Max(Dpi.S(8), (this.ClientSize.Width - totalW) / 2);

                if (hoverT > 0.01f)
                {
                    Rectangle halo = new Rectangle(icoX - Dpi.S(2), icoY - Dpi.S(2), icoSz + Dpi.S(4), icoSz + Dpi.S(4));
                    using (SolidBrush bHalo = new SolidBrush(Color.FromArgb((int)(hoverT * 68), 245, 197, 24)))
                    {
                        e.Graphics.FillEllipse(bHalo, halo);
                    }
                }

                if (LogoImage != null)
                {
                    e.Graphics.DrawImage(LogoImage, new Rectangle(icoX, icoY, icoSz, icoSz));
                }

                int textX = icoX + icoSz + gap;
                int lineH = Dpi.S(14);
                int blockH = lineH * 2;
                int textTopY = (this.ClientSize.Height - blockH) / 2;

                TextRenderer.DrawText(
                    e.Graphics,
                    "QUEST",
                    fTop,
                    new Rectangle(textX, textTopY, textW + Dpi.S(4), lineH),
                    topCol,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                 | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(
                    e.Graphics,
                    "STUDIO",
                    fBot,
                    new Rectangle(textX, textTopY + lineH, textW + Dpi.S(4), lineH),
                    botCol,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                 | TextFormatFlags.NoPrefix);
            }
        }
    }

    public class IconHeaderLabel : Control
    {
        public string IconKind = "";
        public IconHeaderLabel(string iconKind, string text, float fontPt)
        {
            this.IconKind = iconKind;
            this.Text = text;
            this.Font = new Font("Segoe UI", Dpi.F(fontPt), FontStyle.Bold);
            this.ForeColor = Color.FromArgb(243, 245, 248);
            this.BackColor = Color.FromArgb(19, 22, 29);
            this.Margin = Padding.Empty;
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (SolidBrush bg = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(bg, this.ClientRectangle);
            }
            int x = 0;
            if (!string.IsNullOrEmpty(IconKind))
            {
                int isz = Dpi.S(18);
                int iy = (this.ClientSize.Height - isz) / 2;
                GdiHelper.DrawVectorEmoji(e.Graphics, IconKind, new Rectangle(0, iy, isz, isz));
                x = isz + Dpi.S(7);
            }
            Rectangle tr = new Rectangle(x, 0, Math.Max(10, this.ClientSize.Width - x), this.ClientSize.Height);
            TextRenderer.DrawText(
                e.Graphics,
                this.Text,
                this.Font,
                tr,
                this.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
             | TextFormatFlags.NoPrefix);
        }
    }

    public class DarkSortDropdown : Control
    {
        private List<string> items = new List<string>();
        private int selectedIndex = 0;
        private float hoverT = 0f;
        private bool isHovered = false;
        private System.Windows.Forms.Timer animTimer;
        private ContextMenuStrip popupMenu;

        public event EventHandler SelectedIndexChanged;

        public void SetOptions(string[] options)
        {
            items.Clear();
            popupMenu.Items.Clear();
            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    int idx = i;
                    items.Add(options[i]);
                    ToolStripMenuItem mi = new ToolStripMenuItem(options[i]);
                    mi.BackColor = Color.FromArgb(18, 21, 28);
                    mi.ForeColor = Color.FromArgb(243, 245, 248);
                    mi.Click += (s, e) => { this.SelectedIndex = idx; };
                    popupMenu.Items.Add(mi);
                }
            }
            this.Invalidate();
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                if (value >= 0 && value < items.Count && value != selectedIndex)
                {
                    selectedIndex = value;
                    this.Invalidate();
                    if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
                }
            }
        }

        public void SetSelectedIndexSilently(int index)
        {
            if (index >= 0 && index < items.Count && index != selectedIndex)
            {
                selectedIndex = index;
                this.Invalidate();
            }
        }

        public DarkSortDropdown(string[] options)
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.FromArgb(19, 22, 29);
            this.ForeColor = Color.FromArgb(243, 245, 248);
            this.Font = new Font("Segoe UI", Dpi.F(8.8f), FontStyle.Regular);

            popupMenu = new ContextMenuStrip();
            popupMenu.ShowImageMargin = false;
            popupMenu.BackColor = Color.FromArgb(18, 21, 28);
            popupMenu.ForeColor = Color.FromArgb(243, 245, 248);
            popupMenu.Font = new Font("Segoe UI", Dpi.F(9f), FontStyle.Regular);

            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    int idx = i;
                    items.Add(options[i]);
                    ToolStripMenuItem mi = new ToolStripMenuItem(options[i]);
                    mi.BackColor = Color.FromArgb(18, 21, 28);
                    mi.ForeColor = Color.FromArgb(243, 245, 248);
                    mi.Click += (s, e) =>
                    {
                        this.SelectedIndex = idx;
                    };
                    popupMenu.Items.Add(mi);
                }
            }

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 16;
            animTimer.Tick += (s, e) =>
            {
                float target = isHovered ? 1.0f : 0.0f;
                if (Math.Abs(hoverT - target) < 0.05f)
                {
                    hoverT = target;
                    animTimer.Stop();
                }
                else
                {
                    hoverT += (target - hoverT) * 0.30f;
                }
                this.Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            animTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            animTimer.Start();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            for (int i = 0; i < popupMenu.Items.Count; i++)
            {
                popupMenu.Items[i].ForeColor = (i == selectedIndex) ? Color.FromArgb(245, 197, 24) : Color.FromArgb(243, 245, 248);
            }
            popupMenu.Show(this, new Point(0, this.Height));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (SolidBrush cb = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(cb, this.ClientRectangle);
            }

            Rectangle rect = new Rectangle(1, 1, this.ClientSize.Width - 3, this.ClientSize.Height - 3);
            Color fill = GdiHelper.LerpColor(Color.FromArgb(13, 15, 20), Color.FromArgb(20, 24, 32), hoverT);
            Color border = GdiHelper.LerpColor(Color.FromArgb(38, 44, 56), Color.FromArgb(245, 197, 24), hoverT * 0.85f);

            using (GraphicsPath gp = GdiHelper.RoundRect(rect, Dpi.S(8)))
            {
                using (SolidBrush br = new SolidBrush(fill)) e.Graphics.FillPath(br, gp);
                using (Pen pen = new Pen(border, 1.2f)) e.Graphics.DrawPath(pen, gp);
            }

            string curText = (selectedIndex >= 0 && selectedIndex < items.Count) ? items[selectedIndex] : "";
            Rectangle textRect = new Rectangle(Dpi.S(12), 0, Math.Max(20, this.ClientSize.Width - Dpi.S(34)), this.ClientSize.Height);
            TextRenderer.DrawText(
                e.Graphics,
                curText,
                this.Font,
                textRect,
                this.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
             | TextFormatFlags.NoPrefix);

            Color arrowCol = GdiHelper.LerpColor(Color.FromArgb(158, 166, 178), Color.FromArgb(245, 197, 24), hoverT);
            Rectangle arrowRect = new Rectangle(this.ClientSize.Width - Dpi.S(26), 0, Dpi.S(20), this.ClientSize.Height);
            TextRenderer.DrawText(
                e.Graphics,
                "▼",
                new Font("Segoe UI", Dpi.F(7.5f), FontStyle.Bold),
                arrowRect,
                arrowCol,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
             | TextFormatFlags.NoPrefix);
        }
    }

    public class RoundedPanelCard : Panel
    {
        public int Radius = 16;
        public Color FillColor = Color.FromArgb(19, 22, 29);
        public Color BorderColor = Color.FromArgb(36, 41, 54);

        public RoundedPanelCard()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
            this.BackColor = Color.FromArgb(11, 12, 14);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Color cornerBg = (this.Parent != null) ? this.Parent.BackColor : this.BackColor;
            using (SolidBrush cb = new SolidBrush(cornerBg))
            {
                e.Graphics.FillRectangle(cb, this.ClientRectangle);
            }
            Rectangle rect = new Rectangle(1, 1, this.ClientSize.Width - 3, this.ClientSize.Height - 3);
            using (GraphicsPath path = GdiHelper.RoundRect(rect, Dpi.S(Radius)))
            {
                using (SolidBrush br = new SolidBrush(FillColor))
                {
                    e.Graphics.FillPath(br, path);
                }
                using (Pen pen = new Pen(BorderColor, 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }

    public class PillActionButton : Control
    {
        public Color FillNormal = Color.FromArgb(26, 30, 38);
        public Color FillHover = Color.FromArgb(38, 44, 58);
        public Color BorderNormal = Color.FromArgb(46, 52, 66);
        public Color BorderHover = Color.FromArgb(245, 197, 24);
        public Color TextColor = Color.FromArgb(243, 245, 248);
        public string IconKind = "";
        public Color StatusDotColor = Color.Empty;

        private bool hovered = false;
        private bool pressed = false;
        private float hoverT = 0f;
        private System.Windows.Forms.Timer animTimer;

        protected override Size DefaultSize
        {
            get { return new Size(Dpi.S(140), Dpi.S(34)); }
        }

        public PillActionButton(string text)
        {
            this.Text = text;
            this.Cursor = Cursors.Hand;
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
            this.Font = new Font("Segoe UI", Dpi.F(9.0f), FontStyle.Bold);
            this.BackColor = Color.FromArgb(19, 22, 29);

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 16;
            animTimer.Tick += (s, e) =>
            {
                float target = hovered ? 1.0f : 0.0f;
                if (Math.Abs(hoverT - target) < 0.04f)
                {
                    hoverT = target;
                    animTimer.Stop();
                }
                else
                {
                    hoverT += (target - hoverT) * 0.30f;
                }
                this.Invalidate();
            };
        }

        public void AutoSizeToText(int padHorizontal, int heightAt96Dpi)
        {
            int iconExtra = string.IsNullOrEmpty(IconKind) ? 0 : Dpi.S(20);
            int dotExtra = (StatusDotColor != Color.Empty) ? Dpi.S(14) : 0;
            if (string.IsNullOrEmpty(this.Text))
            {
                this.Size = new Size(Dpi.S(heightAt96Dpi + 6), Dpi.S(heightAt96Dpi));
                return;
            }
            Size sz = TextRenderer.MeasureText(this.Text, this.Font);
            this.Size = new Size(sz.Width + iconExtra + dotExtra + Dpi.S(padHorizontal), Dpi.S(heightAt96Dpi));
        }

        public static PillActionButton CreateGold(string text, Color parentBg, string iconKind = "")
        {
            PillActionButton b = new PillActionButton(text);
            b.IconKind = iconKind;
            b.BackColor = parentBg;
            b.FillNormal = Color.FromArgb(245, 197, 24);
            b.FillHover = Color.FromArgb(255, 218, 68);
            b.BorderNormal = Color.FromArgb(245, 197, 24);
            b.BorderHover = Color.FromArgb(255, 238, 130);
            b.TextColor = Color.FromArgb(11, 12, 14);
            return b;
        }

        public static PillActionButton CreateDark(string text, Color parentBg, string iconKind = "")
        {
            PillActionButton b = new PillActionButton(text);
            b.IconKind = iconKind;
            b.BackColor = parentBg;
            b.FillNormal = Color.FromArgb(26, 30, 38);
            b.FillHover = Color.FromArgb(38, 45, 58);
            b.BorderNormal = Color.FromArgb(46, 52, 66);
            b.BorderHover = Color.FromArgb(245, 197, 24);
            b.TextColor = Color.FromArgb(243, 245, 248);
            return b;
        }

        public static PillActionButton CreateDanger(string text, Color parentBg, string iconKind = "")
        {
            PillActionButton b = new PillActionButton(text);
            b.IconKind = iconKind;
            b.BackColor = parentBg;
            b.FillNormal = Color.FromArgb(44, 18, 22);
            b.FillHover = Color.FromArgb(72, 26, 34);
            b.BorderNormal = Color.FromArgb(96, 36, 44);
            b.BorderHover = Color.FromArgb(255, 110, 120);
            b.TextColor = Color.FromArgb(255, 110, 110);
            return b;
        }

        public static PillActionButton CreateNavTab(string text, bool active, Color parentBg, string iconKind = "")
        {
            PillActionButton b = new PillActionButton(text);
            b.IconKind = iconKind;
            b.BackColor = parentBg;
            b.SetNavActive(active);
            b.AutoSizeToText(26, 34);
            return b;
        }

        public void SetNavActive(bool active)
        {
            if (active)
            {
                FillNormal = Color.FromArgb(38, 33, 18);
                FillHover = Color.FromArgb(52, 44, 22);
                BorderNormal = Color.FromArgb(110, 88, 26);
                BorderHover = Color.FromArgb(245, 197, 24);
                TextColor = Color.FromArgb(245, 197, 24);
            }
            else
            {
                FillNormal = this.BackColor;
                FillHover = Color.FromArgb(26, 30, 38);
                BorderNormal = this.BackColor;
                BorderHover = Color.FromArgb(70, 80, 100);
                TextColor = Color.FromArgb(172, 180, 194);
            }
            this.Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hovered = true;
            animTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovered = false;
            pressed = false;
            animTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            pressed = true;
            this.Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            pressed = false;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color bgClear = (this.Parent != null) ? this.Parent.BackColor : this.BackColor;
            using (SolidBrush cb = new SolidBrush(bgClear))
            {
                e.Graphics.FillRectangle(cb, this.ClientRectangle);
            }

            int yShift = pressed ? 1 : 0;
            Rectangle rect = new Rectangle(2, 2 + yShift, this.ClientSize.Width - 5, this.ClientSize.Height - 5);
            int radius = Math.Max(6, rect.Height / 2);

            Color fill = GdiHelper.LerpColor(FillNormal, FillHover, hoverT);
            Color border = GdiHelper.LerpColor(BorderNormal, BorderHover, hoverT);

            if (hoverT > 0.02f)
            {
                Rectangle glowRect = new Rectangle(1, 1 + yShift, this.ClientSize.Width - 3, this.ClientSize.Height - 3);
                using (GraphicsPath gpGlow = GdiHelper.RoundRect(glowRect, radius + 1))
                using (Pen penGlow = new Pen(Color.FromArgb((int)(hoverT * 55), BorderHover), 2.2f))
                {
                    e.Graphics.DrawPath(penGlow, gpGlow);
                }
            }

            using (GraphicsPath path = GdiHelper.RoundRect(rect, radius))
            {
                using (SolidBrush br = new SolidBrush(fill))
                {
                    e.Graphics.FillPath(br, path);
                }
                if (border != bgClear || hoverT > 0.05f)
                {
                    using (Pen pen = new Pen(border, 1.25f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            if (string.IsNullOrEmpty(this.Text) && !string.IsNullOrEmpty(IconKind))
            {
                int isz = Dpi.S(14);
                Rectangle ir = new Rectangle(
                    rect.X + (rect.Width - isz) / 2,
                    rect.Y + (rect.Height - isz) / 2,
                    isz,
                    isz
                );
                GdiHelper.DrawVectorEmoji(e.Graphics, IconKind, ir);
                return;
            }

            if (!string.IsNullOrEmpty(IconKind) || StatusDotColor != Color.Empty)
            {
                Size tSz = TextRenderer.MeasureText(this.Text, this.Font);
                int dotSz = (StatusDotColor != Color.Empty) ? Dpi.S(8) : 0;
                int dotGap = (StatusDotColor != Color.Empty) ? Dpi.S(6) : 0;
                int isz = !string.IsNullOrEmpty(IconKind) ? Dpi.S(15) : 0;
                int gap = !string.IsNullOrEmpty(IconKind) ? Dpi.S(5) : 0;
                int totalW = dotSz + dotGap + isz + gap + tSz.Width;
                int curX = rect.X + Math.Max(Dpi.S(8), (rect.Width - totalW) / 2);

                if (StatusDotColor != Color.Empty)
                {
                    int dy = rect.Y + (rect.Height - dotSz) / 2;
                    using (SolidBrush dBr = new SolidBrush(StatusDotColor))
                    {
                        e.Graphics.FillEllipse(dBr, curX, dy, dotSz, dotSz);
                    }
                    using (Pen dPen = new Pen(Color.FromArgb(140, 11, 12, 14), 1f))
                    {
                        e.Graphics.DrawEllipse(dPen, curX, dy, dotSz, dotSz);
                    }
                    curX += dotSz + dotGap;
                }

                if (!string.IsNullOrEmpty(IconKind))
                {
                    int iy = rect.Y + (rect.Height - isz) / 2;
                    GdiHelper.DrawVectorEmoji(e.Graphics, IconKind, new Rectangle(curX, iy, isz, isz));
                    curX += isz + gap;
                }

                Rectangle textR = new Rectangle(curX, rect.Y, Math.Max(10, rect.Right - curX - Dpi.S(4)), rect.Height);
                TextRenderer.DrawText(
                    e.Graphics,
                    this.Text,
                    this.Font,
                    textR,
                    TextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                 | TextFormatFlags.NoPrefix);
            }
            else
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    this.Text,
                    this.Font,
                    rect,
                    TextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                 | TextFormatFlags.NoPrefix);
            }
        }
    }

    public class GoldCheckBoxControl : Control
    {
        public bool Checked = false;
        private bool hovered = false;
        private float hoverT = 0f;
        private System.Windows.Forms.Timer animTimer;
        public event EventHandler CheckedChanged;

        protected override Size DefaultSize
        {
            get { return new Size(Dpi.S(240), Dpi.S(22)); }
        }

        public GoldCheckBoxControl(string text, bool initialChecked, Color bgColor, Color fgColor)
        {
            this.Text = text;
            this.Checked = initialChecked;
            this.BackColor = bgColor;
            this.ForeColor = fgColor;
            this.Font = new Font("Segoe UI", Dpi.F(8.8f), FontStyle.Regular);
            this.Cursor = Cursors.Hand;
            this.Margin = Padding.Empty;
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 16;
            animTimer.Tick += (s, e) =>
            {
                float target = hovered ? 1.0f : 0.0f;
                if (Math.Abs(hoverT - target) < 0.05f)
                {
                    hoverT = target;
                    animTimer.Stop();
                }
                else
                {
                    hoverT += (target - hoverT) * 0.30f;
                }
                this.Invalidate();
            };
        }

        public int MeasureNeededWidth()
        {
            Size sz = TextRenderer.MeasureText(this.Text, this.Font);
            return sz.Width + Dpi.S(32);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hovered = true;
            animTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovered = false;
            animTimer.Start();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            this.Checked = !this.Checked;
            this.Invalidate();
            if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush cb = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(cb, this.ClientRectangle);
            }

            int boxSize = Dpi.S(15);
            int by = (this.ClientSize.Height - boxSize) / 2;
            Rectangle boxRect = new Rectangle(1, by, boxSize, boxSize);

            using (GraphicsPath path = GdiHelper.RoundRect(boxRect, Dpi.S(4)))
            {
                if (Checked)
                {
                    Color fillC = GdiHelper.LerpColor(Color.FromArgb(245, 197, 24), Color.FromArgb(255, 218, 68), hoverT);
                    using (SolidBrush br = new SolidBrush(fillC))
                    {
                        e.Graphics.FillPath(br, path);
                    }
                    using (Pen checkPen = new Pen(Color.FromArgb(11, 12, 14), 2.0f * Dpi.Factor))
                    {
                        e.Graphics.DrawLines(checkPen, new Point[] {
                            new Point(1 + Dpi.S(3), by + Dpi.S(7)),
                            new Point(1 + Dpi.S(6), by + Dpi.S(11)),
                            new Point(1 + Dpi.S(12), by + Dpi.S(4))
                        });
                    }
                }
                else
                {
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(13, 15, 20)))
                    {
                        e.Graphics.FillPath(br, path);
                    }
                    Color bCol = GdiHelper.LerpColor(Color.FromArgb(52, 60, 76), Color.FromArgb(245, 197, 24), hoverT);
                    using (Pen pen = new Pen(bCol, 1.2f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            int textX = boxSize + Dpi.S(8);
            Rectangle textRect = new Rectangle(textX, 0, Math.Max(10, this.ClientSize.Width - textX), this.ClientSize.Height);
            Color fg = GdiHelper.LerpColor(this.ForeColor, Color.White, hoverT * 0.5f);
            TextRenderer.DrawText(
                e.Graphics,
                this.Text,
                this.Font,
                textRect,
                fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
             | TextFormatFlags.NoPrefix);
        }
    }

    public class RoundedInputBox : Panel
    {
        public TextBox InnerTextBox;
        private bool isFocused = false;
        private bool isHovered = false;

        protected override Size DefaultSize
        {
            get { return new Size(Dpi.S(200), Dpi.S(30)); }
        }

        public RoundedInputBox(string initialText, bool readOnly)
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
            this.BackColor = Color.FromArgb(19, 22, 29);
            this.Padding = Dpi.Pad(10, 5, 10, 4);

            InnerTextBox = new TextBox();
            InnerTextBox.Text = initialText;
            InnerTextBox.ReadOnly = readOnly;
            InnerTextBox.BorderStyle = BorderStyle.None;
            InnerTextBox.BackColor = Color.FromArgb(13, 15, 20);
            InnerTextBox.ForeColor = Color.FromArgb(243, 245, 248);
            InnerTextBox.Font = new Font("Segoe UI", Dpi.F(9.0f), FontStyle.Regular);
            InnerTextBox.Dock = DockStyle.Fill;
            InnerTextBox.GotFocus += (s, e) => { isFocused = true; this.Invalidate(); };
            InnerTextBox.LostFocus += (s, e) => { isFocused = false; this.Invalidate(); };
            InnerTextBox.MouseEnter += (s, e) => { isHovered = true; this.Invalidate(); };
            InnerTextBox.MouseLeave += (s, e) => { isHovered = false; this.Invalidate(); };
            this.MouseEnter += (s, e) => { isHovered = true; this.Invalidate(); };
            this.MouseLeave += (s, e) => { isHovered = false; this.Invalidate(); };
            this.Controls.Add(InnerTextBox);
        }

        public string Value
        {
            get { return InnerTextBox.Text; }
            set { InnerTextBox.Text = value ?? ""; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color clearCol = (this.Parent != null) ? this.Parent.BackColor : this.BackColor;
            using (SolidBrush cb = new SolidBrush(clearCol))
            {
                e.Graphics.FillRectangle(cb, this.ClientRectangle);
            }

            Rectangle rect = new Rectangle(1, 1, this.ClientSize.Width - 3, this.ClientSize.Height - 3);
            using (GraphicsPath path = GdiHelper.RoundRect(rect, Dpi.S(8)))
            {
                using (SolidBrush br = new SolidBrush(Color.FromArgb(13, 15, 20)))
                {
                    e.Graphics.FillPath(br, path);
                }
                Color bCol = isFocused
                    ? Color.FromArgb(245, 197, 24)
                    : (isHovered ? Color.FromArgb(72, 82, 104) : Color.FromArgb(38, 44, 56));
                using (Pen pen = new Pen(bCol, 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }

    public class SmoothGoldBar : Control
    {
        private int percent = 0;
        public int Percent
        {
            get { return percent; }
            set
            {
                int v = Math.Max(0, Math.Min(100, value));
                if (v != percent)
                {
                    percent = v;
                    this.Invalidate();
                }
            }
        }

        public SmoothGoldBar()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
            this.Height = Dpi.S(7);
            this.BackColor = Color.FromArgb(23, 26, 34);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle fullRect = new Rectangle(0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            int rad = Math.Max(2, fullRect.Height / 2);
            using (GraphicsPath bgPath = GdiHelper.RoundRect(fullRect, rad))
            using (SolidBrush bgBr = new SolidBrush(Color.FromArgb(34, 39, 50)))
            {
                e.Graphics.FillPath(bgBr, bgPath);
            }
            if (percent > 0 && fullRect.Width > 4)
            {
                int fw = Math.Max(fullRect.Height, (int)((fullRect.Width * percent) / 100.0));
                Rectangle fillRect = new Rectangle(0, 0, fw, fullRect.Height);
                using (GraphicsPath fPath = GdiHelper.RoundRect(fillRect, rad))
                using (SolidBrush fBr = new SolidBrush(Color.FromArgb(245, 197, 24)))
                {
                    e.Graphics.FillPath(fBr, fPath);
                }
            }
        }
    }

    public class QuestRowViewModel
    {
        public string Id = "";
        public string StatusLabel = "Доступно";
        public string StatusKind = "avail";
        public string GameName = "";
        public int CalcOrbs = 0;
        public string RewardDisplay = "";
        public string ProgressDisplay = "";
        public string Expires = "—";
        public int PillWidth = 84;
        public QuestItem SourceQuest;
    }

    public class DarkScrollBar : Control
    {
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private int minVal = 0;
        private int maxVal = 100;
        private int largeChange = 10;
        private int smallChange = 1;
        private int curVal = 0;
        private bool isHover = false;
        private bool isDragging = false;
        private int dragStartY = 0;
        private int dragStartVal = 0;

        public event ScrollEventHandler Scroll;
        public event EventHandler ValueChanged;

        public DarkScrollBar()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
            this.Width = Dpi.S(12);
            this.BackColor = Color.FromArgb(14, 16, 21);
            this.Cursor = Cursors.Hand;
        }

        public int Minimum
        {
            get { return minVal; }
            set { minVal = value; ClampValue(); this.Invalidate(); }
        }

        public int Maximum
        {
            get { return maxVal; }
            set { maxVal = Math.Max(minVal, value); ClampValue(); this.Invalidate(); }
        }

        public int LargeChange
        {
            get { return largeChange; }
            set { largeChange = Math.Max(1, value); ClampValue(); this.Invalidate(); }
        }

        public int SmallChange
        {
            get { return smallChange; }
            set { smallChange = Math.Max(1, value); }
        }

        public int Value
        {
            get { return curVal; }
            set
            {
                int next = Math.Max(minVal, Math.Min(GetMaxScrollPos(), value));
                if (curVal != next)
                {
                    curVal = next;
                    if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
                    this.Invalidate();
                }
            }
        }

        private int GetMaxScrollPos()
        {
            return Math.Max(minVal, maxVal - largeChange);
        }

        private void ClampValue()
        {
            int m = GetMaxScrollPos();
            if (curVal > m) curVal = m;
            if (curVal < minVal) curVal = minVal;
        }

        private Rectangle GetThumbRect()
        {
            int trackH = Math.Max(1, this.ClientSize.Height - Dpi.S(6));
            int range = Math.Max(1, maxVal - minVal);
            int thumbH = Math.Max(Dpi.S(28), (int)((largeChange / (double)range) * trackH));
            if (thumbH > trackH) thumbH = trackH;
            int maxPos = GetMaxScrollPos() - minVal;
            int availMove = Math.Max(0, trackH - thumbH);
            int thumbY = Dpi.S(3) + (maxPos > 0 ? (int)(((curVal - minVal) / (double)maxPos) * availMove) : 0);
            return new Rectangle(Dpi.S(2), thumbY, Math.Max(Dpi.S(4), this.ClientSize.Width - Dpi.S(4)), thumbH);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(14, 16, 21));

            using (Pen borderPen = new Pen(Color.FromArgb(28, 33, 44), 1f))
            {
                g.DrawLine(borderPen, 0, 0, 0, this.Height);
            }

            Rectangle tr = GetThumbRect();
            Color thumbCol = isDragging
                ? Color.FromArgb(245, 197, 24)
                : (isHover ? Color.FromArgb(74, 86, 110) : Color.FromArgb(44, 52, 68));

            using (GraphicsPath path = GdiHelper.RoundRect(tr, 4))
            using (SolidBrush br = new SolidBrush(thumbCol))
            {
                g.FillPath(br, path);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHover = true;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHover = false;
            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Rectangle tr = GetThumbRect();
            if (tr.Contains(e.Location))
            {
                isDragging = true;
                dragStartY = e.Y;
                dragStartVal = curVal;
                this.Invalidate();
            }
            else
            {
                int delta = (e.Y < tr.Top) ? -largeChange : largeChange;
                this.Value = curVal + delta;
                if (Scroll != null) Scroll(this, new ScrollEventArgs(ScrollEventType.ThumbPosition, this.Value));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!isDragging) return;
            int trackH = Math.Max(1, this.ClientSize.Height - Dpi.S(6));
            Rectangle tr = GetThumbRect();
            int availMove = Math.Max(1, trackH - tr.Height);
            int maxPos = Math.Max(1, GetMaxScrollPos() - minVal);
            int dy = e.Y - dragStartY;
            int dVal = (int)Math.Round((dy / (double)availMove) * maxPos);
            this.Value = dragStartVal + dVal;
            if (Scroll != null) Scroll(this, new ScrollEventArgs(ScrollEventType.ThumbTrack, this.Value));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (isDragging)
            {
                isDragging = false;
                this.Invalidate();
            }
        }

        public static void AttachToTextBox(TextBox tb, Control parentContainer)
        {
            tb.ScrollBars = ScrollBars.None;
            DarkScrollBar dsb = new DarkScrollBar();
            dsb.Dock = DockStyle.Right;
            dsb.SmallChange = 1;
            dsb.LargeChange = 10;
            bool syncing = false;

            Action updateBar = () =>
            {
                if (syncing || !tb.IsHandleCreated) return;
                syncing = true;
                try
                {
                    int totalLines = Math.Max(1, SendMessage(tb.Handle, 0x00BA, 0, 0));
                    int firstLine = Math.Max(0, SendMessage(tb.Handle, 0x00CE, 0, 0));
                    int visLines = Math.Max(1, tb.ClientSize.Height / Math.Max(12, tb.Font.Height));
                    if (totalLines <= visLines)
                    {
                        dsb.Visible = false;
                    }
                    else
                    {
                        dsb.Visible = true;
                        dsb.Minimum = 0;
                        dsb.Maximum = totalLines;
                        dsb.LargeChange = visLines;
                        dsb.Value = Math.Min(firstLine, Math.Max(0, totalLines - visLines));
                    }
                }
                catch { }
                syncing = false;
            };

            dsb.ValueChanged += (s, e) =>
            {
                if (syncing || !tb.IsHandleCreated) return;
                syncing = true;
                try
                {
                    int firstLine = Math.Max(0, SendMessage(tb.Handle, 0x00CE, 0, 0));
                    int diff = dsb.Value - firstLine;
                    if (diff != 0) SendMessage(tb.Handle, 0x00B6, 0, diff);
                }
                catch { }
                syncing = false;
            };

            tb.MouseWheel += (s, e) =>
            {
                if (!tb.IsHandleCreated) return;
                int lines = e.Delta > 0 ? -3 : 3;
                SendMessage(tb.Handle, 0x00B6, 0, lines);
                updateBar();
            };
            tb.TextChanged += (s, e) => updateBar();
            tb.Resize += (s, e) => updateBar();
            tb.KeyUp += (s, e) => updateBar();
            tb.MouseUp += (s, e) => updateBar();
            parentContainer.Controls.Add(dsb);
            dsb.BringToFront();
        }
    }

    public class QuestTableControl : Control
    {
        private List<QuestRowViewModel> rows = new List<QuestRowViewModel>();
        private DarkScrollBar vScroll;
        public string SelectedQuestId = "";
        private int hoveredIndex = -1;
        private int hoveredHeaderCol = -1;
        private int headerHeight;
        private int rowHeight;

        private Font fHeadCached;
        private Font fBadgeCached;
        private Font fBoldCached;
        private Font fRegCached;
        private Font fEmptyCached;
        private Pen headLinePen;
        private Pen rowLinePen;

        public string SortColumn = "reward";
        public bool SortDesc = true;

        public event Action<QuestItem> QuestSelected;
        public event Action<QuestItem> QuestDoubleClicked;
        public event Action<string> ColumnHeaderClicked;

        private readonly string[] colIds = new string[] { "status", "game", "reward", "progress", "expires" };
        private string[] GetColTitles() { return Loc.IsEn ? new string[] { "Status", "Game / Quest", "Reward", "Type / Progress", "Ends" } : new string[] { "Статус", "Игра / Задание", "Награда", "Тип / Прогресс", "До" }; }
        private readonly float[] colRatios = new float[] { 0.17f, 0.33f, 0.20f, 0.21f, 0.09f };

        public QuestTableControl()
        {
            headerHeight = Dpi.S(34);
            rowHeight = Dpi.S(40);
            fHeadCached = new Font("Segoe UI", Dpi.F(8.8f), FontStyle.Bold);
            fBadgeCached = new Font("Segoe UI", Dpi.F(8.0f), FontStyle.Bold);
            fBoldCached = new Font("Segoe UI", Dpi.F(9.0f), FontStyle.Bold);
            fRegCached = new Font("Segoe UI", Dpi.F(8.8f), FontStyle.Regular);
            fEmptyCached = new Font("Segoe UI", Dpi.F(9.2f), FontStyle.Regular);
            headLinePen = new Pen(Color.FromArgb(36, 41, 54), 1f);
            rowLinePen = new Pen(Color.FromArgb(27, 31, 40), 1f);

            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true
            );
            this.BackColor = Color.FromArgb(19, 22, 29);

            vScroll = new DarkScrollBar();
            vScroll.Dock = DockStyle.Right;
            vScroll.Width = Dpi.S(14);
            vScroll.Scroll += (s, e) => this.Invalidate();
            vScroll.ValueChanged += (s, e) => this.Invalidate();
            this.Controls.Add(vScroll);
        }

        public int MeasureBadgePillWidth(string statusText)
        {
            Size sz = TextRenderer.MeasureText(statusText ?? "", fBadgeCached);
            return Math.Max(Dpi.S(76), sz.Width + Dpi.S(18));
        }

        public void SetRows(List<QuestRowViewModel> newRows)
        {
            this.rows = newRows ?? new List<QuestRowViewModel>();
            UpdateScrollBar();
            this.Invalidate();
        }

        private void UpdateScrollBar()
        {
            int viewH = Math.Max(1, this.ClientSize.Height - headerHeight);
            int totalH = rows.Count * rowHeight;
            if (totalH <= viewH)
            {
                if (vScroll.Visible) vScroll.Visible = false;
                if (vScroll.Value != 0) vScroll.Value = 0;
            }
            else
            {
                if (!vScroll.Visible) vScroll.Visible = true;
                vScroll.Minimum = 0;
                vScroll.Maximum = totalH;
                vScroll.LargeChange = viewH;
                vScroll.SmallChange = rowHeight;
                int maxPos = Math.Max(0, totalH - viewH);
                if (vScroll.Value > maxPos)
                {
                    vScroll.Value = maxPos;
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!vScroll.Visible) return;
            int maxVal = Math.Max(0, vScroll.Maximum - vScroll.LargeChange);
            int delta = e.Delta > 0 ? -rowHeight : rowHeight;
            int next = Math.Max(0, Math.Min(maxVal, vScroll.Value + delta));
            if (vScroll.Value != next)
            {
                vScroll.Value = next;
                this.Invalidate();
            }
        }

        private void SetCursorIfNeeded(Cursor c)
        {
            if (this.Cursor != c) this.Cursor = c;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int availW = this.ClientSize.Width - (vScroll.Visible ? vScroll.Width : 0);
            if (e.Y < headerHeight)
            {
                SetCursorIfNeeded(Cursors.Hand);
                int newHovCol = -1;
                int x = Dpi.S(6);
                for (int i = 0; i < colIds.Length; i++)
                {
                    int cw = (int)((availW - Dpi.S(12)) * colRatios[i]);
                    if (e.X >= x && e.X <= x + cw)
                    {
                        newHovCol = i;
                        break;
                    }
                    x += cw;
                }
                if (hoveredIndex != -1 || hoveredHeaderCol != newHovCol)
                {
                    hoveredIndex = -1;
                    hoveredHeaderCol = newHovCol;
                    this.Invalidate();
                }
                return;
            }

            int idx = (e.Y - headerHeight + vScroll.Value) / rowHeight;
            if (idx >= 0 && idx < rows.Count)
            {
                SetCursorIfNeeded(Cursors.Hand);
                if (hoveredIndex != idx || hoveredHeaderCol != -1)
                {
                    hoveredIndex = idx;
                    hoveredHeaderCol = -1;
                    this.Invalidate();
                }
            }
            else
            {
                SetCursorIfNeeded(Cursors.Default);
                if (hoveredIndex != -1 || hoveredHeaderCol != -1)
                {
                    hoveredIndex = -1;
                    hoveredHeaderCol = -1;
                    this.Invalidate();
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoveredIndex != -1 || hoveredHeaderCol != -1)
            {
                hoveredIndex = -1;
                hoveredHeaderCol = -1;
                this.Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int availW = this.ClientSize.Width - (vScroll.Visible ? vScroll.Width : 0);
            if (e.Y <= headerHeight)
            {
                int x = Dpi.S(6);
                for (int i = 0; i < colIds.Length; i++)
                {
                    int cw = (int)((availW - Dpi.S(12)) * colRatios[i]);
                    if (e.X >= x && e.X <= x + cw)
                    {
                        if (ColumnHeaderClicked != null) ColumnHeaderClicked(colIds[i]);
                        return;
                    }
                    x += cw;
                }
                return;
            }

            int idx = (e.Y - headerHeight + vScroll.Value) / rowHeight;
            if (idx >= 0 && idx < rows.Count)
            {
                SelectedQuestId = rows[idx].Id;
                this.Invalidate();
                if (QuestSelected != null && rows[idx].SourceQuest != null)
                {
                    QuestSelected(rows[idx].SourceQuest);
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Y <= headerHeight) return;
            int idx = (e.Y - headerHeight + vScroll.Value) / rowHeight;
            if (idx >= 0 && idx < rows.Count)
            {
                SelectedQuestId = rows[idx].Id;
                this.Invalidate();
                if (QuestDoubleClicked != null && rows[idx].SourceQuest != null)
                {
                    QuestDoubleClicked(rows[idx].SourceQuest);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int availW = this.ClientSize.Width - (vScroll.Visible ? vScroll.Width : 0);
            using (SolidBrush bgBr = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(bgBr, 0, 0, availW, this.ClientSize.Height);
            }

            Region oldClip = e.Graphics.Clip;
            e.Graphics.SetClip(new Rectangle(0, headerHeight, availW, Math.Max(1, this.ClientSize.Height - headerHeight)));

            if (rows.Count == 0)
            {
                Rectangle emptyRect = new Rectangle(Dpi.S(16), headerHeight + Dpi.S(24), Math.Max(40, availW - Dpi.S(32)), Dpi.S(68));
                TextRenderer.DrawText(
                    e.Graphics,
                    Loc.T("Список заданий пуст. Нажмите «Подключить Discord →» в правом верхнем углу, чтобы загрузить квесты из API.", "Quest list is empty. Click «Connect Discord →» in the top-right corner to load quests from API."),
                    fEmptyCached,
                    emptyRect,
                    Color.FromArgb(142, 150, 164),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak
                 | TextFormatFlags.NoPrefix);
            }
            else
            {
                int c0 = (int)((availW - Dpi.S(12)) * colRatios[0]);
                int c1 = (int)((availW - Dpi.S(12)) * colRatios[1]);
                int c2 = (int)((availW - Dpi.S(12)) * colRatios[2]);
                int c3 = (int)((availW - Dpi.S(12)) * colRatios[3]);
                int c4 = (int)((availW - Dpi.S(12)) * colRatios[4]);
                int pillH = Dpi.S(22);

                for (int i = 0; i < rows.Count; i++)
                {
                    int y = headerHeight + i * rowHeight - vScroll.Value;
                    if (y + rowHeight < headerHeight || y > this.ClientSize.Height) continue;

                    QuestRowViewModel r = rows[i];
                    if (r.Id == SelectedQuestId)
                    {
                        using (SolidBrush selBr = new SolidBrush(Color.FromArgb(38, 33, 20)))
                        {
                            e.Graphics.FillRectangle(selBr, 0, y, availW, rowHeight);
                        }
                    }
                    else if (i == hoveredIndex)
                    {
                        using (SolidBrush hovBr = new SolidBrush(Color.FromArgb(25, 29, 38)))
                        {
                            e.Graphics.FillRectangle(hovBr, 0, y, availW, rowHeight);
                        }
                    }

                    e.Graphics.DrawLine(rowLinePen, 0, y + rowHeight - 1, availW, y + rowHeight - 1);

                    int cx = Dpi.S(6);
                    Color pBg, pFg, pBd;
                    if (r.StatusKind == "running")
                    {
                        pBg = Color.FromArgb(18, 45, 31);
                        pFg = Color.FromArgb(46, 204, 113);
                        pBd = Color.FromArgb(30, 77, 53);
                    }
                    else if (r.StatusKind == "done")
                    {
                        pBg = Color.FromArgb(41, 34, 18);
                        pFg = Color.FromArgb(245, 197, 24);
                        pBd = Color.FromArgb(71, 58, 26);
                    }
                    else if (r.StatusKind == "expired")
                    {
                        pBg = Color.FromArgb(23, 25, 32);
                        pFg = Color.FromArgb(104, 112, 126);
                        pBd = Color.FromArgb(35, 39, 50);
                    }
                    else
                    {
                        pBg = Color.FromArgb(27, 32, 43);
                        pFg = Color.FromArgb(176, 184, 198);
                        pBd = Color.FromArgb(43, 50, 66);
                    }

                    int pillW = Math.Min(c0 - Dpi.S(8), r.PillWidth);
                    Rectangle pillRect = new Rectangle(cx + Dpi.S(4), y + (rowHeight - pillH) / 2, pillW, pillH);
                    using (GraphicsPath pPath = GdiHelper.RoundRect(pillRect, pillH / 2))
                    {
                        using (SolidBrush pBr = new SolidBrush(pBg)) e.Graphics.FillPath(pBr, pPath);
                        using (Pen pPen = new Pen(pBd, 1f)) e.Graphics.DrawPath(pPen, pPath);
                    }
                    TextRenderer.DrawText(e.Graphics, r.StatusLabel, fBadgeCached, pillRect, pFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPrefix);
                    cx += c0;

                    Color gameFg = (r.StatusKind == "expired") ? Color.FromArgb(104, 112, 126) : Color.FromArgb(243, 245, 248);
                    TextRenderer.DrawText(
                        e.Graphics,
                        r.GameName,
                        fBoldCached,
                        new Rectangle(cx + Dpi.S(4), y, c1 - Dpi.S(8), rowHeight),
                        gameFg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping
                     | TextFormatFlags.NoPrefix);
                    cx += c1;

                    bool hasOrbs = r.CalcOrbs > 0;
                    Color rewFg = (r.StatusKind == "expired") ? Color.FromArgb(104, 112, 126) : (hasOrbs ? Color.FromArgb(245, 197, 24) : Color.FromArgb(210, 216, 226));
                    int rewOffset = Dpi.S(4);
                    if (hasOrbs)
                    {
                        int gemSz = Dpi.S(14);
                        GdiHelper.DrawVectorEmoji(e.Graphics, "gem", new Rectangle(cx + Dpi.S(4), y + (rowHeight - gemSz) / 2, gemSz, gemSz));
                        rewOffset = Dpi.S(22);
                    }
                    TextRenderer.DrawText(
                        e.Graphics,
                        r.RewardDisplay,
                        hasOrbs ? fBoldCached : fRegCached,
                        new Rectangle(cx + rewOffset, y, c2 - rewOffset - Dpi.S(4), rowHeight),
                        rewFg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping
                     | TextFormatFlags.NoPrefix);
                    cx += c2;

                    Color progFg = (r.StatusKind == "expired") ? Color.FromArgb(104, 112, 126) : Color.FromArgb(158, 166, 178);
                    TextRenderer.DrawText(
                        e.Graphics,
                        r.ProgressDisplay,
                        fRegCached,
                        new Rectangle(cx + Dpi.S(4), y, c3 - Dpi.S(8), rowHeight),
                        progFg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping
                     | TextFormatFlags.NoPrefix);
                    cx += c3;

                    TextRenderer.DrawText(
                        e.Graphics,
                        r.Expires,
                        fRegCached,
                        new Rectangle(cx + Dpi.S(2), y, c4 - Dpi.S(4), rowHeight),
                        progFg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                     | TextFormatFlags.NoPrefix);
                }
            }

            e.Graphics.Clip = oldClip;

            using (SolidBrush headBg = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(headBg, 0, 0, availW, headerHeight);
            }
            int hx = Dpi.S(6);
            for (int i = 0; i < colIds.Length; i++)
            {
                int cw = (int)((availW - Dpi.S(12)) * colRatios[i]);
                bool isSorted = (colIds[i] == SortColumn) || (SortColumn == "orbs" && colIds[i] == "reward");
                string[] cTitles = GetColTitles();
                string title = cTitles[i] + (isSorted ? (SortDesc ? " ▼" : " ▲") : "");
                Color hCol = isSorted
                    ? Color.FromArgb(245, 197, 24)
                    : (i == hoveredHeaderCol ? Color.FromArgb(220, 226, 236) : Color.FromArgb(142, 150, 164));
                Rectangle hRect = new Rectangle(hx + Dpi.S(4), 0, cw - Dpi.S(6), headerHeight);
                TextRenderer.DrawText(e.Graphics, title, fHeadCached, hRect, hCol, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                hx += cw;
            }
            e.Graphics.DrawLine(headLinePen, 0, headerHeight - 1, availW, headerHeight - 1);
        }
    }

    public class GamesCatalogTableControl : Control
    {
        private List<DetectableGameItem> items = new List<DetectableGameItem>();
        private DarkScrollBar vScroll;
        private int selectedIndex = -1;
        private int hoveredIndex = -1;
        private int headerHeight;
        private int rowHeight;

        private Font fHeadCached;
        private Font fBoldCached;
        private Font fRegCached;
        private Pen headLinePen;
        private Pen rowLinePen;

        public event Action<DetectableGameItem> GameSelected;

        private string[] GetColTitles() { return Loc.IsEn ? new string[] { "Game Title", "Executable (.exe)", "ID" } : new string[] { "Название игры", "Исполняемый файл (.exe)", "ID" }; }
        private readonly float[] colRatios = new float[] { 0.38f, 0.42f, 0.20f };

        public GamesCatalogTableControl()
        {
            headerHeight = Dpi.S(34);
            rowHeight = Dpi.S(34);
            fHeadCached = new Font("Segoe UI", Dpi.F(8.8f), FontStyle.Bold);
            fBoldCached = new Font("Segoe UI", Dpi.F(9.0f), FontStyle.Bold);
            fRegCached = new Font("Segoe UI", Dpi.F(8.8f), FontStyle.Regular);
            headLinePen = new Pen(Color.FromArgb(36, 41, 54), 1f);
            rowLinePen = new Pen(Color.FromArgb(27, 31, 40), 1f);

            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true
            );
            this.BackColor = Color.FromArgb(14, 16, 21);

            vScroll = new DarkScrollBar();
            vScroll.Dock = DockStyle.Right;
            vScroll.Width = Dpi.S(14);
            vScroll.Scroll += (s, e) => this.Invalidate();
            vScroll.ValueChanged += (s, e) => this.Invalidate();
            this.Controls.Add(vScroll);
        }

        public int ItemCount
        {
            get { return items.Count; }
        }

        public void SetItems(List<DetectableGameItem> newItems)
        {
            this.items = newItems ?? new List<DetectableGameItem>();
            this.selectedIndex = -1;
            this.hoveredIndex = -1;
            UpdateScrollBar();
            this.Invalidate();
        }

        private void UpdateScrollBar()
        {
            int viewH = Math.Max(1, this.ClientSize.Height - headerHeight);
            int totalH = items.Count * rowHeight;
            if (totalH <= viewH)
            {
                if (vScroll.Visible) vScroll.Visible = false;
                if (vScroll.Value != 0) vScroll.Value = 0;
            }
            else
            {
                if (!vScroll.Visible) vScroll.Visible = true;
                vScroll.Minimum = 0;
                vScroll.Maximum = totalH;
                vScroll.LargeChange = viewH;
                vScroll.SmallChange = rowHeight;
                int maxPos = Math.Max(0, totalH - viewH);
                if (vScroll.Value > maxPos) vScroll.Value = maxPos;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!vScroll.Visible) return;
            int maxVal = Math.Max(0, vScroll.Maximum - vScroll.LargeChange);
            int delta = e.Delta > 0 ? -rowHeight : rowHeight;
            int next = Math.Max(0, Math.Min(maxVal, vScroll.Value + delta));
            if (vScroll.Value != next)
            {
                vScroll.Value = next;
                this.Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Y < headerHeight)
            {
                if (this.Cursor != Cursors.Default) this.Cursor = Cursors.Default;
                if (hoveredIndex != -1) { hoveredIndex = -1; this.Invalidate(); }
                return;
            }
            int idx = (e.Y - headerHeight + vScroll.Value) / rowHeight;
            if (idx >= 0 && idx < items.Count)
            {
                if (this.Cursor != Cursors.Hand) this.Cursor = Cursors.Hand;
                if (hoveredIndex != idx) { hoveredIndex = idx; this.Invalidate(); }
            }
            else
            {
                if (this.Cursor != Cursors.Default) this.Cursor = Cursors.Default;
                if (hoveredIndex != -1) { hoveredIndex = -1; this.Invalidate(); }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoveredIndex != -1) { hoveredIndex = -1; this.Invalidate(); }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Y <= headerHeight) return;
            int idx = (e.Y - headerHeight + vScroll.Value) / rowHeight;
            if (idx >= 0 && idx < items.Count)
            {
                selectedIndex = idx;
                this.Invalidate();
                if (GameSelected != null) GameSelected(items[idx]);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int availW = this.ClientSize.Width - (vScroll.Visible ? vScroll.Width : 0);
            using (SolidBrush bgBody = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(bgBody, 0, headerHeight, availW, Math.Max(0, this.ClientSize.Height - headerHeight));
            }

            Region oldClip = e.Graphics.Clip;
            e.Graphics.SetClip(new Rectangle(0, headerHeight, availW, Math.Max(1, this.ClientSize.Height - headerHeight)));

            int c0 = (int)((availW - Dpi.S(12)) * colRatios[0]);
            int c1 = (int)((availW - Dpi.S(12)) * colRatios[1]);
            int c2 = (int)((availW - Dpi.S(12)) * colRatios[2]);

            for (int i = 0; i < items.Count; i++)
            {
                int y = headerHeight + i * rowHeight - vScroll.Value;
                if (y + rowHeight < headerHeight || y > this.ClientSize.Height) continue;

                if (i == selectedIndex)
                {
                    using (SolidBrush selBr = new SolidBrush(Color.FromArgb(38, 33, 20)))
                    {
                        e.Graphics.FillRectangle(selBr, 0, y, availW, rowHeight);
                    }
                }
                else if (i == hoveredIndex)
                {
                    using (SolidBrush hovBr = new SolidBrush(Color.FromArgb(24, 28, 36)))
                    {
                        e.Graphics.FillRectangle(hovBr, 0, y, availW, rowHeight);
                    }
                }

                e.Graphics.DrawLine(rowLinePen, 0, y + rowHeight - 1, availW, y + rowHeight - 1);

                DetectableGameItem g = items[i];
                string exeText = !string.IsNullOrEmpty(g.ExePath) ? g.ExePath : (g.Name + ".exe");
                Color unifiedFg = (i == selectedIndex) ? Color.FromArgb(245, 197, 24) : Color.FromArgb(228, 232, 240);
                Color exeFg = (i == selectedIndex) ? Color.FromArgb(245, 215, 100) : Color.FromArgb(196, 204, 218);
                Color idFg = Color.FromArgb(142, 150, 164);

                int cx = Dpi.S(6);
                TextRenderer.DrawText(e.Graphics, g.Name, fBoldCached, new Rectangle(cx + Dpi.S(4), y, c0 - Dpi.S(8), rowHeight), unifiedFg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                cx += c0;
                TextRenderer.DrawText(e.Graphics, exeText, fRegCached, new Rectangle(cx + Dpi.S(4), y, c1 - Dpi.S(8), rowHeight), exeFg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                cx += c1;
                TextRenderer.DrawText(e.Graphics, g.Id, fRegCached, new Rectangle(cx + Dpi.S(4), y, c2 - Dpi.S(8), rowHeight), idFg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPrefix);
            }

            e.Graphics.Clip = oldClip;

            using (SolidBrush bgHead = new SolidBrush(Color.FromArgb(19, 22, 29)))
            {
                e.Graphics.FillRectangle(bgHead, 0, 0, availW, headerHeight);
            }
            int hx = Dpi.S(6);
            string[] cTitles = GetColTitles();
            for (int i = 0; i < cTitles.Length; i++)
            {
                int cw = (int)((availW - Dpi.S(12)) * colRatios[i]);
                Rectangle hRect = new Rectangle(hx + Dpi.S(4), 0, cw - Dpi.S(6), headerHeight);
                TextRenderer.DrawText(e.Graphics, cTitles[i], fHeadCached, hRect, Color.FromArgb(142, 150, 164), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                hx += cw;
            }
            e.Graphics.DrawLine(headLinePen, 0, headerHeight - 1, availW, headerHeight - 1);
        }
    }

    internal static class LocalDiscordTokenFinder
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
        {
            public int cbSize;
            public int dwInfoVersion;
            public IntPtr pbNonce;
            public int cbNonce;
            public IntPtr pbAuthData;
            public int cbAuthData;
            public IntPtr pbTag;
            public int cbTag;
            public IntPtr pbMacContext;
            public int cbMacContext;
            public int cbAAD;
            public long cbData;
            public int dwFlags;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, IntPtr ppszDataDescr, IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int BCryptOpenAlgorithmProvider(out IntPtr phAlgorithm, string pszAlgId, string pszImplementation, int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int dwFlags);

        [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int BCryptSetProperty(IntPtr hObject, string pszProperty, byte[] pbInput, int cbInput, int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptGenerateSymmetricKey(IntPtr hAlgorithm, out IntPtr phKey, IntPtr pbKeyObject, int cbKeyObject, byte[] pbSecret, int cbSecret, int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDestroyKey(IntPtr hKey);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDecrypt(IntPtr hKey, byte[] pbInput, int cbInput, ref BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo, byte[] pbIV, int cbIV, byte[] pbOutput, int cbOutput, out int pcbResult, int dwFlags);

        private static byte[] UnprotectDpapi(byte[] data)
        {
            DATA_BLOB inBlob = new DATA_BLOB();
            DATA_BLOB outBlob = new DATA_BLOB();
            inBlob.pbData = Marshal.AllocHGlobal(data.Length);
            inBlob.cbData = data.Length;
            try
            {
                Marshal.Copy(data, 0, inBlob.pbData, data.Length);
                if (!CryptUnprotectData(ref inBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref outBlob))
                {
                    return null;
                }
                byte[] res = new byte[outBlob.cbData];
                Marshal.Copy(outBlob.pbData, res, 0, outBlob.cbData);
                return res;
            }
            finally
            {
                Marshal.FreeHGlobal(inBlob.pbData);
                if (outBlob.pbData != IntPtr.Zero) LocalFree(outBlob.pbData);
            }
        }

        private static string DecryptAesGcm(byte[] key, byte[] encryptedToken)
        {
            if (key == null || encryptedToken == null || encryptedToken.Length < 3 + 12 + 16) return null;
            byte[] nonce = new byte[12];
            Buffer.BlockCopy(encryptedToken, 3, nonce, 0, 12);
            int cipherLen = encryptedToken.Length - 15 - 16;
            if (cipherLen <= 0) return null;
            byte[] cipher = new byte[cipherLen];
            Buffer.BlockCopy(encryptedToken, 15, cipher, 0, cipherLen);
            byte[] tag = new byte[16];
            Buffer.BlockCopy(encryptedToken, encryptedToken.Length - 16, tag, 0, 16);

            IntPtr hAlg = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;
            GCHandle hNonce = GCHandle.Alloc(nonce, GCHandleType.Pinned);
            GCHandle hTag = GCHandle.Alloc(tag, GCHandleType.Pinned);
            try
            {
                if (BCryptOpenAlgorithmProvider(out hAlg, "AES", null, 0) != 0) return null;
                byte[] gcmMode = Encoding.Unicode.GetBytes("ChainingModeGCM\0");
                if (BCryptSetProperty(hAlg, "ChainingMode", gcmMode, gcmMode.Length, 0) != 0) return null;
                if (BCryptGenerateSymmetricKey(hAlg, out hKey, IntPtr.Zero, 0, key, key.Length, 0) != 0) return null;

                BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO info = new BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO();
                info.cbSize = Marshal.SizeOf(typeof(BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO));
                info.dwInfoVersion = 1;
                info.pbNonce = hNonce.AddrOfPinnedObject();
                info.cbNonce = nonce.Length;
                info.pbTag = hTag.AddrOfPinnedObject();
                info.cbTag = tag.Length;

                byte[] plain = new byte[cipherLen];
                int written = 0;
                int status = BCryptDecrypt(hKey, cipher, cipherLen, ref info, null, 0, plain, plain.Length, out written, 0);
                if (status != 0) return null;
                return Encoding.UTF8.GetString(plain, 0, written);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (hNonce.IsAllocated) hNonce.Free();
                if (hTag.IsAllocated) hTag.Free();
                if (hKey != IntPtr.Zero) BCryptDestroyKey(hKey);
                if (hAlg != IntPtr.Zero) BCryptCloseAlgorithmProvider(hAlg, 0);
            }
        }

        public class CandidateTokenInfo
        {
            public string Token;
            public string SourceClient;
        }

        private static string StripZeroBytesBetweenAscii(byte[] buf)
        {
            char[] chars = new char[buf.Length];
            int cLen = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                byte b = buf[i];
                if (b != 0)
                {
                    chars[cLen++] = (char)b;
                }
            }
            return new string(chars, 0, cLen);
        }

        public static List<CandidateTokenInfo> FindCandidateTokensWithSource()
        {
            List<CandidateTokenInfo> results = new List<CandidateTokenInfo>();
            HashSet<string> seen = new HashSet<string>();
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string[] clients = new string[] { "discord", "discordcanary", "discordptb" };

            foreach (string cl in clients)
            {
                string baseDir = Path.Combine(roaming, cl);
                string ldbDir = Path.Combine(Path.Combine(baseDir, "Local Storage"), "leveldb");
                if (!Directory.Exists(ldbDir)) continue;

                string clientLabel = cl == "discordcanary" ? "Discord Canary" : (cl == "discordptb" ? "Discord PTB" : "Discord");

                byte[] masterKey = null;
                string localState = Path.Combine(baseDir, "Local State");
                if (File.Exists(localState))
                {
                    try
                    {
                        string lsText = File.ReadAllText(localState, Encoding.UTF8);
                        Match mk = Regex.Match(lsText, "\"encrypted_key\"\\s*:\\s*\"([^\"]+)\"");
                        if (mk.Success)
                        {
                            byte[] rawKey = Convert.FromBase64String(mk.Groups[1].Value);
                            if (rawKey.Length > 5 && Encoding.ASCII.GetString(rawKey, 0, 5) == "DPAPI")
                            {
                                byte[] dpapiPayload = new byte[rawKey.Length - 5];
                                Buffer.BlockCopy(rawKey, 5, dpapiPayload, 0, dpapiPayload.Length);
                                masterKey = UnprotectDpapi(dpapiPayload);
                            }
                        }
                    }
                    catch { }
                }

                List<string> files = new List<string>();
                try
                {
                    files.AddRange(Directory.GetFiles(ldbDir, "*.ldb"));
                    files.AddRange(Directory.GetFiles(ldbDir, "*.log"));
                }
                catch { }

                foreach (string f in files)
                {
                    try
                    {
                        byte[] rawBytes;
                        using (FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            rawBytes = new byte[fs.Length];
                            fs.Read(rawBytes, 0, rawBytes.Length);
                        }

                        string[] views = new string[]
                        {
                            Encoding.GetEncoding(28591).GetString(rawBytes),
                            StripZeroBytesBetweenAscii(rawBytes)
                        };

                        for (int vIdx = 0; vIdx < views.Length; vIdx++)
                        {
                            string content = views[vIdx];
                            if (masterKey != null)
                            {
                                MatchCollection dqMatches = Regex.Matches(content, "dQw4w9WgXcQ:([^\"\\\\]+)");
                                foreach (Match m in dqMatches)
                                {
                                    try
                                    {
                                        string b64 = m.Groups[1].Value.Trim();
                                        Match cleanB64 = Regex.Match(b64, "^[A-Za-z0-9+/=]+");
                                        if (cleanB64.Success) b64 = cleanB64.Value;
                                        byte[] enc = Convert.FromBase64String(b64);
                                        string dec = DecryptAesGcm(masterKey, enc);
                                        if (!string.IsNullOrEmpty(dec) && seen.Add(dec))
                                        {
                                            results.Add(new CandidateTokenInfo { Token = dec, SourceClient = clientLabel });
                                        }
                                    }
                                    catch { }

                                    int winStart = Math.Max(0, m.Index - 200);
                                    int winLen = Math.Min(2200, content.Length - winStart);
                                    string neighborhood = content.Substring(winStart, winLen);
                                    foreach (Match sm in Regex.Matches(neighborhood, "([A-Za-z0-9+/=]{110,200})\""))
                                    {
                                        string b64Tail = sm.Groups[1].Value;
                                        string[] prefixes = new string[] { "", "djEw", "d", "jEw", "Ew", "w" };
                                        foreach (string pfx in prefixes)
                                        {
                                            string candidateB64 = pfx + b64Tail;
                                            int rem = candidateB64.Length % 4;
                                            if (rem != 0) candidateB64 = candidateB64.Substring(rem);
                                            try
                                            {
                                                byte[] enc = Convert.FromBase64String(candidateB64);
                                                if (enc.Length >= 31 && (enc[0] != 0x76 || enc[1] != 0x31 || enc[2] != 0x30))
                                                {
                                                    byte[] withV10 = new byte[enc.Length + 3];
                                                    withV10[0] = 0x76; withV10[1] = 0x31; withV10[2] = 0x30;
                                                    Buffer.BlockCopy(enc, 0, withV10, 3, enc.Length);
                                                    enc = withV10;
                                                }
                                                string dec = DecryptAesGcm(masterKey, enc);
                                                if (!string.IsNullOrEmpty(dec) && seen.Add(dec))
                                                {
                                                    results.Add(new CandidateTokenInfo { Token = dec, SourceClient = clientLabel });
                                                    break;
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }

                            if (vIdx == 0)
                            foreach (Match m in Regex.Matches(content, @"[A-Za-z0-9_\-]{24,28}\.[A-Za-z0-9_\-]{6}\.[A-Za-z0-9_\-]{25,50}|mfa\.[A-Za-z0-9_\-]{80,95}"))
                            {
                                string tok = m.Value;
                                if (seen.Add(tok))
                                {
                                    results.Add(new CandidateTokenInfo { Token = tok, SourceClient = clientLabel });
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            return results;
        }

        public static List<string> FindCandidateTokens()
        {
            List<string> results = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string[] clients = new string[] { "discord", "discordcanary", "discordptb" };

            foreach (string cl in clients)
            {
                string baseDir = Path.Combine(roaming, cl);
                string ldbDir = Path.Combine(Path.Combine(baseDir, "Local Storage"), "leveldb");
                if (!Directory.Exists(ldbDir)) continue;

                byte[] masterKey = null;
                string localState = Path.Combine(baseDir, "Local State");
                if (File.Exists(localState))
                {
                    try
                    {
                        string lsText = File.ReadAllText(localState, Encoding.UTF8);
                        Match mk = Regex.Match(lsText, "\"encrypted_key\"\\s*:\\s*\"([^\"]+)\"");
                        if (mk.Success)
                        {
                            byte[] rawKey = Convert.FromBase64String(mk.Groups[1].Value);
                            if (rawKey.Length > 5 && Encoding.ASCII.GetString(rawKey, 0, 5) == "DPAPI")
                            {
                                byte[] dpapiPayload = new byte[rawKey.Length - 5];
                                Buffer.BlockCopy(rawKey, 5, dpapiPayload, 0, dpapiPayload.Length);
                                masterKey = UnprotectDpapi(dpapiPayload);
                            }
                        }
                    }
                    catch { }
                }

                List<string> files = new List<string>();
                try
                {
                    files.AddRange(Directory.GetFiles(ldbDir, "*.ldb"));
                    files.AddRange(Directory.GetFiles(ldbDir, "*.log"));
                }
                catch { }

                foreach (string f in files)
                {
                    try
                    {
                        string content;
                        using (FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding(28591)))
                        {
                            content = sr.ReadToEnd();
                        }

                        if (masterKey != null)
                        {
                            foreach (Match m in Regex.Matches(content, "dQw4w9WgXcQ:([^\"\\\\]+)"))
                            {
                                try
                                {
                                    byte[] enc = Convert.FromBase64String(m.Groups[1].Value);
                                    string dec = DecryptAesGcm(masterKey, enc);
                                    if (!string.IsNullOrEmpty(dec) && seen.Add(dec))
                                    {
                                        results.Add(dec);
                                    }
                                }
                                catch { }
                            }
                        }

                        foreach (Match pm in Regex.Matches(content, @"[A-Za-z0-9_\-]{24,28}\.[A-Za-z0-9_\-]{6}\.[A-Za-z0-9_\-]{27,}"))
                        {
                            if (seen.Add(pm.Value)) results.Add(pm.Value);
                        }
                        foreach (Match mm in Regex.Matches(content, @"mfa\.[A-Za-z0-9_\-]{84}"))
                        {
                            if (seen.Add(mm.Value)) results.Add(mm.Value);
                        }
                    }
                    catch { }
                }
            }
            return results;
        }
    }

    public class MainStudioForm : Form
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x00020000 | 0x00010000; // WS_MINIMIZEBOX | WS_MAXIMIZEBOX
                cp.ClassStyle |= 0x0008; // CS_DBLCLKS
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int dark = 1;
                DwmSetWindowAttribute(this.Handle, 20, ref dark, sizeof(int));
                int roundPref = 2; // DWMWCP_ROUND on Windows 11
                DwmSetWindowAttribute(this.Handle, 33, ref roundPref, sizeof(int));
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x0084 && this.WindowState == FormWindowState.Normal) // WM_NCHITTEST
            {
                int lp = m.LParam.ToInt32();
                int sx = (short)(lp & 0xFFFF);
                int sy = (short)((lp >> 16) & 0xFFFF);
                Point pt = this.PointToClient(new Point(sx, sy));
                int grip = Dpi.S(6);
                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;

                if (pt.X <= grip && pt.Y <= grip) m.Result = (IntPtr)13; // HTTOPLEFT
                else if (pt.X >= w - grip && pt.Y <= grip) m.Result = (IntPtr)14; // HTTOPRIGHT
                else if (pt.X <= grip && pt.Y >= h - grip) m.Result = (IntPtr)16; // HTBOTTOMLEFT
                else if (pt.X >= w - grip && pt.Y >= h - grip) m.Result = (IntPtr)17; // HTBOTTOMRIGHT
                else if (pt.X <= grip) m.Result = (IntPtr)10; // HTLEFT
                else if (pt.X >= w - grip) m.Result = (IntPtr)11; // HTRIGHT
                else if (pt.Y <= grip) m.Result = (IntPtr)12; // HTTOP
                else if (pt.Y >= h - grip) m.Result = (IntPtr)15; // HTBOTTOM
            }
        }

        private static readonly Color BgRoot = Color.FromArgb(11, 12, 14);
        private static readonly Color BgNav = Color.FromArgb(15, 17, 22);
        private static readonly Color BgCard = Color.FromArgb(19, 22, 29);
        private static readonly Color BgCardInner = Color.FromArgb(24, 28, 36);
        private static readonly Color Gold = Color.FromArgb(245, 197, 24);
        private static readonly Color TextMain = Color.FromArgb(243, 245, 248);
        private static readonly Color TextSub = Color.FromArgb(158, 166, 178);
        private static readonly Color BorderCol = Color.FromArgb(36, 41, 54);

        private string userDataDir;
        private string stateFilePath;
        private string vaultFilePath;
        private string cacheFilePath;
        private string etagFilePath;
        private int serverOrbsBalance = -1;
        private string assetsDir;

        private List<QuestItem> activeQuests = new List<QuestItem>();
        private List<DetectableGameItem> detectableGames = new List<DetectableGameItem>();
        private List<ActiveSpoofItem> activeProcesses = new List<ActiveSpoofItem>();
        private HashSet<string> completedQuestIds = new HashSet<string>();
        private HashSet<string> sessionCompletedQuestIds = new HashSet<string>();
        private Dictionary<string, string> savedAccountTokens = new Dictionary<string, string>();

        private int manualOrbsOffset = 0;
        private bool nitroBonus = false;
        private bool hideExpired = false;
        private string sortColumn = "reward";
        private bool sortDescending = true;
        private string discordToken = "";
        private string discordUsername = "";
        private string proxyHost = "";
        private string proxyUser = "";
        private string proxyPass = "";
        private string tokenStatus = "none"; // "none", "checking", "online", "expired"
        private QuestItem selectedQuest = null;

        private bool queueActive = false;
        private List<QuestItem> questQueue = new List<QuestItem>();
        private int queueCooldownSec = 0;
        private QuestItem pendingQueueQuest = null;
        private PillActionButton btnQueueAll;
        private NotifyIcon trayIcon;

        private Icon appIcon;
        private Bitmap appLogoBmp;

        private PillActionButton langBtn;
        private PillActionButton tabQuestsBtn;
        private PillActionButton tabGamesBtn;
        private PillActionButton guideNavBtn;
        private PillActionButton licNavBtn;
        private PillActionButton killBtn;
        private PillActionButton bugBtn;
        private PillActionButton accountBtn;
        private Panel navInnerPanel;
        private FlowLayoutPanel navCenterTabs;
        private FlowLayoutPanel navRightActions;
        private bool navCompactMode = false;

        private Label lblHeroTitle;
        private Label lblHeroSub;
        private PillActionButton btnSyncHero;
        private PillActionButton btnGuideHero;
        private Label lblM1Sub;
        private Label lblM2Sub;
        private Label lblRewHead;
        private IconHeaderLabel lblQHead;
        private PillActionButton btnToggleDone;
        private PillActionButton btnClaimAll;
        private IconHeaderLabel lblGHead;
        private PillActionButton btnReloadDb;
        private IconHeaderLabel lblRightHead;
        private Label lblF1;
        private Label lblF2;
        private Label lblMin;
        private Label lblSteamDir;
        private PillActionButton btnBrowseSteam;
        private PillActionButton btnStartEmu;
        private PillActionButton btnStopEmu;
        private IconHeaderLabel lblGuideHead;
        private TextBox txtGuideInline;
        private IconHeaderLabel lblLicHead;
        private TextBox txtLicInline;
        private ToolStripMenuItem miTrayOpen;
        private ToolStripMenuItem miTrayStop;
        private ToolStripMenuItem miTrayExit;

        private Label lblEarnedOrbs;
        private PillActionButton btnEditOrbs;
        private Label lblAvailOrbs;
        private Label lblQuestsCounts;
        private Label lblDbStatus;
        private GoldCheckBoxControl chkNitro;

        private Label lblSpotBadge;
        private Label lblSpotTitle;
        private Label lblSpotSub;
        private Label lblSpotReward;

        private Panel questsViewPanel;
        private Panel gamesViewPanel;
        private Panel guideViewPanel;
        private Panel licenseViewPanel;
        private QuestTableControl questTable;
        private DarkSortDropdown sortCombo;
        private GoldCheckBoxControl chkHideExpired;

        private RoundedInputBox searchGameInput;
        private GamesCatalogTableControl gamesListView;

        private RoundedInputBox inpGameName;
        private RoundedInputBox inpExeRel;
        private Label lblSmartProgTitle;
        private Label lblSmartProgRem;
        private SmoothGoldBar smartProgBar;
        private RoundedInputBox inpMinutes;
        private RoundedInputBox inpSteamId;
        private RoundedInputBox inpSteamappsDir;
        private GoldCheckBoxControl chkRandomTail;
        private GoldCheckBoxControl chkCreateAcf;

        private Label lblActiveProcHeader;
        private Panel procListContainer;
        private Label lblEmptyProcs;

        private System.Windows.Forms.Timer uiTickTimer;
        private System.Windows.Forms.Timer autoSyncTimer;

        public MainStudioForm()
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;
            }
            catch { }

            InitStoragePaths();
            LoadBranding();
            LoadState();
            tokenStatus = string.IsNullOrEmpty(discordToken) ? "none" : "checking";
            BuildMainInterface();
            InitSystemTray();
            ApplyLocalization();
            RefreshAccountBadge();
            AdjustNavBarLayout();
            UpdateOrbsCounters();
            RefreshQuestTable();

            Thread dbThread = new Thread(() => LoadDetectableGamesDatabase(false));
            dbThread.IsBackground = true;
            dbThread.Start();

            if (!string.IsNullOrEmpty(discordToken))
            {
                Thread syncThread = new Thread(() => PollDiscordApi(false));
                syncThread.IsBackground = true;
                syncThread.Start();
            }

            uiTickTimer = new System.Windows.Forms.Timer();
            uiTickTimer.Interval = 1000;
            uiTickTimer.Tick += OnSecondTick;
            uiTickTimer.Start();

            autoSyncTimer = new System.Windows.Forms.Timer();
            autoSyncTimer.Interval = 15000;
            autoSyncTimer.Tick += (s, e) =>
            {
                if (!string.IsNullOrEmpty(discordToken))
                {
                    Thread t = new Thread(() => PollDiscordApi(false));
                    t.IsBackground = true;
                    t.Start();
                }
            };
            autoSyncTimer.Start();

            this.FormClosing += OnMainFormClosing;
        }

        private void InitStoragePaths()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            userDataDir = Path.Combine(home, ".discord_quest_studio");
            assetsDir = Path.Combine(userDataDir, "assets");
            Directory.CreateDirectory(userDataDir);
            Directory.CreateDirectory(assetsDir);

            stateFilePath = Path.Combine(userDataDir, "studio_state.json");
            vaultFilePath = Path.Combine(userDataDir, "vault.dat");
            cacheFilePath = Path.Combine(userDataDir, "detectable_games_cache.json");
            etagFilePath = Path.Combine(userDataDir, "detectable_games_etag.txt");
        }

        private void LoadBranding()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string icoPath = Path.Combine(baseDir, "icon.ico");
            if (!File.Exists(icoPath)) icoPath = Path.Combine(baseDir, "app", "icon.ico");
            if (File.Exists(icoPath))
            {
                try { appIcon = new Icon(icoPath); } catch { }
            }

            string pngPath = Path.Combine(baseDir, "icon.png");
            if (!File.Exists(pngPath)) pngPath = Path.Combine(baseDir, "app", "icon.png");
            if (File.Exists(pngPath))
            {
                try { appLogoBmp = new Bitmap(pngPath); } catch { }
            }
        }

        private static string DetectDefaultSteamapps()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("SteamPath");
                        if (v != null)
                        {
                            return Path.GetFullPath(Path.Combine(v.ToString(), "steamapps"));
                        }
                    }
                }
            }
            catch { }
            return @"c:\program files (x86)\steam\steamapps";
        }

        private byte[] DeriveKeyStream(string pin, byte[] salt, int length)
        {
            string fp = (Environment.MachineName ?? "PC") + "|" + (Environment.UserName ?? "USER") + "|DQS_V6";
            byte[] secret = Encoding.UTF8.GetBytes(fp + "::" + (pin ?? ""));
            byte[] dk;
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(secret, salt, 120000))
            {
                dk = pbkdf2.GetBytes(64);
            }
            List<byte> result = new List<byte>(length + 32);
            int counter = 0;
            while (result.Count < length)
            {
                byte[] cntBytes = new byte[4];
                cntBytes[0] = (byte)((counter >> 24) & 0xFF);
                cntBytes[1] = (byte)((counter >> 16) & 0xFF);
                cntBytes[2] = (byte)((counter >> 8) & 0xFF);
                cntBytes[3] = (byte)(counter & 0xFF);

                byte[] msg = new byte[salt.Length + 4];
                Buffer.BlockCopy(salt, 0, msg, 0, salt.Length);
                Buffer.BlockCopy(cntBytes, 0, msg, salt.Length, 4);

                using (HMACSHA256 hmac = new HMACSHA256(dk))
                {
                    result.AddRange(hmac.ComputeHash(msg));
                }
                counter++;
            }
            byte[] outBytes = new byte[length];
            for (int i = 0; i < length; i++) outBytes[i] = result[i];
            return outBytes;
        }

        private string EncryptToken(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            byte[] raw = Encoding.UTF8.GetBytes(plain.Trim());
            byte[] salt = new byte[16];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            byte[] stream = DeriveKeyStream("", salt, raw.Length);
            byte[] cipher = new byte[raw.Length];
            for (int i = 0; i < raw.Length; i++) cipher[i] = (byte)(raw[i] ^ stream[i]);

            byte[] macKey = new byte[Math.Min(32, stream.Length)];
            if (stream.Length >= 32) Buffer.BlockCopy(stream, 0, macKey, 0, 32);
            else macKey = salt;

            byte[] saltAndCipher = new byte[salt.Length + cipher.Length];
            Buffer.BlockCopy(salt, 0, saltAndCipher, 0, salt.Length);
            Buffer.BlockCopy(cipher, 0, saltAndCipher, salt.Length, cipher.Length);

            byte[] mac;
            using (HMACSHA256 h = new HMACSHA256(macKey))
            {
                byte[] fullMac = h.ComputeHash(saltAndCipher);
                mac = new byte[16];
                Buffer.BlockCopy(fullMac, 0, mac, 0, 16);
            }

            byte[] finalBlob = new byte[16 + 16 + cipher.Length];
            Buffer.BlockCopy(salt, 0, finalBlob, 0, 16);
            Buffer.BlockCopy(mac, 0, finalBlob, 16, 16);
            Buffer.BlockCopy(cipher, 0, finalBlob, 32, cipher.Length);

            string b64 = Convert.ToBase64String(finalBlob).Replace('+', '-').Replace('/', '_');
            return "ENCv5:" + b64;
        }

        private string DecryptToken(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return "";
            try
            {
                if (stored.StartsWith("ENCv5:") || stored.StartsWith("ENCv3:"))
                {
                    string b64 = stored.Substring(6).Replace('-', '+').Replace('_', '/');
                    int mod4 = b64.Length % 4;
                    if (mod4 > 0) b64 += new string('=', 4 - mod4);
                    byte[] blob = Convert.FromBase64String(b64);
                    if (blob.Length < 32) return "";
                    byte[] salt = new byte[16];
                    byte[] cipher = new byte[blob.Length - 32];
                    Buffer.BlockCopy(blob, 0, salt, 0, 16);
                    Buffer.BlockCopy(blob, 32, cipher, 0, cipher.Length);

                    byte[] stream = DeriveKeyStream("", salt, cipher.Length);
                    byte[] plain = new byte[cipher.Length];
                    for (int i = 0; i < cipher.Length; i++) plain[i] = (byte)(cipher[i] ^ stream[i]);
                    return Encoding.UTF8.GetString(plain);
                }
            }
            catch { }
            return "";
        }

        private void LoadState()
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = int.MaxValue;

            if (File.Exists(stateFilePath))
            {
                try
                {
                    Dictionary<string, object> dict = ser.DeserializeObject(File.ReadAllText(stateFilePath, Encoding.UTF8)) as Dictionary<string, object>;
                    if (dict != null)
                    {
                        if (dict.ContainsKey("manual_orbs_offset")) manualOrbsOffset = Convert.ToInt32(dict["manual_orbs_offset"]);
                        else if (dict.ContainsKey("earned_orbs_manual_offset")) manualOrbsOffset = Convert.ToInt32(dict["earned_orbs_manual_offset"]);

                        if (dict.ContainsKey("nitro_bonus")) nitroBonus = Convert.ToBoolean(dict["nitro_bonus"]);
                        else if (dict.ContainsKey("nitro_multiplier")) nitroBonus = Convert.ToBoolean(dict["nitro_multiplier"]);

                        if (dict.ContainsKey("hide_expired")) hideExpired = Convert.ToBoolean(dict["hide_expired"]);
                        if (dict.ContainsKey("sort_column") && dict["sort_column"] != null) sortColumn = dict["sort_column"].ToString();
                        if (dict.ContainsKey("sort_descending")) sortDescending = Convert.ToBoolean(dict["sort_descending"]);
                        if (dict.ContainsKey("discord_username") && dict["discord_username"] != null) discordUsername = dict["discord_username"].ToString();
                        if (dict.ContainsKey("language") && dict["language"] != null) Loc.Lang = (dict["language"].ToString() == "en") ? "en" : "ru";
                        if (dict.ContainsKey("proxy_host") && dict["proxy_host"] != null) proxyHost = dict["proxy_host"].ToString();
                        if (dict.ContainsKey("proxy_user") && dict["proxy_user"] != null) proxyUser = dict["proxy_user"].ToString();

                        if (dict.ContainsKey("completed_quest_ids") && dict["completed_quest_ids"] is IEnumerable)
                        {
                            foreach (object o in (IEnumerable)dict["completed_quest_ids"])
                            {
                                if (o != null) completedQuestIds.Add(o.ToString());
                            }
                        }

                        if (dict.ContainsKey("cached_quests") && dict["cached_quests"] is IEnumerable)
                        {
                            activeQuests.Clear();
                            foreach (object qo in (IEnumerable)dict["cached_quests"])
                            {
                                Dictionary<string, object> qd = qo as Dictionary<string, object>;
                                if (qd != null)
                                {
                                    QuestItem qi = new QuestItem();
                                    if (qd.ContainsKey("id") && qd["id"] != null) qi.Id = qd["id"].ToString();
                                    if (qd.ContainsKey("app_id") && qd["app_id"] != null) qi.AppId = qd["app_id"].ToString();
                                    if (qd.ContainsKey("quest_name") && qd["quest_name"] != null) qi.QuestName = qd["quest_name"].ToString();
                                    if (qd.ContainsKey("game_name") && qd["game_name"] != null) qi.GameName = qd["game_name"].ToString();
                                    if (qd.ContainsKey("expires") && qd["expires"] != null) qi.Expires = qd["expires"].ToString();
                                    if (qd.ContainsKey("expires_ts") && qd["expires_ts"] != null) qi.ExpiresTs = Convert.ToDouble(qd["expires_ts"]);
                                    if (qd.ContainsKey("task_type") && qd["task_type"] != null) qi.TaskType = qd["task_type"].ToString();
                                    if (qd.ContainsKey("task_label") && qd["task_label"] != null) qi.TaskLabel = qd["task_label"].ToString();
                                    if (qd.ContainsKey("orbs") && qd["orbs"] != null) qi.BaseOrbs = Convert.ToInt32(qd["orbs"]);
                                    if (qd.ContainsKey("reward_text") && qd["reward_text"] != null) qi.RewardText = qd["reward_text"].ToString();
                                    if (qd.ContainsKey("exe_rel_path") && qd["exe_rel_path"] != null) qi.ExeRelPath = qd["exe_rel_path"].ToString();
                                    if (qd.ContainsKey("steam_id") && qd["steam_id"] != null) qi.SteamId = qd["steam_id"].ToString();
                                    if (qd.ContainsKey("duration_min") && qd["duration_min"] != null) qi.DurationMin = Convert.ToInt32(qd["duration_min"]);
                                    if (qd.ContainsKey("remaining_min") && qd["remaining_min"] != null) qi.RemainingMin = Convert.ToInt32(qd["remaining_min"]);
                                    if (qd.ContainsKey("target_sec") && qd["target_sec"] != null) qi.TargetSec = Convert.ToInt32(qd["target_sec"]);
                                    if (qd.ContainsKey("done_sec") && qd["done_sec"] != null) qi.DoneSec = Convert.ToInt32(qd["done_sec"]);
                                    if (qd.ContainsKey("server_progress_pct") && qd["server_progress_pct"] != null) qi.ServerProgressPct = Convert.ToInt32(qd["server_progress_pct"]);
                                    if (qd.ContainsKey("is_enrolled") && qd["is_enrolled"] != null) qi.IsEnrolled = Convert.ToBoolean(qd["is_enrolled"]);
                                    activeQuests.Add(qi);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            if (File.Exists(vaultFilePath))
            {
                try
                {
                    string vText = File.ReadAllText(vaultFilePath, Encoding.UTF8).Trim();
                    if (vText.StartsWith("{"))
                    {
                        Dictionary<string, object> vd = ser.DeserializeObject(vText) as Dictionary<string, object>;
                        if (vd != null && vd.ContainsKey("token_enc") && vd["token_enc"] != null)
                        {
                            discordToken = DecryptToken(vd["token_enc"].ToString());
                        }
                        if (vd != null && vd.ContainsKey("saved_accounts_enc") && vd["saved_accounts_enc"] != null)
                        {
                            string saJson = DecryptToken(vd["saved_accounts_enc"].ToString());
                            if (!string.IsNullOrEmpty(saJson))
                            {
                                Dictionary<string, object> saDict = ser.DeserializeObject(saJson) as Dictionary<string, object>;
                                if (saDict != null)
                                {
                                    foreach (KeyValuePair<string, object> kv in saDict)
                                    {
                                        if (kv.Value != null) savedAccountTokens[kv.Key] = kv.Value.ToString();
                                    }
                                }
                            }
                        }
                        if (vd != null && vd.ContainsKey("proxy_pass_enc") && vd["proxy_pass_enc"] != null)
                        {
                            proxyPass = DecryptToken(vd["proxy_pass_enc"].ToString());
                        }
                        if (vd != null && vd.ContainsKey("proxy_pass_enc") && vd["proxy_pass_enc"] != null)
                        {
                            proxyPass = DecryptToken(vd["proxy_pass_enc"].ToString());
                        }
                    }
                    else if (vText.StartsWith("ENCv"))
                    {
                        discordToken = DecryptToken(vText);
                    }
                }
                catch { }
            }
        }

        private readonly object stateWriteLock = new object();

        private void SaveTokenVault()
        {
            string tokSnapshot = discordToken;
            string vPath = vaultFilePath;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    lock (stateWriteLock)
                    {
                        if (!string.IsNullOrEmpty(tokSnapshot))
                        {
                            savedAccountTokens[tokSnapshot] = "Saved";
                        }
                        if (!string.IsNullOrEmpty(tokSnapshot) || !string.IsNullOrEmpty(proxyPass) || savedAccountTokens.Count > 0)
                        {
                            JavaScriptSerializer ser = new JavaScriptSerializer();
                            Dictionary<string, object> vd = new Dictionary<string, object>();
                            if (!string.IsNullOrEmpty(tokSnapshot)) vd["token_enc"] = EncryptToken(tokSnapshot);
                            if (!string.IsNullOrEmpty(proxyPass)) vd["proxy_pass_enc"] = EncryptToken(proxyPass);
                            if (savedAccountTokens.Count > 0) vd["saved_accounts_enc"] = EncryptToken(ser.Serialize(savedAccountTokens));
                            File.WriteAllText(vPath, ser.Serialize(vd), Encoding.UTF8);
                        }
                        else
                        {
                            if (File.Exists(vPath)) File.Delete(vPath);
                        }
                    }
                }
                catch { }
            });
        }

        private void SaveState(bool sync = false)
        {
            try
            {
                List<Dictionary<string, object>> qList = new List<Dictionary<string, object>>(activeQuests.Count);
                foreach (QuestItem q in activeQuests)
                {
                    Dictionary<string, object> d = new Dictionary<string, object>();
                    d["id"] = q.Id;
                    d["app_id"] = q.AppId;
                    d["quest_name"] = q.QuestName;
                    d["game_name"] = q.GameName;
                    d["expires"] = q.Expires;
                    d["expires_ts"] = q.ExpiresTs;
                    d["task_type"] = q.TaskType;
                    d["task_label"] = q.TaskLabel;
                    d["orbs"] = q.BaseOrbs;
                    d["reward_text"] = q.RewardText;
                    d["exe_rel_path"] = q.ExeRelPath;
                    d["steam_id"] = q.SteamId;
                    d["duration_min"] = q.DurationMin;
                    d["remaining_min"] = q.RemainingMin;
                    d["target_sec"] = q.TargetSec;
                    d["done_sec"] = q.DoneSec;
                    d["server_progress_pct"] = q.ServerProgressPct;
                    d["is_enrolled"] = q.IsEnrolled;
                    qList.Add(d);
                }

                Dictionary<string, object> st = new Dictionary<string, object>();
                st["manual_orbs_offset"] = manualOrbsOffset;
                st["nitro_bonus"] = nitroBonus;
                st["hide_expired"] = hideExpired;
                st["sort_column"] = sortColumn;
                st["sort_descending"] = sortDescending;
                st["discord_username"] = discordUsername;
                st["language"] = Loc.Lang;
                st["proxy_host"] = proxyHost;
                st["proxy_user"] = proxyUser;
                st["steamapps_dir"] = (inpSteamappsDir != null) ? inpSteamappsDir.Value : DetectDefaultSteamapps();
                st["completed_quest_ids"] = new List<string>(completedQuestIds);
                st["cached_quests"] = qList;

                string sPath = stateFilePath;
                Action writeAction = () =>
                {
                    try
                    {
                        JavaScriptSerializer ser = new JavaScriptSerializer();
                        ser.MaxJsonLength = int.MaxValue;
                        string json = ser.Serialize(st);
                        lock (stateWriteLock)
                        {
                            File.WriteAllText(sPath, json, Encoding.UTF8);
                        }
                    }
                    catch { }
                };
                if (sync) writeAction();
                else ThreadPool.QueueUserWorkItem(_ => writeAction());
            }
            catch { }
        }

        private void BuildMainInterface()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Text = "Discord Quest Studio";
            if (appIcon != null) this.Icon = appIcon;
            this.BackColor = BgRoot;
            this.ForeColor = TextMain;
            this.Font = new Font("Segoe UI", Dpi.F(9.2f), FontStyle.Regular);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Padding = new Padding(1);

            Rectangle screenArea = Screen.PrimaryScreen.WorkingArea;
            this.MaximizedBounds = screenArea;
            this.LocationChanged += (s, e) => { try { this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea; } catch { } };
            int initW = Math.Min(Dpi.S(1320), (int)(screenArea.Width * 0.86f));
            int initH = Math.Min(Dpi.S(854), (int)(screenArea.Height * 0.88f));
            this.ClientSize = new Size(Math.Max(Dpi.S(1120), initW), Math.Max(Dpi.S(750), initH));
            this.MinimumSize = new Size(Dpi.S(1060), Dpi.S(690));
            this.DoubleBuffered = true;
            this.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(44, 50, 64), 1f))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
                }
            };

            DarkGoldTitleBar customTitleBar = new DarkGoldTitleBar(this, appLogoBmp, "Discord Quest Studio", true, true);
            customTitleBar.TrayRequested += () => MinimizeToTray();

            Panel navBar = new Panel();
            navBar.Dock = DockStyle.Top;
            navBar.Height = Dpi.S(60);
            navBar.BackColor = BgNav;
            navBar.Padding = new Padding(0);
            navBar.Paint += (s, e) =>
            {
                using (Pen p = new Pen(BorderCol, 1f))
                {
                    e.Graphics.DrawLine(p, 0, navBar.Height - 1, navBar.Width, navBar.Height - 1);
                }
            };

            Panel langWrap = new Panel();
            langWrap.Dock = DockStyle.Left;
            langWrap.Width = Dpi.S(46);
            langWrap.BackColor = BgNav;
            langWrap.Padding = Dpi.Pad(0, 6, 6, 6);

            langBtn = PillActionButton.CreateDark(Loc.IsEn ? "EN" : "RU", BgNav);
            langBtn.TextColor = Gold;
            langBtn.BorderNormal = Color.FromArgb(62, 54, 24);
            langBtn.Dock = DockStyle.Fill;
            langBtn.Font = new Font("Segoe UI", Dpi.F(8.2f), FontStyle.Bold);
            langBtn.Click += (s, e) =>
            {
                Loc.Lang = Loc.IsEn ? "ru" : "en";
                SaveState();
                ApplyLocalization();
            };
            langWrap.Controls.Add(langBtn);

            HighDpiLogoControl brandLogo = new HighDpiLogoControl(appLogoBmp);
            brandLogo.Dock = DockStyle.Left;
            brandLogo.Width = Dpi.S(148);
            brandLogo.Click += (s, e) => SwitchView("quests");

            navRightActions = new FlowLayoutPanel();
            navRightActions.Dock = DockStyle.Right;
            navRightActions.AutoSize = true;
            navRightActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            navRightActions.FlowDirection = FlowDirection.LeftToRight;
            navRightActions.WrapContents = false;
            navRightActions.Padding = Dpi.Pad(0, 2, 0, 0);

            bugBtn = new PillActionButton("Баг-репорт · DS: hellwalk3rr");
            bugBtn.IconKind = "bug";
            bugBtn.BackColor = BgNav;
            bugBtn.FillNormal = Color.FromArgb(30, 27, 16);
            bugBtn.FillHover = Color.FromArgb(46, 40, 20);
            bugBtn.BorderNormal = Color.FromArgb(92, 74, 24);
            bugBtn.BorderHover = Gold;
            bugBtn.TextColor = Gold;
            bugBtn.AutoSizeToText(28, 34);
            bugBtn.Margin = Dpi.Pad(0, 0, 8, 0);
            bugBtn.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText("hellwalk3rr");
                    bugBtn.Text = Loc.T("Скопировано: hellwalk3rr", "Copied: hellwalk3rr");
                    bugBtn.AutoSizeToText(28, 34);
                    bugBtn.Invalidate();
                    System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
                    t.Interval = 2200;
                    t.Tick += (ts, te) =>
                    {
                        t.Stop();
                        t.Dispose();
                        bugBtn.Text = navCompactMode ? "DS: hellwalk3rr" : Loc.T("Баг-репорт · DS: hellwalk3rr", "Bug Report · DS: hellwalk3rr");
                        bugBtn.AutoSizeToText(28, 34);
                        bugBtn.Invalidate();
                    };
                    t.Start();
                }
                catch { }
            };

            killBtn = PillActionButton.CreateDanger("АВАРИЙНЫЙ СТОП", BgNav, "siren");
            killBtn.AutoSizeToText(28, 34);
            killBtn.Margin = Dpi.Pad(0, 0, 8, 0);
            killBtn.Click += (s, e) => EmergencyStopAll();

            accountBtn = PillActionButton.CreateGold(Loc.T("Подключить Discord  →", "Connect Discord  →"), BgNav, "bolt");
            accountBtn.AutoSizeToText(30, 34);
            accountBtn.Margin = new Padding(0);
            accountBtn.Click += (s, e) => OpenAccountDialog();

            navRightActions.Controls.Add(bugBtn);
            navRightActions.Controls.Add(killBtn);
            navRightActions.Controls.Add(accountBtn);

            navCenterTabs = new FlowLayoutPanel();
            navCenterTabs.Dock = DockStyle.Fill;
            navCenterTabs.FlowDirection = FlowDirection.LeftToRight;
            navCenterTabs.WrapContents = false;
            navCenterTabs.Padding = Dpi.Pad(14, 2, 0, 0);

            tabQuestsBtn = PillActionButton.CreateNavTab("Задания Discord", true, BgNav, "fire");
            tabQuestsBtn.Margin = Dpi.Pad(0, 0, 6, 0);
            tabQuestsBtn.Click += (s, e) => SwitchView("quests");

            tabGamesBtn = PillActionButton.CreateNavTab("Каталог игр API", false, BgNav, "gamepad");
            tabGamesBtn.Margin = Dpi.Pad(0, 0, 6, 0);
            tabGamesBtn.Click += (s, e) => SwitchView("games");

            guideNavBtn = PillActionButton.CreateNavTab("Гайд", false, BgNav, "book");
            guideNavBtn.Margin = Dpi.Pad(0, 0, 6, 0);
            guideNavBtn.Click += (s, e) => SwitchView("guide");

            licNavBtn = PillActionButton.CreateNavTab("Лицензия", false, BgNav, "scales");
            licNavBtn.Margin = new Padding(0);
            licNavBtn.Click += (s, e) => SwitchView("license");

            navCenterTabs.Controls.Add(tabQuestsBtn);
            navCenterTabs.Controls.Add(tabGamesBtn);
            navCenterTabs.Controls.Add(guideNavBtn);
            navCenterTabs.Controls.Add(licNavBtn);

            navInnerPanel = new Panel();
            navInnerPanel.BackColor = BgNav;
            navInnerPanel.Padding = Dpi.Pad(18, 10, 18, 10);
            navInnerPanel.Controls.Add(navCenterTabs);
            navInnerPanel.Controls.Add(navRightActions);
            navInnerPanel.Controls.Add(brandLogo);
            navInnerPanel.Controls.Add(langWrap);
            navBar.Controls.Add(navInnerPanel);

            Panel viewportHost = new Panel();
            viewportHost.Dock = DockStyle.Fill;
            viewportHost.BackColor = BgRoot;
            viewportHost.AutoScroll = false;

            TableLayoutPanel rootBody = new TableLayoutPanel();
            rootBody.BackColor = BgRoot;
            rootBody.Padding = Dpi.Pad(18, 10, 18, 12);
            rootBody.ColumnCount = 1;
            rootBody.RowCount = 2;
            rootBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootBody.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(230)));
            rootBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            RoundedPanelCard heroCard = new RoundedPanelCard();
            heroCard.Dock = DockStyle.Fill;
            heroCard.Margin = Dpi.Pad(0, 0, 0, 10);
            heroCard.Padding = Dpi.Pad(20, 12, 20, 10);

            TableLayoutPanel heroGrid = new TableLayoutPanel();
            heroGrid.Dock = DockStyle.Fill;
            heroGrid.Margin = Padding.Empty;
            heroGrid.BackColor = BgCard;
            heroGrid.ColumnCount = 2;
            heroGrid.RowCount = 1;
            heroGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            heroGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi.S(320)));
            heroGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            TableLayoutPanel heroLeft = new TableLayoutPanel();
            heroLeft.Dock = DockStyle.Fill;
            heroLeft.BackColor = BgCard;
            heroLeft.Margin = Dpi.Pad(0, 0, 16, 0);
            heroLeft.ColumnCount = 1;
            heroLeft.RowCount = 5;
            heroLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            heroLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(20)));
            heroLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(54)));
            heroLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(36)));
            heroLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(42)));
            heroLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label lblKicker = new Label();
            lblKicker.Text = "— DISCORD QUEST STUDIO · AUTHOR: HELLWALK3RR";
            lblKicker.ForeColor = Gold;
            lblKicker.Font = new Font("Segoe UI", Dpi.F(8.0f), FontStyle.Bold);
            lblKicker.Margin = Padding.Empty;
            lblKicker.Dock = DockStyle.Fill;
            lblKicker.TextAlign = ContentAlignment.MiddleLeft;

            lblHeroTitle = new Label();
            lblHeroTitle.Text = "Эмуляция игровых заданий\r\nDiscord без скачивания игр";
            lblHeroTitle.ForeColor = TextMain;
            lblHeroTitle.Font = new Font("Segoe UI", Dpi.F(14.5f), FontStyle.Bold);
            lblHeroTitle.Margin = Padding.Empty;
            lblHeroTitle.Dock = DockStyle.Fill;
            lblHeroTitle.TextAlign = ContentAlignment.MiddleLeft;

            lblHeroSub = new Label();
            lblHeroSub.Text = "Работает напрямую с официальным реестром определяемых игр Discord API и вашим списком заданий, автоматически рассчитывает оставшееся время по прогрессу квеста и защищает токен по стандарту PBKDF2-SHA256.";
            lblHeroSub.ForeColor = TextSub;
            lblHeroSub.Font = new Font("Segoe UI", Dpi.F(8.4f), FontStyle.Regular);
            lblHeroSub.Margin = Padding.Empty;
            lblHeroSub.Dock = DockStyle.Fill;
            lblHeroSub.TextAlign = ContentAlignment.MiddleLeft;

            FlowLayoutPanel heroBtnsRow = new FlowLayoutPanel();
            heroBtnsRow.Dock = DockStyle.Fill;
            heroBtnsRow.Margin = Padding.Empty;
            heroBtnsRow.FlowDirection = FlowDirection.LeftToRight;
            heroBtnsRow.WrapContents = false;
            heroBtnsRow.Padding = Dpi.Pad(0, 2, 0, 0);

            btnSyncHero = PillActionButton.CreateGold("Синхронизировать задания  →", BgCard, "sync");
            btnSyncHero.AutoSizeToText(30, 32);
            btnSyncHero.Margin = Dpi.Pad(0, 0, 10, 0);
            btnSyncHero.Click += (s, e) => ManualSyncQuests();

            btnGuideHero = PillActionButton.CreateDark("Как получить токен за 15 сек  →", BgCard, "book");
            btnGuideHero.AutoSizeToText(30, 32);
            btnGuideHero.Margin = new Padding(0);
            btnGuideHero.Click += (s, e) => SwitchView("guide");

            heroBtnsRow.Controls.Add(btnSyncHero);
            heroBtnsRow.Controls.Add(btnGuideHero);

            TableLayoutPanel metricsGrid = new TableLayoutPanel();
            metricsGrid.Dock = DockStyle.Fill;
            metricsGrid.Margin = Padding.Empty;
            metricsGrid.Padding = Dpi.Pad(0, 6, 0, 0);
            metricsGrid.ColumnCount = 3;
            metricsGrid.RowCount = 1;
            metricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            metricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            metricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            metricsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            metricsGrid.Paint += (s, e) =>
            {
                using (Pen divPen = new Pen(Color.FromArgb(32, 37, 48), 1f))
                {
                    e.Graphics.DrawLine(divPen, 0, 1, metricsGrid.Width, 1);
                }
            };

            TableLayoutPanel mCol1 = new TableLayoutPanel();
            mCol1.Dock = DockStyle.Fill;
            mCol1.Margin = Padding.Empty;
            mCol1.ColumnCount = 1;
            mCol1.RowCount = 2;
            mCol1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mCol1.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(25)));
            mCol1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            FlowLayoutPanel m1Top = new FlowLayoutPanel();
            m1Top.Dock = DockStyle.Fill;
            m1Top.FlowDirection = FlowDirection.LeftToRight;
            m1Top.WrapContents = false;
            m1Top.Margin = Padding.Empty;

            lblEarnedOrbs = new Label();
            lblEarnedOrbs.Text = "0 Orbs";
            lblEarnedOrbs.ForeColor = Gold;
            lblEarnedOrbs.Font = new Font("Segoe UI", Dpi.F(12.2f), FontStyle.Bold);
            lblEarnedOrbs.AutoSize = true;
            lblEarnedOrbs.Margin = Dpi.Pad(0, 0, 6, 0);

            btnEditOrbs = PillActionButton.CreateDark("", BgCard, "pencil");
            btnEditOrbs.AutoSizeToText(12, 22);
            btnEditOrbs.Margin = Dpi.Pad(0, 1, 0, 0);

            RoundedInputBox inlineOrbsInput = new RoundedInputBox("0", false);
            inlineOrbsInput.Size = new Size(Dpi.S(96), Dpi.S(24));
            inlineOrbsInput.Padding = Dpi.Pad(8, 3, 8, 2);
            inlineOrbsInput.Margin = Dpi.Pad(0, 0, 6, 0);
            inlineOrbsInput.Visible = false;

            PillActionButton btnSaveInlineOrbs = PillActionButton.CreateGold("✓", BgCard);
            btnSaveInlineOrbs.AutoSizeToText(16, 22);
            btnSaveInlineOrbs.Margin = Dpi.Pad(0, 1, 4, 0);
            btnSaveInlineOrbs.Visible = false;

            PillActionButton btnCancelInlineOrbs = PillActionButton.CreateDark("✕", BgCard);
            btnCancelInlineOrbs.AutoSizeToText(14, 22);
            btnCancelInlineOrbs.Margin = Dpi.Pad(0, 1, 0, 0);
            btnCancelInlineOrbs.Visible = false;

            Action closeInlineOrbs = () =>
            {
                inlineOrbsInput.Visible = false;
                btnSaveInlineOrbs.Visible = false;
                btnCancelInlineOrbs.Visible = false;
                lblEarnedOrbs.Visible = true;
                btnEditOrbs.Visible = true;
            };

            Action commitInlineOrbs = () =>
            {
                int val;
                if (int.TryParse(inlineOrbsInput.Value.Trim(), out val) && val >= 0)
                {
                    int eq = 0;
                    foreach (QuestItem q in activeQuests)
                    {
                        if (completedQuestIds.Contains(q.Id)) eq += CalcOrbs(q.BaseOrbs);
                    }
                    manualOrbsOffset = val - eq;
                    SaveState();
                    UpdateOrbsCounters();
                }
                closeInlineOrbs();
            };

            btnEditOrbs.Click += (s, e) =>
            {
                int eq = 0;
                foreach (QuestItem q in activeQuests)
                {
                    if (completedQuestIds.Contains(q.Id)) eq += CalcOrbs(q.BaseOrbs);
                }
                inlineOrbsInput.Value = Math.Max(0, eq + manualOrbsOffset).ToString();
                lblEarnedOrbs.Visible = false;
                btnEditOrbs.Visible = false;
                inlineOrbsInput.Visible = true;
                btnSaveInlineOrbs.Visible = true;
                btnCancelInlineOrbs.Visible = true;
                inlineOrbsInput.InnerTextBox.Focus();
                inlineOrbsInput.InnerTextBox.SelectAll();
            };

            btnSaveInlineOrbs.Click += (s, e) => commitInlineOrbs();
            btnCancelInlineOrbs.Click += (s, e) => closeInlineOrbs();
            inlineOrbsInput.InnerTextBox.KeyDown += (s, ke) =>
            {
                if (ke.KeyCode == Keys.Enter) { ke.SuppressKeyPress = true; commitInlineOrbs(); }
                else if (ke.KeyCode == Keys.Escape) { ke.SuppressKeyPress = true; closeInlineOrbs(); }
            };

            m1Top.Controls.Add(lblEarnedOrbs);
            m1Top.Controls.Add(btnEditOrbs);
            m1Top.Controls.Add(inlineOrbsInput);
            m1Top.Controls.Add(btnSaveInlineOrbs);
            m1Top.Controls.Add(btnCancelInlineOrbs);

            lblM1Sub = new Label();
            lblM1Sub.Text = "Заработано Orbs (с учётом выполненных)";
            lblM1Sub.ForeColor = TextSub;
            lblM1Sub.Font = new Font("Segoe UI", Dpi.F(7.8f), FontStyle.Regular);
            lblM1Sub.Margin = Padding.Empty;
            lblM1Sub.Dock = DockStyle.Fill;
            lblM1Sub.TextAlign = ContentAlignment.MiddleLeft;

            mCol1.Controls.Add(m1Top, 0, 0);
            mCol1.Controls.Add(lblM1Sub, 0, 1);

            TableLayoutPanel mCol2 = new TableLayoutPanel();
            mCol2.Dock = DockStyle.Fill;
            mCol2.Margin = Padding.Empty;
            mCol2.ColumnCount = 1;
            mCol2.RowCount = 2;
            mCol2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mCol2.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(25)));
            mCol2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            FlowLayoutPanel m2Top = new FlowLayoutPanel();
            m2Top.Dock = DockStyle.Fill;
            m2Top.FlowDirection = FlowDirection.LeftToRight;
            m2Top.WrapContents = false;
            m2Top.Margin = Padding.Empty;

            lblAvailOrbs = new Label();
            lblAvailOrbs.Text = "0 Orbs";
            lblAvailOrbs.ForeColor = TextMain;
            lblAvailOrbs.Font = new Font("Segoe UI", Dpi.F(12.2f), FontStyle.Bold);
            lblAvailOrbs.AutoSize = true;
            lblAvailOrbs.Margin = Dpi.Pad(0, 0, 8, 0);

            chkNitro = new GoldCheckBoxControl("x1.2 Nitro", nitroBonus, BgCard, Gold);
            chkNitro.Size = new Size(chkNitro.MeasureNeededWidth(), Dpi.S(22));
            chkNitro.Margin = Dpi.Pad(0, 1, 0, 0);
            chkNitro.CheckedChanged += (s, e) =>
            {
                nitroBonus = chkNitro.Checked;
                SaveState();
                UpdateOrbsCounters();
                RefreshQuestTable();
            };

            m2Top.Controls.Add(lblAvailOrbs);
            m2Top.Controls.Add(chkNitro);

            lblM2Sub = new Label();
            lblM2Sub.Text = "Доступно сейчас по активным квестам";
            lblM2Sub.ForeColor = TextSub;
            lblM2Sub.Font = new Font("Segoe UI", Dpi.F(7.8f), FontStyle.Regular);
            lblM2Sub.Margin = Padding.Empty;
            lblM2Sub.Dock = DockStyle.Fill;
            lblM2Sub.TextAlign = ContentAlignment.MiddleLeft;

            mCol2.Controls.Add(m2Top, 0, 0);
            mCol2.Controls.Add(lblM2Sub, 0, 1);

            TableLayoutPanel mCol3 = new TableLayoutPanel();
            mCol3.Dock = DockStyle.Fill;
            mCol3.Margin = Padding.Empty;
            mCol3.ColumnCount = 1;
            mCol3.RowCount = 2;
            mCol3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mCol3.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(25)));
            mCol3.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblQuestsCounts = new Label();
            lblQuestsCounts.Text = "Выполнено: 0 | Доступно: 0";
            lblQuestsCounts.ForeColor = TextMain;
            lblQuestsCounts.Font = new Font("Segoe UI", Dpi.F(12.2f), FontStyle.Bold);
            lblQuestsCounts.Margin = Padding.Empty;
            lblQuestsCounts.Dock = DockStyle.Fill;

            lblDbStatus = new Label();
            lblDbStatus.Text = "База Discord API: загрузка...";
            lblDbStatus.ForeColor = TextSub;
            lblDbStatus.Font = new Font("Segoe UI", Dpi.F(7.8f), FontStyle.Regular);
            lblDbStatus.Margin = Padding.Empty;
            lblDbStatus.Dock = DockStyle.Fill;
            lblDbStatus.TextAlign = ContentAlignment.MiddleLeft;

            mCol3.Controls.Add(lblQuestsCounts, 0, 0);
            mCol3.Controls.Add(lblDbStatus, 0, 1);

            metricsGrid.Controls.Add(mCol1, 0, 0);
            metricsGrid.Controls.Add(mCol2, 1, 0);
            metricsGrid.Controls.Add(mCol3, 2, 0);

            heroLeft.Controls.Add(lblKicker, 0, 0);
            heroLeft.Controls.Add(lblHeroTitle, 0, 1);
            heroLeft.Controls.Add(lblHeroSub, 0, 2);
            heroLeft.Controls.Add(heroBtnsRow, 0, 3);
            heroLeft.Controls.Add(metricsGrid, 0, 4);

            RoundedPanelCard heroRightCard = new RoundedPanelCard();
            heroRightCard.Radius = 14;
            heroRightCard.FillColor = BgCardInner;
            heroRightCard.BorderColor = Color.FromArgb(42, 48, 64);
            heroRightCard.Dock = DockStyle.Fill;
            heroRightCard.Margin = Padding.Empty;
            heroRightCard.Padding = Dpi.Pad(14, 12, 14, 12);

            TableLayoutPanel spotGrid = new TableLayoutPanel();
            spotGrid.Dock = DockStyle.Fill;
            spotGrid.Margin = Padding.Empty;
            spotGrid.BackColor = BgCardInner;
            spotGrid.ColumnCount = 1;
            spotGrid.RowCount = 4;
            spotGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            spotGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(22)));
            spotGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(28)));
            spotGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            spotGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(42)));

            lblSpotBadge = new Label();
            lblSpotBadge.Text = Loc.T("● Система готова", "● System Ready");
            lblSpotBadge.ForeColor = Gold;
            lblSpotBadge.Font = new Font("Segoe UI", Dpi.F(8.3f), FontStyle.Bold);
            lblSpotBadge.Margin = Padding.Empty;
            lblSpotBadge.Dock = DockStyle.Fill;

            lblSpotTitle = new Label();
            lblSpotTitle.Text = "Выберите задание";
            lblSpotTitle.ForeColor = TextMain;
            lblSpotTitle.Font = new Font("Segoe UI", Dpi.F(11.2f), FontStyle.Bold);
            lblSpotTitle.Margin = Padding.Empty;
            lblSpotTitle.Dock = DockStyle.Fill;

            lblSpotSub = new Label();
            lblSpotSub.Text = "Кликните по квесту в таблице или выберите игру в каталоге API";
            lblSpotSub.ForeColor = TextSub;
            lblSpotSub.Font = new Font("Segoe UI", Dpi.F(8.2f), FontStyle.Regular);
            lblSpotSub.Margin = Padding.Empty;
            lblSpotSub.Dock = DockStyle.Fill;

            TableLayoutPanel spotBot = new TableLayoutPanel();
            spotBot.Dock = DockStyle.Fill;
            spotBot.Margin = Padding.Empty;
            spotBot.ColumnCount = 2;
            spotBot.RowCount = 1;
            spotBot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            spotBot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi.S(42)));
            spotBot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            TableLayoutPanel rewInfo = new TableLayoutPanel();
            rewInfo.Dock = DockStyle.Fill;
            rewInfo.Margin = Padding.Empty;
            rewInfo.ColumnCount = 1;
            rewInfo.RowCount = 2;
            rewInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rewInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(15)));
            rewInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblRewHead = new Label();
            lblRewHead.Text = "НАГРАДА ЗА КВЕСТ";
            lblRewHead.ForeColor = Color.FromArgb(120, 128, 142);
            lblRewHead.Font = new Font("Segoe UI", Dpi.F(7.0f), FontStyle.Bold);
            lblRewHead.Margin = Padding.Empty;
            lblRewHead.Dock = DockStyle.Fill;

            lblSpotReward = new Label();
            lblSpotReward.Text = "—";
            lblSpotReward.ForeColor = Gold;
            lblSpotReward.Font = new Font("Segoe UI", Dpi.F(9.2f), FontStyle.Bold);
            lblSpotReward.Margin = Padding.Empty;
            lblSpotReward.Dock = DockStyle.Fill;

            rewInfo.Controls.Add(lblRewHead, 0, 0);
            rewInfo.Controls.Add(lblSpotReward, 0, 1);

            PillActionButton btnQuickStart = PillActionButton.CreateGold("→", BgCardInner);
            btnQuickStart.Dock = DockStyle.Fill;
            btnQuickStart.Margin = Dpi.Pad(2, 2, 0, 2);
            btnQuickStart.Click += (s, e) => StartEmulationProcess();

            spotBot.Controls.Add(rewInfo, 0, 0);
            spotBot.Controls.Add(btnQuickStart, 1, 0);

            spotGrid.Controls.Add(lblSpotBadge, 0, 0);
            spotGrid.Controls.Add(lblSpotTitle, 0, 1);
            spotGrid.Controls.Add(lblSpotSub, 0, 2);
            spotGrid.Controls.Add(spotBot, 0, 3);
            heroRightCard.Controls.Add(spotGrid);

            heroGrid.Controls.Add(heroLeft, 0, 0);
            heroGrid.Controls.Add(heroRightCard, 1, 0);
            heroCard.Controls.Add(heroGrid);

            TableLayoutPanel workspaceGrid = new TableLayoutPanel();
            workspaceGrid.Dock = DockStyle.Fill;
            workspaceGrid.Margin = Padding.Empty;
            workspaceGrid.ColumnCount = 2;
            workspaceGrid.RowCount = 1;
            workspaceGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            workspaceGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            workspaceGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            RoundedPanelCard leftCard = new RoundedPanelCard();
            leftCard.Dock = DockStyle.Fill;
            leftCard.Margin = Dpi.Pad(0, 0, 8, 0);
            leftCard.Padding = Dpi.Pad(16, 12, 16, 12);

            questsViewPanel = new Panel();
            questsViewPanel.Dock = DockStyle.Fill;
            questsViewPanel.BackColor = BgCard;

            TableLayoutPanel qLayout = new TableLayoutPanel();
            qLayout.Dock = DockStyle.Fill;
            qLayout.Margin = Padding.Empty;
            qLayout.ColumnCount = 1;
            qLayout.RowCount = 3;
            qLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            qLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(36)));
            qLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(36)));
            qLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            btnQueueAll = PillActionButton.CreateDark("Очередь всех", BgCard, "rocket");
            btnQueueAll.TextColor = Gold;
            btnQueueAll.BorderNormal = Color.FromArgb(72, 60, 24);
            btnQueueAll.AutoSizeToText(24, 30);

            btnToggleDone = new PillActionButton("Выполнено / Снять");
            btnToggleDone.IconKind = "check";
            btnToggleDone.BackColor = BgCard;
            btnToggleDone.FillNormal = Color.FromArgb(20, 38, 30);
            btnToggleDone.FillHover = Color.FromArgb(30, 58, 44);
            btnToggleDone.BorderNormal = Color.FromArgb(42, 74, 58);
            btnToggleDone.BorderHover = Color.FromArgb(88, 242, 135);
            btnToggleDone.TextColor = Color.FromArgb(88, 242, 135);
            btnToggleDone.AutoSizeToText(24, 30);

            btnClaimAll = PillActionButton.CreateDark("Забрать награды", BgCard, "gem");
            btnClaimAll.TextColor = Color.FromArgb(46, 204, 113);
            btnClaimAll.AutoSizeToText(24, 30);

            TableLayoutPanel qHeadRow = new TableLayoutPanel();
            qHeadRow.Dock = DockStyle.Fill;
            qHeadRow.Margin = Padding.Empty;
            qHeadRow.ColumnCount = 4;
            qHeadRow.RowCount = 1;
            qHeadRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            qHeadRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, btnClaimAll.Width + Dpi.S(8)));
            qHeadRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, btnQueueAll.Width + Dpi.S(8)));
            qHeadRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, btnToggleDone.Width + Dpi.S(4)));
            qHeadRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblQHead = new IconHeaderLabel("fire", "Актуальные задания Discord Quests (API)", 10.5f);
            lblQHead.Dock = DockStyle.Fill;

            btnClaimAll.Dock = DockStyle.Fill;
            btnClaimAll.Margin = Dpi.Pad(0, 2, 6, 2);
            btnClaimAll.Click += (s, e) =>
            {
                Thread ct = new Thread(() => ClaimAllCompletedRewards(true));
                ct.IsBackground = true;
                ct.Start();
            };

            btnQueueAll.Dock = DockStyle.Fill;
            btnQueueAll.Margin = Dpi.Pad(0, 2, 6, 2);
            btnQueueAll.Click += (s, e) => ToggleQuestQueue();

            btnToggleDone.Dock = DockStyle.Fill;
            btnToggleDone.Margin = Dpi.Pad(0, 2, 0, 2);
            btnToggleDone.Click += (s, e) => ToggleSelectedQuestCompleted();

            qHeadRow.Controls.Add(lblQHead, 0, 0);
            qHeadRow.Controls.Add(btnClaimAll, 1, 0);
            qHeadRow.Controls.Add(btnQueueAll, 2, 0);
            qHeadRow.Controls.Add(btnToggleDone, 3, 0);

            chkHideExpired = new GoldCheckBoxControl("Скрыть истекшие", hideExpired, BgCard, TextSub);
            int hideExpWidth = chkHideExpired.MeasureNeededWidth();

            TableLayoutPanel qFilterRow = new TableLayoutPanel();
            qFilterRow.Dock = DockStyle.Fill;
            qFilterRow.Margin = Padding.Empty;
            qFilterRow.ColumnCount = 2;
            qFilterRow.RowCount = 1;
            qFilterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            qFilterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, hideExpWidth));
            qFilterRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            sortCombo = new DarkSortDropdown(new string[] {
                "Сортировка: Награда (Orbs по убыванию, прочие в конце)",
                "Сортировка: Награда (Orbs по возрастанию, прочие в конце)",
                "Сортировка: Статус (сначала Доступные, Выполнено внизу)",
                "Сортировка: Статус (сначала Выполненные)",
                "Сортировка: Игра / Задание (по алфавиту А–Я / A–Z)",
                "Сортировка: Дата окончания (сначала ближайшие)",
                "Сортировка: Тип задания и прогресс (%)"
            });
            sortCombo.Dock = DockStyle.Fill;
            sortCombo.Margin = Dpi.Pad(0, 2, 12, 3);
            sortCombo.SelectedIndexChanged += OnSortComboChanged;

            chkHideExpired.Dock = DockStyle.Fill;
            chkHideExpired.CheckedChanged += (s, e) =>
            {
                hideExpired = chkHideExpired.Checked;
                SaveState();
                RefreshQuestTable();
            };

            qFilterRow.Controls.Add(sortCombo, 0, 0);
            qFilterRow.Controls.Add(chkHideExpired, 1, 0);

            questTable = new QuestTableControl();
            questTable.Dock = DockStyle.Fill;
            questTable.SortColumn = sortColumn;
            questTable.SortDesc = sortDescending;
            questTable.QuestSelected += OnQuestSelected;
            questTable.QuestDoubleClicked += (q) => ToggleSelectedQuestCompleted();
            questTable.ColumnHeaderClicked += OnQuestColumnClicked;

            qLayout.Controls.Add(qHeadRow, 0, 0);
            qLayout.Controls.Add(qFilterRow, 0, 1);
            qLayout.Controls.Add(questTable, 0, 2);
            questsViewPanel.Controls.Add(qLayout);

            gamesViewPanel = new Panel();
            gamesViewPanel.Dock = DockStyle.Fill;
            gamesViewPanel.BackColor = BgCard;
            gamesViewPanel.Visible = false;

            TableLayoutPanel gLayout = new TableLayoutPanel();
            gLayout.Dock = DockStyle.Fill;
            gLayout.Margin = Padding.Empty;
            gLayout.ColumnCount = 1;
            gLayout.RowCount = 3;
            gLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            gLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(36)));
            gLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(42)));
            gLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            btnReloadDb = PillActionButton.CreateDark("Обновить базу API", BgCard);
            btnReloadDb.TextColor = Gold;
            btnReloadDb.AutoSizeToText(24, 30);

            TableLayoutPanel gHeadRow = new TableLayoutPanel();
            gHeadRow.Dock = DockStyle.Fill;
            gHeadRow.Margin = Padding.Empty;
            gHeadRow.ColumnCount = 2;
            gHeadRow.RowCount = 1;
            gHeadRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            gHeadRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, btnReloadDb.Width + Dpi.S(4)));
            gHeadRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblGHead = new IconHeaderLabel("gamepad", "Каталог определяемых игр Discord API (/applications/detectable)", 10.0f);
            lblGHead.Dock = DockStyle.Fill;

            btnReloadDb.Dock = DockStyle.Fill;
            btnReloadDb.Margin = Dpi.Pad(0, 2, 0, 2);
            btnReloadDb.Click += (s, e) =>
            {
                Thread t = new Thread(() => LoadDetectableGamesDatabase(true));
                t.IsBackground = true;
                t.Start();
            };

            gHeadRow.Controls.Add(lblGHead, 0, 0);
            gHeadRow.Controls.Add(btnReloadDb, 1, 0);

            searchGameInput = new RoundedInputBox("", false);
            searchGameInput.Dock = DockStyle.Fill;
            searchGameInput.Margin = Dpi.Pad(0, 2, 0, 4);
            searchGameInput.InnerTextBox.TextChanged += (s, e) => FilterDetectableGames();

            gamesListView = new GamesCatalogTableControl();
            gamesListView.Dock = DockStyle.Fill;
            gamesListView.GameSelected += OnCatalogGameSelected;

            gLayout.Controls.Add(gHeadRow, 0, 0);
            gLayout.Controls.Add(searchGameInput, 0, 1);
            gLayout.Controls.Add(gamesListView, 0, 2);
            gamesViewPanel.Controls.Add(gLayout);

            guideViewPanel = new Panel();
            guideViewPanel.Dock = DockStyle.Fill;
            guideViewPanel.BackColor = BgCard;
            guideViewPanel.Visible = false;

            TableLayoutPanel guideLayout = new TableLayoutPanel();
            guideLayout.Dock = DockStyle.Fill;
            guideLayout.BackColor = BgCard;
            guideLayout.Margin = Padding.Empty;
            guideLayout.ColumnCount = 1;
            guideLayout.RowCount = 2;
            guideLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            guideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(36)));
            guideLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblGuideHead = new IconHeaderLabel("book", "Пошаговый гайд: Как подключить Discord и выполнять квесты", 10.5f);
            lblGuideHead.Dock = DockStyle.Fill;

            RoundedPanelCard guideBoxWrap = new RoundedPanelCard();
            guideBoxWrap.Radius = 12;
            guideBoxWrap.FillColor = Color.FromArgb(14, 16, 21);
            guideBoxWrap.BorderColor = BorderCol;
            guideBoxWrap.Dock = DockStyle.Fill;
            guideBoxWrap.Padding = Dpi.Pad(16, 14, 16, 14);

            txtGuideInline = new TextBox();
            txtGuideInline.Multiline = true;
            txtGuideInline.ReadOnly = true;
            txtGuideInline.WordWrap = true;
            txtGuideInline.ScrollBars = ScrollBars.Vertical;
            txtGuideInline.BorderStyle = BorderStyle.None;
            txtGuideInline.BackColor = Color.FromArgb(14, 16, 21);
            txtGuideInline.ForeColor = TextMain;
            txtGuideInline.Font = new Font("Segoe UI", Dpi.F(9.5f), FontStyle.Regular);
            txtGuideInline.Dock = DockStyle.Fill;
            txtGuideInline.Text =
                "ШАГ 1 · ОТКРОЙТЕ КОНСОЛЬ РАЗРАБОТЧИКА В БРАУЗЕРЕ\r\n" +
                "Откройте discord.com/app в браузере, нажмите клавишу F12 (или Ctrl + Shift + I) и сверху выберите вкладку Network («Сеть»).\r\n\r\n" +
                "ШАГ 2 · НАЙДИТЕ ЗАПРОС ВАШИХ ЗАДАНИЙ (@me)\r\n" +
                "В строке фильтра напишите quests, затем в самом Discord слева откройте раздел «Путеводитель → Задания (Quests)». В списке запросов появится строка @me (или quests/@me).\r\n\r\n" +
                "ШАГ 3 · СКОПИРУЙТЕ ТОКЕН ИЛИ JSON-ОТВЕТ\r\n" +
                "• Для автосинхронизации каждые 15 секунд: откройте вкладку Headers запроса @me и скопируйте значение строки authorization.\r\n" +
                "• Для разового импорта без токена: откройте вкладку Response, нажмите Ctrl + A → Ctrl + C и вставьте в окно «Подключить Discord».\r\n\r\n" +
                "ШАГ 4 · ОБРАТНАЯ СВЯЗЬ И БАГ-РЕПОРТЫ\r\n" +
                "При обнаружении неисправностей свяжитесь с автором программы в Discord: hellwalk3rr";
            txtGuideInline.SelectionStart = 0;
            txtGuideInline.SelectionLength = 0;
            guideBoxWrap.Controls.Add(txtGuideInline);
            DarkScrollBar.AttachToTextBox(txtGuideInline, guideBoxWrap);

            guideLayout.Controls.Add(lblGuideHead, 0, 0);
            guideLayout.Controls.Add(guideBoxWrap, 0, 1);
            guideViewPanel.Controls.Add(guideLayout);

            licenseViewPanel = new Panel();
            licenseViewPanel.Dock = DockStyle.Fill;
            licenseViewPanel.BackColor = BgCard;
            licenseViewPanel.Visible = false;

            TableLayoutPanel licLayout = new TableLayoutPanel();
            licLayout.Dock = DockStyle.Fill;
            licLayout.BackColor = BgCard;
            licLayout.Margin = Padding.Empty;
            licLayout.ColumnCount = 1;
            licLayout.RowCount = 2;
            licLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            licLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(36)));
            licLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblLicHead = new IconHeaderLabel("scales", "Лицензионное соглашение и Авторские права", 10.5f);
            lblLicHead.Dock = DockStyle.Fill;

            RoundedPanelCard licBoxWrap = new RoundedPanelCard();
            licBoxWrap.Radius = 12;
            licBoxWrap.FillColor = Color.FromArgb(14, 16, 21);
            licBoxWrap.BorderColor = BorderCol;
            licBoxWrap.Dock = DockStyle.Fill;
            licBoxWrap.Padding = Dpi.Pad(16, 14, 16, 14);

            txtLicInline = new TextBox();
            txtLicInline.Multiline = true;
            txtLicInline.ReadOnly = true;
            txtLicInline.WordWrap = true;
            txtLicInline.ScrollBars = ScrollBars.None;
            txtLicInline.BorderStyle = BorderStyle.None;
            txtLicInline.BackColor = Color.FromArgb(14, 16, 21);
            txtLicInline.ForeColor = TextMain;
            txtLicInline.Font = new Font("Consolas", Dpi.F(9.2f), FontStyle.Regular);
            txtLicInline.Dock = DockStyle.Fill;
            txtLicInline.Text = "ЛИЦЕНЗИОННОЕ СОГЛАШЕНИЕ И АВТОРСКИЕ ПРАВА\r\nПрограмма: Discord Quest Studio\r\nАвтор и правообладатель: hellwalk3rr (Discord: hellwalk3rr)\r\n© 2026 hellwalk3rr. Все права защищены.\r\n\r\nУстановка и использование программы «Discord Quest Studio» означают ваше согласие со следующими условиями:\r\n\r\n1. АВТОРСКИЕ ПРАВА\r\n1.1. Все авторские права на программу «Discord Quest Studio» принадлежат её создателю — hellwalk3rr (Discord для связи: hellwalk3rr).\r\n1.2. Пользователю предоставляется бесплатное право на установку и личное некоммерческое использование программы на компьютерах под управлением ОС Windows.\r\n\r\n2. ЗАПРЕТ НА ПРОДАЖУ И КОММЕРЧЕСКОЕ ИСПОЛЬЗОВАНИЕ\r\n2.1. Программа «Discord Quest Studio» распространяется исключительно бесплатно в открытом доступе.\r\n2.2. Пользователям и третьим лицам строго запрещается:\r\n  • продавать, перепродавать, сдавать в аренду или распространять программу за любую плату;\r\n  • включать программу в состав платных подписок, закрытых платных каналов или коммерческих сборок;\r\n  • требовать оплату за предоставление ссылки на скачивание или за установку программы.\r\n\r\n3. СОХРАНЕНИЕ УКАЗАНИЯ АВТОРСТВА\r\n3.1. Запрещается удалять, скрывать или изменять сведения об авторе (hellwalk3rr) внутри программы и установщика.\r\n3.2. При публикации программы на сторонних ресурсах обязательно указание автора (Discord: hellwalk3rr) и сохранение бесплатного статуса проекта.\r\n\r\n4. БЕЗОПАСНОСТЬ И ОТКАЗ ОТ ОТВЕТСТВЕННОСТИ\r\n4.1. Программа работает локально на компьютере пользователя. Токен авторизации шифруется по стандарту PBKDF2-HMAC-SHA256 и хранится только на вашем устройстве (~/.discord_quest_studio/vault.dat).\r\n4.2. Программа предоставляется по принципу «как есть» (AS IS). По всем вопросам и баг-репортам обращайтесь к автору в Discord: hellwalk3rr.";
            txtLicInline.SelectionStart = 0;
            txtLicInline.SelectionLength = 0;
            licBoxWrap.Controls.Add(txtLicInline);
            DarkScrollBar.AttachToTextBox(txtLicInline, licBoxWrap);

            licLayout.Controls.Add(lblLicHead, 0, 0);
            licLayout.Controls.Add(licBoxWrap, 0, 1);
            licenseViewPanel.Controls.Add(licLayout);

            leftCard.Controls.Add(questsViewPanel);
            leftCard.Controls.Add(gamesViewPanel);
            leftCard.Controls.Add(guideViewPanel);
            leftCard.Controls.Add(licenseViewPanel);

            RoundedPanelCard rightCard = new RoundedPanelCard();
            rightCard.Dock = DockStyle.Fill;
            rightCard.Margin = Dpi.Pad(8, 0, 0, 0);
            rightCard.Padding = Dpi.Pad(16, 12, 16, 12);

            TableLayoutPanel rLayout = new TableLayoutPanel();
            rLayout.Dock = DockStyle.Fill;
            rLayout.Margin = Padding.Empty;
            rLayout.BackColor = BgCard;
            rLayout.ColumnCount = 1;
            rLayout.RowCount = 11;
            rLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(24)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(17)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(32)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(17)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(32)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(44)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(49)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(49)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(44)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(38)));
            rLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblRightHead = new IconHeaderLabel("gear", "Параметры эмуляции процесса", 10.5f);
            lblRightHead.Dock = DockStyle.Fill;

            lblF1 = new Label();
            lblF1.Text = "Название игры (заголовок окна процесса)";
            lblF1.ForeColor = TextSub;
            lblF1.Font = new Font("Segoe UI", Dpi.F(8.1f), FontStyle.Regular);
            lblF1.Margin = Padding.Empty;
            lblF1.Dock = DockStyle.Fill;

            inpGameName = new RoundedInputBox("", false);
            inpGameName.Dock = DockStyle.Fill;
            inpGameName.Margin = Dpi.Pad(0, 1, 0, 2);

            lblF2 = new Label();
            lblF2.Text = "Относительный путь и имя .exe (из реестра Discord API)";
            lblF2.ForeColor = TextSub;
            lblF2.Font = new Font("Segoe UI", Dpi.F(8.1f), FontStyle.Regular);
            lblF2.Margin = Padding.Empty;
            lblF2.Dock = DockStyle.Fill;

            inpExeRel = new RoundedInputBox("", false);
            inpExeRel.Dock = DockStyle.Fill;
            inpExeRel.Margin = Dpi.Pad(0, 1, 0, 2);

            RoundedPanelCard smartBarCard = new RoundedPanelCard();
            smartBarCard.Radius = 9;
            smartBarCard.FillColor = BgCardInner;
            smartBarCard.BorderColor = Color.FromArgb(40, 46, 60);
            smartBarCard.Dock = DockStyle.Fill;
            smartBarCard.Margin = Dpi.Pad(0, 1, 0, 2);
            smartBarCard.Padding = Dpi.Pad(10, 6, 10, 6);

            TableLayoutPanel smartBarGrid = new TableLayoutPanel();
            smartBarGrid.Dock = DockStyle.Fill;
            smartBarGrid.Margin = Padding.Empty;
            smartBarGrid.BackColor = BgCardInner;
            smartBarGrid.ColumnCount = 2;
            smartBarGrid.RowCount = 2;
            smartBarGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            smartBarGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            smartBarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(18)));
            smartBarGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblSmartProgTitle = new Label();
            lblSmartProgTitle.Text = "Прогресс задания в Discord: 0%";
            lblSmartProgTitle.ForeColor = TextSub;
            lblSmartProgTitle.Font = new Font("Segoe UI", Dpi.F(8.0f), FontStyle.Regular);
            lblSmartProgTitle.Margin = Padding.Empty;
            lblSmartProgTitle.Dock = DockStyle.Fill;

            lblSmartProgRem = new Label();
            lblSmartProgRem.Text = "Осталось: 15 мин. (+ хвост)";
            lblSmartProgRem.ForeColor = Gold;
            lblSmartProgRem.Font = new Font("Segoe UI", Dpi.F(8.0f), FontStyle.Bold);
            lblSmartProgRem.Margin = Padding.Empty;
            lblSmartProgRem.Dock = DockStyle.Fill;
            lblSmartProgRem.TextAlign = ContentAlignment.TopRight;

            smartProgBar = new SmoothGoldBar();
            smartProgBar.Dock = DockStyle.Top;

            smartBarGrid.Controls.Add(lblSmartProgTitle, 0, 0);
            smartBarGrid.Controls.Add(lblSmartProgRem, 1, 0);
            smartBarGrid.Controls.Add(smartProgBar, 0, 1);
            smartBarGrid.SetColumnSpan(smartProgBar, 2);
            smartBarCard.Controls.Add(smartBarGrid);

            TableLayoutPanel twoColsRow = new TableLayoutPanel();
            twoColsRow.Dock = DockStyle.Fill;
            twoColsRow.Margin = Padding.Empty;
            twoColsRow.ColumnCount = 2;
            twoColsRow.RowCount = 2;
            twoColsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            twoColsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            twoColsRow.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(17)));
            twoColsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblMin = new Label();
            lblMin.Text = "Оставшееся время (мин.)";
            lblMin.ForeColor = TextSub;
            lblMin.Font = new Font("Segoe UI", Dpi.F(8.1f), FontStyle.Regular);
            lblMin.Margin = Padding.Empty;
            lblMin.Dock = DockStyle.Fill;

            Label lblStId = new Label();
            lblStId.Text = "Steam AppID";
            lblStId.ForeColor = TextSub;
            lblStId.Font = new Font("Segoe UI", Dpi.F(8.1f), FontStyle.Regular);
            lblStId.Margin = Padding.Empty;
            lblStId.Dock = DockStyle.Fill;

            inpMinutes = new RoundedInputBox("15", false);
            inpMinutes.Dock = DockStyle.Fill;
            inpMinutes.Margin = Dpi.Pad(0, 1, 6, 1);

            inpSteamId = new RoundedInputBox("", false);
            inpSteamId.Dock = DockStyle.Fill;
            inpSteamId.Margin = Dpi.Pad(6, 1, 0, 1);

            twoColsRow.Controls.Add(lblMin, 0, 0);
            twoColsRow.Controls.Add(lblStId, 1, 0);
            twoColsRow.Controls.Add(inpMinutes, 0, 1);
            twoColsRow.Controls.Add(inpSteamId, 1, 1);

            btnBrowseSteam = PillActionButton.CreateDark("Обзор...", BgCard);
            btnBrowseSteam.AutoSizeToText(24, 30);

            TableLayoutPanel steamappsRow = new TableLayoutPanel();
            steamappsRow.Dock = DockStyle.Fill;
            steamappsRow.Margin = Padding.Empty;
            steamappsRow.ColumnCount = 2;
            steamappsRow.RowCount = 2;
            steamappsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            steamappsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, btnBrowseSteam.Width + Dpi.S(4)));
            steamappsRow.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(17)));
            steamappsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblSteamDir = new Label();
            lblSteamDir.Text = "Рабочая папка steamapps (создание временной пустышки)";
            lblSteamDir.ForeColor = TextSub;
            lblSteamDir.Font = new Font("Segoe UI", Dpi.F(8.1f), FontStyle.Regular);
            lblSteamDir.Margin = Padding.Empty;
            lblSteamDir.Dock = DockStyle.Fill;

            inpSteamappsDir = new RoundedInputBox(DetectDefaultSteamapps(), false);
            inpSteamappsDir.Dock = DockStyle.Fill;
            inpSteamappsDir.Margin = Dpi.Pad(0, 1, 8, 1);

            btnBrowseSteam.Dock = DockStyle.Fill;
            btnBrowseSteam.Margin = Dpi.Pad(0, 1, 0, 1);
            btnBrowseSteam.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = inpSteamappsDir.Value;
                    if (fbd.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(fbd.SelectedPath))
                    {
                        inpSteamappsDir.Value = fbd.SelectedPath;
                        SaveState();
                    }
                }
            };

            steamappsRow.Controls.Add(lblSteamDir, 0, 0);
            steamappsRow.SetColumnSpan(lblSteamDir, 2);
            steamappsRow.Controls.Add(inpSteamappsDir, 0, 1);
            steamappsRow.Controls.Add(btnBrowseSteam, 1, 1);

            TableLayoutPanel checksPanel = new TableLayoutPanel();
            checksPanel.Dock = DockStyle.Fill;
            checksPanel.Margin = Padding.Empty;
            checksPanel.ColumnCount = 1;
            checksPanel.RowCount = 2;
            checksPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            checksPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            checksPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            chkRandomTail = new GoldCheckBoxControl("Добавлять случайный «хвост» (+30..90 сек) к оставшемуся времени", true, BgCard, TextMain);
            chkRandomTail.Dock = DockStyle.Fill;
            chkCreateAcf = new GoldCheckBoxControl("Создавать временный Steam appmanifest_<id>.acf", true, BgCard, TextMain);
            chkCreateAcf.Dock = DockStyle.Fill;
            checksPanel.Controls.Add(chkRandomTail, 0, 0);
            checksPanel.Controls.Add(chkCreateAcf, 0, 1);

            TableLayoutPanel startStopRow = new TableLayoutPanel();
            startStopRow.Dock = DockStyle.Fill;
            startStopRow.Margin = Padding.Empty;
            startStopRow.ColumnCount = 2;
            startStopRow.RowCount = 1;
            startStopRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f));
            startStopRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            startStopRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            btnStartEmu = PillActionButton.CreateGold("Запустить эмуляцию  →", BgCard, "rocket");
            btnStartEmu.Dock = DockStyle.Fill;
            btnStartEmu.Margin = Dpi.Pad(0, 2, 8, 2);
            btnStartEmu.Click += (s, e) => StartEmulationProcess();

            btnStopEmu = PillActionButton.CreateDanger("Стоп", BgCard, "stop");
            btnStopEmu.Dock = DockStyle.Fill;
            btnStopEmu.Margin = Dpi.Pad(0, 2, 0, 2);
            btnStopEmu.Click += (s, e) => EmergencyStopAll();

            startStopRow.Controls.Add(btnStartEmu, 0, 0);
            startStopRow.Controls.Add(btnStopEmu, 1, 0);

            TableLayoutPanel activeProcsSection = new TableLayoutPanel();
            activeProcsSection.Dock = DockStyle.Fill;
            activeProcsSection.Margin = Padding.Empty;
            activeProcsSection.ColumnCount = 1;
            activeProcsSection.RowCount = 2;
            activeProcsSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            activeProcsSection.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(20)));
            activeProcsSection.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblActiveProcHeader = new Label();
            lblActiveProcHeader.Text = "Активные запущенные процессы (0)";
            lblActiveProcHeader.ForeColor = TextMain;
            lblActiveProcHeader.Font = new Font("Segoe UI", Dpi.F(8.8f), FontStyle.Bold);
            lblActiveProcHeader.Margin = Padding.Empty;
            lblActiveProcHeader.Dock = DockStyle.Fill;

            RoundedPanelCard procListWrap = new RoundedPanelCard();
            procListWrap.Radius = 10;
            procListWrap.FillColor = Color.FromArgb(14, 16, 21);
            procListWrap.BorderColor = BorderCol;
            procListWrap.Dock = DockStyle.Fill;
            procListWrap.Margin = Padding.Empty;
            procListWrap.Padding = Dpi.Pad(8, 6, 8, 6);

            procListContainer = new Panel();
            procListContainer.Dock = DockStyle.Fill;
            procListContainer.AutoScroll = true;
            procListContainer.BackColor = Color.FromArgb(14, 16, 21);

            lblEmptyProcs = new Label();
            lblEmptyProcs.Text = "Нет активных процессов эмуляции.\r\nВыберите задание слева и нажмите «Запустить эмуляцию →».";
            lblEmptyProcs.ForeColor = Color.FromArgb(110, 118, 132);
            lblEmptyProcs.Font = new Font("Segoe UI", Dpi.F(8.3f), FontStyle.Regular);
            lblEmptyProcs.Dock = DockStyle.Fill;
            lblEmptyProcs.TextAlign = ContentAlignment.MiddleCenter;
            procListContainer.Controls.Add(lblEmptyProcs);

            procListWrap.Controls.Add(procListContainer);
            activeProcsSection.Controls.Add(lblActiveProcHeader, 0, 0);
            activeProcsSection.Controls.Add(procListWrap, 0, 1);

            rLayout.Controls.Add(lblRightHead, 0, 0);
            rLayout.Controls.Add(lblF1, 0, 1);
            rLayout.Controls.Add(inpGameName, 0, 2);
            rLayout.Controls.Add(lblF2, 0, 3);
            rLayout.Controls.Add(inpExeRel, 0, 4);
            rLayout.Controls.Add(smartBarCard, 0, 5);
            rLayout.Controls.Add(twoColsRow, 0, 6);
            rLayout.Controls.Add(steamappsRow, 0, 7);
            rLayout.Controls.Add(checksPanel, 0, 8);
            rLayout.Controls.Add(startStopRow, 0, 9);
            rLayout.Controls.Add(activeProcsSection, 0, 10);
            rightCard.Controls.Add(rLayout);

            workspaceGrid.Controls.Add(leftCard, 0, 0);
            workspaceGrid.Controls.Add(rightCard, 1, 0);

            rootBody.Controls.Add(heroCard, 0, 0);
            rootBody.Controls.Add(workspaceGrid, 0, 1);

            viewportHost.Controls.Add(rootBody);

            Action applyUniversalViewport = () =>
            {
                int availW = viewportHost.ClientSize.Width;
                int availH = viewportHost.ClientSize.Height;
                if (availW <= 0 || availH <= 0) return;

                int maxAspectW = Math.Max(Dpi.S(1680), (int)(availH * 1.90f));
                int maxAspectH = Math.Max(Dpi.S(1020), (int)(availW * 0.85f));

                int targetW = Math.Min(availW, maxAspectW);
                int targetH = Math.Min(availH, maxAspectH);

                int offsetX = Math.Max(0, (availW - targetW) / 2);
                int offsetY = Math.Max(0, (availH - targetH) / 2);

                rootBody.SetBounds(offsetX, offsetY, targetW, targetH);
                navInnerPanel.SetBounds(offsetX, 0, targetW, navBar.ClientSize.Height - 1);
            };

            viewportHost.Resize += (s, e) => applyUniversalViewport();
            navBar.Resize += (s, e) =>
            {
                applyUniversalViewport();
                AdjustNavBarLayout();
            };
            this.Shown += (s, e) => applyUniversalViewport();

            this.Controls.Add(viewportHost);
            this.Controls.Add(navBar);
            this.Controls.Add(customTitleBar);
            applyUniversalViewport();
        }

        private void ApplyLocalization()
        {
            if (langBtn != null)
            {
                langBtn.Text = Loc.IsEn ? "EN" : "RU";
                langBtn.Invalidate();
            }
            if (tabQuestsBtn != null) { tabQuestsBtn.Text = Loc.T("Задания Discord", "Discord Quests"); tabQuestsBtn.AutoSizeToText(26, 34); tabQuestsBtn.Invalidate(); }
            if (tabGamesBtn != null) { tabGamesBtn.Text = Loc.T("Каталог игр API", "API Games Catalog"); tabGamesBtn.AutoSizeToText(26, 34); tabGamesBtn.Invalidate(); }
            if (guideNavBtn != null) { guideNavBtn.Text = Loc.T("Гайд", "Guide"); guideNavBtn.AutoSizeToText(26, 34); guideNavBtn.Invalidate(); }
            if (licNavBtn != null) { licNavBtn.Text = Loc.T("Лицензия", "License"); licNavBtn.AutoSizeToText(26, 34); licNavBtn.Invalidate(); }

            if (bugBtn != null) { bugBtn.Text = navCompactMode ? "DS: hellwalk3rr" : Loc.T("Баг-репорт · DS: hellwalk3rr", "Bug Report · DS: hellwalk3rr"); bugBtn.AutoSizeToText(28, 34); bugBtn.Invalidate(); }
            if (killBtn != null) { killBtn.Text = Loc.T("АВАРИЙНЫЙ СТОП", "EMERGENCY STOP"); killBtn.AutoSizeToText(28, 34); killBtn.Invalidate(); }
            RefreshAccountBadge();

            if (lblHeroTitle != null) lblHeroTitle.Text = Loc.T("Эмуляция игровых заданий\r\nDiscord без скачивания игр", "Emulate Discord Game Quests\r\nWithout Downloading Games");
            if (lblHeroSub != null) lblHeroSub.Text = Loc.T("Работает напрямую с официальным реестром определяемых игр Discord API и вашим списком заданий, автоматически рассчитывает оставшееся время по прогрессу квеста и защищает токен по стандарту PBKDF2-SHA256.", "Works directly with the official Discord detectable games API and your active quests, automatically calculates remaining quest time, and encrypts your token using PBKDF2-SHA256.");
            if (btnSyncHero != null) { btnSyncHero.Text = Loc.T("Синхронизировать задания  →", "Sync Quests  →"); btnSyncHero.AutoSizeToText(30, 32); btnSyncHero.Invalidate(); }
            if (btnGuideHero != null) { btnGuideHero.Text = Loc.T("Как получить токен за 15 сек  →", "How to Get Token in 15s  →"); btnGuideHero.AutoSizeToText(30, 32); btnGuideHero.Invalidate(); }

            if (lblM1Sub != null) lblM1Sub.Text = Loc.T("Заработано Orbs (с учётом выполненных)", "Total Orbs Earned (incl. completed)");
            if (lblM2Sub != null) lblM2Sub.Text = Loc.T("Доступно сейчас по активным квестам", "Available now from active quests");
            if (lblRewHead != null) lblRewHead.Text = Loc.T("НАГРАДА ЗА КВЕСТ", "QUEST REWARD");

            if (lblQHead != null) { lblQHead.Text = Loc.T("Актуальные задания Discord Quests (API)", "Active Discord Quests (API)"); lblQHead.Invalidate(); }
            if (chkHideExpired != null) { chkHideExpired.Text = Loc.T("Скрыть истекшие", "Hide expired"); chkHideExpired.Invalidate(); }
            if (sortCombo != null)
            {
                sortCombo.SetOptions(Loc.IsEn ? new string[] {
                    "Sort: Reward (Orbs desc, others last)",
                    "Sort: Reward (Orbs asc, others last)",
                    "Sort: Status (Available first, Done last)",
                    "Sort: Status (Completed first)",
                    "Sort: Game / Quest (A–Z)",
                    "Sort: Expiration Date (soonest first)",
                    "Sort: Task Type & Progress (%)"
                } : new string[] {
                    "Сортировка: Награда (Orbs по убыванию, прочие в конце)",
                    "Сортировка: Награда (Orbs по возрастанию, прочие в конце)",
                    "Сортировка: Статус (сначала Доступные, Выполнено внизу)",
                    "Сортировка: Статус (сначала Выполненные)",
                    "Сортировка: Игра / Задание (по алфавиту А–Я / A–Z)",
                    "Сортировка: Дата окончания (сначала ближайшие)",
                    "Сортировка: Тип задания и прогресс (%)"
                });
            }
            if (btnToggleDone != null) { btnToggleDone.Text = Loc.T("Выполнено / Снять", "Done / Undo"); btnToggleDone.AutoSizeToText(24, 30); btnToggleDone.Invalidate(); }
            if (btnClaimAll != null) { btnClaimAll.Text = Loc.T("Забрать награды", "Claim Rewards"); btnClaimAll.AutoSizeToText(24, 30); btnClaimAll.Invalidate(); }
            RefreshQueueButtonUi();

            if (lblGHead != null) { lblGHead.Text = Loc.T("Каталог определяемых игр Discord API (/applications/detectable)", "Discord Detectable Games Catalog (/applications/detectable)"); lblGHead.Invalidate(); }
            if (btnReloadDb != null) { btnReloadDb.Text = Loc.T("Обновить базу API", "Refresh API DB"); btnReloadDb.AutoSizeToText(24, 30); btnReloadDb.Invalidate(); }

            if (lblRightHead != null) { lblRightHead.Text = Loc.T("Параметры эмуляции процесса", "Process Emulation Settings"); lblRightHead.Invalidate(); }
            if (lblF1 != null) lblF1.Text = Loc.T("Название игры (заголовок окна процесса)", "Game Name (process window title)");
            if (lblF2 != null) lblF2.Text = Loc.T("Относительный путь и имя .exe (из реестра Discord API)", "Relative .exe path (from Discord API registry)");
            if (lblMin != null) lblMin.Text = Loc.T("Оставшееся время (мин.)", "Remaining time (min)");
            if (lblSteamDir != null) lblSteamDir.Text = Loc.T("Рабочая папка steamapps (создание временной пустышки)", "Steamapps Folder (temporary dummy host)");
            if (btnBrowseSteam != null) { btnBrowseSteam.Text = Loc.T("Обзор...", "Browse..."); btnBrowseSteam.Invalidate(); }
            if (chkRandomTail != null) { chkRandomTail.Text = Loc.T("Добавлять случайный «хвост» (+30..90 сек) к оставшемуся времени", "Add random tail (+30..90s) to remaining time"); chkRandomTail.Invalidate(); }
            if (chkCreateAcf != null) { chkCreateAcf.Text = Loc.T("Создавать временный Steam appmanifest_<id>.acf", "Create temporary Steam appmanifest_<id>.acf"); chkCreateAcf.Invalidate(); }
            if (btnStartEmu != null) { btnStartEmu.Text = Loc.T("Запустить эмуляцию  →", "Start Emulation  →"); btnStartEmu.Invalidate(); }
            if (btnStopEmu != null) { btnStopEmu.Text = Loc.T("Стоп", "Stop"); btnStopEmu.Invalidate(); }
            if (lblEmptyProcs != null) lblEmptyProcs.Text = Loc.T("Нет активных процессов эмуляции.\r\nВыберите задание слева и нажмите «Запустить эмуляцию →».", "No active emulation processes.\r\nSelect a quest on the left and click «Start Emulation →».");

            if (lblGuideHead != null) { lblGuideHead.Text = Loc.T("Пошаговый гайд: Как подключить Discord и выполнять квесты", "Step-by-Step Guide: Connect Discord & Complete Quests"); lblGuideHead.Invalidate(); }
            if (txtGuideInline != null)
            {
                txtGuideInline.Text = Loc.IsEn ? (
                    "STEP 1 · AUTO-DETECT OR OPEN DEVTOOLS\r\n" +
                    "Click «Connect Discord →» in the top-right corner and try «Auto-Find Token» to detect your local Discord desktop token in 1 click.\r\n\r\n" +
                    "STEP 2 · MANUAL METHOD (BROWSER CONSOLE)\r\n" +
                    "If needed, open discord.com/app in your browser, press F12 (or Ctrl + Shift + I), open the Network tab, filter by quests, and open Quests in Discord.\r\n\r\n" +
                    "STEP 3 · COPY TOKEN OR JSON PAYLOAD\r\n" +
                    "• For auto-sync every 15s: copy the authorization header from the @me request.\r\n" +
                    "• For one-time JSON import: copy the Response of @me and paste it in «Connect Discord».\r\n\r\n" +
                    "STEP 4 · PROXY SETUP (FOR RESTRICTED REGIONS)\r\n" +
                    "• Where to find credentials: open your proxy provider dashboard (HTTP/HTTPS proxy) or your local VPN/Xray/Amnezia client settings (local HTTP port, e.g. 127.0.0.1:10808).\r\n" +
                    "• How to connect: open «Connect Discord →», scroll to Section 3, enter IP:PORT (or IP:PORT:USER:PASS) and optional Login / Password, then click «Save & Test Proxy».\r\n" +
                    "• Note: Video and Activity quests work 100% via the built-in proxy; for Desktop game quests (PLAY_ON_DESKTOP), your Discord desktop app must also use a non-restricted IP (VPN TUN mode).\r\n\r\n" +
                    "STEP 5 · SUPPORT & BUG REPORTS\r\n" +
                    "For issues or feedback, contact the author on Discord: hellwalk3rr"
                ) : (
                    "ШАГ 1 · ОТКРОЙТЕ КОНСОЛЬ РАЗРАБОТЧИКА В БРАУЗЕРЕ\r\n" +
                    "Откройте discord.com/app в браузере, нажмите клавишу F12 (или Ctrl + Shift + I) и сверху выберите вкладку Network («Сеть»).\r\n\r\n" +
                    "ШАГ 2 · НАЙДИТЕ ЗАПРОС ВАШИХ ЗАДАНИЙ (@me)\r\n" +
                    "В строке фильтра напишите quests, затем в самом Discord слева откройте раздел «Путеводитель → Задания (Quests)». В списке запросов появится строка @me (или quests/@me).\r\n\r\n" +
                    "ШАГ 3 · СКОПИРУЙТЕ ТОКЕН ИЛИ JSON-ОТВЕТ\r\n" +
                    "• Для автосинхронизации каждые 15 секунд: откройте вкладку Headers запроса @me и скопируйте значение строки authorization.\r\n" +
                    "• Для разового импорта без токена: откройте вкладку Response, нажмите Ctrl + A → Ctrl + C и вставьте в окно «Подключить Discord».\r\n\r\n" +
                    "ШАГ 4 · ПОДКЛЮЧЕНИЕ ПРОКСИ (ДЛЯ РФ И РЕГИОНОВ С ОГРАНИЧЕНИЯМИ)\r\n" +
                    "• Где взять креды: в личном кабинете вашего прокси-сервиса (выберите протокол HTTP/HTTPS — там указаны IP, порт, логин и пароль) либо в настройках вашего VPN/Xray/Amnezia (локальный HTTP-порт, например 127.0.0.1:10808).\r\n" +
                    "• Как подключить: откройте «Подключить Discord →», в пункте 3 введите IP:PORT (или IP:PORT:LOGIN:PASS), при необходимости логин и пароль в соседние поля и нажмите «Сохранить и проверить прокси».\r\n" +
                    "• Важно: видео-квесты (WATCH_VIDEO) и активность (PLAY_ACTIVITY) работают полностью через встроенный прокси, а для игровых квестов (PLAY_ON_DESKTOP) сам клиент Discord на ПК тоже должен работать через VPN (в режиме TUN).\r\n\r\n" +
                    "ШАГ 5 · ОБРАТНАЯ СВЯЗЬ И БАГ-РЕПОРТЫ\r\n" +
                    "При обнаружении неисправностей свяжитесь с автором программы в Discord: hellwalk3rr"
                );
            }

            if (lblLicHead != null) { lblLicHead.Text = Loc.T("Лицензионное соглашение и Авторские права", "License Agreement & Copyright Notice"); lblLicHead.Invalidate(); }
            if (txtLicInline != null) txtLicInline.Text = Loc.IsEn ? "LICENSE AGREEMENT AND COPYRIGHT NOTICE\r\nSoftware: Discord Quest Studio\r\nAuthor & Copyright Holder: hellwalk3rr (Discord: hellwalk3rr)\r\n© 2026 hellwalk3rr. All Rights Reserved.\r\n\r\nBy installing and using «Discord Quest Studio», you agree to the following terms:\r\n\r\n1. COPYRIGHT\r\n1.1. All copyrights to «Discord Quest Studio» belong to its creator — hellwalk3rr (Discord contact: hellwalk3rr).\r\n1.2. Users are granted a free, non-exclusive license to install and use the software for personal, non-commercial purposes on Microsoft Windows PCs.\r\n\r\n2. STRICT PROHIBITION OF SALE & COMMERCIAL USE\r\n2.1. «Discord Quest Studio» is distributed strictly free of charge.\r\n2.2. Users and third parties are strictly prohibited from:\r\n  • selling, reselling, renting, or distributing the software for any fee;\r\n  • bundling the software into paid subscriptions, private paid channels, or commercial packs;\r\n  • charging money for download links or installation assistance.\r\n\r\n3. ATTRIBUTION\r\n3.1. Removing, hiding, or altering author credits (hellwalk3rr) inside the app or installer is prohibited.\r\n3.2. Any redistribution must credit the author (Discord: hellwalk3rr) and remain completely free.\r\n\r\n4. SECURITY & DISCLAIMER\r\n4.1. The software runs locally on your PC. Your authorization token is encrypted using PBKDF2-HMAC-SHA256 and stored only on your device (~/.discord_quest_studio/vault.dat).\r\n4.2. The software is provided «AS IS». For questions or bug reports, contact the author on Discord: hellwalk3rr." : "ЛИЦЕНЗИОННОЕ СОГЛАШЕНИЕ И АВТОРСКИЕ ПРАВА\r\nПрограмма: Discord Quest Studio\r\nАвтор и правообладатель: hellwalk3rr (Discord: hellwalk3rr)\r\n© 2026 hellwalk3rr. Все права защищены.\r\n\r\nУстановка и использование программы «Discord Quest Studio» означают ваше согласие со следующими условиями:\r\n\r\n1. АВТОРСКИЕ ПРАВА\r\n1.1. Все авторские права на программу «Discord Quest Studio» принадлежат её создателю — hellwalk3rr (Discord для связи: hellwalk3rr).\r\n1.2. Пользователю предоставляется бесплатное право на установку и личное некоммерческое использование программы на компьютерах под управлением ОС Windows.\r\n\r\n2. ЗАПРЕТ НА ПРОДАЖУ И КОММЕРЧЕСКОЕ ИСПОЛЬЗОВАНИЕ\r\n2.1. Программа «Discord Quest Studio» распространяется исключительно бесплатно в открытом доступе.\r\n2.2. Пользователям и третьим лицам строго запрещается:\r\n  • продавать, перепродавать, сдавать в аренду или распространять программу за любую плату;\r\n  • включать программу в состав платных подписок, закрытых платных каналов или коммерческих сборок;\r\n  • требовать оплату за предоставление ссылки на скачивание или за установку программы.\r\n\r\n3. СОХРАНЕНИЕ УКАЗАНИЯ АВТОРСТВА\r\n3.1. Запрещается удалять, скрывать или изменять сведения об авторе (hellwalk3rr) внутри программы и установщика.\r\n3.2. При публикации программы на сторонних ресурсах обязательно указание автора (Discord: hellwalk3rr) и сохранение бесплатного статуса проекта.\r\n\r\n4. БЕЗОПАСНОСТЬ И ОТКАЗ ОТ ОТВЕТСТВЕННОСТИ\r\n4.1. Программа работает локально на компьютере пользователя. Токен авторизации шифруется по стандарту PBKDF2-HMAC-SHA256 и хранится только на вашем устройстве (~/.discord_quest_studio/vault.dat).\r\n4.2. Программа предоставляется по принципу «как есть» (AS IS). По всем вопросам и баг-репортам обращайтесь к автору в Discord: hellwalk3rr.";

            if (miTrayOpen != null) miTrayOpen.Text = Loc.T("Открыть Discord Quest Studio", "Open Discord Quest Studio");
            if (miTrayStop != null) miTrayStop.Text = Loc.T("Аварийный стоп эмуляции", "Emergency Stop Emulation");
            if (miTrayExit != null) miTrayExit.Text = Loc.T("Выход", "Exit");

            UpdateOrbsCounters();
            RefreshQuestTable();
            RefreshProcessesPanel();
            if (detectableGames.Count > 0 && lblDbStatus != null)
            {
                NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
                nfi.NumberGroupSeparator = " ";
                lblDbStatus.Text = Loc.T("База Discord API: ", "Discord API DB: ") + detectableGames.Count.ToString("#,0", nfi) + Loc.T(" игр", " games");
            }
            if (selectedQuest == null && lblSpotTitle != null)
            {
                lblSpotBadge.Text = Loc.T("● Система готова", "● System Ready");
                lblSpotTitle.Text = Loc.T("Выберите задание", "Select a Quest");
                lblSpotSub.Text = Loc.T("Кликните по квесту в таблице или выберите игру в каталоге API", "Click a quest in the table or choose a game from the API catalog");
                if (lblSmartProgTitle != null) lblSmartProgTitle.Text = Loc.T("Прогресс задания в Discord: 0%", "Discord Quest Progress: 0%");
                if (lblSmartProgRem != null) lblSmartProgRem.Text = Loc.T("Осталось: 15 мин. (+ хвост)", "Remaining: 15 min (+ tail)");
            }
            else if (selectedQuest != null)
            {
                OnQuestSelected(selectedQuest);
            }
        }

        private void AdjustNavBarLayout()
        {
            if (navInnerPanel == null || navCenterTabs == null || navRightActions == null || bugBtn == null || killBtn == null) return;
            int tabsNeedW = 0;
            foreach (Control c in navCenterTabs.Controls)
            {
                tabsNeedW += c.Width + c.Margin.Left + c.Margin.Right;
            }
            int leftUsed = Dpi.S(194) + Dpi.S(18) + tabsNeedW + Dpi.S(24);
            int availForRight = navInnerPanel.ClientSize.Width - leftUsed;
            int curRightW = navRightActions.Width;

            bool shouldCompact = (availForRight < curRightW) || (navInnerPanel.ClientSize.Width < Dpi.S(1250));
            if (shouldCompact != navCompactMode)
            {
                navCompactMode = shouldCompact;
                if (navCompactMode)
                {
                    bugBtn.Text = "DS: hellwalk3rr";
                    killBtn.Text = Loc.T("СТОП", "STOP");
                }
                else
                {
                    bugBtn.Text = navCompactMode ? "DS: hellwalk3rr" : Loc.T("Баг-репорт · DS: hellwalk3rr", "Bug Report · DS: hellwalk3rr");
                    killBtn.Text = Loc.T("АВАРИЙНЫЙ СТОП", "EMERGENCY STOP");
                }
                bugBtn.AutoSizeToText(24, 34);
                killBtn.AutoSizeToText(24, 34);
                bugBtn.Invalidate();
                killBtn.Invalidate();
            }
        }

        private void SwitchView(string view)
        {
            bool isQuests = (view == "quests");
            bool isGames = (view == "games");
            bool isGuide = (view == "guide");
            bool isLic = (view == "license");

            if (questsViewPanel != null) questsViewPanel.Visible = isQuests;
            if (gamesViewPanel != null) gamesViewPanel.Visible = isGames;
            if (guideViewPanel != null) guideViewPanel.Visible = isGuide;
            if (licenseViewPanel != null) licenseViewPanel.Visible = isLic;

            if (tabQuestsBtn != null) tabQuestsBtn.SetNavActive(isQuests);
            if (tabGamesBtn != null) tabGamesBtn.SetNavActive(isGames);
            if (guideNavBtn != null) guideNavBtn.SetNavActive(isGuide);
            if (licNavBtn != null) licNavBtn.SetNavActive(isLic);

            if (isGames && gamesListView != null && gamesListView.ItemCount == 0)
            {
                FilterDetectableGames();
            }
        }

        private void RefreshAccountBadge()
        {
            if (accountBtn == null) return;
            if (string.IsNullOrEmpty(discordToken))
            {
                tokenStatus = "none";
                accountBtn.StatusDotColor = Color.FromArgb(142, 150, 164);
                accountBtn.FillNormal = Gold;
                accountBtn.FillHover = Color.FromArgb(255, 218, 68);
                accountBtn.BorderNormal = Gold;
                accountBtn.TextColor = Color.FromArgb(11, 12, 14);
                accountBtn.Text = Loc.T("Подключить Discord  →", "Connect Discord  →");
            }
            else if (tokenStatus == "expired")
            {
                accountBtn.StatusDotColor = Color.FromArgb(255, 76, 92);
                accountBtn.FillNormal = Color.FromArgb(44, 18, 22);
                accountBtn.FillHover = Color.FromArgb(68, 24, 32);
                accountBtn.BorderNormal = Color.FromArgb(120, 44, 54);
                accountBtn.TextColor = Color.FromArgb(255, 130, 130);
                accountBtn.Text = Loc.T("Токен истёк (401)  →", "Token Expired (401)  →");
            }
            else if (tokenStatus == "online")
            {
                accountBtn.StatusDotColor = Color.FromArgb(46, 204, 113);
                accountBtn.FillNormal = Gold;
                accountBtn.FillHover = Color.FromArgb(255, 218, 68);
                accountBtn.BorderNormal = Gold;
                accountBtn.TextColor = Color.FromArgb(11, 12, 14);
                accountBtn.Text = (!string.IsNullOrEmpty(discordUsername) ? discordUsername : Loc.T("Онлайн", "Online")) + "  →";
            }
            else
            {
                accountBtn.StatusDotColor = Color.FromArgb(245, 197, 24);
                accountBtn.FillNormal = Gold;
                accountBtn.FillHover = Color.FromArgb(255, 218, 68);
                accountBtn.BorderNormal = Gold;
                accountBtn.TextColor = Color.FromArgb(11, 12, 14);
                accountBtn.Text = (!string.IsNullOrEmpty(discordUsername) ? discordUsername : Loc.T("Токен подключен", "Token Connected")) + "  →";
            }
            accountBtn.AutoSizeToText(28, 34);
            accountBtn.Invalidate();
        }

        private int CalcOrbs(int baseOrbs)
        {
            if (baseOrbs <= 0) return 0;
            return nitroBonus ? (int)Math.Round(baseOrbs * 1.2) : baseOrbs;
        }

        private bool IsQuestExpired(QuestItem q)
        {
            if (completedQuestIds.Contains(q.Id)) return false;
            if (q.ExpiresTs > 1000000)
            {
                double nowTs = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                return q.ExpiresTs < nowTs;
            }
            return false;
        }

        private void UpdateOrbsCounters()
        {
            int earnedQuests = 0;
            int availNow = 0;
            int doneCnt = 0;
            int availCnt = 0;

            foreach (QuestItem q in activeQuests)
            {
                int o = CalcOrbs(q.BaseOrbs);
                if (completedQuestIds.Contains(q.Id))
                {
                    doneCnt++;
                    earnedQuests += o;
                }
                else if (!IsQuestExpired(q))
                {
                    availCnt++;
                    availNow += o;
                }
            }

            int totalEarned = (serverOrbsBalance >= 0) ? Math.Max(0, serverOrbsBalance + manualOrbsOffset) : Math.Max(0, earnedQuests + manualOrbsOffset);
            NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberGroupSeparator = " ";

            if (lblEarnedOrbs != null) lblEarnedOrbs.Text = totalEarned.ToString("#,0", nfi) + " Orbs";
            if (lblAvailOrbs != null) lblAvailOrbs.Text = availNow.ToString("#,0", nfi) + " Orbs";
            if (lblQuestsCounts != null) lblQuestsCounts.Text = string.Format(Loc.T("Выполнено: {0} | Доступно: {1}", "Completed: {0} | Available: {1}"), doneCnt, availCnt);
        }

        private static string FormatTaskLabel(QuestItem q)
        {
            if (q == null) return "";
            if (q.IsVideoQuest())
            {
                return (q.TargetSec < 60)
                    ? Loc.T("Видео " + q.TargetSec + " сек.", "Video " + q.TargetSec + "s")
                    : Loc.T("Видео " + q.DurationMin + " мин.", "Video " + q.DurationMin + " min");
            }
            if (!string.IsNullOrEmpty(q.TaskType) && q.TaskType.IndexOf("STREAM", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Loc.T("Стрим " + q.DurationMin + " мин.", "Stream " + q.DurationMin + " min");
            }
            return Loc.T("Играть " + q.DurationMin + " мин.", "Play " + q.DurationMin + " min");
        }

        private int GetStatusRankFast(QuestItem q, HashSet<string> runningIds, double nowTs)
        {
            if (runningIds.Contains(q.Id)) return 0;
            if (completedQuestIds.Contains(q.Id)) return 2;
            if (q.ExpiresTs > 1000000 && q.ExpiresTs < nowTs) return 3;
            return 1;
        }

        private void RefreshQuestTable()
        {
            if (questTable == null) return;
            questTable.SortColumn = sortColumn;
            questTable.SortDesc = sortDescending;

            double nowTs = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

            HashSet<string> runningIds = new HashSet<string>();
            Dictionary<string, ActiveSpoofItem> runningByQid = new Dictionary<string, ActiveSpoofItem>();
            foreach (ActiveSpoofItem ap in activeProcesses)
            {
                if (ap.IsRunning() && !string.IsNullOrEmpty(ap.QuestId))
                {
                    runningIds.Add(ap.QuestId);
                    runningByQid[ap.QuestId] = ap;
                }
            }

            List<QuestItem> filtered = new List<QuestItem>(activeQuests.Count);
            Dictionary<string, int> statusRankCache = new Dictionary<string, int>(activeQuests.Count);
            foreach (QuestItem q in activeQuests)
            {
                bool isExp = !completedQuestIds.Contains(q.Id) && (q.ExpiresTs > 1000000 && q.ExpiresTs < nowTs);
                if (hideExpired && isExp) continue;
                filtered.Add(q);
                statusRankCache[q.Id] = GetStatusRankFast(q, runningIds, nowTs);
            }

            if (sortColumn == "reward" || sortColumn == "orbs")
            {
                List<QuestItem> withOrbs = new List<QuestItem>();
                List<QuestItem> others = new List<QuestItem>();
                foreach (QuestItem q in filtered)
                {
                    if (q.BaseOrbs > 0) withOrbs.Add(q);
                    else others.Add(q);
                }
                withOrbs.Sort((a, b) =>
                {
                    int cmp = a.BaseOrbs.CompareTo(b.BaseOrbs);
                    if (cmp != 0) return sortDescending ? -cmp : cmp;
                    return string.Compare(a.GameName, b.GameName, StringComparison.OrdinalIgnoreCase);
                });
                others.Sort((a, b) => string.Compare(a.GameName, b.GameName, StringComparison.OrdinalIgnoreCase));
                filtered.Clear();
                filtered.AddRange(withOrbs);
                filtered.AddRange(others);
            }
            else if (sortColumn == "status")
            {
                filtered.Sort((a, b) =>
                {
                    int ra = statusRankCache.ContainsKey(a.Id) ? statusRankCache[a.Id] : 1;
                    int rb = statusRankCache.ContainsKey(b.Id) ? statusRankCache[b.Id] : 1;
                    int cmp = ra.CompareTo(rb);
                    if (cmp != 0) return sortDescending ? -cmp : cmp;
                    return b.BaseOrbs.CompareTo(a.BaseOrbs);
                });
            }
            else if (sortColumn == "game")
            {
                filtered.Sort((a, b) =>
                {
                    int cmp = string.Compare(a.GameName, b.GameName, StringComparison.OrdinalIgnoreCase);
                    return sortDescending ? -cmp : cmp;
                });
            }
            else if (sortColumn == "expires")
            {
                filtered.Sort((a, b) =>
                {
                    double ta = a.ExpiresTs > 0 ? a.ExpiresTs : 9999999999.0;
                    double tb = b.ExpiresTs > 0 ? b.ExpiresTs : 9999999999.0;
                    int cmp = ta.CompareTo(tb);
                    return sortDescending ? -cmp : cmp;
                });
            }
            else if (sortColumn == "progress")
            {
                filtered.Sort((a, b) =>
                {
                    int cmp = a.ServerProgressPct.CompareTo(b.ServerProgressPct);
                    return sortDescending ? -cmp : cmp;
                });
            }

            List<QuestRowViewModel> vms = new List<QuestRowViewModel>(filtered.Count);
            foreach (QuestItem q in filtered)
            {
                QuestRowViewModel vm = new QuestRowViewModel();
                vm.Id = q.Id;
                vm.GameName = q.GameName;
                vm.CalcOrbs = CalcOrbs(q.BaseOrbs);
                vm.RewardDisplay = vm.CalcOrbs > 0 ? (vm.CalcOrbs + " Orbs") : q.RewardText;
                vm.Expires = q.Expires;
                vm.SourceQuest = q;

                bool isExp = !completedQuestIds.Contains(q.Id) && (q.ExpiresTs > 1000000 && q.ExpiresTs < nowTs);

                if (runningByQid.ContainsKey(q.Id))
                {
                    ActiveSpoofItem ap = runningByQid[q.Id];
                    vm.StatusLabel = Loc.T("Эмулируется", "Running");
                    vm.StatusKind = "running";
                    int pct = Math.Max(q.ServerProgressPct, ap.ProgressPct());
                    vm.ProgressDisplay = string.Format(Loc.T("{0}% (осталось {1})", "{0}% ({1} left)"), pct, FormatTime(ap.RemainingSeconds()));
                }
                else if (completedQuestIds.Contains(q.Id))
                {
                    vm.StatusLabel = Loc.T("Выполнено", "Done");
                    vm.StatusKind = "done";
                    vm.ProgressDisplay = string.Format("100% ({0})", FormatTaskLabel(q));
                }
                else if (isExp)
                {
                    vm.StatusLabel = Loc.T("Истекло", "Expired");
                    vm.StatusKind = "expired";
                    vm.ProgressDisplay = string.Format("{0}% ({1})", q.ServerProgressPct, FormatTaskLabel(q));
                }
                else
                {
                    vm.StatusLabel = Loc.T("Доступно", "Available");
                    vm.StatusKind = "avail";
                    vm.ProgressDisplay = string.Format("{0}% ({1})", q.ServerProgressPct, FormatTaskLabel(q));
                }
                vm.PillWidth = questTable.MeasureBadgePillWidth(vm.StatusLabel);
                vms.Add(vm);
            }

            questTable.SetRows(vms);
        }

        private void SyncSortDropdownSilently()
        {
            if (sortCombo == null) return;
            if (sortColumn == "reward" || sortColumn == "orbs")
            {
                sortCombo.SetSelectedIndexSilently(sortDescending ? 0 : 1);
            }
            else if (sortColumn == "status")
            {
                sortCombo.SetSelectedIndexSilently(sortDescending ? 3 : 2);
            }
            else if (sortColumn == "game")
            {
                sortCombo.SetSelectedIndexSilently(4);
            }
            else if (sortColumn == "expires")
            {
                sortCombo.SetSelectedIndexSilently(5);
            }
            else if (sortColumn == "progress")
            {
                sortCombo.SetSelectedIndexSilently(6);
            }
        }

        private void OnQuestColumnClicked(string colId)
        {
            if (sortColumn == colId) sortDescending = !sortDescending;
            else
            {
                sortColumn = colId;
                sortDescending = (colId == "reward" || colId == "progress");
            }
            SyncSortDropdownSilently();
            RefreshQuestTable();
            SaveState();
        }

        private void OnSortComboChanged(object sender, EventArgs e)
        {
            int idx = sortCombo.SelectedIndex;
            if (idx == 0) { sortColumn = "reward"; sortDescending = true; }
            else if (idx == 1) { sortColumn = "reward"; sortDescending = false; }
            else if (idx == 2) { sortColumn = "status"; sortDescending = false; }
            else if (idx == 3) { sortColumn = "status"; sortDescending = true; }
            else if (idx == 4) { sortColumn = "game"; sortDescending = false; }
            else if (idx == 5) { sortColumn = "expires"; sortDescending = false; }
            else if (idx == 6) { sortColumn = "progress"; sortDescending = true; }
            RefreshQuestTable();
            SaveState();
        }

        private void OnQuestSelected(QuestItem q)
        {
            selectedQuest = q;
            inpGameName.Value = q.GameName;
            inpExeRel.Value = !string.IsNullOrEmpty(q.ExeRelPath) ? q.ExeRelPath : MakeFallbackExe(q.GameName);
            inpSteamId.Value = q.SteamId;

            int remMin = q.RemainingMin > 0 ? q.RemainingMin : q.DurationMin;
            inpMinutes.Value = remMin.ToString();

            smartProgBar.Percent = q.ServerProgressPct;
            lblSmartProgTitle.Text = string.Format(Loc.T("Прогресс задания в Discord: {0}%", "Discord Quest Progress: {0}%"), q.ServerProgressPct);
            lblSmartProgRem.Text = q.ServerProgressPct >= 100
                ? Loc.T("Задание выполнено на 100%", "Quest 100% completed")
                : string.Format(Loc.T("Осталось: {0} мин. (+ хвост)", "Remaining: {0} min (+ tail)"), remMin);

            lblSpotTitle.Text = q.GameName;
            lblSpotSub.Text = string.Format(Loc.T("Прогресс: {0}% · Таймер: {1} мин.", "Progress: {0}% · Timer: {1} min"), q.ServerProgressPct, remMin);
            int orbs = CalcOrbs(q.BaseOrbs);
            lblSpotReward.Text = orbs > 0 ? (orbs + " Orbs") : q.RewardText;
        }

        private void ToggleSelectedQuestCompleted()
        {
            if (selectedQuest == null && string.IsNullOrEmpty(questTable.SelectedQuestId))
            {
                MessageBox.Show(this, Loc.T("Сначала выберите задание в таблице слева.", "Please select a quest in the table on the left first."), "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string qid = (selectedQuest != null) ? selectedQuest.Id : questTable.SelectedQuestId;
            if (completedQuestIds.Contains(qid)) { completedQuestIds.Remove(qid); sessionCompletedQuestIds.Remove(qid); }
            else { completedQuestIds.Add(qid); sessionCompletedQuestIds.Add(qid); }
            SaveState();
            UpdateOrbsCounters();
            RefreshQuestTable();
        }

        private static string MakeFallbackExe(string gameName)
        {
            MatchCollection matches = Regex.Matches(gameName ?? "Game", "[A-Za-z0-9]+");
            StringBuilder sb = new StringBuilder();
            int count = 0;
            foreach (Match m in matches)
            {
                string w = m.Value;
                if (w.Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(w[0]));
                    if (w.Length > 1) sb.Append(w.Substring(1).ToLowerInvariant());
                    count++;
                    if (count >= 4) break;
                }
            }
            string pascal = sb.Length > 0 ? sb.ToString() : "GameClient";
            return pascal + "/" + pascal + ".exe";
        }

        private void LoadDetectableGamesDatabase(bool forceOnline)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = int.MaxValue;
            List<DetectableGameItem> loaded = new List<DetectableGameItem>();

            if (!forceOnline && File.Exists(cacheFilePath))
            {
                try
                {
                    object raw = ser.DeserializeObject(File.ReadAllText(cacheFilePath, Encoding.UTF8));
                    loaded = ParseDetectableList(raw);
                }
                catch { }
            }

            if (loaded.Count == 0 || forceOnline)
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/applications/detectable");
                    ApplyProxy(req);
                    req.Method = "GET";
                    req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
                    req.Accept = "application/json";
                    req.Timeout = 20000;
                    if (loaded.Count > 0 && File.Exists(etagFilePath))
                    {
                        string savedEtag = File.ReadAllText(etagFilePath, Encoding.UTF8).Trim();
                        if (!string.IsNullOrEmpty(savedEtag))
                        {
                            req.Headers["If-None-Match"] = savedEtag;
                        }
                    }
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = sr.ReadToEnd();
                        File.WriteAllText(cacheFilePath, json, Encoding.UTF8);
                        string newEtag = resp.Headers["ETag"];
                        if (!string.IsNullOrEmpty(newEtag)) File.WriteAllText(etagFilePath, newEtag, Encoding.UTF8);
                        loaded = ParseDetectableList(ser.DeserializeObject(json));
                    }
                }
                catch (WebException wex)
                {
                    HttpWebResponse er = wex.Response as HttpWebResponse;
                    if (er != null && er.StatusCode == HttpStatusCode.NotModified && loaded.Count > 0)
                    {
                        // 304 Not Modified: keep cached detectableGames without re-downloading
                    }
                }
                catch { }
            }

            detectableGames = loaded;
            if (this.IsHandleCreated)
            {
                this.BeginInvoke(new Action(() =>
                {
                    NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
                    nfi.NumberGroupSeparator = " ";
                    lblDbStatus.Text = detectableGames.Count > 0
                        ? Loc.T("База Discord API: " + detectableGames.Count.ToString("#,0", nfi) + " игр", "Discord API DB: " + detectableGames.Count.ToString("#,0", nfi) + " games")
                        : Loc.T("База Discord API: автономный режим", "Discord API DB: offline mode");

                    foreach (QuestItem q in activeQuests)
                    {
                        if (string.IsNullOrEmpty(q.ExeRelPath))
                        {
                            q.ExeRelPath = ResolveExecutableForQuest(q.GameName, q.AppId);
                        }
                    }
                    FilterDetectableGames();
                }));
            }
        }

        private List<DetectableGameItem> ParseDetectableList(object rawObj)
        {
            List<DetectableGameItem> list = new List<DetectableGameItem>();
            IEnumerable arr = rawObj as IEnumerable;
            if (arr == null) return list;
            foreach (object item in arr)
            {
                Dictionary<string, object> d = item as Dictionary<string, object>;
                if (d == null) continue;
                DetectableGameItem g = new DetectableGameItem();
                if (d.ContainsKey("id") && d["id"] != null) g.Id = d["id"].ToString();
                if (d.ContainsKey("name") && d["name"] != null) g.Name = d["name"].ToString();
                if (d.ContainsKey("executables") && d["executables"] is IEnumerable)
                {
                    foreach (object eo in (IEnumerable)d["executables"])
                    {
                        Dictionary<string, object> ed = eo as Dictionary<string, object>;
                        if (ed != null && ed.ContainsKey("os") && ed["os"] != null && ed["os"].ToString() == "win32")
                        {
                            if (ed.ContainsKey("name") && ed["name"] != null)
                            {
                                g.ExePath = ed["name"].ToString().TrimStart('/', '\\');
                                break;
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(g.Name)) list.Add(g);
            }
            return list;
        }

        private string ResolveExecutableForQuest(string gameName, string appId)
        {
            if (!string.IsNullOrEmpty(appId))
            {
                foreach (DetectableGameItem g in detectableGames)
                {
                    if (g.Id == appId && !string.IsNullOrEmpty(g.ExePath)) return g.ExePath;
                }
            }
            if (!string.IsNullOrEmpty(gameName))
            {
                foreach (DetectableGameItem g in detectableGames)
                {
                    if (string.Equals(g.Name, gameName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(g.ExePath))
                    {
                        return g.ExePath;
                    }
                }
            }
            return MakeFallbackExe(gameName);
        }

        private void FilterDetectableGames()
        {
            if (gamesListView == null) return;
            string q = (searchGameInput != null ? searchGameInput.Value : "").Trim().ToLowerInvariant();
            List<DetectableGameItem> filtered = new List<DetectableGameItem>(250);
            foreach (DetectableGameItem g in detectableGames)
            {
                if (!string.IsNullOrEmpty(q))
                {
                    bool match = g.Name.ToLowerInvariant().Contains(q) || (!string.IsNullOrEmpty(g.ExePath) && g.ExePath.ToLowerInvariant().Contains(q));
                    if (!match) continue;
                }
                DetectableGameItem copy = new DetectableGameItem();
                copy.Id = g.Id;
                copy.Name = g.Name;
                copy.ExePath = !string.IsNullOrEmpty(g.ExePath) ? g.ExePath : MakeFallbackExe(g.Name);
                copy.SteamId = g.SteamId;
                filtered.Add(copy);
                if (filtered.Count >= 250) break;
            }
            gamesListView.SetItems(filtered);
        }

        private void OnCatalogGameSelected(DetectableGameItem g)
        {
            if (g == null) return;
            selectedQuest = null;
            inpGameName.Value = g.Name;
            inpExeRel.Value = !string.IsNullOrEmpty(g.ExePath) ? g.ExePath : MakeFallbackExe(g.Name);
            inpSteamId.Value = g.Id;
            inpMinutes.Value = "15";
            smartProgBar.Percent = 0;
            lblSmartProgTitle.Text = Loc.T("Выбрана игра из каталога Discord API", "Selected game from Discord API catalog");
            lblSmartProgRem.Text = Loc.T("Таймер: 15 мин. (+ хвост)", "Timer: 15 min (+ tail)");
            SwitchView("quests");
        }

        private void ManualSyncQuests()
        {
            if (string.IsNullOrEmpty(discordToken))
            {
                OpenAccountDialog();
                return;
            }
            Thread t = new Thread(() => PollDiscordApi(true));
            t.IsBackground = true;
            t.Start();
        }

        private void ApplyProxy(HttpWebRequest req)
        {
            if (req == null || string.IsNullOrEmpty(proxyHost)) return;
            try
            {
                string raw = proxyHost.Trim();
                if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) raw = raw.Substring(7);
                else if (raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) raw = raw.Substring(8);

                string hostPart = raw;
                string uPart = proxyUser;
                string pPart = proxyPass;
                string[] segs = raw.Split(':');
                if (segs.Length >= 4)
                {
                    hostPart = segs[0] + ":" + segs[1];
                    if (string.IsNullOrEmpty(uPart)) uPart = segs[2];
                    if (string.IsNullOrEmpty(pPart)) pPart = segs[3];
                }

                WebProxy wp = new WebProxy("http://" + hostPart);
                if (!string.IsNullOrEmpty(uPart))
                {
                    wp.Credentials = new NetworkCredential(uPart, pPart ?? "");
                }
                req.Proxy = wp;
            }
            catch { }
        }

        private static readonly string DesktopSuperPropsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            "{\"os\":\"Windows\",\"browser\":\"Discord Client\",\"release_channel\":\"stable\",\"client_version\":\"1.0.9175\",\"os_version\":\"10.0.22631\",\"os_arch\":\"x64\",\"app_arch\":\"x64\",\"system_locale\":\"en-US\",\"browser_user_agent\":\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) discord/1.0.9175 Chrome/128.0.6613.186 Electron/32.2.7 Safari/537.36\",\"browser_version\":\"32.2.7\",\"client_build_number\":366955,\"native_build_number\":58405,\"client_event_source\":null}"
        ));

        private void ApplyDiscordHeaders(HttpWebRequest req, string tok, bool includeDesktopProps = false)
        {
            ApplyProxy(req);
            req.Method = "GET";
            req.Headers["Authorization"] = tok;
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) discord/1.0.9175 Chrome/128.0.6613.186 Electron/32.2.7 Safari/537.36";
            req.Accept = "*/*";
            req.Headers["Accept-Language"] = "en-US,en;q=0.9";
            req.Headers["X-Discord-Locale"] = "en-US";
            if (includeDesktopProps)
            {
                req.Headers["X-Super-Properties"] = DesktopSuperPropsBase64;
            }
            req.Headers["Origin"] = "https://discord.com";
            req.Referer = "https://discord.com/channels/@me";
        }

        private void PollDiscordApi(bool showErrors)
        {
            if (string.IsNullOrEmpty(discordToken)) return;
            string tok = discordToken.Trim().Trim('\"', '\'');
            if (tok.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                tok = tok.Substring(7).Trim();
            }

            try
            {
                HttpWebRequest reqU = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/users/@me");
                ApplyDiscordHeaders(reqU, tok);
                reqU.Timeout = 10000;
                using (HttpWebResponse respU = (HttpWebResponse)reqU.GetResponse())
                using (StreamReader srU = new StreamReader(respU.GetResponseStream(), Encoding.UTF8))
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    Dictionary<string, object> ud = ser.DeserializeObject(srU.ReadToEnd()) as Dictionary<string, object>;
                    if (ud != null)
                    {
                        if (ud.ContainsKey("global_name") && ud["global_name"] != null) discordUsername = ud["global_name"].ToString();
                        else if (ud.ContainsKey("username") && ud["username"] != null) discordUsername = ud["username"].ToString();
                    }
                }
            }
            catch { }

            try
            {
                HttpWebRequest reqBal = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/users/@me/virtual-currency/balance");
                ApplyDiscordHeaders(reqBal, tok, false);
                reqBal.Timeout = 8000;
                using (HttpWebResponse respBal = (HttpWebResponse)reqBal.GetResponse())
                using (StreamReader srBal = new StreamReader(respBal.GetResponseStream(), Encoding.UTF8))
                {
                    JavaScriptSerializer serBal = new JavaScriptSerializer();
                    Dictionary<string, object> bd = serBal.DeserializeObject(srBal.ReadToEnd()) as Dictionary<string, object>;
                    if (bd != null && bd.ContainsKey("balance") && bd["balance"] != null)
                    {
                        serverOrbsBalance = Convert.ToInt32(bd["balance"]);
                    }
                }
            }
            catch { }

            try
            {
                HttpWebRequest reqQ = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/quests/@me");
                ApplyDiscordHeaders(reqQ, tok, false);
                reqQ.Timeout = 12000;
                using (HttpWebResponse respQ = (HttpWebResponse)reqQ.GetResponse())
                using (StreamReader srQ = new StreamReader(respQ.GetResponseStream(), Encoding.UTF8))
                {
                    string qJson = srQ.ReadToEnd();
                    if (qJson.IndexOf("\"quests\":[]", StringComparison.OrdinalIgnoreCase) >= 0 || qJson.IndexOf("\"quests\": []", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try
                        {
                            HttpWebRequest reqDec = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/quests/get-decisions?placement=3&num_decisions_requested=15");
                            ApplyDiscordHeaders(reqDec, tok, true);
                            reqDec.Timeout = 8000;
                            using (HttpWebResponse respDec = (HttpWebResponse)reqDec.GetResponse())
                            using (StreamReader srDec = new StreamReader(respDec.GetResponseStream(), Encoding.UTF8))
                            {
                                string decJson = srDec.ReadToEnd();
                                JavaScriptSerializer ser = new JavaScriptSerializer();
                                Dictionary<string, object> decRoot = ser.DeserializeObject(decJson) as Dictionary<string, object>;
                                if (decRoot != null && decRoot.ContainsKey("decisions") && decRoot["decisions"] is IEnumerable)
                                {
                                    List<object> convQuests = new List<object>();
                                    foreach (object dObj in (IEnumerable)decRoot["decisions"])
                                    {
                                        Dictionary<string, object> dDict = dObj as Dictionary<string, object>;
                                        if (dDict == null || !dDict.ContainsKey("creative")) continue;
                                        Dictionary<string, object> cr = dDict["creative"] as Dictionary<string, object>;
                                        if (cr == null || !cr.ContainsKey("creative_content")) continue;
                                        Dictionary<string, object> cc = cr["creative_content"] as Dictionary<string, object>;
                                        if (cc == null || !cc.ContainsKey("id")) continue;
                                        Dictionary<string, object> qWrap = new Dictionary<string, object>();
                                        qWrap["id"] = cc["id"];
                                        qWrap["config"] = cc;
                                        qWrap["user_status"] = null;
                                        if (dDict.ContainsKey("traffic_metadata_sealed")) qWrap["traffic_metadata_sealed"] = dDict["traffic_metadata_sealed"];
                                        if (dDict.ContainsKey("metadata_sealed")) qWrap["metadata_sealed"] = dDict["metadata_sealed"];
                                        convQuests.Add(qWrap);
                                    }
                                    if (convQuests.Count > 0)
                                    {
                                        Dictionary<string, object> merged = new Dictionary<string, object>();
                                        merged["quests"] = convQuests;
                                        qJson = ser.Serialize(merged);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            tokenStatus = "online";
                            SaveTokenVault();
                            int loadedCount = ApplyQuestsJsonPayload(qJson);
                            RefreshAccountBadge();
                            Thread autoEnrollTh = new Thread(() => AutoEnrollAllActiveQuests());
                            autoEnrollTh.IsBackground = true;
                            autoEnrollTh.Start();
                            if (showErrors && loadedCount == 0)
                            {
                                MessageBox.Show(
                                    this,
                                    Loc.T("Discord API вернул 0 заданий для этого аккаунта (список пуст).\r\n\r\nВозможные причины:\r\n• На аккаунте или вашем IP действует региональное ограничение (например, РФ) — подключите зарубежный HTTP/HTTPS-прокси в окне «Подключить Discord».\r\n• Для этого аккаунта сейчас нет доступных рекламных кампаний.", "Discord API returned 0 quests for this account (list is empty).\r\n\r\nPossible reasons:\r\n• Regional restriction on this account or IP (e.g., Russia) — connect a foreign HTTP/HTTPS proxy in «Connect Discord».\r\n• No active quest campaigns are currently assigned to this account."),
                                    "Discord Quest Studio",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information
                                );
                            }
                        }));
                    }
                }
            }
            catch (WebException wex)
            {
                HttpWebResponse errResp = wex.Response as HttpWebResponse;
                bool is401 = (errResp != null && errResp.StatusCode == HttpStatusCode.Unauthorized);
                if (is401 && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        tokenStatus = "expired";
                        RefreshAccountBadge();
                    }));
                }
                if (showErrors && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (is401)
                        {
                            DialogResult dr = MessageBox.Show(
                                this,
                                Loc.T("Срок действия вашего токена Discord истёк или он был сброшен (ошибка 401).\r\n\r\nОткрыть окно «Подключить Discord», чтобы вставить свежий токен?", "Your Discord token has expired or was revoked (HTTP 401).\r\n\r\nOpen «Connect Discord» to paste a fresh token?"),
                                "Discord Quest Studio",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning
                            );
                            if (dr == DialogResult.Yes)
                            {
                                OpenAccountDialog();
                            }
                        }
                        else
                        {
                            MessageBox.Show(this, Loc.T("Не удалось получить список заданий из Discord API:\r\n", "Failed to fetch quests from Discord API:\r\n") + wex.Message, "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                if (showErrors && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show(this, Loc.T("Не удалось получить список заданий из Discord API:\r\n", "Failed to fetch quests from Discord API:\r\n") + ex.Message, "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }));
                }
            }
        }

        private void ResetQuestsProgressForNewAccount()
        {
            completedQuestIds.Clear();
            sessionCompletedQuestIds.Clear();
            manualOrbsOffset = 0;
            serverOrbsBalance = -1;
            for (int i = activeQuests.Count - 1; i >= 0; i--)
            {
                QuestItem q = activeQuests[i];
                if (q == null || IsQuestExpired(q))
                {
                    activeQuests.RemoveAt(i);
                    continue;
                }
                q.IsEnrolled = false;
                q.DoneSec = 0;
                q.ServerProgressPct = 0;
                q.RemainingMin = Math.Max(1, q.DurationMin);
            }
            UpdateOrbsCounters();
            RefreshQuestTable();
        }

        private int ApplyQuestsJsonPayload(string rawJson)
        {
            List<QuestItem> previousActive = new List<QuestItem>();
            foreach (QuestItem pq in activeQuests)
            {
                if (pq != null && !IsQuestExpired(pq)) previousActive.Add(pq);
            }

            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = int.MaxValue;
            object rootObj = ser.DeserializeObject(rawJson);
            IEnumerable questsList = null;

            Dictionary<string, object> rootDict = rootObj as Dictionary<string, object>;
            if (rootDict != null && rootDict.ContainsKey("quests") && rootDict["quests"] is IEnumerable)
            {
                questsList = (IEnumerable)rootDict["quests"];
            }
            else if (rootObj is IEnumerable)
            {
                questsList = (IEnumerable)rootObj;
            }
            if (questsList == null) return 0;

            completedQuestIds.Clear();
            foreach (string sid in sessionCompletedQuestIds) completedQuestIds.Add(sid);
            List<QuestItem> parsed = new List<QuestItem>();
            foreach (object qo in questsList)
            {
                Dictionary<string, object> q = qo as Dictionary<string, object>;
                if (q == null) continue;

                QuestItem item = new QuestItem();
                item.Id = (q.ContainsKey("id") && q["id"] != null) ? q["id"].ToString() : ("quest_" + parsed.Count);
                if (q.ContainsKey("traffic_metadata_sealed") && q["traffic_metadata_sealed"] != null) item.TrafficMetadataSealed = q["traffic_metadata_sealed"].ToString();
                if (q.ContainsKey("metadata_sealed") && q["metadata_sealed"] != null) item.MetadataSealed = q["metadata_sealed"].ToString();

                Dictionary<string, object> cfg = (q.ContainsKey("config") ? q["config"] : null) as Dictionary<string, object>;
                if (cfg != null)
                {
                    Dictionary<string, object> msgs = (cfg.ContainsKey("messages") ? cfg["messages"] : null) as Dictionary<string, object>;
                    Dictionary<string, object> appBlock = (cfg.ContainsKey("application") ? cfg["application"] : null) as Dictionary<string, object>;

                    if (appBlock != null && appBlock.ContainsKey("id") && appBlock["id"] != null) item.AppId = appBlock["id"].ToString();

                    if (msgs != null && msgs.ContainsKey("game_title") && msgs["game_title"] != null) item.GameName = msgs["game_title"].ToString();
                    else if (appBlock != null && appBlock.ContainsKey("name") && appBlock["name"] != null) item.GameName = appBlock["name"].ToString();
                    else if (msgs != null && msgs.ContainsKey("quest_name") && msgs["quest_name"] != null) item.GameName = msgs["quest_name"].ToString();
                    else item.GameName = "Discord Quest";

                    item.QuestName = (msgs != null && msgs.ContainsKey("quest_name") && msgs["quest_name"] != null) ? msgs["quest_name"].ToString() : item.GameName;

                    if (cfg.ContainsKey("expires_at") && cfg["expires_at"] != null)
                    {
                        DateTime dt;
                        if (DateTime.TryParse(cfg["expires_at"].ToString(), out dt))
                        {
                            item.Expires = dt.ToLocalTime().ToString("dd.MM");
                            item.ExpiresTs = (dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                        }
                    }

                    Dictionary<string, object> taskCfg = (cfg.ContainsKey("task_config") ? cfg["task_config"] : (cfg.ContainsKey("task_config_v2") ? cfg["task_config_v2"] : null)) as Dictionary<string, object>;
                    if (taskCfg != null && taskCfg.ContainsKey("tasks") && taskCfg["tasks"] is Dictionary<string, object>)
                    {
                        Dictionary<string, object> tasksDict = (Dictionary<string, object>)taskCfg["tasks"];
                        foreach (KeyValuePair<string, object> kv in tasksDict)
                        {
                            item.TaskType = kv.Key;
                            Dictionary<string, object> tv = kv.Value as Dictionary<string, object>;
                            if (tv != null && tv.ContainsKey("target") && tv["target"] != null)
                            {
                                item.TargetSec = Math.Max(1, Convert.ToInt32(tv["target"]));
                            }
                            break;
                        }
                    }

                    Dictionary<string, object> rewCfg = (cfg.ContainsKey("rewards_config") ? cfg["rewards_config"] : null) as Dictionary<string, object>;
                    if (rewCfg != null && rewCfg.ContainsKey("rewards") && rewCfg["rewards"] is IEnumerable)
                    {
                        foreach (object ro in (IEnumerable)rewCfg["rewards"])
                        {
                            Dictionary<string, object> rd = ro as Dictionary<string, object>;
                            if (rd == null) continue;
                            if (rd.ContainsKey("orb_quantity") && rd["orb_quantity"] != null)
                            {
                                item.BaseOrbs = Convert.ToInt32(rd["orb_quantity"]);
                                item.RewardText = item.BaseOrbs + " Orbs";
                                break;
                            }
                            Dictionary<string, object> rMsgs = (rd.ContainsKey("messages") ? rd["messages"] : null) as Dictionary<string, object>;
                            if (rMsgs != null && rMsgs.ContainsKey("name") && rMsgs["name"] != null)
                            {
                                string rName = rMsgs["name"].ToString();
                                Match m = Regex.Match(rName, @"(\d+)\s*Orbs", RegexOptions.IgnoreCase);
                                if (m.Success)
                                {
                                    item.BaseOrbs = int.Parse(m.Groups[1].Value);
                                    item.RewardText = item.BaseOrbs + " Orbs";
                                }
                                else
                                {
                                    item.RewardText = rName;
                                }
                                break;
                            }
                        }
                    }
                }

                Dictionary<string, object> uStatus = (q.ContainsKey("user_status") ? q["user_status"] : null) as Dictionary<string, object>;
                if (uStatus != null)
                {
                    if (uStatus.ContainsKey("enrolled_at") && uStatus["enrolled_at"] != null)
                    {
                        item.IsEnrolled = true;
                    }
                    if (uStatus.ContainsKey("completed_at") && uStatus["completed_at"] != null)
                    {
                        completedQuestIds.Add(item.Id);
                    }
                    if (uStatus.ContainsKey("claimed_at") && uStatus["claimed_at"] != null)
                    {
                        item.IsClaimed = true;
                    }
                    if (uStatus.ContainsKey("progress") && uStatus["progress"] is Dictionary<string, object>)
                    {
                        Dictionary<string, object> progDict = (Dictionary<string, object>)uStatus["progress"];
                        foreach (object pv in progDict.Values)
                        {
                            Dictionary<string, object> pvd = pv as Dictionary<string, object>;
                            if (pvd != null && pvd.ContainsKey("value") && pvd["value"] != null)
                            {
                                int v = Convert.ToInt32(pvd["value"]);
                                if (v > item.DoneSec) item.DoneSec = v;
                            }
                        }
                    }
                }

                item.DurationMin = Math.Max(1, (int)Math.Round(item.TargetSec / 60.0));
                if (item.IsVideoQuest())
                {
                    item.TaskLabel = (item.TargetSec < 60)
                        ? ("Видео " + item.TargetSec + " сек.")
                        : ("Видео " + item.DurationMin + " мин.");
                }
                else if (!string.IsNullOrEmpty(item.TaskType) && item.TaskType.IndexOf("STREAM", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    item.TaskLabel = "Стрим " + item.DurationMin + " мин.";
                }
                else
                {
                    item.TaskLabel = "Играть " + item.DurationMin + " мин.";
                }

                if (completedQuestIds.Contains(item.Id))
                {
                    item.ServerProgressPct = 100;
                    item.RemainingMin = 1;
                }
                else
                {
                    item.ServerProgressPct = item.DoneSec > 0 ? Math.Min(99, (int)((item.DoneSec * 100.0) / Math.Max(1, item.TargetSec))) : 0;
                    int remSec = Math.Max(60, item.TargetSec - item.DoneSec);
                    item.RemainingMin = Math.Max(1, (int)Math.Ceiling(remSec / 60.0));
                }

                item.ExeRelPath = ResolveExecutableForQuest(item.GameName, item.AppId);
                parsed.Add(item);
            }

            HashSet<string> parsedIds = new HashSet<string>();
            foreach (QuestItem pi in parsed) parsedIds.Add(pi.Id);
            foreach (QuestItem prev in previousActive)
            {
                if (!parsedIds.Contains(prev.Id) && !IsQuestExpired(prev))
                {
                    prev.IsEnrolled = false;
                    prev.DoneSec = 0;
                    prev.ServerProgressPct = 0;
                    prev.RemainingMin = Math.Max(1, prev.DurationMin);
                    parsed.Add(prev);
                }
            }

            activeQuests = parsed;
            if (selectedQuest != null)
            {
                foreach (QuestItem qi in activeQuests)
                {
                    if (qi.Id == selectedQuest.Id)
                    {
                        selectedQuest = qi;
                        smartProgBar.Percent = qi.ServerProgressPct;
                        if (qi.ServerProgressPct >= 100)
                        {
                            lblSmartProgTitle.Text = Loc.T("Прогресс задания в Discord: 100% (API)", "Discord Quest Progress: 100% (API)");
                            lblSmartProgRem.Text = qi.IsClaimed ? Loc.T("Награда уже получена", "Reward already claimed") : Loc.T("Выполнено! Можно забрать награду", "Completed! Ready to claim reward");
                        }
                        else
                        {
                            lblSmartProgTitle.Text = string.Format(Loc.T("Прогресс задания в Discord: {0}% (API)", "Discord Quest Progress: {0}% (API)"), qi.ServerProgressPct);
                            lblSmartProgRem.Text = string.Format(Loc.T("Осталось: {0} мин. (+ хвост)", "Remaining: {0} min (+ tail)"), Math.Max(1, qi.RemainingMin));
                        }
                        break;
                    }
                }
            }
            SaveState();
            UpdateOrbsCounters();
            RefreshQuestTable();
            return parsed.Count;
        }

        private string EnsureDummyHostCompiled()
        {
            string hostExe = Path.Combine(assetsDir, "dqs_dummy_host_v7.exe");
            if (File.Exists(hostExe) && new FileInfo(hostExe).Length > 2048) return hostExe;

            string code = string.Join("\r\n", new string[] {
                "using System;",
                "using System.Diagnostics;",
                "using System.Drawing;",
                "using System.Windows.Forms;",
                "static class DummyGameProcess {",
                "    [STAThread]",
                "    static void Main(string[] args) {",
                "        string title = args.Length > 0 ? args[0] : \"Game\";",
                "        int parentPid = 0;",
                "        int maxSeconds = 3600;",
                "        if (args.Length > 1) int.TryParse(args[1], out parentPid);",
                "        if (args.Length > 2) int.TryParse(args[2], out maxSeconds);",
                "        DateTime start = DateTime.UtcNow;",
                "        Application.EnableVisualStyles();",
                "        Form f = new Form();",
                "        f.Text = title;",
                "        f.Size = new Size(300, 100);",
                "        f.StartPosition = FormStartPosition.Manual;",
                "        f.Location = new Point(-32000, -32000);",
                "        f.WindowState = FormWindowState.Minimized;",
                "        f.ShowInTaskbar = true;",
                "        f.FormBorderStyle = FormBorderStyle.FixedToolWindow;",
                "        Timer t = new Timer();",
                "        t.Interval = 1000;",
                "        t.Tick += (s, e) => {",
                "            if ((DateTime.UtcNow - start).TotalSeconds >= maxSeconds) Environment.Exit(0);",
                "            if (parentPid > 0) {",
                "                try { if (Process.GetProcessById(parentPid).HasExited) Environment.Exit(0); }",
                "                catch { Environment.Exit(0); }",
                "            }",
                "        };",
                "        t.Start();",
                "        Application.Run(f);",
                "    }",
                "}"
            });
            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                CompilerParameters cp = new CompilerParameters();
                cp.GenerateExecutable = true;
                cp.OutputAssembly = hostExe;
                cp.CompilerOptions = "/target:winexe /optimize";
                cp.ReferencedAssemblies.Add("System.dll");
                cp.ReferencedAssemblies.Add("System.Drawing.dll");
                cp.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                provider.CompileAssemblyFromSource(cp, code);
            }
            return hostExe;
        }

        private bool ClaimQuestReward(QuestItem q)
        {
            if (q == null || q.IsClaimed || string.IsNullOrEmpty(discordToken)) return false;
            string tok = discordToken.Trim().Trim('"', '\'');
            if (tok.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) tok = tok.Substring(7).Trim();

            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                Dictionary<string, object> payload = new Dictionary<string, object>();
                payload["platform"] = 0;
                payload["location"] = 11;
                if (!string.IsNullOrEmpty(q.MetadataSealed)) payload["metadata_sealed"] = q.MetadataSealed;
                if (!string.IsNullOrEmpty(q.TrafficMetadataSealed)) payload["traffic_metadata_sealed"] = q.TrafficMetadataSealed;

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/quests/" + q.Id + "/claim-reward");
                ApplyDiscordHeaders(req, tok, true);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = 10000;
                byte[] body = Encoding.UTF8.GetBytes(ser.Serialize(payload));
                req.ContentLength = body.Length;
                using (Stream st = req.GetRequestStream())
                {
                    st.Write(body, 0, body.Length);
                }
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300)
                    {
                        q.IsClaimed = true;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private void ClaimAllCompletedRewards(bool showDialog)
        {
            if (string.IsNullOrEmpty(discordToken))
            {
                if (showDialog && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() => OpenAccountDialog()));
                }
                return;
            }
            int claimedCnt = 0;
            List<QuestItem> snap = new List<QuestItem>(activeQuests);
            foreach (QuestItem q in snap)
            {
                if (q != null && completedQuestIds.Contains(q.Id) && !q.IsClaimed)
                {
                    if (ClaimQuestReward(q))
                    {
                        claimedCnt++;
                        Thread.Sleep(500);
                    }
                }
            }
            PollDiscordApi(false);
            if (showDialog && this.IsHandleCreated)
            {
                this.BeginInvoke(new Action(() =>
                {
                    string msg = claimedCnt > 0
                        ? string.Format(Loc.T("Успешно получено наград: {0}", "Successfully claimed rewards: {0}"), claimedCnt)
                        : Loc.T("Нет доступных незабранных наград (либо для получения требуется капча в клиенте Discord).", "No unclaimed rewards available (or Discord requires a captcha in the client).");
                    MessageBox.Show(this, msg, "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }
        }

        private void AutoEnrollAllActiveQuests()
        {
            if (string.IsNullOrEmpty(discordToken)) return;
            List<QuestItem> snap = new List<QuestItem>(activeQuests);
            foreach (QuestItem q in snap)
            {
                if (q == null || q.IsEnrolled || completedQuestIds.Contains(q.Id) || IsQuestExpired(q)) continue;
                EnsureQuestEnrolled(q);
                Thread.Sleep(600);
            }
        }

        private void EnsureQuestEnrolled(QuestItem q)
        {
            if (q == null || q.IsEnrolled || string.IsNullOrEmpty(discordToken)) return;
            string tok = discordToken.Trim().Trim('"', '\'');
            if (tok.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) tok = tok.Substring(7).Trim();

            List<string> payloads = new List<string>();
            if (!string.IsNullOrEmpty(q.TrafficMetadataSealed) || !string.IsNullOrEmpty(q.MetadataSealed))
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                Dictionary<string, object> pDict = new Dictionary<string, object>();
                pDict["location"] = 11;
                pDict["is_targeted"] = false;
                pDict["metadata_raw"] = null;
                if (!string.IsNullOrEmpty(q.MetadataSealed)) pDict["metadata_sealed"] = q.MetadataSealed;
                if (!string.IsNullOrEmpty(q.TrafficMetadataSealed)) pDict["traffic_metadata_sealed"] = q.TrafficMetadataSealed;
                payloads.Add(ser.Serialize(pDict));
            }
            payloads.Add("{\"location\":11,\"is_targeted\":false,\"metadata_raw\":null}");
            payloads.Add("{\"location\":11}");

            foreach (string pl in payloads)
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/quests/" + q.Id + "/enroll");
                    ApplyDiscordHeaders(req, tok, true);
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.Timeout = 8000;
                    byte[] body = Encoding.UTF8.GetBytes(pl);
                    req.ContentLength = body.Length;
                    using (Stream st = req.GetRequestStream())
                    {
                        st.Write(body, 0, body.Length);
                    }
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300)
                        {
                            q.IsEnrolled = true;
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        private void StartVideoQuestEmulation(QuestItem q)
        {
            if (string.IsNullOrEmpty(discordToken))
            {
                MessageBox.Show(this, Loc.T("Для выполнения видео-квеста подключите токен Discord в правом верхнем углу.", "Connect your Discord token in the top-right corner to run video quests."), "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (queueActive) StopQuestQueue(true);
                return;
            }

            int totalSec = Math.Max(5, q.TargetSec - q.DoneSec);
            int vidPid = -Math.Abs((q.Id ?? "vid").GetHashCode() % 90000 + 10000);

            ActiveSpoofItem ap = new ActiveSpoofItem();
            ap.Pid = vidPid;
            ap.GameName = q.GameName + Loc.T(" (Видео)", " (Video)");
            ap.ExeFullPath = "API: /quests/" + q.Id + "/video-progress";
            ap.DurationSec = totalSec;
            ap.QuestId = q.Id;
            ap.OrbsReward = q.BaseOrbs;
            ap.IsVideoQuest = true;

            activeProcesses.Add(ap);
            RefreshProcessesPanel();
            RefreshQuestTable();

            Thread vThread = new Thread(() =>
            {
                EnsureQuestEnrolled(q);
                string tok = discordToken.Trim().Trim('"', '\'');
                if (tok.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) tok = tok.Substring(7).Trim();

                double currentTs = Math.Max(0, q.DoneSec);
                double targetTs = Math.Max(1, q.TargetSec);
                JavaScriptSerializer ser = new JavaScriptSerializer();

                while (!ap.VideoStopRequested && currentTs < targetTs)
                {
                    currentTs = Math.Min(targetTs, currentTs + 6.0);
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/quests/" + q.Id + "/video-progress");
                        ApplyDiscordHeaders(req, tok);
                        req.Method = "POST";
                        req.ContentType = "application/json";
                        req.Timeout = 10000;
                        string payload = string.Format(CultureInfo.InvariantCulture, "{{\"timestamp\":{0:0.0}}}", currentTs);
                        byte[] body = Encoding.UTF8.GetBytes(payload);
                        req.ContentLength = body.Length;
                        using (Stream st = req.GetRequestStream())
                        {
                            st.Write(body, 0, body.Length);
                        }
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        {
                            string rJson = sr.ReadToEnd();
                            Dictionary<string, object> rd = ser.DeserializeObject(rJson) as Dictionary<string, object>;
                            if (rd != null && rd.ContainsKey("completed_at") && rd["completed_at"] != null)
                            {
                                break;
                            }
                        }
                    }
                    catch { }

                    for (int w = 0; w < 60 && !ap.VideoStopRequested; w++)
                    {
                        Thread.Sleep(100);
                    }
                }

                if (!ap.VideoStopRequested)
                {
                    ap.VideoFinished = true;
                }
            });
            vThread.IsBackground = true;
            vThread.Start();
        }

        private void StartHeartbeatQuestEmulation(QuestItem q)
        {
            if (string.IsNullOrEmpty(discordToken))
            {
                MessageBox.Show(this, Loc.T("Для выполнения этого квеста подключите токен Discord в правом верхнем углу.", "Connect your Discord token in the top-right corner to run this quest."), "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (queueActive) StopQuestQueue(true);
                return;
            }

            int totalSec = Math.Max(30, q.TargetSec - q.DoneSec);
            int hbPid = -Math.Abs((q.Id ?? "hb").GetHashCode() % 90000 + 20000);

            ActiveSpoofItem ap = new ActiveSpoofItem();
            ap.Pid = hbPid;
            ap.GameName = q.GameName + Loc.T(" (Активность)", " (Activity)");
            ap.ExeFullPath = "API: /quests/" + q.Id + "/heartbeat";
            ap.DurationSec = totalSec;
            ap.QuestId = q.Id;
            ap.OrbsReward = q.BaseOrbs;
            ap.IsHeartbeatQuest = true;

            activeProcesses.Add(ap);
            RefreshProcessesPanel();
            RefreshQuestTable();

            Thread hbThread = new Thread(() =>
            {
                EnsureQuestEnrolled(q);
                string tok = discordToken.Trim().Trim('"', '\'');
                if (tok.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) tok = tok.Substring(7).Trim();

                JavaScriptSerializer ser = new JavaScriptSerializer();
                while (!ap.VideoStopRequested && ap.RemainingSeconds() > 0)
                {
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/quests/" + q.Id + "/heartbeat");
                        ApplyDiscordHeaders(req, tok);
                        req.Method = "POST";
                        req.ContentType = "application/json";
                        req.Timeout = 10000;
                        byte[] body = Encoding.UTF8.GetBytes("{\"stream_key\":\"call:0:0\",\"terminal\":false}");
                        req.ContentLength = body.Length;
                        using (Stream st = req.GetRequestStream())
                        {
                            st.Write(body, 0, body.Length);
                        }
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        {
                            Dictionary<string, object> rd = ser.DeserializeObject(sr.ReadToEnd()) as Dictionary<string, object>;
                            if (rd != null && rd.ContainsKey("completed_at") && rd["completed_at"] != null)
                            {
                                break;
                            }
                        }
                    }
                    catch { }

                    for (int w = 0; w < 300 && !ap.VideoStopRequested && ap.RemainingSeconds() > 0; w++)
                    {
                        Thread.Sleep(100);
                    }
                }

                try
                {
                    HttpWebRequest reqEnd = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/quests/" + q.Id + "/heartbeat");
                    ApplyDiscordHeaders(reqEnd, tok);
                    reqEnd.Method = "POST";
                    reqEnd.ContentType = "application/json";
                    reqEnd.Timeout = 6000;
                    byte[] endBody = Encoding.UTF8.GetBytes("{\"stream_key\":\"call:0:0\",\"terminal\":true}");
                    reqEnd.ContentLength = endBody.Length;
                    using (Stream st = reqEnd.GetRequestStream())
                    {
                        st.Write(endBody, 0, endBody.Length);
                    }
                    using (HttpWebResponse r = (HttpWebResponse)reqEnd.GetResponse()) { }
                }
                catch { }

                if (!ap.VideoStopRequested)
                {
                    ap.VideoFinished = true;
                }
            });
            hbThread.IsBackground = true;
            hbThread.Start();
        }

        private void StartEmulationProcess()
        {
            if (selectedQuest != null && selectedQuest.IsVideoQuest())
            {
                StartVideoQuestEmulation(selectedQuest);
                return;
            }
            if (selectedQuest != null && selectedQuest.IsHeartbeatQuest())
            {
                StartHeartbeatQuestEmulation(selectedQuest);
                return;
            }
            QuestItem currentQuestObj = selectedQuest;

            string gameName = inpGameName.Value.Trim();
            string exeRel = inpExeRel.Value.Trim().Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrEmpty(gameName) || string.IsNullOrEmpty(exeRel))
            {
                MessageBox.Show(this, Loc.T("Выберите задание в таблице слева или укажите название игры и путь .exe.", "Select a quest from the left table or enter a game name and .exe path."), "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!exeRel.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) exeRel += ".exe";

            int mins = 15;
            int.TryParse(inpMinutes.Value.Trim(), out mins);
            if (mins < 1) mins = 15;

            Random rnd = new Random();
            int tailSec = chkRandomTail.Checked ? rnd.Next(30, 91) : 0;
            int totalSec = mins * 60 + tailSec;

            string steamBase = inpSteamappsDir.Value.Trim();
            if (string.IsNullOrEmpty(steamBase)) steamBase = DetectDefaultSteamapps();
            string commonDir = Path.Combine(steamBase, "common");

            string[] parts = exeRel.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string relCombined;
            if (parts.Length == 1)
            {
                string safeFolder = Regex.Replace(gameName, @"[^A-Za-z0-9 _\-]", "").Trim();
                if (string.IsNullOrEmpty(safeFolder)) safeFolder = "DiscordQuestGame";
                relCombined = Path.Combine(safeFolder, parts[0]);
            }
            else
            {
                relCombined = Path.Combine(parts);
            }

            string targetExe = Path.Combine(commonDir, relCombined);
            List<string> createdDirs = new List<string>();
            try
            {
                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(targetExe));
                while (di != null && !di.Exists && !string.Equals(di.FullName, commonDir, StringComparison.OrdinalIgnoreCase))
                {
                    createdDirs.Add(di.FullName);
                    di = di.Parent;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(targetExe));
            }
            catch
            {
                steamBase = Path.Combine(userDataDir, "steamapps");
                commonDir = Path.Combine(steamBase, "common");
                targetExe = Path.Combine(commonDir, relCombined);
                Directory.CreateDirectory(Path.GetDirectoryName(targetExe));
            }

            string acfPath = "";
            string stId = inpSteamId.Value.Trim();
            if (chkCreateAcf.Checked && !string.IsNullOrEmpty(stId))
            {
                try
                {
                    Directory.CreateDirectory(steamBase);
                    string candidateAcf = Path.Combine(steamBase, "appmanifest_" + stId + ".acf");
                    if (!File.Exists(candidateAcf))
                    {
                        string acfBody = "\"AppState\"\n{\n\t\"appid\"\t\t\"" + stId + "\"\n\t\"Universe\"\t\t\"1\"\n\t\"name\"\t\t\"" + gameName + "\"\n\t\"StateFlags\"\t\t\"4\"\n}\n";
                        File.WriteAllText(candidateAcf, acfBody, Encoding.UTF8);
                        acfPath = candidateAcf;
                    }
                }
                catch { }
            }

            try
            {
                string baseDummy = EnsureDummyHostCompiled();
                File.Copy(baseDummy, targetExe, true);

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = targetExe;
                psi.Arguments = string.Format("\"{0}\" {1} {2}", gameName.Replace("\"", ""), Process.GetCurrentProcess().Id, totalSec + 15);
                psi.WorkingDirectory = Path.GetDirectoryName(targetExe);
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);

                ActiveSpoofItem ap = new ActiveSpoofItem();
                ap.Pid = p.Id;
                ap.GameName = gameName;
                ap.ExeFullPath = targetExe;
                ap.CreatedDirs = createdDirs;
                ap.AcfPath = acfPath;
                ap.DurationSec = totalSec;
                ap.QuestId = (selectedQuest != null) ? selectedQuest.Id : "";
                ap.OrbsReward = (selectedQuest != null) ? selectedQuest.BaseOrbs : 0;
                ap.ProcObj = p;

                activeProcesses.Add(ap);
                RefreshProcessesPanel();
                RefreshQuestTable();

                if (currentQuestObj != null && !string.IsNullOrEmpty(discordToken))
                {
                    QuestItem qRef = currentQuestObj;
                    ActiveSpoofItem apRef = ap;
                    Thread deskHbThread = new Thread(() =>
                    {
                        EnsureQuestEnrolled(qRef);
                        string tok = discordToken.Trim().Trim('"', '\'');
                        if (tok.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) tok = tok.Substring(7).Trim();
                        JavaScriptSerializer ser = new JavaScriptSerializer();

                        while (apRef.IsRunning() && apRef.RemainingSeconds() > 0 && !string.IsNullOrEmpty(discordToken))
                        {
                            if (!string.IsNullOrEmpty(qRef.AppId))
                            {
                                try
                                {
                                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/quests/" + qRef.Id + "/heartbeat");
                                    ApplyDiscordHeaders(req, tok, true);
                                    req.Method = "POST";
                                    req.ContentType = "application/json";
                                    req.Timeout = 10000;
                                    string hbJson = "{\"application_id\":\"" + qRef.AppId + "\",\"terminal\":false}";
                                    byte[] body = Encoding.UTF8.GetBytes(hbJson);
                                    req.ContentLength = body.Length;
                                    using (Stream st = req.GetRequestStream())
                                    {
                                        st.Write(body, 0, body.Length);
                                    }
                                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                                    {
                                        Dictionary<string, object> rd = ser.DeserializeObject(sr.ReadToEnd()) as Dictionary<string, object>;
                                        if (rd != null && rd.ContainsKey("completed_at") && rd["completed_at"] != null)
                                        {
                                            apRef.VideoFinished = true;
                                            break;
                                        }
                                    }
                                }
                                catch { }
                            }
                            for (int w = 0; w < 300 && apRef.IsRunning() && apRef.RemainingSeconds() > 0; w++)
                            {
                                Thread.Sleep(100);
                            }
                        }
                    });
                    deskHbThread.IsBackground = true;
                    deskHbThread.Start();
                }
            }
            catch (Exception ex)
            {
                string extraHint = "";
                if (ex is UnauthorizedAccessException || ex is System.ComponentModel.Win32Exception ||
                    ex.Message.IndexOf("virus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ex.Message.IndexOf("доступ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    extraHint = Loc.T("\r\n\r\nВозможно, антивирус или Windows Defender заблокировал временный файл пустышки. Добавьте папку ~/.discord_quest_studio/assets в исключения или выберите другую рабочую папку через «Обзор...».", "\r\n\r\nWindows Defender or your antivirus may have blocked the temporary dummy host. Add ~/.discord_quest_studio/assets to exclusions or choose another folder via «Browse...».");
                }
                lblSpotBadge.Text = Loc.T("● Ошибка запуска", "● Launch Error");
                lblSpotBadge.ForeColor = Color.FromArgb(255, 92, 92);
                MessageBox.Show(this, Loc.T("Не удалось запустить процесс эмуляции:\r\n", "Failed to start emulation process:\r\n") + ex.Message + extraHint, "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string FormatTime(int sec)
        {
            int m = Math.Max(0, sec) / 60;
            int s = Math.Max(0, sec) % 60;
            return string.Format("{0:00}:{1:00}", m, s);
        }

        private void RefreshProcessesPanel()
        {
            List<ActiveSpoofItem> running = new List<ActiveSpoofItem>();
            foreach (ActiveSpoofItem ap in activeProcesses)
            {
                if (ap.IsRunning()) running.Add(ap);
            }

            lblActiveProcHeader.Text = string.Format(Loc.T("Активные запущенные процессы ({0})", "Active Processes ({0})"), running.Count);

            if (running.Count == 0)
            {
                if (procListContainer.Controls.Count != 1 || procListContainer.Controls[0] != lblEmptyProcs)
                {
                    procListContainer.Controls.Clear();
                    procListContainer.Controls.Add(lblEmptyProcs);
                }
                if (!lblEmptyProcs.Visible) lblEmptyProcs.Visible = true;
                if (queueActive && pendingQueueQuest != null)
                {
                    lblEmptyProcs.Text = string.Format(Loc.T("Очередь активна: запуск «{0}» через {1}...", "Queue active: launching «{0}» in {1}..."), pendingQueueQuest.GameName, FormatTime(queueCooldownSec));
                    return;
                }
                lblEmptyProcs.Text = Loc.T("Нет активных процессов эмуляции.\r\nВыберите задание слева и нажмите «Запустить эмуляцию  →».", "No active emulation processes.\r\nSelect a quest on the left and click «Start Emulation  →».");
                string readyTxt = Loc.T("● Система готова", "● System Ready");
                if (lblSpotBadge.Text != readyTxt)
                {
                    lblSpotBadge.Text = Loc.T("● Система готова", "● System Ready");
                    lblSpotBadge.ForeColor = Gold;
                }
                return;
            }

            lblEmptyProcs.Visible = false;
            HashSet<int> runningPids = new HashSet<int>();
            foreach (ActiveSpoofItem ap in running) runningPids.Add(ap.Pid);

            for (int i = procListContainer.Controls.Count - 1; i >= 0; i--)
            {
                Control c = procListContainer.Controls[i];
                if (c == lblEmptyProcs) continue;
                if (c.Tag is int && !runningPids.Contains((int)c.Tag))
                {
                    procListContainer.Controls.RemoveAt(i);
                    c.Dispose();
                }
            }

            foreach (ActiveSpoofItem ap in running)
            {
                Control existing = null;
                foreach (Control c in procListContainer.Controls)
                {
                    if (c.Tag is int && (int)c.Tag == ap.Pid) { existing = c; break; }
                }

                if (existing == null)
                {
                    RoundedPanelCard rowCard = new RoundedPanelCard();
                    rowCard.Radius = 8;
                    rowCard.FillColor = Color.FromArgb(22, 26, 34);
                    rowCard.BorderColor = Color.FromArgb(40, 46, 60);
                    rowCard.Dock = DockStyle.Top;
                    rowCard.Height = Dpi.S(54);
                    rowCard.Padding = Dpi.Pad(12, 6, 12, 6);
                    rowCard.Tag = ap.Pid;

                    TableLayoutPanel rGrid = new TableLayoutPanel();
                    rGrid.Dock = DockStyle.Fill;
                    rGrid.BackColor = Color.FromArgb(22, 26, 34);
                    rGrid.ColumnCount = 4;
                    rGrid.RowCount = 1;
                    rGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                    rGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi.S(96)));
                    rGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi.S(62)));
                    rGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi.S(74)));
                    rGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                    TableLayoutPanel infoP = new TableLayoutPanel();
                    infoP.Dock = DockStyle.Fill;
                    infoP.ColumnCount = 1;
                    infoP.RowCount = 2;
                    infoP.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                    infoP.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
                    infoP.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

                    Label lTitle = new Label();
                    lTitle.Text = (ap.IsVideoQuest || ap.IsHeartbeatQuest) ? ap.GameName : string.Format("{0} (PID {1})", ap.GameName, ap.Pid);
                    lTitle.ForeColor = TextMain;
                    lTitle.Font = new Font("Segoe UI", Dpi.F(9f), FontStyle.Bold);
                    lTitle.Dock = DockStyle.Fill;

                    Label lPath = new Label();
                    lPath.Text = ap.ExeFullPath;
                    lPath.ForeColor = TextSub;
                    lPath.Font = new Font("Consolas", Dpi.F(8f), FontStyle.Regular);
                    lPath.Dock = DockStyle.Fill;

                    infoP.Controls.Add(lTitle, 0, 0);
                    infoP.Controls.Add(lPath, 0, 1);

                    SmoothGoldBar pBar = new SmoothGoldBar();
                    pBar.Name = "pBar";
                    pBar.Percent = ap.ProgressPct();
                    pBar.Dock = DockStyle.Top;
                    pBar.Margin = Dpi.Pad(6, 16, 6, 0);

                    Label lTime = new Label();
                    lTime.Name = "lTime";
                    lTime.Text = FormatTime(ap.RemainingSeconds());
                    lTime.ForeColor = Gold;
                    lTime.Font = new Font("Consolas", Dpi.F(9.8f), FontStyle.Bold);
                    lTime.Dock = DockStyle.Fill;
                    lTime.TextAlign = ContentAlignment.MiddleRight;

                    int targetPid = ap.Pid;
                    PillActionButton bStop = PillActionButton.CreateDanger(Loc.T("Стоп", "Stop"), Color.FromArgb(22, 26, 34));
                    bStop.Dock = DockStyle.Fill;
                    bStop.Margin = Dpi.Pad(8, 6, 0, 6);
                    bStop.Font = new Font("Segoe UI", Dpi.F(8.2f), FontStyle.Bold);
                    bStop.Click += (s, e) => StopProcessByPid(targetPid);

                    rGrid.Controls.Add(infoP, 0, 0);
                    rGrid.Controls.Add(pBar, 1, 0);
                    rGrid.Controls.Add(lTime, 2, 0);
                    rGrid.Controls.Add(bStop, 3, 0);
                    rowCard.Controls.Add(rGrid);
                    procListContainer.Controls.Add(rowCard);
                }
                else
                {
                    Control[] bars = existing.Controls.Find("pBar", true);
                    if (bars.Length > 0 && bars[0] is SmoothGoldBar)
                    {
                        ((SmoothGoldBar)bars[0]).Percent = ap.ProgressPct();
                    }
                    Control[] times = existing.Controls.Find("lTime", true);
                    if (times.Length > 0)
                    {
                        times[0].Text = FormatTime(ap.RemainingSeconds());
                    }
                }
            }

            ActiveSpoofItem first = running[0];
            lblSpotBadge.Text = first.IsVideoQuest ? Loc.T("● Видео-квест в работе", "● Video Quest Running") : first.IsHeartbeatQuest ? Loc.T("● Heartbeat в работе", "● Heartbeat Running") : string.Format(Loc.T("● В работе · PID {0}", "● Running · PID {0}"), first.Pid);
            lblSpotBadge.ForeColor = Color.FromArgb(46, 204, 113);
            lblSpotTitle.Text = first.GameName;
            lblSpotSub.Text = string.Format(Loc.T("Прогресс: {0}% (осталось {1})", "Progress: {0}% ({1} left)"), first.ProgressPct(), FormatTime(first.RemainingSeconds()));
        }

        private void StopProcessByPid(int pid)
        {
            for (int i = activeProcesses.Count - 1; i >= 0; i--)
            {
                if (activeProcesses[i].Pid == pid)
                {
                    activeProcesses[i].StopAndCleanup();
                    activeProcesses.RemoveAt(i);
                }
            }
            RefreshProcessesPanel();
            RefreshQuestTable();
        }

        private void InitSystemTray()
        {
            try
            {
                trayIcon = new NotifyIcon();
                trayIcon.Icon = (appIcon != null) ? appIcon : SystemIcons.Application;
                trayIcon.Text = "Discord Quest Studio";
                trayIcon.Visible = true;

                ContextMenuStrip trayMenu = new ContextMenuStrip();
                trayMenu.ShowImageMargin = false;
                trayMenu.BackColor = Color.FromArgb(18, 21, 28);
                trayMenu.ForeColor = TextMain;
                trayMenu.Font = new Font("Segoe UI", Dpi.F(9.0f), FontStyle.Regular);

                miTrayOpen = new ToolStripMenuItem("Открыть Discord Quest Studio");
                miTrayOpen.ForeColor = Gold;
                miTrayOpen.Click += (s, e) => RestoreFromTray();

                miTrayStop = new ToolStripMenuItem("Аварийный стоп эмуляции");
                miTrayStop.ForeColor = Color.FromArgb(255, 110, 110);
                miTrayStop.Click += (s, e) => EmergencyStopAll();

                miTrayExit = new ToolStripMenuItem("Выход");
                miTrayExit.ForeColor = TextMain;
                miTrayExit.Click += (s, e) => this.Close();

                trayMenu.Items.Add(miTrayOpen);
                trayMenu.Items.Add(miTrayStop);
                trayMenu.Items.Add(new ToolStripSeparator());
                trayMenu.Items.Add(miTrayExit);

                trayMenu.Opening += (s, e) =>
                {
                    miTrayOpen.Text = Loc.T("Открыть Discord Quest Studio", "Open Discord Quest Studio");
                    miTrayStop.Text = Loc.T("Аварийный стоп эмуляции", "Emergency Stop Emulation");
                    miTrayExit.Text = Loc.T("Выход", "Exit");
                };

                trayIcon.ContextMenuStrip = trayMenu;
                trayIcon.DoubleClick += (s, e) => RestoreFromTray();
            }
            catch { }
        }

        private void MinimizeToTray()
        {
            this.Hide();
            if (trayIcon != null)
            {
                try
                {
                    trayIcon.ShowBalloonTip(2500, "Discord Quest Studio", Loc.T("Программа свёрнута в трей и продолжает работу в фоне.", "Minimized to system tray and running in background."), ToolTipIcon.Info);
                }
                catch { }
            }
        }

        private void RestoreFromTray()
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
        }

        private void UpdateTrayTooltip(string text)
        {
            if (trayIcon == null) return;
            try
            {
                string safe = text ?? "Discord Quest Studio";
                if (safe.Length > 62) safe = safe.Substring(0, 62);
                if (trayIcon.Text != safe) trayIcon.Text = safe;
            }
            catch { }
        }

        private void ToggleQuestQueue()
        {
            if (queueActive)
            {
                StopQuestQueue(true);
                return;
            }

            List<QuestItem> candidates = new List<QuestItem>();
            foreach (QuestItem q in activeQuests)
            {
                if (!completedQuestIds.Contains(q.Id) && !IsQuestExpired(q))
                {
                    candidates.Add(q);
                }
            }
            if (candidates.Count == 0)
            {
                MessageBox.Show(this, Loc.T("Нет доступных невыполненных квестов для очереди.", "No available uncompleted quests for the queue."), "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            candidates.Sort((a, b) =>
            {
                int cmp = b.BaseOrbs.CompareTo(a.BaseOrbs);
                if (cmp != 0) return cmp;
                return a.RemainingMin.CompareTo(b.RemainingMin);
            });

            questQueue = candidates;
            queueActive = true;
            queueCooldownSec = 0;
            pendingQueueQuest = null;
            RefreshQueueButtonUi();

            QuestItem first = questQueue[0];
            questQueue.RemoveAt(0);
            OnQuestSelected(first);
            StartEmulationProcess();
        }

        private void StopQuestQueue(bool showStatus)
        {
            queueActive = false;
            questQueue.Clear();
            queueCooldownSec = 0;
            pendingQueueQuest = null;
            RefreshQueueButtonUi();
            if (showStatus && activeProcesses.Count == 0)
            {
                lblSpotBadge.Text = Loc.T("● Система готова", "● System Ready");
                lblSpotBadge.ForeColor = Gold;
                lblSpotSub.Text = Loc.T("Очередь остановлена", "Queue stopped");
            }
        }

        private void RefreshQueueButtonUi()
        {
            if (btnQueueAll == null) return;
            if (queueActive)
            {
                btnQueueAll.Text = Loc.T("Остановить очередь", "Stop Queue");
                btnQueueAll.IconKind = "stop";
                btnQueueAll.FillNormal = Color.FromArgb(44, 18, 22);
                btnQueueAll.FillHover = Color.FromArgb(72, 26, 34);
                btnQueueAll.BorderNormal = Color.FromArgb(96, 36, 44);
                btnQueueAll.BorderHover = Color.FromArgb(255, 110, 110);
                btnQueueAll.TextColor = Color.FromArgb(255, 110, 110);
            }
            else
            {
                btnQueueAll.Text = Loc.T("Очередь всех", "Queue All");
                btnQueueAll.IconKind = "rocket";
                btnQueueAll.FillNormal = Color.FromArgb(26, 30, 38);
                btnQueueAll.FillHover = Color.FromArgb(38, 45, 58);
                btnQueueAll.BorderNormal = Color.FromArgb(46, 52, 66);
                btnQueueAll.BorderHover = Gold;
                btnQueueAll.TextColor = Gold;
            }
            btnQueueAll.Invalidate();
        }

        private void EmergencyStopAll()
        {
            StopQuestQueue(false);
            for (int i = activeProcesses.Count - 1; i >= 0; i--)
            {
                activeProcesses[i].StopAndCleanup();
            }
            activeProcesses.Clear();
            RefreshProcessesPanel();
            RefreshQuestTable();
            UpdateTrayTooltip("Discord Quest Studio");
        }

        private void LaunchNextFromQueueImmediate()
        {
            while (questQueue.Count > 0)
            {
                QuestItem candidate = questQueue[0];
                questQueue.RemoveAt(0);
                if (candidate == null || completedQuestIds.Contains(candidate.Id) || IsQuestExpired(candidate))
                {
                    continue;
                }
                OnQuestSelected(candidate);
                StartEmulationProcess();
                if (activeProcesses.Count > 0)
                {
                    return;
                }
            }

            if (activeProcesses.Count == 0)
            {
                queueActive = false;
                pendingQueueQuest = null;
                RefreshQueueButtonUi();
                RefreshProcessesPanel();
                UpdateTrayTooltip("Discord Quest Studio");
                if (trayIcon != null)
                {
                    try
                    {
                        trayIcon.ShowBalloonTip(4000, Loc.T("Очередь завершена", "Queue Finished"), Loc.T("Все доступные задания в очереди выполнены.", "All available quests in the queue are complete."), ToolTipIcon.Info);
                    }
                    catch { }
                }
            }
        }

        private void OnSecondTick(object sender, EventArgs e)
        {
            if (activeProcesses.Count == 0 && !queueActive) return;

            bool changed = false;
            string completedGameName = "";
            for (int i = activeProcesses.Count - 1; i >= 0; i--)
            {
                ActiveSpoofItem ap = activeProcesses[i];
                bool serverMarkedDone = !string.IsNullOrEmpty(ap.QuestId) && completedQuestIds.Contains(ap.QuestId);
                bool timerOrVideoDone = ap.VideoFinished || ap.RemainingSeconds() <= 0 || serverMarkedDone;

                if (timerOrVideoDone)
                {
                    if (!string.IsNullOrEmpty(ap.QuestId))
                    {
                        completedQuestIds.Add(ap.QuestId);
                        sessionCompletedQuestIds.Add(ap.QuestId);
                    }
                    completedGameName = ap.GameName;
                    ap.StopAndCleanup();
                    activeProcesses.RemoveAt(i);
                    changed = true;
                }
                else if (!ap.IsRunning())
                {
                    ap.StopAndCleanup();
                    activeProcesses.RemoveAt(i);
                    changed = true;
                }
            }

            if (activeProcesses.Count > 0)
            {
                ActiveSpoofItem cur = activeProcesses[0];
                UpdateTrayTooltip(string.Format("DQS: {0} ({1})", cur.GameName, FormatTime(cur.RemainingSeconds())));
            }
            else if (queueActive && pendingQueueQuest != null && queueCooldownSec > 0)
            {
                queueCooldownSec--;
                lblSpotBadge.Text = string.Format(Loc.T("● Пауза очереди · {0}", "● Queue Cooldown · {0}"), FormatTime(queueCooldownSec));
                lblSpotBadge.ForeColor = Gold;
                lblSpotTitle.Text = pendingQueueQuest.GameName;
                lblSpotSub.Text = string.Format(Loc.T("Автозапуск следующего квеста через {0} сек. (в очереди: {1})", "Auto-starting next quest in {0}s (queued: {1})"), queueCooldownSec, questQueue.Count + 1);
                UpdateTrayTooltip(string.Format(Loc.T("DQS Очередь: {0} ({1})", "DQS Queue: {0} ({1})"), pendingQueueQuest.GameName, FormatTime(queueCooldownSec)));

                if (queueCooldownSec <= 0)
                {
                    QuestItem nextQ = pendingQueueQuest;
                    pendingQueueQuest = null;
                    OnQuestSelected(nextQ);
                    StartEmulationProcess();
                    if (activeProcesses.Count == 0 && questQueue.Count > 0)
                    {
                        LaunchNextFromQueueImmediate();
                    }
                }
            }

            if (changed)
            {
                SaveState();
                UpdateOrbsCounters();
                RefreshQuestTable();

                if (!string.IsNullOrEmpty(completedGameName) && !string.IsNullOrEmpty(discordToken))
                {
                    Thread quickSync = new Thread(() =>
                    {
                        Thread.Sleep(1500);
                        ClaimAllCompletedRewards(false);
                    });
                    quickSync.IsBackground = true;
                    quickSync.Start();
                }

                if (!string.IsNullOrEmpty(completedGameName) && trayIcon != null)
                {
                    try
                    {
                        trayIcon.ShowBalloonTip(3500, Loc.T("Квест выполнен!", "Quest Completed!"), completedGameName + Loc.T(" — таймер завершён.", " — timer finished."), ToolTipIcon.Info);
                    }
                    catch { }
                }

                if (queueActive && activeProcesses.Count == 0 && pendingQueueQuest == null)
                {
                    while (questQueue.Count > 0 && (questQueue[0] == null || completedQuestIds.Contains(questQueue[0].Id) || IsQuestExpired(questQueue[0])))
                    {
                        questQueue.RemoveAt(0);
                    }
                    if (questQueue.Count > 0)
                    {
                        pendingQueueQuest = questQueue[0];
                        questQueue.RemoveAt(0);
                        queueCooldownSec = 5;
                        lblSpotBadge.Text = string.Format(Loc.T("● Пауза очереди · {0}", "● Queue Cooldown · {0}"), FormatTime(queueCooldownSec));
                        lblSpotBadge.ForeColor = Gold;
                        lblSpotTitle.Text = pendingQueueQuest.GameName;
                        lblSpotSub.Text = string.Format(Loc.T("Автозапуск следующего квеста через {0} сек. (в очереди: {1})", "Auto-starting next quest in {0}s (queued: {1})"), queueCooldownSec, questQueue.Count + 1);
                    }
                    else
                    {
                        queueActive = false;
                        RefreshQueueButtonUi();
                        UpdateTrayTooltip("Discord Quest Studio");
                        if (trayIcon != null)
                        {
                            try
                            {
                                trayIcon.ShowBalloonTip(4000, Loc.T("Очередь завершена", "Queue Finished"), Loc.T("Все доступные задания в очереди выполнены.", "All available quests in the queue are complete."), ToolTipIcon.Info);
                            }
                            catch { }
                        }
                    }
                }
            }

            RefreshProcessesPanel();
        }

        private void OpenGuideDialog()
        {
            Form dlg = CreateBaseDialog("Пошаговый гайд: Как подключить Discord и выполнять квесты", Dpi.S(700), Dpi.S(520));
            RoundedPanelCard boxWrap = new RoundedPanelCard();
            boxWrap.Radius = 12;
            boxWrap.FillColor = Color.FromArgb(14, 16, 21);
            boxWrap.BorderColor = BorderCol;
            boxWrap.Dock = DockStyle.Fill;
            boxWrap.Padding = Dpi.Pad(14, 14, 14, 14);

            TextBox txt = new TextBox();
            txt.Multiline = true;
            txt.ReadOnly = true;
            txt.WordWrap = true;
            txt.ScrollBars = ScrollBars.Vertical;
            txt.BorderStyle = BorderStyle.None;
            txt.BackColor = Color.FromArgb(14, 16, 21);
            txt.ForeColor = TextMain;
            txt.Font = new Font("Segoe UI", Dpi.F(9.8f), FontStyle.Regular);
            txt.Dock = DockStyle.Fill;
            txt.Text =
                "ШАГ 1 · ОТКРОЙТЕ КОНСОЛЬ РАЗРАБОТЧИКА В БРАУЗЕРЕ\r\n" +
                "Откройте discord.com/app в браузере, нажмите клавишу F12 (или Ctrl + Shift + I) и сверху выберите вкладку Network («Сеть»).\r\n\r\n" +
                "ШАГ 2 · НАЙДИТЕ ЗАПРОС ВАШИХ ЗАДАНИЙ (@me)\r\n" +
                "В строке фильтра напишите quests, затем в самом Discord слева откройте раздел «Путеводитель → Задания (Quests)». В списке запросов появится строка @me (или quests/@me).\r\n\r\n" +
                "ШАГ 3 · СКОПИРУЙТЕ ТОКЕН ИЛИ JSON-ОТВЕТ\r\n" +
                "• Для автосинхронизации каждые 15 секунд: откройте вкладку Headers запроса @me и скопируйте значение строки authorization.\r\n" +
                "• Для разового импорта без токена: откройте вкладку Response, нажмите Ctrl + A → Ctrl + C и вставьте в окно «Подключить Discord».\r\n\r\n" +
                "ШАГ 4 · ОБРАТНАЯ СВЯЗЬ И БАГ-РЕПОРТЫ\r\n" +
                "При обнаружении неисправностей свяжитесь с автором программы в Discord: hellwalk3rr";
            txt.SelectionStart = 0;
            txt.SelectionLength = 0;

            boxWrap.Controls.Add(txt);
            dlg.Controls[0].Controls.Add(boxWrap);
            dlg.ShowDialog(this);
        }

        private void OpenLicenseDialog()
        {
            Form dlg = CreateBaseDialog("Лицензионное соглашение и Авторские права", Dpi.S(700), Dpi.S(520));
            RoundedPanelCard boxWrap = new RoundedPanelCard();
            boxWrap.Radius = 12;
            boxWrap.FillColor = Color.FromArgb(14, 16, 21);
            boxWrap.BorderColor = BorderCol;
            boxWrap.Dock = DockStyle.Fill;
            boxWrap.Padding = Dpi.Pad(14, 14, 14, 14);

            TextBox txt = new TextBox();
            txt.Multiline = true;
            txt.ReadOnly = true;
            txt.WordWrap = true;
            txt.ScrollBars = ScrollBars.Vertical;
            txt.BorderStyle = BorderStyle.None;
            txt.BackColor = Color.FromArgb(14, 16, 21);
            txt.ForeColor = TextMain;
            txt.Font = new Font("Consolas", Dpi.F(9.5f), FontStyle.Regular);
            txt.Dock = DockStyle.Fill;
            txt.Text = "ЛИЦЕНЗИОННОЕ СОГЛАШЕНИЕ И АВТОРСКИЕ ПРАВА\r\nПрограмма: Discord Quest Studio\r\nАвтор и правообладатель: hellwalk3rr (Discord: hellwalk3rr)\r\n© 2026 hellwalk3rr. Все права защищены.\r\n\r\nУстановка и использование программы «Discord Quest Studio» означают ваше согласие со следующими условиями:\r\n\r\n1. АВТОРСКИЕ ПРАВА\r\n1.1. Все авторские права на программу «Discord Quest Studio» принадлежат её создателю — hellwalk3rr (Discord для связи: hellwalk3rr).\r\n1.2. Пользователю предоставляется бесплатное право на установку и личное некоммерческое использование программы на компьютерах под управлением ОС Windows.\r\n\r\n2. ЗАПРЕТ НА ПРОДАЖУ И КОММЕРЧЕСКОЕ ИСПОЛЬЗОВАНИЕ\r\n2.1. Программа «Discord Quest Studio» распространяется исключительно бесплатно в открытом доступе.\r\n2.2. Пользователям и третьим лицам строго запрещается:\r\n  • продавать, перепродавать, сдавать в аренду или распространять программу за любую плату;\r\n  • включать программу в состав платных подписок, закрытых платных каналов или коммерческих сборок;\r\n  • требовать оплату за предоставление ссылки на скачивание или за установку программы.\r\n\r\n3. СОХРАНЕНИЕ УКАЗАНИЯ АВТОРСТВА\r\n3.1. Запрещается удалять, скрывать или изменять сведения об авторе (hellwalk3rr) внутри программы и установщика.\r\n3.2. При публикации программы на сторонних ресурсах обязательно указание автора (Discord: hellwalk3rr) и сохранение бесплатного статуса проекта.\r\n\r\n4. БЕЗОПАСНОСТЬ И ОТКАЗ ОТ ОТВЕТСТВЕННОСТИ\r\n4.1. Программа работает локально на компьютере пользователя. Токен авторизации шифруется по стандарту PBKDF2-HMAC-SHA256 и хранится только на вашем устройстве (~/.discord_quest_studio/vault.dat).\r\n4.2. Программа предоставляется по принципу «как есть» (AS IS). По всем вопросам и баг-репортам обращайтесь к автору в Discord: hellwalk3rr.";
            txt.SelectionStart = 0;
            txt.SelectionLength = 0;

            boxWrap.Controls.Add(txt);
            dlg.Controls[0].Controls.Add(boxWrap);
            dlg.ShowDialog(this);
        }

        private void OpenEditOrbsDialog()
        {
            int earnedQuests = 0;
            foreach (QuestItem q in activeQuests)
            {
                if (completedQuestIds.Contains(q.Id)) earnedQuests += CalcOrbs(q.BaseOrbs);
            }
            int curTotal = Math.Max(0, earnedQuests + manualOrbsOffset);

            Form dlg = CreateBaseDialog("Баланс заработанных Orbs", Dpi.S(460), Dpi.S(250));
            Panel host = (Panel)dlg.Controls[0];

            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 1;
            grid.RowCount = 3;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(32)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(42)));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label lbl = new Label();
            lbl.Text = "Введите ваш текущий общий баланс заработанных Orbs в Discord:";
            lbl.ForeColor = TextSub;
            lbl.Font = new Font("Segoe UI", Dpi.F(9.5f), FontStyle.Regular);
            lbl.Dock = DockStyle.Fill;

            RoundedInputBox inp = new RoundedInputBox(curTotal.ToString(), false);
            inp.Dock = DockStyle.Fill;

            Panel btnRow = new Panel();
            btnRow.Dock = DockStyle.Fill;
            PillActionButton btnSave = PillActionButton.CreateGold("Сохранить баланс  →", BgCard);
            btnSave.AutoSizeToText(36, 36);
            btnSave.Dock = DockStyle.Right;
            btnSave.Click += (s, e) =>
            {
                int val;
                if (int.TryParse(inp.Value.Trim(), out val) && val >= 0)
                {
                    manualOrbsOffset = val - earnedQuests;
                    SaveState();
                    UpdateOrbsCounters();
                    dlg.Close();
                }
            };
            btnRow.Controls.Add(btnSave);

            grid.Controls.Add(lbl, 0, 0);
            grid.Controls.Add(inp, 0, 1);
            grid.Controls.Add(btnRow, 0, 2);
            host.Controls.Add(grid);
            dlg.ShowDialog(this);
        }

        private Dictionary<string, string> ShowSelectAccountDialog(Form owner, List<Dictionary<string, string>> accounts)
        {
            Dictionary<string, string> selected = null;
            int listH = Math.Min(Dpi.S(250), Math.Max(Dpi.S(126), accounts.Count * Dpi.S(58) + Dpi.S(12)));
            Form pickDlg = CreateBaseDialog(Loc.T("Выбор аккаунта Discord (" + accounts.Count + ")", "Select Discord Account (" + accounts.Count + ")"), Dpi.S(520), listH + Dpi.S(130));
            Panel host = (Panel)pickDlg.Controls[0];
            Panel foot = (Panel)pickDlg.Controls[1];

            Label lblHint = new Label();
            lblHint.Text = Loc.T("Выберите аккаунт Discord для подключения токена:", "Select a Discord account to connect:");
            lblHint.Font = new Font("Segoe UI", Dpi.F(9.5f), FontStyle.Bold);
            lblHint.ForeColor = TextMain;
            lblHint.Dock = DockStyle.Top;
            lblHint.Height = Dpi.S(28);
            lblHint.UseMnemonic = false;

            Panel listBoxWrap = new Panel();
            listBoxWrap.Dock = DockStyle.Fill;
            listBoxWrap.BackColor = BgRoot;
            listBoxWrap.AutoScroll = false;

            int selectedIdx = 0;
            List<Panel> rowPanels = new List<Panel>();

            Action refreshSelection = () =>
            {
                for (int i = 0; i < rowPanels.Count; i++)
                {
                    bool isSel = (i == selectedIdx);
                    rowPanels[i].BackColor = isSel ? Color.FromArgb(42, 36, 18) : BgCardInner;
                    rowPanels[i].Invalidate();
                }
            };

            int yPos = Dpi.S(6);
            for (int i = 0; i < accounts.Count; i++)
            {
                int idx = i;
                Dictionary<string, string> acc = accounts[i];
                Panel row = new Panel();
                row.Location = new Point(Dpi.S(6), yPos);
                row.Size = new Size(Dpi.S(448), Dpi.S(50));
                row.BackColor = (i == 0) ? Color.FromArgb(42, 36, 18) : BgCardInner;
                row.Cursor = Cursors.Hand;
                row.Paint += (s, pe) =>
                {
                    bool isSel = (idx == selectedIdx);
                    using (Pen pen = new Pen(isSel ? Gold : BorderCol, isSel ? 1.5f : 1f))
                    {
                        pe.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
                    }
                };

                Label lblMain = new Label();
                lblMain.Text = acc["display"] + "  (@" + acc["username"] + ")";
                lblMain.Font = new Font("Segoe UI", Dpi.F(9.6f), FontStyle.Bold);
                lblMain.ForeColor = TextMain;
                lblMain.Location = new Point(Dpi.S(12), Dpi.S(6));
                lblMain.Size = new Size(Dpi.S(424), Dpi.S(20));
                lblMain.UseMnemonic = false;
                lblMain.Cursor = Cursors.Hand;

                Label lblSub = new Label();
                lblSub.Text = "ID: " + acc["id"] + "  ·  " + acc["client"];
                lblSub.Font = new Font("Segoe UI", Dpi.F(8.4f));
                lblSub.ForeColor = TextSub;
                lblSub.Location = new Point(Dpi.S(12), Dpi.S(26));
                lblSub.Size = new Size(Dpi.S(424), Dpi.S(18));
                lblSub.UseMnemonic = false;
                lblSub.Cursor = Cursors.Hand;

                EventHandler onRowClick = (s, e) => { selectedIdx = idx; refreshSelection(); };
                EventHandler onRowDblClick = (s, e) => { selected = accounts[idx]; pickDlg.DialogResult = DialogResult.OK; pickDlg.Close(); };

                row.Click += onRowClick;
                lblMain.Click += onRowClick;
                lblSub.Click += onRowClick;
                row.DoubleClick += onRowDblClick;
                lblMain.DoubleClick += onRowDblClick;
                lblSub.DoubleClick += onRowDblClick;

                row.Controls.Add(lblMain);
                row.Controls.Add(lblSub);
                listBoxWrap.Controls.Add(row);
                rowPanels.Add(row);
                yPos += Dpi.S(56);
            }

            if (yPos > listH)
            {
                DarkScrollBar accScroll = new DarkScrollBar();
                accScroll.Dock = DockStyle.Right;
                accScroll.Minimum = 0;
                accScroll.Maximum = yPos;
                accScroll.LargeChange = listH;
                accScroll.SmallChange = Dpi.S(28);
                int prevScroll = 0;
                accScroll.ValueChanged += (s, e) =>
                {
                    int delta = accScroll.Value - prevScroll;
                    prevScroll = accScroll.Value;
                    foreach (Panel rp in rowPanels)
                    {
                        rp.Top -= delta;
                    }
                };
                listBoxWrap.MouseWheel += (s, e) =>
                {
                    accScroll.Value = accScroll.Value + (e.Delta > 0 ? -Dpi.S(28) : Dpi.S(28));
                };
                listBoxWrap.Controls.Add(accScroll);
                accScroll.BringToFront();
            }

            host.Controls.Add(listBoxWrap);
            host.Controls.Add(lblHint);

            PillActionButton btnUse = PillActionButton.CreateGold(Loc.T("Выбрать аккаунт  →", "Select Account  →"), foot.BackColor);
            btnUse.AutoSizeToText(28, 34);
            btnUse.Dock = DockStyle.Left;
            btnUse.Click += (s, e) =>
            {
                if (selectedIdx >= 0 && selectedIdx < accounts.Count)
                {
                    selected = accounts[selectedIdx];
                    pickDlg.DialogResult = DialogResult.OK;
                    pickDlg.Close();
                }
            };
            foot.Controls.Add(btnUse);

            pickDlg.ShowDialog(owner);
            return selected;
        }

        private void OpenAccountDialog()
        {
            Form dlg = CreateBaseDialog(Loc.T("Подключение аккаунта Discord (PBKDF2-SHA256)", "Connect Discord Account (PBKDF2-SHA256)"), Dpi.S(840), Dpi.S(640));
            Panel host = (Panel)dlg.Controls[0];

            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 1;
            grid.RowCount = 9;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(24)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(38)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(46)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(26)));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(48)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(26)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(40)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi.S(46)));

            Label l1 = new Label();
            l1.Text = Loc.T("1. Токен авторизации Discord (для автосинхронизации каждые 15 секунд):", "1. Discord Authorization Token (for auto-sync every 15s):");
            l1.ForeColor = TextMain;
            l1.Font = new Font("Segoe UI", Dpi.F(9.2f), FontStyle.Bold);
            l1.Dock = DockStyle.Fill;

            RoundedInputBox tokInput = new RoundedInputBox(discordToken, false);
            tokInput.InnerTextBox.UseSystemPasswordChar = true;
            tokInput.Dock = DockStyle.Fill;

            FlowLayoutPanel tokBtns = new FlowLayoutPanel();
            tokBtns.Dock = DockStyle.Fill;
            tokBtns.FlowDirection = FlowDirection.LeftToRight;
            tokBtns.WrapContents = false;
            tokBtns.Padding = Dpi.Pad(0, 4, 0, 0);

            PillActionButton btnSaveTok = PillActionButton.CreateGold(Loc.T("Сохранить токен и синхронизировать  →", "Save Token & Sync  →"), BgCard);
            btnSaveTok.AutoSizeToText(32, 34);
            btnSaveTok.Margin = Dpi.Pad(0, 0, 10, 0);
            btnSaveTok.Click += (s, e) =>
            {
                string raw = tokInput.Value.Trim().Trim('"', '\'');
                if (string.IsNullOrEmpty(raw)) return;
                if (discordToken != raw)
                {
                    ResetQuestsProgressForNewAccount();
                }
                discordToken = raw;
                tokenStatus = "checking";
                SaveTokenVault();
                SaveState();
                RefreshAccountBadge();
                Thread t = new Thread(() => PollDiscordApi(true));
                t.IsBackground = true;
                t.Start();
                dlg.Close();
            };

            PillActionButton btnAutoTok = PillActionButton.CreateDark(Loc.T("Найти токен автоматически", "Auto-Find Token"), BgCard, "bolt");
            btnAutoTok.TextColor = Gold;
            btnAutoTok.AutoSizeToText(26, 34);
            btnAutoTok.Margin = Dpi.Pad(0, 0, 10, 0);
            btnAutoTok.Click += (s, e) =>
            {
                btnAutoTok.Text = Loc.T("Поиск...", "Searching...");
                btnAutoTok.Invalidate();
                Thread findTh = new Thread(() =>
                {
                    List<LocalDiscordTokenFinder.CandidateTokenInfo> candidates = LocalDiscordTokenFinder.FindCandidateTokensWithSource();
                    HashSet<string> existingToks = new HashSet<string>();
                    foreach (LocalDiscordTokenFinder.CandidateTokenInfo ci in candidates) existingToks.Add(ci.Token);
                    if (!string.IsNullOrEmpty(discordToken) && existingToks.Add(discordToken))
                    {
                        candidates.Add(new LocalDiscordTokenFinder.CandidateTokenInfo { Token = discordToken, SourceClient = Loc.T("Текущий аккаунт", "Current Account") });
                    }
                    foreach (KeyValuePair<string, string> kv in savedAccountTokens)
                    {
                        if (!string.IsNullOrEmpty(kv.Key) && existingToks.Add(kv.Key))
                        {
                            candidates.Add(new LocalDiscordTokenFinder.CandidateTokenInfo { Token = kv.Key, SourceClient = Loc.T("Сохранённый аккаунт", "Saved Account") });
                        }
                    }
                    List<Dictionary<string, string>> foundAccounts = new List<Dictionary<string, string>>();
                    HashSet<string> seenUserIds = new HashSet<string>();

                    object syncObj = new object();
                    List<Thread> checkThreads = new List<Thread>();
                    foreach (LocalDiscordTokenFinder.CandidateTokenInfo candItem in candidates)
                    {
                        LocalDiscordTokenFinder.CandidateTokenInfo cand = candItem;
                        Thread ct = new Thread(() =>
                        {
                            try
                            {
                                HttpWebRequest rq = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/users/@me");
                                ApplyDiscordHeaders(rq, cand.Token);
                                rq.Timeout = 4500;
                                using (HttpWebResponse rp = (HttpWebResponse)rq.GetResponse())
                                using (StreamReader sr = new StreamReader(rp.GetResponseStream(), Encoding.UTF8))
                                {
                                    JavaScriptSerializer js = new JavaScriptSerializer();
                                    Dictionary<string, object> ud = js.DeserializeObject(sr.ReadToEnd()) as Dictionary<string, object>;
                                    if (ud != null && ud.ContainsKey("username"))
                                    {
                                        string uid = ud.ContainsKey("id") && ud["id"] != null ? ud["id"].ToString() : cand.Token;
                                        string uname = ud["username"].ToString();
                                        string gname = (ud.ContainsKey("global_name") && ud["global_name"] != null && !string.IsNullOrEmpty(ud["global_name"].ToString())) ? ud["global_name"].ToString() : uname;
                                        lock (syncObj)
                                        {
                                            if (seenUserIds.Add(uid))
                                            {
                                                Dictionary<string, string> acc = new Dictionary<string, string>();
                                                acc["token"] = cand.Token;
                                                acc["id"] = uid;
                                                acc["username"] = uname;
                                                acc["display"] = gname;
                                                acc["client"] = cand.SourceClient;
                                                foundAccounts.Add(acc);
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        });
                        ct.IsBackground = true;
                        ct.Start();
                        checkThreads.Add(ct);
                    }
                    foreach (Thread ct in checkThreads)
                    {
                        try { ct.Join(5000); } catch { }
                    }

                    dlg.BeginInvoke(new Action(() =>
                    {
                        btnAutoTok.Text = Loc.T("Найти токен автоматически", "Auto-Find Token");
                        btnAutoTok.Invalidate();

                        if (foundAccounts.Count == 0)
                        {
                            MessageBox.Show(dlg, Loc.T("Активный токен в локальном клиенте Discord не найден.\r\nВставьте токен вручную через консоль браузера (вкладка «Гайд»).", "No active token found in local Discord client.\r\nPlease paste your token manually via Browser DevTools (see Guide tab)."), "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        foreach (Dictionary<string, string> fa in foundAccounts)
                        {
                            savedAccountTokens[fa["token"]] = fa["display"];
                        }
                        SaveTokenVault();
                        Dictionary<string, string> chosen = ShowSelectAccountDialog(dlg, foundAccounts);

                        if (chosen != null)
                        {
                            if (discordToken != chosen["token"])
                            {
                                ResetQuestsProgressForNewAccount();
                            }
                            tokInput.Value = chosen["token"];
                            discordToken = chosen["token"];
                            discordUsername = chosen["display"];
                            tokenStatus = "online";
                            SaveTokenVault();
                            SaveState();
                            RefreshAccountBadge();
                            Thread syncTh = new Thread(() => PollDiscordApi(true));
                            syncTh.IsBackground = true;
                            syncTh.Start();
                            MessageBox.Show(dlg, Loc.T("Токен успешно подключён для аккаунта: ", "Token connected for account: ") + chosen["display"] + " (@" + chosen["username"] + ")", "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            dlg.Close();
                        }
                    }));
                });
                findTh.IsBackground = true;
                findTh.Start();
            };

            PillActionButton btnManualGuide = PillActionButton.CreateDark(Loc.T("Ручной поиск", "Manual Guide"), BgCard, "book");
            btnManualGuide.AutoSizeToText(26, 34);
            btnManualGuide.Margin = Dpi.Pad(0, 0, 10, 0);
            btnManualGuide.Click += (s, e) =>
            {
                dlg.Close();
                SwitchView("guide");
            };

            PillActionButton btnClearTok = PillActionButton.CreateDanger(Loc.T("Удалить токен", "Remove Token"), BgCard);
            btnClearTok.AutoSizeToText(26, 34);
            btnClearTok.Margin = new Padding(0);
            btnClearTok.Click += (s, e) =>
            {
                discordToken = "";
                discordUsername = "";
                tokInput.Value = "";
                activeQuests.Clear();
                completedQuestIds.Clear();
                sessionCompletedQuestIds.Clear();
                manualOrbsOffset = 0;
                SaveTokenVault();
                SaveState();
                UpdateOrbsCounters();
                RefreshQuestTable();
                RefreshAccountBadge();
            };
            tokBtns.Controls.Add(btnSaveTok);
            tokBtns.Controls.Add(btnAutoTok);
            tokBtns.Controls.Add(btnManualGuide);
            tokBtns.Controls.Add(btnClearTok);

            Label l2 = new Label();
            l2.Text = Loc.T("2. Или вставьте JSON-ответ из вкладки Network → quests/@me:", "2. Or paste the JSON response from Network → quests/@me:");
            l2.ForeColor = TextMain;
            l2.Font = new Font("Segoe UI", Dpi.F(9.2f), FontStyle.Bold);
            l2.Dock = DockStyle.Fill;

            TextBox jsonBox = new TextBox();
            jsonBox.Multiline = true;
            jsonBox.ScrollBars = ScrollBars.None;
            jsonBox.BackColor = Color.FromArgb(13, 15, 20);
            jsonBox.ForeColor = TextMain;
            jsonBox.BorderStyle = BorderStyle.None;
            jsonBox.Font = new Font("Consolas", Dpi.F(9f), FontStyle.Regular);
            jsonBox.Dock = DockStyle.Fill;

            FlowLayoutPanel impRow = new FlowLayoutPanel();
            impRow.Dock = DockStyle.Fill;
            impRow.FlowDirection = FlowDirection.LeftToRight;
            impRow.WrapContents = false;
            impRow.Padding = Dpi.Pad(0, 5, 0, 0);

            PillActionButton btnImpJson = PillActionButton.CreateDark(Loc.T("Импортировать JSON список заданий", "Import Quests JSON"), BgCard);
            btnImpJson.AutoSizeToText(32, 34);
            btnImpJson.Click += (s, e) =>
            {
                string rj = jsonBox.Text.Trim();
                if (string.IsNullOrEmpty(rj)) return;
                try
                {
                    int cnt = ApplyQuestsJsonPayload(rj);
                    MessageBox.Show(dlg, Loc.T("Импортировано заданий: ", "Quests imported: ") + cnt, "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(dlg, Loc.T("Ошибка разбора JSON:\r\n", "JSON parse error:\r\n") + ex.Message, "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            impRow.Controls.Add(btnImpJson);

            Label l3 = new Label();
            l3.Text = Loc.T("3. Прокси HTTP/HTTPS для обхода региона и блокировок (формат ip:port или ip:port:user:pass):", "3. HTTP/HTTPS Proxy for region/block bypass (format ip:port or ip:port:user:pass):");
            l3.ForeColor = TextMain;
            l3.Font = new Font("Segoe UI", Dpi.F(9.2f), FontStyle.Bold);
            l3.Dock = DockStyle.Fill;

            TableLayoutPanel proxyInputsRow = new TableLayoutPanel();
            proxyInputsRow.Dock = DockStyle.Fill;
            proxyInputsRow.Margin = Padding.Empty;
            proxyInputsRow.ColumnCount = 3;
            proxyInputsRow.RowCount = 1;
            proxyInputsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
            proxyInputsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27f));
            proxyInputsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27f));
            proxyInputsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            RoundedInputBox pxHostInp = new RoundedInputBox(proxyHost, false);
            pxHostInp.Dock = DockStyle.Fill;
            pxHostInp.Margin = Dpi.Pad(0, 2, 8, 2);

            RoundedInputBox pxUserInp = new RoundedInputBox(proxyUser, false);
            pxUserInp.Dock = DockStyle.Fill;
            pxUserInp.Margin = Dpi.Pad(0, 2, 8, 2);

            RoundedInputBox pxPassInp = new RoundedInputBox(proxyPass, false);
            pxPassInp.InnerTextBox.UseSystemPasswordChar = true;
            pxPassInp.Dock = DockStyle.Fill;
            pxPassInp.Margin = Dpi.Pad(0, 2, 0, 2);

            proxyInputsRow.Controls.Add(pxHostInp, 0, 0);
            proxyInputsRow.Controls.Add(pxUserInp, 1, 0);
            proxyInputsRow.Controls.Add(pxPassInp, 2, 0);

            FlowLayoutPanel proxyBtnsRow = new FlowLayoutPanel();
            proxyBtnsRow.Dock = DockStyle.Fill;
            proxyBtnsRow.FlowDirection = FlowDirection.LeftToRight;
            proxyBtnsRow.WrapContents = false;
            proxyBtnsRow.Padding = Dpi.Pad(0, 4, 0, 0);

            PillActionButton btnTestProxy = PillActionButton.CreateDark(Loc.T("Сохранить и проверить прокси", "Save & Test Proxy"), BgCard, "sync");
            btnTestProxy.TextColor = Gold;
            btnTestProxy.AutoSizeToText(26, 34);
            btnTestProxy.Margin = Dpi.Pad(0, 0, 10, 0);
            btnTestProxy.Click += (s, e) =>
            {
                proxyHost = pxHostInp.Value.Trim();
                proxyUser = pxUserInp.Value.Trim();
                proxyPass = pxPassInp.Value.Trim();
                SaveTokenVault();
                SaveState();

                if (string.IsNullOrEmpty(proxyHost))
                {
                    MessageBox.Show(dlg, Loc.T("Прокси очищен — используется прямое подключение.", "Proxy cleared — direct connection is active."), "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                btnTestProxy.Text = Loc.T("Проверка...", "Testing...");
                btnTestProxy.Invalidate();
                Thread pxTh = new Thread(() =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/gateway");
                        ApplyProxy(req);
                        req.Method = "GET";
                        req.Timeout = 10000;
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        {
                            sw.Stop();
                            dlg.BeginInvoke(new Action(() =>
                            {
                                btnTestProxy.Text = Loc.T("Сохранить и проверить прокси", "Save & Test Proxy");
                                btnTestProxy.Invalidate();
                                MessageBox.Show(dlg, string.Format(Loc.T("Прокси работает! Discord API доступен ({0} мс).", "Proxy works! Discord API reachable ({0} ms)."), sw.ElapsedMilliseconds), "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                if (!string.IsNullOrEmpty(discordToken))
                                {
                                    Thread syncTh = new Thread(() => PollDiscordApi(false));
                                    syncTh.IsBackground = true;
                                    syncTh.Start();
                                }
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        dlg.BeginInvoke(new Action(() =>
                        {
                            btnTestProxy.Text = Loc.T("Сохранить и проверить прокси", "Save & Test Proxy");
                            btnTestProxy.Invalidate();
                            MessageBox.Show(dlg, Loc.T("Не удалось подключиться через прокси:\r\n", "Proxy connection failed:\r\n") + ex.Message, "Discord Quest Studio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }));
                    }
                });
                pxTh.IsBackground = true;
                pxTh.Start();
            };

            PillActionButton btnClearProxy = PillActionButton.CreateDanger(Loc.T("Сбросить прокси", "Clear Proxy"), BgCard);
            btnClearProxy.AutoSizeToText(26, 34);
            btnClearProxy.Margin = Dpi.Pad(0, 0, 10, 0);
            btnClearProxy.Click += (s, e) =>
            {
                proxyHost = "";
                proxyUser = "";
                proxyPass = "";
                pxHostInp.Value = "";
                pxUserInp.Value = "";
                pxPassInp.Value = "";
                SaveTokenVault();
                SaveState();
            };

            Label lblProxyHint = new Label();
            lblProxyHint.Text = Loc.T("(Поля: IP:PORT  |  Логин  |  Пароль)", "(Fields: IP:PORT  |  Login  |  Password)");
            lblProxyHint.ForeColor = TextSub;
            lblProxyHint.Font = new Font("Segoe UI", Dpi.F(8.2f), FontStyle.Regular);
            lblProxyHint.AutoSize = true;
            lblProxyHint.Padding = Dpi.Pad(4, 8, 0, 0);

            proxyBtnsRow.Controls.Add(btnTestProxy);
            proxyBtnsRow.Controls.Add(btnClearProxy);
            proxyBtnsRow.Controls.Add(lblProxyHint);

            grid.Controls.Add(l1, 0, 0);
            grid.Controls.Add(tokInput, 0, 1);
            grid.Controls.Add(tokBtns, 0, 2);
            grid.Controls.Add(l2, 0, 3);
            grid.Controls.Add(jsonBox, 0, 4);
            grid.Controls.Add(impRow, 0, 5);
            grid.Controls.Add(l3, 0, 6);
            grid.Controls.Add(proxyInputsRow, 0, 7);
            grid.Controls.Add(proxyBtnsRow, 0, 8);

            host.Controls.Add(grid);
            dlg.ShowDialog(this);
        }

        private Form CreateBaseDialog(string title, int w, int h)
        {
            Form f = new Form();
            f.AutoScaleMode = AutoScaleMode.None;
            f.FormBorderStyle = FormBorderStyle.None;
            f.Text = title;
            if (appIcon != null) f.Icon = appIcon;
            f.BackColor = BgCard;
            f.ForeColor = TextMain;
            f.Font = new Font("Segoe UI", Dpi.F(9.5f), FontStyle.Regular);
            f.StartPosition = FormStartPosition.CenterParent;
            f.ClientSize = new Size(w, h + Dpi.S(34));
            f.MinimumSize = new Size(Dpi.S(420), Dpi.S(274));
            f.Padding = new Padding(1);
            f.MaximizeBox = false;
            f.MinimizeBox = false;
            f.HandleCreated += (s, e) =>
            {
                try
                {
                    int dark = 1;
                    DwmSetWindowAttribute(f.Handle, 20, ref dark, sizeof(int));
                    int roundPref = 2;
                    DwmSetWindowAttribute(f.Handle, 33, ref roundPref, sizeof(int));
                }
                catch { }
            };
            f.Paint += (s, e) =>
            {
                using (Pen bp = new Pen(Color.FromArgb(52, 60, 78), 1f))
                {
                    e.Graphics.DrawRectangle(bp, 0, 0, f.ClientSize.Width - 1, f.ClientSize.Height - 1);
                }
            };

            DarkGoldTitleBar dlgTitleBar = new DarkGoldTitleBar(f, appLogoBmp, title, false, false);

            Panel body = new Panel();
            body.Dock = DockStyle.Fill;
            body.Padding = Dpi.Pad(22, 16, 22, 14);
            body.BackColor = BgCard;

            Panel foot = new Panel();
            foot.Dock = DockStyle.Bottom;
            foot.Height = Dpi.S(56);
            foot.Padding = Dpi.Pad(22, 10, 22, 10);
            foot.BackColor = Color.FromArgb(15, 17, 22);

            PillActionButton btnClose = PillActionButton.CreateDark(Loc.T("Закрыть", "Close"), foot.BackColor);
            btnClose.AutoSizeToText(40, 34);
            btnClose.Dock = DockStyle.Right;
            btnClose.Click += (s, e) => f.Close();
            foot.Controls.Add(btnClose);

            f.Controls.Add(body);
            f.Controls.Add(foot);
            f.Controls.Add(dlgTitleBar);
            return f;
        }

        private void OnMainFormClosing(object sender, FormClosingEventArgs e)
        {
            try { EmergencyStopAll(); } catch { }
            try { SaveState(true); } catch { }
            try
            {
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
            }
            catch { }
        }

        [STAThread]
        public static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            try { SetCurrentProcessExplicitAppUserModelID("DiscordQuestStudio.DesktopApp"); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainStudioForm());
        }
    }
}
