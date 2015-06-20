#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Security.AccessControl;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace EloFactory_Gangplank
{
    internal class Program
    {
        public const string ChampionName = "Gangplank";

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell Ignite = new Spell(SpellSlot.Unknown, 600);

        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;

        public static Items.Item HealthPotion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);
        public static Items.Item CrystallineFlask = new Items.Item(2041, 0);
        public static Items.Item BiscuitofRejuvenation = new Items.Item(2010, 0);

        public static Menu Config;

        private static Obj_AI_Hero Player;

        public static int[] abilitySequence;
        public static int qOff = 0, wOff = 0, eOff = 0, rOff = 0;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.ChampionName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 625f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1150);
            R = new Spell(SpellSlot.R, 25000f);

            R.SetSkillshot(0.7f, 200, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var ignite = Player.Spellbook.Spells.FirstOrDefault(spell => spell.Name == "summonerdot");
            if (ignite != null)
                Ignite.Slot = ignite.Slot;

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            abilitySequence = new int[] { 1, 3, 2, 1, 1, 4, 1, 2, 2, 2, 4, 2, 1, 3, 3, 4, 3, 3 };

            Config = new Menu(ChampionName + " By LuNi", ChampionName + " By LuNi", true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("Gangplank.UseQCombo", "Use Q In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Gangplank.UseWCombo", "Use W In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Gangplank.UseECombo", "Use E In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Gangplank.UseRCombo", "Use R In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Gangplank.UseEtoGap", "Use E To GapClose").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Gangplank.UseEtoGapDist", "Minimum Distance To GapClose With E").SetValue(new Slider(500, 0, 1200)));
            Config.SubMenu("Combo").AddSubMenu(new Menu("W Usage", "W Usage"));
            Config.SubMenu("Combo").SubMenu("W Usage").AddItem(new MenuItem("Gangplank.UseWAuto", "Auto W When Low HP").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").AddItem(new MenuItem("Gangplank.UseWAutoHP", "Minimum Health Percent To Auto W").SetValue(new Slider(20, 0, 100)));
            Config.SubMenu("Combo").SubMenu("W Usage").AddItem(new MenuItem("Gangplank.UseWAutoNoEnemy", "Auto W When Low HP And No Enemy Close to Your Position").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").AddItem(new MenuItem("Gangplank.UseWAutoHPNoEnemy", "Minimum Health Percent To Auto W When Low HP And No Enemy Close to Your Position").SetValue(new Slider(75, 0, 100)));
            Config.SubMenu("Combo").SubMenu("W Usage").AddSubMenu(new Menu("W Cleanse", "W Cleanse"));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.CleanseWithW", "Cleanse With W").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWBlind", "Blind").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWCharm", "Charm").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWFear", "Fear").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWPolymorph", "Polymorph").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWSilence", "Silence").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWSleep", "Sleep").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWSlow", "Slow").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWSnare", "Snare").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWStun", "Stun").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWTaunt", "Taunt").SetValue(true));
            Config.SubMenu("Combo").SubMenu("W Usage").SubMenu("W Cleanse").AddItem(new MenuItem("Gangplank.UseWPoison", "Use W On Poison").SetValue(false));
            Config.SubMenu("Combo").AddSubMenu(new Menu("KS Mode", "KS Mode"));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Gangplank.UseIgniteKS", "KS With Ignite").SetValue(true));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Gangplank.UseQKS", "KS With Q").SetValue(true));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Gangplank.UseRKS", "KS With R").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.UseQHarass", "Use Q In Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.QMiniManaHarass", "Minimum Mana To Use Q In Harass").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.QPriority", "Q Priority on Enemy").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.UseWHarass", "Use W In Harass").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.WMiniManaHarass", "Minimum Mana To Use W In Harass").SetValue(new Slider(70, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.WMiniHPHarass", "Minimum Health Percent To Use W In Harass").SetValue(new Slider(50, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.UseEHarass", "Use E In Harass").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.EMiniManaHarass", "Minimum Mana To Use E In Harass").SetValue(new Slider(70, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Gangplank.HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("LastHit", "LastHit"));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Gangplank.UseQLastHit", "Use Q In LastHit").SetValue(true));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Gangplank.QMiniManaLastHit", "Minimum Mana To Use Q In LastHit").SetValue(new Slider(35, 0, 100)));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Gangplank.SafeQLastHit", "Never Use Q In LastHit When Enemy Close To Your Position").SetValue(false));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Gangplank.UseQLaneClear", "Use Q in LaneClear").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Gangplank.QMiniManaLaneClear", "Minimum Mana To Use Q In LaneClear").SetValue(new Slider(35, 0, 100)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Gangplank.UseELaneClear", "Use E in LaneClear").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Gangplank.EMiniManaLaneClear", "Minimum Mana To Use E In LaneClear").SetValue(new Slider(70, 0, 100)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Gangplank.ECountLaneClear", "Minimum Minion To Use E In LaneClear").SetValue(new Slider(4, 1, 6)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Gangplank.SafeLaneClear", "Never Use Spell In LaneClear When Enemy Close To Your Position").SetValue(false));

            Config.AddSubMenu(new Menu("JungleClear", "JungleClear"));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Gangplank.UseQJungleClear", "Use Q In JungleClear").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Gangplank.QMiniManaJungleClear", "Minimum Mana To Use Q In JungleClear").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Gangplank.UseWJungleClear", "Use W In JungleClear").SetValue(false));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Gangplank.WMiniManaJungleClear", "Minimum Mana To Use W In JungleClear").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Gangplank.WMiniHPJungleClear", "Minimum Health Percent To Use W In JungleClear").SetValue(new Slider(50, 0, 100)));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Gangplank.UseEJungleClear", "Use E In JungleClear").SetValue(false));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Gangplank.EMiniManaJungleClear", "Minimum Mana To Use E In JungleClear").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Gangplank.SafeJungleClear", "Never Use Spell In JungleClear When Enemy Close To Your Position").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddSubMenu(new Menu("Skin Changer", "Skin Changer"));
            Config.SubMenu("Misc").SubMenu("Skin Changer").AddItem(new MenuItem("Gangplank.SkinChanger", "Use Skin Changer").SetValue(false));
            Config.SubMenu("Misc").SubMenu("Skin Changer").AddItem(new MenuItem("Gangplank.SkinChangerName", "Skin choice").SetValue(new StringList(new[] { "Classic", "Spooky", "Minuteman", "Sailor", "Toy Soldier", "Special Forces", "Sultan" })));
            Config.SubMenu("Misc").AddItem(new MenuItem("Gangplank.AutoEEGC", "Auto E On Gapclosers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Gangplank.AutoPotion", "Use Auto Potion").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Gangplank.AutoLevelSpell", "Auto Level Spell").SetValue(true));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.Indigo)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(true, Color.Gold)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawOrbwalkTarget", "Draw Orbwalk target").SetValue(true));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEGapCloseRange", "Draw E GapClose Range").SetValue(true));

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;


        }

        #region ToogleOrder Game_OnUpdate
        public static void Game_OnGameUpdate(EventArgs args)
        {
            Player.SetSkin(Player.BaseSkinName, Config.Item("Gangplank.SkinChanger").GetValue<bool>() ? Config.Item("Gangplank.SkinChangerName").GetValue<StringList>().SelectedIndex : Player.BaseSkinId);

            if (Config.Item("Gangplank.AutoLevelSpell").GetValue<bool>()) LevelUpSpells();

            if (Player.IsDead) return;

            if (Player.GetBuffCount("Recall") == 1) return;

            ManaManager();
            PotionManager();

            KillSteal();

            if (W.IsReady() && Player.Mana >= WMANA && Config.Item("Gangplank.CleanseWithW").GetValue<bool>())
            {

                if ((Config.Item("Gangplank.UseWBlind").GetValue<bool>() && Player.HasBuffOfType(BuffType.Blind)) ||
                   (Config.Item("Gangplank.UseWCharm").GetValue<bool>() && Player.HasBuffOfType(BuffType.Charm)) ||
                   (Config.Item("Gangplank.UseWFear").GetValue<bool>() && Player.HasBuffOfType(BuffType.Fear)) ||
                   (Config.Item("Gangplank.UseWPolymorph").GetValue<bool>() && Player.HasBuffOfType(BuffType.Polymorph)) ||
                   (Config.Item("Gangplank.UseWSilence").GetValue<bool>() && Player.HasBuffOfType(BuffType.Silence)) ||
                   (Config.Item("Gangplank.UseWSleep").GetValue<bool>() && Player.HasBuffOfType(BuffType.Sleep)) ||
                   (Config.Item("Gangplank.UseWSlow").GetValue<bool>() && Player.HasBuffOfType(BuffType.Slow)) ||
                   (Config.Item("Gangplank.UseWSnare").GetValue<bool>() && Player.HasBuffOfType(BuffType.Snare)) ||
                   (Config.Item("Gangplank.UseWStun").GetValue<bool>() && Player.HasBuffOfType(BuffType.Stun)) ||
                   (Config.Item("Gangplank.UseWTaunt").GetValue<bool>() && Player.HasBuffOfType(BuffType.Taunt)) ||
                   (Config.Item("Gangplank.UseWPoison").GetValue<bool>() && Player.HasBuffOfType(BuffType.Poison)))
                {
                    W.Cast();
                }

            }

            if (W.IsReady() && Player.Mana >= WMANA + QMANA && Config.Item("Gangplank.UseWAuto").GetValue<bool>() && Player.HealthPercent <= Config.Item("Gangplank.UseWAutoHP").GetValue<Slider>().Value)
            {
                W.Cast();
            }

            if (W.IsReady() && Player.Mana >= WMANA + QMANA && Config.Item("Gangplank.UseWAutoNoEnemy").GetValue<bool>() && Player.HealthPercent <= Config.Item("Gangplank.UseWAutoHPNoEnemy").GetValue<Slider>().Value && Player.CountEnemiesInRange(1500) == 0)
            {
                W.Cast();
            }


            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
                JungleClear();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                LastHit();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                LastHit();
            }

            if (Config.Item("Gangplank.HarassActive").GetValue<KeyBind>().Active || Config.Item("Gangplank.HarassActiveT").GetValue<KeyBind>().Active)
            {
                Harass();
            }

        }
        #endregion

        #region AntiGapCloser
        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            
            if (Config.Item("Gangplank.AutoEEGC").GetValue<bool>() && E.IsReady() && Player.Mana >= EMANA && Player.Distance(gapcloser.Sender) <= Q.Range)
            {
                E.Cast();
            }

        }
        #endregion

        #region Combo
        public static void Combo()
        {

            var useQ = Program.Config.Item("Gangplank.UseQCombo").GetValue<bool>();
            var useE = Program.Config.Item("Gangplank.UseECombo").GetValue<bool>();
            var useR = Program.Config.Item("Gangplank.UseRCombo").GetValue<bool>();

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget())
            {

                #region Sort R combo mode
                if (useR && R.IsReady() && Player.Mana >= RMANA)
                {
                    if (ComboDamage(target) > target.Health && Player.Distance(target) <= Q.Range && ComboDamageNoUlt(target) < target.Health)
                    {
                        R.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                }
                #endregion

                #region Sort Q combo mode
                if (useQ && Q.IsReady() && Player.Mana >= QMANA)
                {
                    Q.Cast(target, true);
                }
                #endregion

                #region Sort E combo mode
                if (useE && E.IsReady() && Player.Mana >= EMANA)
                {
                    if (Config.Item("Gangplank.UseEtoGap").GetValue<bool>() && Player.Distance(target) >= Config.Item("Gangplank.UseEtoGapDist").GetValue<Slider>().Value)
                    {
                        E.Cast();
                    }

                    if (Player.Distance(target) <= Player.AttackRange)
                    {
                        E.Cast();
                    }
                }
                #endregion
            }

        }
        #endregion

        #region Harass
        public static void Harass()
        {

            var targetH = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            var useQ = Config.Item("Gangplank.UseQHarass").GetValue<bool>();
            var useW = Config.Item("Gangplank.UseWHarass").GetValue<bool>();
            var useE = Config.Item("Gangplank.UseEHarass").GetValue<bool>();

            var QMinMana = Config.Item("Gangplank.QMiniManaHarass").GetValue<Slider>().Value;
            var WMinMana = Config.Item("Gangplank.WMiniManaHarass").GetValue<Slider>().Value;
            var EMinMana = Config.Item("Gangplank.EMiniManaHarass").GetValue<Slider>().Value;

            var WMiniHP = Config.Item("Gangplank.WMiniHPHarass").GetValue<Slider>().Value;


            if (useW && W.IsReady() && Player.ManaPercent >= WMinMana && Player.HealthPercent <= WMiniHP)
            {
                W.Cast();
            }

            if (useQ && Q.IsReady() && Player.Distance(targetH) <= Q.Range && Player.ManaPercent >= QMinMana)
            {
                Q.Cast(targetH, true);
            }

            if (useE && E.IsReady() && Player.Distance(targetH) <= Player.AttackRange && Player.ManaPercent >= EMinMana)
            {
                E.Cast();
            }


        }
        #endregion

        #region LastHit
        public static void LastHit()
        {

            var useQ = Config.Item("Gangplank.UseQLastHit").GetValue<bool>();

            var QMinMana = Config.Item("Gangplank.QMiniManaLastHit").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy);
            var MinionQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth).FirstOrDefault();

            foreach (var minion in allMinionsQ)
            {
                if (useQ && Q.IsReady() && minion.Health < Q.GetDamage(minion) * 0.9 && Player.ManaPercent >= QMinMana)
                {
                    if (Config.Item("Gangplank.QPriority").GetValue<bool>() && Player.CountEnemiesInRange(Q.Range) > 0 && Config.Item("Gangplank.UseQHarass").GetValue<bool>() && Player.ManaPercent >= Config.Item("Gangplank.QMiniManaHarass").GetValue<Slider>().Value)
                    {
                        return;
                    }
                    else
                    Q.Cast(minion);
                }
            }

        }
        #endregion

        #region LaneClear
        public static void LaneClear()
        {

            var useQ = Config.Item("Gangplank.UseQLaneClear").GetValue<bool>();
            var useE = Config.Item("Gangplank.UseELaneClear").GetValue<bool>();

            var QMinMana = Config.Item("Gangplank.QMiniManaLaneClear").GetValue<Slider>().Value;
            var EMinMana = Config.Item("Gangplank.EMiniManaLaneClear").GetValue<Slider>().Value;

            var EMinionCount = Config.Item("Gangplank.ECountLaneClear").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All);

            if (Config.Item("Gangplank.SafeLaneClear").GetValue<bool>() && Player.CountEnemiesInRange(1500) != 0) return;

            foreach (var minion in allMinionsQ)
            {
                if (useQ && Q.IsReady() && Player.Mana > QMANA && minion.Health < Q.GetDamage(minion) * 0.9 && Player.ManaPercent >= QMinMana)
                {
                    Q.Cast(minion);
                }
            }

            if (useE && E.IsReady() && Player.Mana >= EMANA && Player.ManaPercent >= EMinMana)
            {
                if (allMinionsQ.Count(x => x.IsValidTarget(Q.Range)) >= EMinionCount)
                {
                    E.Cast();
                }
            }

        }
        #endregion

        #region JungleClear
        public static void JungleClear()
        {

            var useQ = Program.Config.Item("Gangplank.UseQJungleClear").GetValue<bool>();
            var useW = Program.Config.Item("Gangplank.UseWJungleClear").GetValue<bool>();
            var useE = Program.Config.Item("Gangplank.UseWJungleClear").GetValue<bool>();

            var QMinMana = Config.Item("Gangplank.QMiniManaHarass").GetValue<Slider>().Value;
            var WMinMana = Config.Item("Gangplank.WMiniManaHarass").GetValue<Slider>().Value;
            var EMinMana = Config.Item("Gangplank.EMiniManaHarass").GetValue<Slider>().Value;

            var WMiniHP = Config.Item("Gangplank.WMiniHPJungleClear").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral);
            var MinionN = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (Config.Item("Gangplank.SafeJungleClear").GetValue<bool>() && Player.CountEnemiesInRange(1500) > 0) return;

            if (!MinionN.IsValidTarget() || MinionN == null)
            {
                LaneClear();
                return;
            }

            if (useW && W.IsReady() && Player.HealthPercent <= WMiniHP && Player.Mana >= WMANA && Player.ManaPercent >= WMinMana)
            {
                W.Cast();
            }

            if (useQ && Q.IsReady() && Player.Distance(MinionN) <= Q.Range && Player.Mana >= QMANA && Player.ManaPercent >= QMinMana)
            {
                Q.Cast(MinionN, true);
            }

            if (useE && E.IsReady() && Player.Mana >= EMANA && Player.ManaPercent >= EMinMana && Player.Distance(MinionN) <= Player.AttackRange)
            {
                E.Cast();
            }

        }
        #endregion

        #region KillSteal
        public static void KillSteal()
        {

            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => !target.IsMe && target.Team != ObjectManager.Player.Team))
            {

                if (Config.Item("Gangplank.UseQKS").GetValue<bool>() && Q.IsReady() && Player.Mana >= QMANA && target.Health < Q.GetDamage(target) && Player.Distance(target) <= Q.Range && !target.IsDead && target.IsValidTarget())
                {
                    Q.Cast(target, true);
                    return;
                }

                if (Ignite.Slot != SpellSlot.Unknown && Config.Item("Gangplank.UseIgniteKS").GetValue<bool>() && Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health && target.IsValidTarget(Ignite.Range) && ((!Q.IsReady() && (Player.Distance(target) > Player.AttackRange || target.Health > Player.GetAutoAttackDamage(target))) || !Config.Item("Gangplank.UseQKS").GetValue<bool>() || (Player.Mana < QMANA && (Player.Distance(target) > Player.AttackRange || target.Health > Player.GetAutoAttackDamage(target))) || target.Health > Q.GetDamage(target) ||
                    (target.Health > Player.GetAutoAttackDamage(target) && Player.Distance(target) <= Player.AttackRange)))
                {
                    Ignite.Cast(target, true);
                }

                if (Config.Item("Gangplank.UseRKS").GetValue<bool>() && R.IsReady() && Player.Mana >= RMANA && target.Health < R.GetDamage(target) * 1.5 && !target.IsDead && target.IsValidTarget() && (Player.Distance(target) > Q.Range || Player.HealthPercent < 30 || Player.CountEnemiesInRange(1200) > 1))
                {
                    R.CastIfHitchanceEquals(target, HitChance.High, true);
                    return;
                }

                if (Config.Item("Gangplank.UseQKS").GetValue<bool>() && Q.IsReady() && Ignite.Slot != SpellSlot.Unknown && Config.Item("Gangplank.UseIgniteKS").GetValue<bool>() && Player.Mana >= QMANA && target.Health < Q.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) <= 600 && !target.IsDead && target.IsValidTarget())
                {
                    Q.Cast(target, true);
                    return;
                }

            }
        }
        #endregion

        #region PlayerDamage
        public static float ComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0;
            if (R.IsReady())
            {
                damage += (float)Damage.GetSpellDamage(Player, hero, SpellSlot.R) * 4;
            }
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(Player, hero, SpellSlot.Q) * 3;
            }
            if (Player.Spellbook.CanUseSpell(Player.GetSpellSlot("summonerdot")) == SpellState.Ready)
            {
                damage += (float)Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            return (float)damage;
        }

        public static float ComboDamageNoUlt(Obj_AI_Hero hero)
        {
            double damage = 0;
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(Player, hero, SpellSlot.Q) * 3;
            }
            if (Player.Spellbook.CanUseSpell(Player.GetSpellSlot("summonerdot")) == SpellState.Ready)
            {
                damage += (float)Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            return (float)damage;
        }
        #endregion

        #region ManaManager
        public static void ManaManager()
        {

            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            RMANA = R.Instance.ManaCost;

            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }
        #endregion

        #region PotionManager
        public static void PotionManager()
        {
            if (Player.Level == 1 && Player.CountEnemiesInRange(1000) == 1 && Player.Health >= Player.MaxHealth * 0.35) return;
            if (Player.Level == 1 && Player.CountEnemiesInRange(1000) == 2 && Player.Health >= Player.MaxHealth * 0.50) return;

            if (Config.Item("Gangplank.AutoPotion").GetValue<bool>() && !Player.InFountain() && !Player.IsRecalling() && !Player.IsDead)
            {
                #region BiscuitofRejuvenation
                if (BiscuitofRejuvenation.IsReady() && !Player.HasBuff("ItemMiniRegenPotion") && !Player.HasBuff("ItemCrystalFlask"))
                {

                    if (Player.MaxHealth > Player.Health + 170 && Player.MaxMana > Player.Mana + 10 && Player.CountEnemiesInRange(1000) > 0 &&
                        Player.Health < Player.MaxHealth * 0.75)
                    {
                        BiscuitofRejuvenation.Cast();
                    }

                    else if (Player.MaxHealth > Player.Health + 170 && Player.MaxMana > Player.Mana + 10 && Player.CountEnemiesInRange(1000) == 0 &&
                        Player.Health < Player.MaxHealth * 0.6)
                    {
                        BiscuitofRejuvenation.Cast();
                    }

                }
                #endregion

                #region HealthPotion
                else if (HealthPotion.IsReady() && !Player.HasBuff("RegenerationPotion") && !Player.HasBuff("ItemCrystalFlask"))
                {

                    if (Player.MaxHealth > Player.Health + 150 && Player.CountEnemiesInRange(1000) > 0 &&
                        Player.Health < Player.MaxHealth * 0.75)
                    {
                        HealthPotion.Cast();
                    }

                    else if (Player.MaxHealth > Player.Health + 150 && Player.CountEnemiesInRange(1000) == 0 &&
                        Player.Health < Player.MaxHealth * 0.6)
                    {
                        HealthPotion.Cast();
                    }

                }
                #endregion

                #region CrystallineFlask
                else if (CrystallineFlask.IsReady() && !Player.HasBuff("ItemCrystalFlask") && !Player.HasBuff("RegenerationPotion") && !Player.HasBuff("FlaskOfCrystalWater") && !Player.HasBuff("ItemMiniRegenPotion"))
                {

                    if (Player.MaxHealth > Player.Health + 120 && Player.MaxMana > Player.Mana + 60 && Player.CountEnemiesInRange(1000) > 0 &&
                        (Player.Health < Player.MaxHealth * 0.85 || Player.Mana < Player.MaxMana * 0.65))
                    {
                        CrystallineFlask.Cast();
                    }

                    else if (Player.MaxHealth > Player.Health + 120 && Player.MaxMana > Player.Mana + 60 && Player.CountEnemiesInRange(1000) == 0 &&
                        (Player.Health < Player.MaxHealth * 0.7 || Player.Mana < Player.MaxMana * 0.5))
                    {
                        CrystallineFlask.Cast();
                    }

                }
                #endregion

                #region ManaPotion
                else if (ManaPotion.IsReady() && !Player.HasBuff("FlaskOfCrystalWater") && !Player.HasBuff("ItemCrystalFlask"))
                {

                    if (Player.MaxMana > Player.Mana + 100 && Player.CountEnemiesInRange(1000) > 0 &&
                        Player.Mana < Player.MaxMana * 0.7)
                    {
                        ManaPotion.Cast();
                    }

                    else if (Player.MaxMana > Player.Mana + 100 && Player.CountEnemiesInRange(1000) == 0 &&
                        Player.Mana < Player.MaxMana * 0.4)
                    {
                        ManaPotion.Cast();
                    }

                }
                #endregion
            }
        }
        #endregion

        #region DrawingRange
        public static void Drawing_OnDraw(EventArgs args)
        {

            if (Config.Item("DrawEGapCloseRange").GetValue<bool>())
                Utility.DrawCircle(Player.Position, Config.Item("Gangplank.UseEtoGapDist").GetValue<Slider>().Value, (Player.CountEnemiesInRange(Config.Item("Gangplank.UseEtoGapDist").GetValue<Slider>().Value) > 0) ? Color.OrangeRed : Color.Green, 10, 10);

            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && (spell.Slot != SpellSlot.R || R.Level > 0))
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            if (Config.Item("DrawOrbwalkTarget").GetValue<bool>())
            {
                var orbT = Orbwalker.GetTarget();
                if (orbT.IsValidTarget())
                    Render.Circle.DrawCircle(orbT.Position, 100, System.Drawing.Color.Pink);
            }

        }
        #endregion

        #region Up Spell
        private static void LevelUpSpells()
        {
            int qL = Player.Spellbook.GetSpell(SpellSlot.Q).Level + qOff;
            int wL = Player.Spellbook.GetSpell(SpellSlot.W).Level + wOff;
            int eL = Player.Spellbook.GetSpell(SpellSlot.E).Level + eOff;
            int rL = Player.Spellbook.GetSpell(SpellSlot.R).Level + rOff;
            if (qL + wL + eL + rL < ObjectManager.Player.Level)
            {
                int[] level = new int[] { 0, 0, 0, 0 };
                for (int i = 0; i < ObjectManager.Player.Level; i++)
                {
                    level[abilitySequence[i] - 1] = level[abilitySequence[i] - 1] + 1;
                }
                if (qL < level[0]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                if (wL < level[1]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                if (eL < level[2]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                if (rL < level[3]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);

            }
        }
        #endregion

    }

}
