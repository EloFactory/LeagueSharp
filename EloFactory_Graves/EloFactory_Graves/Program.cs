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

namespace EloFactory_Graves
{
    internal class Program
    {
        public const string ChampionName = "Graves";

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

            Q = new Spell(SpellSlot.Q, 900);
            W = new Spell(SpellSlot.W, 950f);
            E = new Spell(SpellSlot.E, 450f);
            R = new Spell(SpellSlot.R, 1500f);

            Q.SetSkillshot(0.26f, 200f, 1950f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.35f, 250f, 1650f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 120f, 2100f, false, SkillshotType.SkillshotLine);

            var ignite = Player.Spellbook.Spells.FirstOrDefault(spell => spell.Name == "summonerdot");
            if (ignite != null)
                Ignite.Slot = ignite.Slot;

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            abilitySequence = new int[] { 1, 3, 1, 2, 1, 4, 1, 3, 1, 3, 4, 3, 3, 2, 2, 4, 2, 2 };

            Config = new Menu(ChampionName + " By LuNi", ChampionName + " By LuNi", true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("Graves.UseQCombo", "Use Q In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Graves.UseWCombo", "Use W In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Graves.UseECombo", "Use E In Combo").SetValue(true));
            Config.SubMenu("Combo").AddSubMenu(new Menu("KS Mode", "KS Mode"));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Graves.UseEKS", "GapClose With E To KS If Needed").SetValue(true));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Graves.UseIgniteKS", "KS With Ignite").SetValue(true));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Graves.UseQKS", "KS With Q").SetValue(true));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Graves.UseWKS", "KS With W").SetValue(true));
            Config.SubMenu("Combo").SubMenu("KS Mode").AddItem(new MenuItem("Graves.UseRKS", "KS With R").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("Graves.UseQHarass", "Use Q In Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Graves.QMiniManaHarass", "Minimum Mana To Use Q In Harass").SetValue(new Slider(20, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Graves.UseQOnlyEnemyAAHarass", "Use Q Only When Enemy Use AA In Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Graves.UseWHarass", "Use W In Harass").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("Graves.WMiniManaHarass", "Minimum Mana To Use W In Harass").SetValue(new Slider(70, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Graves.UseWOnlyEnemyAAHarass", "Use W Only When Enemy Use AA In Harass").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("Graves.HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Graves.HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Graves.UseQLaneClear", "Use Q in LaneClear").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Graves.QMiniManaLaneClear", "Minimum Mana To Use Q In LaneClear").SetValue(new Slider(70, 0, 100)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Graves.QLaneClearCount", "Minimum Minion To Use Q In LaneClear").SetValue(new Slider(4, 1, 6)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Graves.UseWLaneClear", "Use W in LaneClear").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Graves.WMiniManaLaneClear", "Minimum Mana To Use W In LaneClear").SetValue(new Slider(70, 0, 100)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Graves.WLaneClearCount", "Minimum Minion To Use W In LaneClear").SetValue(new Slider(4, 1, 6)));

            Config.AddSubMenu(new Menu("JungleClear", "JungleClear"));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Graves.UseQJungleClear", "Use Q In JungleClear").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Graves.QMiniManaJungleClear", "Minimum Mana To Use Q In JungleClear").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Graves.UseWJungleClear", "Use W In JungleClear").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Graves.WMiniManaJungleClear", "Minimum Mana To Use W In JungleClear").SetValue(new Slider(40, 0, 100)));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Graves.SafeJungleClear", "Dont Use Spell In Jungle Clear If Enemy in Dangerous Range").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddSubMenu(new Menu("Skin Changer", "Skin Changer"));
            Config.SubMenu("Misc").SubMenu("Skin Changer").AddItem(new MenuItem("Graves.SkinChanger", "Use Skin Changer").SetValue(false));
            Config.SubMenu("Misc").SubMenu("Skin Changer").AddItem(new MenuItem("Graves.SkinChangerName", "Skin choice").SetValue(new StringList(new[] { "Classic", "Hired Gun", "Jailbreak", "Mafia", "Riot", "Pool Party" })));
            Config.SubMenu("Misc").AddItem(new MenuItem("Graves.AutoQEGC", "Auto Q On Gapclosers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Graves.AutoWEGC", "Auto W On Gapclosers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Graves.AutoPotion", "Use Auto Potion").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Graves.AutoLevelSpell", "Auto Level Spell").SetValue(true));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.Indigo)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(true, Color.Green)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(true, Color.Green)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(true, Color.Gold)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawOrbwalkTarget", "Draw Orbwalk target").SetValue(true));

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

        }

        #region ToogleOrder Game_OnUpdate
        public static void Game_OnGameUpdate(EventArgs args)
        {
            Player.SetSkin(Player.BaseSkinName, Config.Item("Graves.SkinChanger").GetValue<bool>() ? Config.Item("Graves.SkinChangerName").GetValue<StringList>().SelectedIndex : Player.BaseSkinId);

            if (Config.Item("Graves.AutoLevelSpell").GetValue<bool>()) LevelUpSpells();

            if (Player.IsDead) return;

            if (Player.GetBuffCount("Recall") == 1) return;

            ManaManager();
            PotionManager();

            KillSteal();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
                JungleClear();
            }

            if (Config.Item("Graves.HarassActive").GetValue<KeyBind>().Active || Config.Item("Graves.HarassActiveT").GetValue<KeyBind>().Active)
            {
                Harass();
            }

        }
        #endregion

        #region Interupt OnProcessSpellCast
        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {

            #region Combo
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                var useW = Config.Item("Graves.UseWCombo").GetValue<bool>();

                if (useW && (unit.IsValid<Obj_AI_Hero>() && !unit.IsValid<Obj_AI_Turret>()) && unit.IsEnemy && args.Target.IsMe && args.SData.IsAutoAttack() && W.IsReady() && Player.Distance(unit) < W.Range)
                {
                    W.CastIfHitchanceEquals(unit, HitChance.High, true);
                }
            }
            #endregion

            #region Harass
            if (Config.Item("Graves.HarassActive").GetValue<KeyBind>().Active || Config.Item("Graves.HarassActiveT").GetValue<KeyBind>().Active)
            {
                var useQ = Config.Item("Graves.UseQHarass").GetValue<bool>();
                var useW = Config.Item("Graves.UseWHarass").GetValue<bool>();
                var useQOnAA = Config.Item("Graves.UseQOnlyEnemyAAHarass").GetValue<bool>();
                var useWOnAA = Config.Item("Graves.UseWOnlyEnemyAAHarass").GetValue<bool>();
                var QMinMana = Config.Item("Graves.QMiniManaHarass").GetValue<Slider>().Value;
                var WMinMana = Config.Item("Graves.WMiniManaHarass").GetValue<Slider>().Value;

                if (useQ && useQOnAA && (unit.IsValid<Obj_AI_Hero>() && !unit.IsValid<Obj_AI_Turret>()) && unit.IsEnemy && args.Target.IsMe && args.SData.IsAutoAttack() && Q.IsReady() && Player.Distance(unit) < Q.Range && Player.Mana >= QMANA && Player.ManaPercent >= QMinMana)
                {
                    Q.CastIfHitchanceEquals(unit, HitChance.High, true);
                }

                if (useW && useWOnAA && (unit.IsValid<Obj_AI_Hero>() && !unit.IsValid<Obj_AI_Turret>()) && unit.IsEnemy && args.Target.IsMe && args.SData.IsAutoAttack() && W.IsReady() && Player.Distance(unit) < W.Range && Player.Mana >= WMANA && Player.ManaPercent >= WMinMana)
                {
                    W.CastIfHitchanceEquals(unit, HitChance.High, true);
                }
            }
            #endregion
        }
        #endregion

        #region AntiGapCloser
        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {

            if (Config.Item("Graves.AutoQEGC").GetValue<bool>() && Q.IsReady() && Player.Mana > QMANA && Player.Distance(gapcloser.Sender) <= Q.Range)
            {
                Q.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High, true);
            }

            if (Config.Item("Graves.AutoWEGC").GetValue<bool>() && W.IsReady() && Player.Mana > WMANA + QMANA && Player.Distance(gapcloser.Sender) < 475)
            {
                W.Cast(Player.ServerPosition, true);
            }

        }
        #endregion

        #region Combo
        public static void Combo()
        {

            var useQ = Config.Item("Graves.UseQCombo").GetValue<bool>();
            var useW = Config.Item("Graves.UseWCombo").GetValue<bool>();

            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget())
            {

                #region Sort W combo mode
                if (useW && W.IsReady() && Player.Mana >= WMANA + QMANA + RMANA)
                {

                    if (target.CountEnemiesInRange(250) > 1)
                    {
                        W.Cast(target, true, true);
                    }

                }
                #endregion

                #region Sort Q combo mode
                if (useQ && Q.IsReady() && Player.Mana >= QMANA)
                {
                    if (target.CountEnemiesInRange(50) != 0 && Player.Distance(target) < 500)
                    {
                        Q.Cast(target, true, true);
                    }

                    else if (target.CountEnemiesInRange(100) != 0 && Player.Distance(target) < 700)
                    {
                        Q.Cast(target, true, true);
                    }

                    else if (target.CountEnemiesInRange(200) != 0 && Player.Distance(target) < Q.Range)
                    {
                        Q.Cast(target, true, true);
                    }

                    else if (Player.Distance(target) < Q.Range - 50)
                    {
                        Q.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                }
                #endregion

            }

        }
        #endregion

        #region Harass
        public static void Harass()
        {

            var targetH = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            var useQ = Config.Item("Graves.UseQHarass").GetValue<bool>();
            var useW = Config.Item("Graves.UseWHarass").GetValue<bool>();
            var QMinMana = Config.Item("Graves.QMiniManaHarass").GetValue<Slider>().Value;
            var WMinMana = Config.Item("Graves.WMiniManaHarass").GetValue<Slider>().Value;
            var useQOnAA = Config.Item("Graves.UseQOnlyEnemyAAHarass").GetValue<bool>();
            var useWOnAA = Config.Item("Graves.UseWOnlyEnemyAAHarass").GetValue<bool>();

            if (!useQOnAA && useQ && Q.IsReady() && Player.Distance(targetH) < Q.Range - 50 && Player.Mana >= QMANA && Player.ManaPercent >= QMinMana)
            {
                Q.CastIfHitchanceEquals(targetH, HitChance.High, true);
            }

            if (!useWOnAA && useW && W.IsReady() && Player.Distance(targetH) < W.Range - 50 && Player.Mana >= WMANA && Player.ManaPercent >= WMinMana)
            {
                W.CastIfHitchanceEquals(targetH, HitChance.High, true);
            }

        }
        #endregion

        #region LaneClear
        public static void LaneClear()
        {

            var useQ = Config.Item("Graves.UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("Graves.UseWLaneClear").GetValue<bool>();

            var QMinMana = Config.Item("Graves.QMiniManaLaneClear").GetValue<Slider>().Value;
            var WMinMana = Config.Item("Graves.WMiniManaLaneClear").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All);

            if (useQ && Q.IsReady() && Player.Mana >= QMANA && Player.ManaPercent >= QMinMana)
            {
                var Qfarm = Q.GetCircularFarmLocation(allMinionsQ, Q.Width);
                if (Qfarm.MinionsHit >= Config.Item("Graves.QLaneClearCount").GetValue<Slider>().Value)
                    Q.Cast(Qfarm.Position);
            }

            if (useW && W.IsReady() && Player.Mana >= WMANA && Player.ManaPercent >= WMinMana)
            {
                var Wfarm = W.GetCircularFarmLocation(allMinionsQ, W.Width);
                if (Wfarm.MinionsHit >= Config.Item("Graves.WLaneClearCount").GetValue<Slider>().Value)
                    W.Cast(Wfarm.Position);
            }

        }
        #endregion

        #region JungleClear
        public static void JungleClear()
        {

            var useQ = Config.Item("Graves.UseQJungleClear").GetValue<bool>();
            var useW = Config.Item("Graves.UseWJungleClear").GetValue<bool>();

            var QMinMana = Config.Item("Graves.QMiniManaJungleClear").GetValue<Slider>().Value;
            var WMinMana = Config.Item("Graves.WMiniManaJungleClear").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral);
            var MinionN = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (Config.Item("Graves.SafeJungleClear").GetValue<bool>() && Player.CountEnemiesInRange(1500) > 0) return;

            if (!MinionN.IsValidTarget() || MinionN == null)
            {
                LaneClear();
                return;
            }

            if (useQ && Q.IsReady() && Player.Mana >= QMANA && Player.ManaPercent >= QMinMana)
            {
                var Qfarm = Q.GetCircularFarmLocation(allMinionsQ, Q.Width);
                if (Qfarm.MinionsHit >= 1)
                    Q.Cast(Qfarm.Position);
            }

            if (useW && W.IsReady() && Player.Mana >= WMANA && Player.ManaPercent >= WMinMana)
            {
                var Wfarm = W.GetCircularFarmLocation(allMinionsQ, W.Width);
                if (Wfarm.MinionsHit >= 1)
                    W.Cast(Wfarm.Position);
            }

        }
        #endregion

        #region KillSteal
        public static void KillSteal()
        {

            var useIgniteKS = Config.Item("Graves.UseIgniteKS").GetValue<bool>();
            var useQKS = Config.Item("Graves.UseQKS").GetValue<bool>();
            var useWKS = Config.Item("Graves.UseWKS").GetValue<bool>();
            var useRKS = Config.Item("Graves.UseRKS").GetValue<bool>();
            var useEKS = Config.Item("Graves.UseEKS").GetValue<bool>();

            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => !target.IsMe && target.Team != ObjectManager.Player.Team))
            {
                if (!target.HasBuff("SionPassiveZombie") && !target.HasBuffOfType(BuffType.Invulnerability) && !target.HasBuffOfType(BuffType.SpellImmunity))
                {
                    if (useWKS && W.IsReady() && Player.Mana >= WMANA && target.Health < W.GetDamage(target) && Player.Distance(target) < W.Range && !target.IsDead && target.IsValidTarget())
                    {
                        W.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useQKS && Q.IsReady() && Player.Mana >= QMANA && target.Health < Q.GetDamage(target) && Player.Distance(target) < Q.Range && !target.IsDead && target.IsValidTarget())
                    {
                        Q.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useRKS && R.IsReady() && Player.Mana >= RMANA && target.Health < R.GetDamage(target) && Player.Distance(target) < R.Range && !target.IsDead && target.IsValidTarget())
                    {
                        R.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useIgniteKS && Ignite.Slot != SpellSlot.Unknown && Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health && target.IsValidTarget(Ignite.Range))
                    {
                        Ignite.Cast(target, true);
                        return;
                    }
                    if (useWKS && useQKS && W.IsReady() && Q.IsReady() && Player.Mana >= WMANA + QMANA && target.Health < W.GetDamage(target) + Q.GetDamage(target) && Player.Distance(target) < Q.Range && !target.IsDead && target.IsValidTarget())
                    {
                        Q.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useWKS && useIgniteKS && W.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= WMANA && target.Health < W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                    {
                        W.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useQKS && useIgniteKS && Q.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= QMANA && target.Health < Q.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                    {
                        Q.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useQKS && useWKS && useIgniteKS && Q.IsReady() && W.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= QMANA + WMANA && target.Health < Q.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) + W.GetDamage(target) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                    {
                        W.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useRKS && useWKS && R.IsReady() && W.IsReady() && Player.Mana >= RMANA + WMANA && target.Health < R.GetDamage(target) + W.GetDamage(target) && Player.Distance(target) < W.Range && !target.IsDead && target.IsValidTarget())
                    {
                        W.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useRKS && useQKS && R.IsReady() && Q.IsReady() && Player.Mana >= RMANA + QMANA && target.Health < R.GetDamage(target) + Q.GetDamage(target) && Player.Distance(target) < Q.Range && !target.IsDead && target.IsValidTarget())
                    {
                        Q.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useRKS && useQKS && useWKS && R.IsReady() && Q.IsReady() && W.IsReady() && Player.Mana >= RMANA + QMANA + WMANA && target.Health < R.GetDamage(target) + Q.GetDamage(target) + W.GetDamage(target) && Player.Distance(target) < Q.Range && !target.IsDead && target.IsValidTarget())
                    {
                        W.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useRKS && useIgniteKS && R.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= RMANA && target.Health < R.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                    {
                        R.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useRKS && useWKS && useIgniteKS && R.IsReady() && W.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= RMANA + WMANA && target.Health < R.GetDamage(target) + W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                    {
                        W.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useRKS && useQKS && useIgniteKS && R.IsReady() && Q.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= RMANA + QMANA && target.Health < R.GetDamage(target) + Q.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                    {
                        Q.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }
                    if (useRKS && useQKS && useWKS && useIgniteKS && R.IsReady() && Q.IsReady() && W.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= RMANA + QMANA + WMANA && target.Health < R.GetDamage(target) + Q.GetDamage(target) + W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                    {
                        W.CastIfHitchanceEquals(target, HitChance.High, true);
                        return;
                    }

                    if (useEKS && E.IsReady())
                    {
                        if (useWKS && W.IsReady() && Player.Mana >= EMANA + WMANA && target.Health < W.GetDamage(target) && Player.Distance(target) > W.Range && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + W.Range)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useQKS && Q.IsReady() && Player.Mana >= EMANA + QMANA && target.Health < Q.GetDamage(target) && Player.Distance(target) > Q.Range && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + Q.Range)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useRKS && R.IsReady() && Player.Mana >= EMANA + RMANA && target.Health < R.GetDamage(target) && Player.Distance(target) > R.Range && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + R.Range)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useIgniteKS && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= EMANA && Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health && Player.Distance(target) > Ignite.Range && target.IsValidTarget() && Player.Distance(target) < E.Range + Ignite.Range)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useWKS && useQKS && W.IsReady() && Q.IsReady() && Player.Mana >= EMANA + WMANA + QMANA && target.Health < W.GetDamage(target) + Q.GetDamage(target) && Player.Distance(target) > Q.Range && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + Q.Range)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useWKS && useIgniteKS && W.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= EMANA + WMANA && target.Health < W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) > 600 && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + 600)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useQKS && useIgniteKS && Q.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= EMANA + QMANA && target.Health < Q.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) > 600 && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + 600)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useQKS && useWKS && useIgniteKS && Q.IsReady() && W.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= EMANA + QMANA + WMANA && target.Health < Q.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) + W.GetDamage(target) && Player.Distance(target) > 600 && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + 600)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useRKS && useWKS && R.IsReady() && W.IsReady() && Player.Mana >= EMANA + RMANA + WMANA && target.Health < R.GetDamage(target) + W.GetDamage(target) && Player.Distance(target) > W.Range && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + W.Range)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useRKS && useQKS && R.IsReady() && Q.IsReady() && Player.Mana >= EMANA + RMANA + QMANA && target.Health < R.GetDamage(target) + Q.GetDamage(target) && Player.Distance(target) > Q.Range && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + Q.Range)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useRKS && useQKS && useWKS && R.IsReady() && Q.IsReady() && W.IsReady() && Player.Mana >= EMANA + RMANA + QMANA + WMANA && target.Health < R.GetDamage(target) + Q.GetDamage(target) + W.GetDamage(target) && Player.Distance(target) > Q.Range && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + Q.Range)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useRKS && useIgniteKS && R.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= EMANA + RMANA && target.Health < R.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) > 600 && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + 600)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useRKS && useWKS && useIgniteKS && R.IsReady() && W.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= EMANA + RMANA + WMANA && target.Health < R.GetDamage(target) + W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) > 600 && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + 600)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useRKS && useQKS && useIgniteKS && R.IsReady() && Q.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= EMANA + RMANA + QMANA && target.Health < R.GetDamage(target) + Q.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) > 600 && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + 600)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }
                        if (useRKS && useQKS && useWKS && useIgniteKS && R.IsReady() && Q.IsReady() && W.IsReady() && Ignite.Slot != SpellSlot.Unknown && Player.Mana >= EMANA + RMANA + QMANA + WMANA && target.Health < R.GetDamage(target) + Q.GetDamage(target) + W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) > 600 && !target.IsDead && target.IsValidTarget() && Player.Distance(target) < E.Range + 600)
                        {
                            E.Cast(target.ServerPosition, true);
                            return;
                        }                         
                    }
                }
               
            }
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

            if (Config.Item("Graves.AutoPotion").GetValue<bool>() && !Player.InFountain() && !Player.IsRecalling() && !Player.IsDead)
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

    }

}
