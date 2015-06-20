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

namespace EloFactory_Ryze
{
    internal class Program
    {
        public const string ChampionName = "Ryze";

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
        public static Items.Item TearoftheGoddess = new Items.Item(3070, 0);
        public static Items.Item TearoftheGoddessCrystalScar = new Items.Item(3073, 0);
        public static Items.Item ArchangelsStaff = new Items.Item(3003, 0);
        public static Items.Item ArchangelsStaffCrystalScar = new Items.Item(3007, 0);
        public static Items.Item Manamune = new Items.Item(3004, 0);
        public static Items.Item ManamuneCrystalScar = new Items.Item(3008, 0);
        public static Items.Item SeraphsEmbrace = new Items.Item(3040, 0);
        public static int Muramana = 3042;

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

            Q = new Spell(SpellSlot.Q, 900f);
            W = new Spell(SpellSlot.W, 580f);
            E = new Spell(SpellSlot.E, 580f);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 60f, 1800f, true, SkillshotType.SkillshotLine);
            E.SetTargetted(0.20f, float.MaxValue);

            var ignite = Player.Spellbook.Spells.FirstOrDefault(spell => spell.Name == "summonerdot");
            if (ignite != null)
                Ignite.Slot = ignite.Slot;

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            abilitySequence = new int[] { 1, 2, 1, 3, 1, 4, 1, 2, 1, 2, 4, 2, 2, 3, 3, 4, 3, 3 };

            Config = new Menu(ChampionName + " By LuNi", ChampionName + " By LuNi", true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddSubMenu(new Menu("KS Mode", "KS Mode"));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Ryze.UseIgniteKS", "KS With Ignite").SetValue(true));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Ryze.UseQKS", "KS With Q").SetValue(true));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Ryze.UseWKS", "KS With W").SetValue(true));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Ryze.UseEKS", "KS With E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.UseQCombo", "Use Q In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.UseWCombo", "Use W In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.UseECombo", "Use E In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.UseRCombo", "Use R In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.AutoSeraphsEmbrace", "Auto Seraph's Embrace Usage").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.AutoSeraphsEmbraceMiniHP", "Minimum HP To Use Auto Seraph's Embrace").SetValue(new Slider(30, 0, 100)));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.AutoMuramana", "Auto Muramana Usage").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.AutoMuramanaMiniMana", "Minimum Mana To Use Auto Muramana").SetValue(new Slider(10, 0, 100)));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.AA", "AA Usage In Combo").SetValue(new StringList(new[] { "Minimum AA", "Inteligent AA", "No AA" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ryze.ComboActive", "Combo!").SetValue(new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Chase", "Chase"));
            Config.SubMenu("Chase").AddItem(new MenuItem("Ryze.UseWChase", "Use W When Chasing").SetValue(true));
            Config.SubMenu("Chase").AddItem(new MenuItem("Ryze.UseRChase", "Use R When Chasing").SetValue(false));
            Config.SubMenu("Chase").AddItem(new MenuItem("Ryze.UseRChaseMiniHP", "Minimum Target Health % To Use R When Chasing").SetValue(new Slider(100, 0, 100)));
            Config.SubMenu("Chase").AddItem(new MenuItem("Ryze.ChaseActive", "Chase !").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ryze.UseQHarass", "Use Q In Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ryze.QMiniManaHarass", "Minimum Mana To Use Q In Harass").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ryze.UseWHarass", "Use W In Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ryze.WMiniManaHarass", "Minimum Mana To Use W In Harass").SetValue(new Slider(20, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ryze.UseEHarass", "Use E In Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ryze.EMiniManaHarass", "Minimum Mana To Use E In Harass").SetValue(new Slider(20, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ryze.HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ryze.HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("LastHit", "LastHit"));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ryze.UseQLastHit", "Use Q In LastHit").SetValue(true));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ryze.QMiniManaLastHit", "Minimum Mana To Use Q In LastHit").SetValue(new Slider(35, 0, 100)));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ryze.UseWLastHit", "Use W In LastHit").SetValue(false));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ryze.WMiniManaLastHit", "Minimum Mana To Use W In LastHit").SetValue(new Slider(65, 0, 100)));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ryze.SafeWLastHit", "Never Use W In LastHit When Enemy Close To Your Position").SetValue(true));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ryze.UseELastHit", "Use E In LastHit").SetValue(false));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ryze.EMiniManaLastHit", "Minimum Mana To Use E In LastHit").SetValue(new Slider(35, 0, 100)));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ryze.NoPassiveProcLastHit", "Never Use Spell In LastHit When Passive Will Proc").SetValue(true));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ryze.UseQLaneClear", "Use Q in LaneClear").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ryze.QMiniManaLaneClear", "Minimum Mana To Use Q In LaneClear").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ryze.UseWLaneClear", "Use W in LaneClear").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ryze.WMiniManaLaneClear", "Minimum Mana To Use W In LaneClear").SetValue(new Slider(65, 0, 100)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ryze.SafeWLaneClear", "Never Use W In LaneClear When Enemy Close To Your Position").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ryze.UseELaneClear", "Use E in LaneClear").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ryze.EMiniManaLaneClear", "Minimum Mana To Use E In LaneClear").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ryze.EMiniMinionLaneClear", "Minimum Minion To Use E In LaneClear").SetValue(new Slider(4, 1, 6)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ryze.NoPassiveProcLaneClear", "Never Use Spell In LaneClear When Passive Will Proc").SetValue(false));

            Config.AddSubMenu(new Menu("JungleClear", "JungleClear"));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ryze.UseQJungleClear", "Use Q In JungleClear").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ryze.UseWJungleClear", "Use W In JungleClear").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ryze.UseEJungleClear", "Use E In JungleClear").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ryze.SafeJungleClear", "Never Use Spell In JungleClear When Enemy Close To Your Position").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddSubMenu(new Menu("Skin Changer", "Skin Changer"));
            Config.SubMenu("Misc").SubMenu("Skin Changer").AddItem(new MenuItem("Ryze.SkinChanger", "Use Skin Changer").SetValue(false));
            Config.SubMenu("Misc").SubMenu("Skin Changer").AddItem(new MenuItem("Ryze.SkinChangerName", "Skin choice").SetValue(new StringList(new[] { "Classic", "Human", "Tribal", "Uncle", "Triumphant", "Professor", "Zombie", "Dark Crystal", "Pirate" })));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ryze.AutoQEGC", "Auto Q On Gapclosers").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ryze.AutoWEGC", "Auto W On Gapclosers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ryze.AutoPotion", "Use Auto Potion").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ryze.AutoLevelSpell", "Auto Level Spell").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ryze.StackTearInFountain", "Auto Use Q to Stack Tear in Fountain").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ryze.AutoQTearMinMana", "Minimum mana to Stack Tear").SetValue(new Slider(90, 0, 100)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.Indigo)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(true, Color.Green)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "E range").SetValue(new Circle(true, Color.Green)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawOrbwalkTarget", "Draw Orbwalk target").SetValue(true));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawDmg", "Draw Combo Damage On Enemy Healthbar").SetValue(true));
            var DrawComboDmg = Config.SubMenu("Drawings").AddItem(new MenuItem("DrawDmg", "Draw Combo Damage On Enemy Healthbar").SetValue(true));
            Utility.HpBarDamageIndicator.DamageToUnit = getComboDamage;
            Utility.HpBarDamageIndicator.Enabled = Config.Item("DrawDmg").GetValue<bool>();
            DrawComboDmg.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            CustomEvents.Unit.OnDash += Unit_OnDash;

        }

        #region ToogleOrder Game_OnUpdate
        public static void Game_OnGameUpdate(EventArgs args)
        {
            Player.SetSkin(Player.BaseSkinName, Config.Item("Ryze.SkinChanger").GetValue<bool>() ? Config.Item("Ryze.SkinChangerName").GetValue<StringList>().SelectedIndex : Player.BaseSkinId);

            if (Config.Item("Ryze.AutoLevelSpell").GetValue<bool>()) LevelUpSpells();

            if (Player.IsDead) return;

            if (Player.GetBuffCount("Recall") == 1) return;

            ManaManager();
            PotionManager();

            KillSteal();

            if (Config.Item("Ryze.StackTearInFountain").GetValue<bool>() && Q.IsReady() && ObjectManager.Player.InFountain() && Player.ManaPercent >= Config.Item("Ryze.AutoQTearMinMana").GetValue<Slider>().Value &&
                (TearoftheGoddess.IsOwned(Player) || TearoftheGoddessCrystalScar.IsOwned(Player) || ArchangelsStaff.IsOwned(Player) || ArchangelsStaffCrystalScar.IsOwned(Player) || Manamune.IsOwned(Player) || ManamuneCrystalScar.IsOwned(Player)))
                Q.Cast(ObjectManager.Player, true, true);

            if (Config.Item("Ryze.ChaseActive").GetValue<KeyBind>().Active)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                Chase();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Config.Item("Ryze.ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                Orbwalking.Attack = true;
                JungleClear();
                LaneClear();

            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                Orbwalking.Attack = true;
                LastHit();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                Orbwalking.Attack = true;
                LastHit();
            }

            if (Config.Item("Ryze.HarassActive").GetValue<KeyBind>().Active || Config.Item("Ryze.HarassActiveT").GetValue<KeyBind>().Active)
            {
                Orbwalking.Attack = true;
                Harass();
            }

        }
        #endregion

        #region AntiGapCloser
        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {

            if (Config.Item("Ryze.AutoWEGC").GetValue<bool>() && W.IsReady() && Player.Mana >= WMANA && Player.Distance(gapcloser.Sender) <= W.Range)
            {
                W.Cast(gapcloser.Sender, true);
            }

            if (Config.Item("Ryze.AutoQEGC").GetValue<bool>() && Q.IsReady() && Player.Mana >= QMANA + WMANA && Player.Distance(gapcloser.Sender) < Q.Range)
            {
                Q.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High, true);
            }

        }
        #endregion

        #region OnProcessSpellCast
        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (Config.Item("Ryze.AutoSeraphsEmbrace").GetValue<bool>() && SeraphsEmbrace.IsReady() && Player.HealthPercent <= Config.Item("Ryze.AutoSeraphsEmbraceMiniHP").GetValue<Slider>().Value && (unit.IsValid<Obj_AI_Hero>() || unit.IsValid<Obj_AI_Turret>()) && unit.IsEnemy && args.Target.IsMe)
            {
                SeraphsEmbrace.Cast();
            }
        }
        #endregion

        #region On Dash
        static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            var useQ = Config.Item("Ryze.UseQCombo").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (!sender.IsEnemy) return;

            if (sender.NetworkId == target.NetworkId)
            {

                if (useQ && Q.IsReady() && Player.Mana >= QMANA && args.EndPos.Distance(Player) < Q.Range)
                {

                    var delay = (int)(args.EndTick - Game.Time - Q.Delay - 0.1f);
                    if (delay > 0)
                    {
                        Utility.DelayAction.Add(delay * 1000, () => Q.Cast(args.EndPos));
                    }
                    else
                    {
                        Q.Cast(args.EndPos);
                    }
                }
            }
        }
        #endregion

        #region Combo
        public static void Combo()
        {



            var useQ = Config.Item("Ryze.UseQCombo").GetValue<bool>();
            var useW = Config.Item("Ryze.UseWCombo").GetValue<bool>();
            var useE = Config.Item("Ryze.UseECombo").GetValue<bool>();
            var useR = Config.Item("Ryze.UseRCombo").GetValue<bool>();



            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                switch (Config.Item("Ryze.AA").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        {
                            var t = TargetSelector.GetTarget(ObjectManager.Player.AttackRange, TargetSelector.DamageType.Magical);
                            if (t.IsValidTarget() && ((Player.GetAutoAttackDamage(t) > t.Health) || (!Q.IsReady() && !W.IsReady() && !E.IsReady() && Player.Distance(t) < W.Range * 0.65) || (Player.Mana < QMANA || Player.Mana < WMANA || Player.Mana < EMANA)))
                                Orbwalking.Attack = true;
                            else
                                Orbwalking.Attack = false;
                            break;
                        }

                    case 1:
                        {
                            var t = TargetSelector.GetTarget(ObjectManager.Player.AttackRange, TargetSelector.DamageType.Magical);
                            if (t.IsValidTarget() && ((ObjectManager.Player.GetAutoAttackDamage(t) > t.Health) || (Player.Distance(t) <= W.Range - 150 && Player.MoveSpeed <= t.MoveSpeed && !W.IsReady() && !E.IsReady()) || (Player.Distance(t) <= W.Range - 75 && Player.MoveSpeed > t.MoveSpeed && !W.IsReady() && !E.IsReady())))
                                Orbwalking.Attack = true;
                            else
                                Orbwalking.Attack = false;
                            break;
                        }
                    case 2:
                        {
                            var t = TargetSelector.GetTarget(ObjectManager.Player.AttackRange, TargetSelector.DamageType.Magical);
                            if (t.IsValidTarget() && ObjectManager.Player.GetAutoAttackDamage(t) > t.Health || (Player.Mana < QMANA || Player.Mana < WMANA || Player.Mana < EMANA))
                                Orbwalking.Attack = true;
                            else
                                Orbwalking.Attack = false;
                            break;
                        }

                }

            }




            var target2 = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget())
            {

                #region Sort R combo mode
                if (useR && R.IsReady())
                {
                    RUsage();
                }
                #endregion

                #region Sort E combo mode
                if (useE && E.IsReady() && Player.Mana > EMANA)
                {

                    if (Player.Distance(target) <= E.Range)
                    {
                        E.Cast(target, true);
                    }

                }
                #endregion

                #region Sort W combo mode
                if (useW && W.IsReady() && Player.Mana >= WMANA)
                {

                    if (Player.Distance(target) <= W.Range)
                    {
                        W.Cast(target, true);
                    }

                }
                #endregion

                #region Sort Q combo mode
                if (useQ && Q.IsReady() && Player.Mana >= QMANA)
                {
                    var pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.High)
                    {
                        Q.Cast(pred.CastPosition);
                    }
                    else
                    {
                        var dEnemyPred =
                            HeroManager.Enemies.Where(e => e.NetworkId != target.NetworkId)
                                .Select(enemy => Q.GetPrediction(enemy))
                                .FirstOrDefault(pred2 => pred2.Hitchance >= HitChance.High);
                        if (dEnemyPred != null)
                        {
                            Q.Cast(dEnemyPred.CastPosition);
                        }
                    }
                }
                #endregion
            }

        }
        #endregion

        #region Harass
        public static void Harass()
        {

            var useQ = Config.Item("Ryze.UseQHarass").GetValue<bool>();
            var useW = Config.Item("Ryze.UseWHarass").GetValue<bool>();
            var useE = Config.Item("Ryze.UseEHarass").GetValue<bool>();

            var MinManaQ = Config.Item("Ryze.QMiniManaHarass").GetValue<Slider>().Value;
            var MinManaW = Config.Item("Ryze.WMiniManaHarass").GetValue<Slider>().Value;
            var MinManaE = Config.Item("Ryze.EMiniManaHarass").GetValue<Slider>().Value;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);


            #region Sort E Harass mode
            if (useE && E.IsReady() && Player.Mana > EMANA && Player.ManaPercent > MinManaE)
            {

                if (Player.Distance(target) <= E.Range)
                {
                    E.Cast(target, true);
                }

            }
            #endregion

            #region Sort W Harass mode
            if (useW && W.IsReady() && Player.Mana >= WMANA && Player.ManaPercent > MinManaW)
            {

                if (Player.Distance(target) <= W.Range)
                {
                    W.Cast(target, true);
                }

            }
            #endregion

            #region Sort Q Harass mode
            if (useQ && Q.IsReady() && Player.Mana >= QMANA && Player.ManaPercent > MinManaQ)
            {
                if (Player.Distance(target) < Q.Range)
                {
                    Q.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
                }
            }
            #endregion



        }
        #endregion

        #region Chase
        public static void Chase()
        {
            var useW = Config.Item("Ryze.UseWChase").GetValue<bool>();
            var useR = Config.Item("Ryze.UseRChase").GetValue<bool>();
            var THP = Config.Item("Ryze.UseRChaseMiniHP").GetValue<Slider>().Value;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (useW && W.IsReady() && Player.Distance(target) <= W.Range && Player.Mana >= WMANA)
            {
                W.Cast(target, true);
            }

            if (useR && R.IsReady() && Player.Distance(target) > W.Range && target.HealthPercent >= THP)
            {
                R.Cast(Player, true);
            }


        }
        #endregion

        #region LastHit
        public static void LastHit()
        {

            if (GetPassiveBuff == 4 && Config.Item("Ryze.NoPassiveProcLastHit").GetValue<bool>()) return;


            var useQ = Config.Item("Ryze.UseQLastHit").GetValue<bool>();
            var useW = Config.Item("Ryze.UseWLastHit").GetValue<bool>();
            var useE = Config.Item("Ryze.UseELastHit").GetValue<bool>();

            var MinManaQ = Config.Item("Ryze.QMiniManaLastHit").GetValue<Slider>().Value;
            var MinManaW = Config.Item("Ryze.WMiniManaLastHit").GetValue<Slider>().Value;
            var MinManaE = Config.Item("Ryze.EMiniManaLastHit").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            var MinionQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (useW && W.IsReady())
            {

                if (Player.GetAutoAttackDamage(MinionQ) < MinionQ.Health && Player.ManaPercent > MinManaW && Player.Distance(MinionQ) <= W.Range && MinionQ.Health < W.GetDamage(MinionQ) && MinionQ.Health < W.GetDamage(MinionQ))
                {
                    if (Config.Item("Ryze.SafeWLastHit").GetValue<bool>() && Player.CountEnemiesInRange(1500) > 0)
                    {
                        return;
                    }
                    else
                        W.Cast(MinionQ, true);
                }

            }

            if (useE && E.IsReady())
            {

                if (Player.GetAutoAttackDamage(MinionQ) < MinionQ.Health && Player.ManaPercent > MinManaE && Player.Distance(MinionQ) <= E.Range && MinionQ.Health < E.GetDamage(MinionQ))
                {
                    E.Cast(MinionQ, true);
                }

            }

            if (useQ && Q.IsReady())
            {
                var allMinionsQr = MinionManager.GetMinions(Player.AttackRange, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                foreach (var minion in allMinionsQr)
                {
                    if (Player.GetAutoAttackDamage(minion) < minion.Health && Player.ManaPercent > MinManaQ && minion.Health < Q.GetDamage(minion))
                    {
                        Q.CastIfHitchanceEquals(minion, HitChance.VeryHigh, true);
                    }
                }
            }

        }
        #endregion

        #region LaneClear
        public static void LaneClear()
        {

            if (GetPassiveBuff == 4 && Config.Item("Ryze.NoPassiveProcLaneClear").GetValue<bool>()) return;

            var useQ = Config.Item("Ryze.UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("Ryze.UseWLaneClear").GetValue<bool>();
            var useE = Config.Item("Ryze.UseELaneClear").GetValue<bool>();

            var MinManaQ = Config.Item("Ryze.QMiniManaLaneClear").GetValue<Slider>().Value;
            var MinManaW = Config.Item("Ryze.WMiniManaLaneClear").GetValue<Slider>().Value;
            var MinManaE = Config.Item("Ryze.EMiniManaLaneClear").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            var MinionQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (useW && W.IsReady())
            {

                if (Player.GetAutoAttackDamage(MinionQ) < MinionQ.Health && Player.ManaPercent > MinManaW && Player.Distance(MinionQ) <= W.Range && MinionQ.Health < W.GetDamage(MinionQ))
                {
                    if (Config.Item("Ryze.SafeWLaneClear").GetValue<bool>() && Player.CountEnemiesInRange(1500) > 0)
                    {
                        return;
                    }
                    else
                        W.Cast(MinionQ, true);
                }

            }

            if (useE && E.IsReady())
            {

                if (Player.GetAutoAttackDamage(MinionQ) < MinionQ.Health && Player.ManaPercent > MinManaE && Player.Distance(MinionQ) <= E.Range && allMinionsQ.Count >= Config.Item("Ryze.EMiniMinionLaneClear").GetValue<Slider>().Value)
                {
                    E.Cast(MinionQ, true);
                }

            }

            if (useQ && Q.IsReady())
            {
                var allMinionsQr = MinionManager.GetMinions(Player.AttackRange, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                foreach (var minion in allMinionsQr)
                {
                    if (Player.GetAutoAttackDamage(minion) < minion.Health && Player.ManaPercent > MinManaQ && minion.Health < Q.GetDamage(minion))
                    {
                        Q.CastIfHitchanceEquals(minion, HitChance.VeryHigh, true);
                    }
                }
            }

        }
        #endregion

        #region JungleClear
        public static void JungleClear()
        {

            if (Config.Item("Ryze.SafeJungleClear").GetValue<bool>() && Player.CountEnemiesInRange(1500) > 0) return;

            var useQ = Config.Item("Ryze.UseQJungleClear").GetValue<bool>();
            var useW = Config.Item("Ryze.UseWJungleClear").GetValue<bool>();
            var useE = Config.Item("Ryze.UseEJungleClear").GetValue<bool>();

            var MinionN = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (!MinionN.IsValidTarget() || MinionN == null)
            {
                LaneClear();
                return;
            }

            if (useE && E.IsReady() && Player.Distance(MinionN) <= E.Range && Player.Mana > EMANA)
            {
                E.Cast(MinionN, true);
            }

            if (useW && W.IsReady() && Player.Distance(MinionN) <= W.Range && Player.Mana > WMANA)
            {
                W.Cast(MinionN, true);
            }

            if (useQ && Q.IsReady() && Player.Distance(MinionN) < Q.Range && Player.Mana > QMANA)
            {
                Q.CastIfHitchanceEquals(MinionN, HitChance.VeryHigh, true);
            }

        }
        #endregion

        #region KillSteal
        public static void KillSteal()
        {

            var UseIgniteKS = Config.Item("Ryze.UseIgniteKS").GetValue<bool>();
            var useQKS = Config.Item("Ryze.UseQKS").GetValue<bool>();
            var useWKS = Config.Item("Ryze.UseWKS").GetValue<bool>();
            var useEKS = Config.Item("Ryze.UseEKS").GetValue<bool>();

            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => !target.IsMe && target.Team != ObjectManager.Player.Team))
            {

                if (useQKS && Q.IsReady() && Player.Mana >= QMANA && target.Health < Q.GetDamage(target) && Player.Distance(target) < Q.Range && !target.IsDead && target.IsValidTarget())
                {
                    Q.CastIfHitchanceEquals(target, HitChance.High, true);
                    return;
                }

                if (useEKS && E.IsReady() && Player.Mana >= EMANA && target.Health < E.GetDamage(target) && Player.Distance(target) <= E.Range && !target.IsDead && target.IsValidTarget())
                {
                    E.Cast(target, true);
                    return;
                }

                if (useWKS && W.IsReady() && Player.Mana >= WMANA && target.Health < W.GetDamage(target) && Player.Distance(target) <= W.Range && !target.IsDead && target.IsValidTarget())
                {
                    W.Cast(target, true);
                    return;
                }

                if (UseIgniteKS && Ignite.Slot != SpellSlot.Unknown && Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health && target.IsValidTarget(Ignite.Range))
                {
                    Ignite.Cast(target, true);
                }

                if (UseIgniteKS && useQKS && Q.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= QMANA && target.Health < Q.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                {
                    Q.CastIfHitchanceEquals(target, HitChance.High, true);
                    return;
                }

                if (UseIgniteKS && useEKS && E.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= EMANA && target.Health < E.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                {
                    E.Cast(target, true);
                    return;
                }

                if (UseIgniteKS && useWKS && W.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= WMANA && target.Health < W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                {
                    W.Cast(target, true);
                    return;
                }

                if (useQKS && useEKS && Q.IsReady() && E.IsReady() && Player.Mana >= QMANA + EMANA && target.Health < Q.GetDamage(target) + E.GetDamage(target) && Player.Distance(target) <= E.Range && !target.IsDead && target.IsValidTarget())
                {
                    E.Cast(target, true);
                    return;
                }

                if (useQKS && useWKS && Q.IsReady() && W.IsReady() && Player.Mana >= QMANA + WMANA && target.Health < Q.GetDamage(target) + W.GetDamage(target) && Player.Distance(target) <= W.Range && !target.IsDead && target.IsValidTarget())
                {
                    W.Cast(target, true);
                    return;
                }

                if (useEKS && useWKS && E.IsReady() && W.IsReady() && Player.Mana >= EMANA + WMANA && target.Health <= E.GetDamage(target) + W.GetDamage(target) && Player.Distance(target) <= E.Range && !target.IsDead && target.IsValidTarget())
                {
                    E.Cast(target, true);
                    return;
                }

                if (useQKS && useEKS && useWKS && Q.IsReady() && E.IsReady() && W.IsReady() && Player.Mana >= QMANA + EMANA + WMANA && target.Health < Q.GetDamage(target) + E.GetDamage(target) + W.GetDamage(target) && Player.Distance(target) <= W.Range && !target.IsDead && target.IsValidTarget())
                {
                    W.Cast(target, true);
                    return;
                }

                if (UseIgniteKS && useQKS && useEKS && Ignite.Slot != SpellSlot.Unknown && Q.IsReady() && E.IsReady() && Player.Mana >= QMANA + EMANA && target.Health < Q.GetDamage(target) + E.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                {
                    E.Cast(target, true);
                    return;
                }

                if (UseIgniteKS && useQKS && useWKS && Ignite.Slot != SpellSlot.Unknown && Q.IsReady() && W.IsReady() && Player.Mana >= QMANA + WMANA && target.Health < Q.GetDamage(target) + W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                {
                    W.Cast(target, true);
                    return;
                }

                if (UseIgniteKS && useEKS && useWKS && Ignite.Slot != SpellSlot.Unknown && E.IsReady() && W.IsReady() && Player.Mana >= EMANA + WMANA && target.Health <= E.GetDamage(target) + W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                {
                    E.Cast(target, true);
                    return;
                }

                if (UseIgniteKS && useQKS && useEKS && useWKS && Ignite.Slot != SpellSlot.Unknown && Q.IsReady() && E.IsReady() && W.IsReady() && Player.Mana >= QMANA + EMANA + WMANA && target.Health < Q.GetDamage(target) + E.GetDamage(target) + W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                {
                    W.Cast(target, true);
                    return;
                }

            }

        }
        #endregion

        #region Player Damage
        public static float getComboDamage(Obj_AI_Hero target)
        {
            float damage = 0f;
            if (GetPassiveBuff == 0)
            {
                if (Q.IsReady() && !W.IsReady() && !E.IsReady() && Player.Mana >= QMANA)
                {
                    damage += Q.GetDamage(target);
                    return damage;
                }
                if (W.IsReady() && !Q.IsReady() && !E.IsReady() && Player.Mana >= WMANA)
                {
                    damage += W.GetDamage(target);
                    return damage;
                }
                if (E.IsReady() && !Q.IsReady() && !W.IsReady() && Player.Mana >= EMANA)
                {
                    damage += E.GetDamage(target);
                    return damage;
                }
                if (Q.IsReady() && W.IsReady() && !E.IsReady() && Player.Mana >= QMANA + WMANA)
                {
                    damage += Q.GetDamage(target);
                    damage += W.GetDamage(target);
                    return damage;
                }
                if (Q.IsReady() && E.IsReady() && !W.IsReady() && Player.Mana >= QMANA + EMANA)
                {
                    damage += Q.GetDamage(target);
                    damage += E.GetDamage(target);
                    return damage;
                }
                if (W.IsReady() && E.IsReady() && !Q.IsReady() && Player.Mana >= QMANA + EMANA)
                {
                    damage += E.GetDamage(target);
                    damage += W.GetDamage(target);
                    return damage;
                }
                if (Q.IsReady() && W.IsReady() && E.IsReady() && Player.Mana >= QMANA + WMANA + EMANA)
                {
                    damage += Q.GetDamage(target);
                    damage += W.GetDamage(target);
                    damage += E.GetDamage(target);
                    return damage;
                }
                return damage;
            }

            if (GetPassiveBuff > 0)
            {
                if (Q.IsReady() && !W.IsReady() && !E.IsReady() && Player.Mana >= QMANA)
                {
                    damage += Q.GetDamage(target) * 2.5f;
                    return damage;
                }
                if (W.IsReady() && !Q.IsReady() && !E.IsReady() && Player.Mana >= WMANA)
                {
                    damage += W.GetDamage(target);
                    return damage;
                }
                if (E.IsReady() && !Q.IsReady() && !W.IsReady() && Player.Mana >= EMANA)
                {
                    damage += E.GetDamage(target);
                    return damage;
                }
                if (Q.IsReady() && W.IsReady() && !E.IsReady() && Player.Mana >= QMANA + WMANA)
                {
                    damage += Q.GetDamage(target) * 2.5f;
                    damage += W.GetDamage(target);
                    return damage;
                }
                if (Q.IsReady() && E.IsReady() && !W.IsReady() && Player.Mana >= QMANA + EMANA)
                {
                    damage += Q.GetDamage(target) * 2.5f;
                    damage += E.GetDamage(target);
                    return damage;
                }
                if (W.IsReady() && E.IsReady() && !Q.IsReady() && Player.Mana >= QMANA + EMANA)
                {
                    damage += E.GetDamage(target);
                    damage += W.GetDamage(target);
                    return damage;
                }
                if (Q.IsReady() && W.IsReady() && E.IsReady() && Player.Mana >= QMANA + WMANA + EMANA)
                {
                    damage += Q.GetDamage(target) * 2.5f;
                    damage += W.GetDamage(target);
                    damage += E.GetDamage(target);
                    return damage;
                }
                return damage;
            }
            return damage;
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

            if (Config.Item("Ryze.AutoPotion").GetValue<bool>() && !Player.InFountain() && !Player.IsRecalling() && !Player.IsDead)
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

        #region PassiveBuff
        private static int GetPassiveBuff
        {
            get
            {
                var data = ObjectManager.Player.Buffs.FirstOrDefault(b => b.DisplayName == "RyzePassiveStack");
                return data != null ? data.Count : 0;
            }
        }
        #endregion

        #region R Usage
        public static void RUsage()
        {

            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);


            if (Player.CountEnemiesInRange(1200) > 1)
            {
                R.Cast(Player, true);
                return;
            }

            if (Player.CountEnemiesInRange(1200) == 1)
            {
                if (Player.HealthPercent <= target.HealthPercent)
                {
                    R.Cast(Player, true);
                    return;
                }
                return;
            }

        }
        #endregion

        #region BeforeAA
        static void Orbwalking_BeforeAttack(LeagueSharp.Common.Orbwalking.BeforeAttackEventArgs args)
        {

            if (Config.Item("Ryze.AutoMuramana").GetValue<bool>())
            {
                int Muramanaitem = Items.HasItem(Muramana) ? 3042 : 3043;
                if (args.Target.IsValid<Obj_AI_Hero>() && args.Target.IsEnemy && Items.HasItem(Muramanaitem) && Items.CanUseItem(Muramanaitem) && Player.ManaPercent > Config.Item("Ryze.AutoMuramanaMiniMana").GetValue<Slider>().Value)
                {
                    if (!ObjectManager.Player.HasBuff("Muramana"))
                        Items.UseItem(Muramanaitem);
                }
                else if (ObjectManager.Player.HasBuff("Muramana") && Items.HasItem(Muramanaitem) && Items.CanUseItem(Muramanaitem))
                    Items.UseItem(Muramanaitem);
            }



        }
        #endregion
    }
}
