using Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using UnityEngine;
using BingoSync.CustomGoals;
using BingoSync.Interfaces;
using BingoGoalPack1.CustomVariables;

namespace BingoGoalPack1 {
    public class BingoGoalPack1: Mod {
        new public string GetName() => "BingoGoalPack1";
        public override string GetVersion() => "1.7.0.0";

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects) {
            //set up goal lists
            OrderedLoader.OnReadyForGoalsGameModes += SetupGoalsGameModes;

            //add hooks
            On.GameManager.AwardAchievement += Neglect.CheckNeglectAchievement;
            ModHooks.SetPlayerBoolHook += CoreShard.CheckIfCoreShardWasCollected;
            On.PlayMakerFSM.OnEnable += Totems.CreateSoulTotemTrigger;
            On.DialogueBox.StartConversation += DialogueExtension.StartConversation;
            ModHooks.SetPlayerIntHook += GrubsExtension.CheckIfGrubWasSaved;
            On.PlayMakerFSM.OnEnable += Diary.CreateDiaryTrigger;
            On.PlayMakerFSM.OnEnable += Arenas.CreateArenaTrigger;
            On.PlayMakerFSM.OnEnable += ChestsExtension.CreateJunkpitChestTrigger;
            On.PlayMakerFSM.OnEnable += ChestsExtension.CreateMLordsChestTrigger;
            On.PlayMakerFSM.OnEnable += Hardsaves.CreateHardsaveTrigger;
            On.RespawnTrigger.OnTriggerEnter2D += Hardsaves.CreateRespawnTriggerTrigger;
            On.PlayMakerFSM.OnEnable += GrubsExtension.SaveOneGrub;
            On.PlayMakerFSM.OnEnable += Ghosts.CreateGhostKilledTrigger;
            On.PlayMakerFSM.OnEnable += DreamNailMace.CreateMaceTrigger;
            On.BossStatue.SetPlaqueState += HallOfGodsTracker.CreateHogStatueTrigger;
            On.PlayMakerFSM.OnEnable += Sit.CreateSitTrigger;

            //dream tree logic
            var _hook = new ILHook
            (
                typeof(DreamPlant).GetMethod("CheckOrbs", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                DreamTreesExtension.TrackDreamTrees
            );

            Log("Initialized");
        }

        private void SetupGoalsGameModes(object _, EventArgs __) {
            Assembly assembly = Assembly.GetExecutingAssembly();

            Dictionary<string, BingoGoal> vanillaGoals = Goals.GetVanillaGoals();

            processEmbeddedJson(assembly, "Extended");
            Dictionary<string, BingoGoal> extendedGoals = setupExtendedDict();
            Goals.RegisterGoalsForCustom("Extended", extendedGoals);
            Dictionary<string, BingoGoal> extendedPlusGoals = setupExtendedPlusDict();
            Goals.RegisterGoalsForCustom("Extended+", extendedPlusGoals);

            extendedGoals.AddRange(vanillaGoals);
            IGameMode mode_extended = new SimpleGameMode("Extended", extendedGoals);
            Goals.AddGameMode(mode_extended);

            extendedPlusGoals.AddRange(extendedGoals);
            IGameMode mode_extendedPlus = new SimpleGameMode("Extended+", extendedPlusGoals);
            Goals.AddGameMode(mode_extendedPlus);

            Dictionary<string, BingoGoal> hardsaveGoals = processEmbeddedJson(assembly, "BenchBingo");
            setupHardsaveDict(hardsaveGoals);
            IGameMode mode_hardsaves = new SimpleGameMode("Hardsaves", hardsaveGoals);
            Goals.AddGameMode(mode_hardsaves);
            Goals.RegisterGoalsForCustom("Hardsaves", hardsaveGoals);

            Dictionary<string, BingoGoal> grubGoals = processEmbeddedJson(assembly, "GrubBingo");
            setupGrubDict(grubGoals);
            IGameMode mode_grubs = new SimpleGameMode("Grubs", grubGoals);
            Goals.AddGameMode(mode_grubs);
            Goals.RegisterGoalsForCustom("Grubs", grubGoals);

            Dictionary<string, BingoGoal> hogGoals = processEmbeddedJson(assembly, "GodhomeBingo");
            setupHogDict(hogGoals);
            IGameMode mode_godhome = new GodhomeMode();
            Goals.AddGameMode(mode_godhome);
            Goals.RegisterGoalsForCustom("Hall of Gods", hogGoals);

            Dictionary<string, BingoGoal> relicGoals = processEmbeddedJson(assembly, "RelicBingo");
            setupRelicDict(relicGoals);
            IGameMode mode_relics = new SimpleGameMode("Relics", relicGoals);
            Goals.AddGameMode(mode_relics);
            Goals.RegisterGoalsForCustom("Relics", relicGoals);
        }

        private Dictionary<string, BingoGoal> processEmbeddedJson(Assembly assembly, string jsonName) {
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("Squares." + jsonName + ".json"));
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            return Goals.ProcessGoalsStream(stream);
        }

        private Dictionary<string, BingoGoal> setupExtendedDict() {
            Dictionary<string, BingoGoal> output = new();
            foreach(string goal in goalEnum.extended.extendedList) {
                output.Add(goal, new BingoGoal(goal));
            }
            output[goalEnum.extended.fourKeys].Exclusions.Add(goalEnum.vanilla.paleLurker);
            output[goalEnum.extended.poggyJoni].Exclusions.Add(goalEnum.extended.lifeblood10);
            output[goalEnum.extended.fungalShard].Exclusions.Add(goalEnum.extended.fungalElder);
            output[goalEnum.extended.crownOre].Exclusions.Add(goalEnum.vanilla.paleOre);
            output[goalEnum.extended.crownOre].Exclusions.Add(goalEnum.vanilla.nail2);
            output[goalEnum.extended.godtuner].Exclusions.Add(goalEnum.extended.nothing);
            output[goalEnum.extended.diary].Exclusions.Add(goalEnum.vanilla.brettaSly);
            output[goalEnum.extended.grubsHive].Exclusions.Add(goalEnum.vanilla.hiveShard);
            output[goalEnum.extended.slashMillibelle].Exclusions.Add(goalEnum.extended.marissa);
            output[goalEnum.extended.tuk].Exclusions.Add(goalEnum.vanilla.fourEggs);
            return output;
        }

        private Dictionary<string, BingoGoal> setupExtendedPlusDict() {
            Dictionary<string, BingoGoal> output = new();
            foreach(string goal in goalEnum.extended.extendedPlusList) {
                output.Add(goal, new BingoGoal(goal));
            }
            output[goalEnum.extended.lifeblood10].Exclusions.Add(goalEnum.vanilla.lifeblood);
            output[goalEnum.extended.lifeblood10].Exclusions.Add(goalEnum.extended.poggyJoni);
            output[goalEnum.extended.waterwaysArenas].Exclusions.Add(goalEnum.extended.waterwaysCornifer);
            output[goalEnum.extended.mossProphet].Exclusions.Add(goalEnum.extended.vagabonds);
            output[goalEnum.extended.fungalElder].Exclusions.Add(goalEnum.extended.fungalShard);
            output[goalEnum.extended.markers4].Exclusions.Add(goalEnum.vanilla.pins6);
            output[goalEnum.extended.markers4].Exclusions.Add(goalEnum.vanilla.pins8);
            output[goalEnum.extended.dreamWarriors3].Exclusions.Add(goalEnum.vanilla.essence);
            output[goalEnum.extended.dreamWarriors3].Exclusions.Add(goalEnum.vanilla.wielder);
            output[goalEnum.extended.shrineTablets].Exclusions.Add(goalEnum.vanilla.revek);
            output[goalEnum.extended.marissa].Exclusions.Add(goalEnum.extended.slashMillibelle);
            output[goalEnum.extended.equip5Charms].Exclusions.Add(goalEnum.vanilla.notches);
            output[goalEnum.extended.scarecrow].Exclusions.Add(goalEnum.vanilla.dashSlash);
            output[goalEnum.extended.vagabonds].Exclusions.Add(goalEnum.extended.mossProphet);
            output[goalEnum.extended.telescope].Exclusions.Add(goalEnum.vanilla.lurien);
            output[goalEnum.extended.nothing].Exclusions.Add(goalEnum.extended.godtuner);
            output[goalEnum.extended.dirtmouthElevator].Exclusions.Add(goalEnum.vanilla.mimics);
            output[goalEnum.extended.tollBenches].Exclusions.Add(goalEnum.vanilla.sixTolls);
            return output;
        }

        private void setupHardsaveDict(Dictionary<string, BingoGoal> goals) {
            goals[goalEnum.hardsaves.archives].Exclusions.Add(goalEnum.grubs.archives);
            goals[goalEnum.hardsaves.basinToll].Exclusions.Add(goalEnum.extended.grubsBasin);
            goals[goalEnum.hardsaves.basinToll].Exclusions.Add(goalEnum.extended.tollBenches);
            goals[goalEnum.hardsaves.basinToll].Exclusions.Add(goalEnum.grubs.basinDive);
            goals[goalEnum.hardsaves.basinToll].Exclusions.Add(goalEnum.grubs.basinWings);
            goals[goalEnum.hardsaves.denHerrah].Exclusions.Add(goalEnum.hardsaves.denBench);
            goals[goalEnum.hardsaves.denHerrah].Exclusions.Add(goalEnum.vanilla.herrah);
            goals[goalEnum.hardsaves.denHerrah].Exclusions.Add(goalEnum.vanilla.hornetHerrah);
            goals[goalEnum.hardsaves.denHerrah].Exclusions.Add(goalEnum.relics.sealDen);
            goals[goalEnum.hardsaves.denBench].Exclusions.Add(goalEnum.hardsaves.denHerrah);
            goals[goalEnum.hardsaves.denBench].Exclusions.Add(goalEnum.vanilla.herrah);
            goals[goalEnum.hardsaves.denBench].Exclusions.Add(goalEnum.vanilla.hornetHerrah);
            goals[goalEnum.hardsaves.bretta].Exclusions.Add(goalEnum.extended.fungalBenches);
            goals[goalEnum.hardsaves.camp].Exclusions.Add(goalEnum.vanilla.hornet2);
            goals[goalEnum.hardsaves.camp].Exclusions.Add(goalEnum.relics.journalCamp);
            goals[goalEnum.hardsaves.camp].Exclusions.Add(goalEnum.hardsaves.brand);
            goals[goalEnum.hardsaves.cityGate].Exclusions.Add(goalEnum.relics.sealRafters);
            goals[goalEnum.hardsaves.cityGate].Exclusions.Add(goalEnum.hardsaves.cityQuirrel);
            goals[goalEnum.hardsaves.cityQuirrel].Exclusions.Add(goalEnum.hardsaves.cityGate);
            goals[goalEnum.hardsaves.cityQuirrel].Exclusions.Add(goalEnum.relics.sealRafters);
            goals[goalEnum.hardsaves.cityToll].Exclusions.Add(goalEnum.extended.tollBenches);
            goals[goalEnum.hardsaves.colo].Exclusions.Add(goalEnum.vanilla.colo);
            goals[goalEnum.hardsaves.colo].Exclusions.Add(goalEnum.vanilla.coloZote);
            goals[goalEnum.hardsaves.colo].Exclusions.Add(goalEnum.vanilla.paleLurker);
            goals[goalEnum.hardsaves.colo].Exclusions.Add(goalEnum.vanilla.hotSprings);
            goals[goalEnum.hardsaves.colo].Exclusions.Add(goalEnum.extended.tiso);
            goals[goalEnum.hardsaves.xroadsSpring].Exclusions.Add(goalEnum.vanilla.hotSprings);
            goals[goalEnum.hardsaves.xroadsStag].Exclusions.Add(goalEnum.extended.tiso);
            goals[goalEnum.hardsaves.deepnestSpring].Exclusions.Add(goalEnum.vanilla.hotSprings);
            goals[goalEnum.hardsaves.dive].Exclusions.Add(goalEnum.vanilla.dive);
            goals[goalEnum.hardsaves.dive].Exclusions.Add(goalEnum.vanilla.soulMaster);
            goals[goalEnum.hardsaves.dive].Exclusions.Add(goalEnum.grubs.citySanctumDive);
            goals[goalEnum.hardsaves.dive].Exclusions.Add(goalEnum.relics.sealSanctum);
            goals[goalEnum.hardsaves.failedTram].Exclusions.Add(goalEnum.extended.failedTramBench);
            goals[goalEnum.hardsaves.godseeker].Exclusions.Add(goalEnum.extended.godtuner);
            goals[goalEnum.hardsaves.godseeker].Exclusions.Add(goalEnum.extended.nothing);
            goals[goalEnum.hardsaves.greenpathStag].Exclusions.Add(goalEnum.vanilla.thornsBaldurSpore);
            goals[goalEnum.hardsaves.greenpathStag].Exclusions.Add(goalEnum.extended.greenpathBenches);
            goals[goalEnum.hardsaves.greenpathStag].Exclusions.Add(goalEnum.grubs.greenpathMossKnight);
            goals[goalEnum.hardsaves.greenpathStag].Exclusions.Add(goalEnum.relics.journalGreenpathStag);
            goals[goalEnum.hardsaves.greenpathToll].Exclusions.Add(goalEnum.extended.greenpathBenches);
            goals[goalEnum.hardsaves.greenpathToll].Exclusions.Add(goalEnum.extended.tollBenches);
            goals[goalEnum.hardsaves.greyMourner].Exclusions.Add(goalEnum.vanilla.flowerQuest);
            goals[goalEnum.hardsaves.hiddenStag].Exclusions.Add(goalEnum.vanilla.stagHidden);
            goals[goalEnum.hardsaves.hive].Exclusions.Add(goalEnum.extended.grubsHive);
            goals[goalEnum.hardsaves.hive].Exclusions.Add(goalEnum.grubs.hiveInternal);
            goals[goalEnum.hardsaves.brand].Exclusions.Add(goalEnum.vanilla.hornet2);
            goals[goalEnum.hardsaves.brand].Exclusions.Add(goalEnum.relics.journalCamp);
            goals[goalEnum.hardsaves.brand].Exclusions.Add(goalEnum.hardsaves.camp);
            goals[goalEnum.hardsaves.kingsStag].Exclusions.Add(goalEnum.relics.sealKings);
            goals[goalEnum.hardsaves.kingsStag].Exclusions.Add(goalEnum.relics.journalAboveKings);
            goals[goalEnum.hardsaves.unn].Exclusions.Add(goalEnum.extended.greenpathBenches);
            goals[goalEnum.hardsaves.legEater].Exclusions.Add(goalEnum.vanilla.fragiles);
            goals[goalEnum.hardsaves.legEater].Exclusions.Add(goalEnum.extended.fungalBenches);
            goals[goalEnum.hardsaves.legEater].Exclusions.Add(goalEnum.extended.maceBugLeggyBench);
            goals[goalEnum.hardsaves.legEater].Exclusions.Add(goalEnum.extended.visitShops);
            goals[goalEnum.hardsaves.lowerTram].Exclusions.Add(goalEnum.vanilla.tram);
            goals[goalEnum.hardsaves.lurien].Exclusions.Add(goalEnum.vanilla.lurien);
            goals[goalEnum.hardsaves.lurien].Exclusions.Add(goalEnum.extended.telescope);
            goals[goalEnum.hardsaves.mantisVillage].Exclusions.Add(goalEnum.vanilla.mantisLords);
            goals[goalEnum.hardsaves.mantisVillage].Exclusions.Add(goalEnum.vanilla.longnail);
            goals[goalEnum.hardsaves.mantisVillage].Exclusions.Add(goalEnum.extended.fungalBenches);
            goals[goalEnum.hardsaves.mantisVillage].Exclusions.Add(goalEnum.relics.sealMantisLords);
            goals[goalEnum.hardsaves.mato].Exclusions.Add(goalEnum.vanilla.cyclone);
            goals[goalEnum.hardsaves.monomon].Exclusions.Add(goalEnum.vanilla.monomon);
            goals[goalEnum.hardsaves.oro].Exclusions.Add(goalEnum.vanilla.dashSlash);
            goals[goalEnum.hardsaves.oro].Exclusions.Add(goalEnum.extended.scarecrow);
            goals[goalEnum.hardsaves.oro].Exclusions.Add(goalEnum.grubs.keOro);
            goals[goalEnum.hardsaves.pleasureHouse].Exclusions.Add(goalEnum.vanilla.hotSprings);
            goals[goalEnum.hardsaves.pleasureHouse].Exclusions.Add(goalEnum.extended.slashMillibelle);
            goals[goalEnum.hardsaves.pleasureHouse].Exclusions.Add(goalEnum.extended.marissa);
            goals[goalEnum.hardsaves.pleasureHouse].Exclusions.Add(goalEnum.relics.journalPleasureHouse);
            goals[goalEnum.hardsaves.qgCornifer].Exclusions.Add(goalEnum.extended.mapsQGFog);
            goals[goalEnum.hardsaves.qgStag].Exclusions.Add(goalEnum.vanilla.marmu);
            goals[goalEnum.hardsaves.qgStag].Exclusions.Add(goalEnum.vanilla.stagQG);
            goals[goalEnum.hardsaves.qgStag].Exclusions.Add(goalEnum.relics.sealQG);
            goals[goalEnum.hardsaves.qgToll].Exclusions.Add(goalEnum.extended.tollBenches);
            goals[goalEnum.hardsaves.queensStationStag].Exclusions.Add(goalEnum.vanilla.banker);
            goals[goalEnum.hardsaves.queensStationStag].Exclusions.Add(goalEnum.extended.fungalBenches);
            goals[goalEnum.hardsaves.rgStag].Exclusions.Add(goalEnum.vanilla.essence);
            goals[goalEnum.hardsaves.rgStag].Exclusions.Add(goalEnum.vanilla.dnail);
            goals[goalEnum.hardsaves.rgStag].Exclusions.Add(goalEnum.vanilla.wielder);
            goals[goalEnum.hardsaves.rgStag].Exclusions.Add(goalEnum.vanilla.revek);
            goals[goalEnum.hardsaves.rgStag].Exclusions.Add(goalEnum.vanilla.xero);
            goals[goalEnum.hardsaves.salubra].Exclusions.Add(goalEnum.extended.visitShops);
            goals[goalEnum.hardsaves.shadeCloak].Exclusions.Add(goalEnum.vanilla.shadeCloak);
            goals[goalEnum.hardsaves.shadeCloak].Exclusions.Add(goalEnum.relics.eggShadeCloak);
            goals[goalEnum.hardsaves.sheo].Exclusions.Add(goalEnum.vanilla.greatSlash);
            goals[goalEnum.hardsaves.sheo].Exclusions.Add(goalEnum.extended.greenpathBenches);
            goals[goalEnum.hardsaves.stoneSanc].Exclusions.Add(goalEnum.extended.greenpathBenches);
            goals[goalEnum.hardsaves.upperTram].Exclusions.Add(goalEnum.vanilla.tram);
            goals[goalEnum.hardsaves.spire].Exclusions.Add(goalEnum.vanilla.watchers);
            goals[goalEnum.hardsaves.spire].Exclusions.Add(goalEnum.relics.sealSpire);
            goals[goalEnum.hardsaves.waterfall].Exclusions.Add(goalEnum.extended.greenpathBenches);
            goals[goalEnum.hardsaves.waterways].Exclusions.Add(goalEnum.grubs.waterwaysCenter);
        }

        private void setupGrubDict(Dictionary<string, BingoGoal> goals) {
            goals[goalEnum.grubs.xroadsWall].Exclusions.Add(goalEnum.vanilla.grubsCross);
            goals[goalEnum.grubs.xroadsGuarded].Exclusions.Add(goalEnum.vanilla.grubsCross);
            goals[goalEnum.grubs.xroadsSpike].Exclusions.Add(goalEnum.vanilla.grubsCross);
            goals[goalEnum.grubs.xroadsVengefly].Exclusions.Add(goalEnum.vanilla.grubsCross);
            goals[goalEnum.grubs.xroadsAcid].Exclusions.Add(goalEnum.vanilla.grubsCross);
            goals[goalEnum.grubs.peakSpike].Exclusions.Add(goalEnum.vanilla.grubsPeak);
            goals[goalEnum.grubs.peakCrown].Exclusions.Add(goalEnum.vanilla.grubsPeak);
            goals[goalEnum.grubs.peakCrown].Exclusions.Add(goalEnum.vanilla.cdash);
            goals[goalEnum.grubs.peakCdash].Exclusions.Add(goalEnum.vanilla.cdash);
            goals[goalEnum.grubs.peakCdash].Exclusions.Add(goalEnum.vanilla.grubsPeak);
            goals[goalEnum.grubs.peakCrushers].Exclusions.Add(goalEnum.vanilla.cdash);
            goals[goalEnum.grubs.peakCrushers].Exclusions.Add(goalEnum.vanilla.grubsPeak);
            goals[goalEnum.grubs.peakBottom].Exclusions.Add(goalEnum.vanilla.cdash);
            goals[goalEnum.grubs.peakBottom].Exclusions.Add(goalEnum.vanilla.grubsPeak);
            goals[goalEnum.grubs.peakMound].Exclusions.Add(goalEnum.vanilla.ddark);
            goals[goalEnum.grubs.peakMound].Exclusions.Add(goalEnum.vanilla.grubsPeak);
            goals[goalEnum.grubs.peakMimic].Exclusions.Add(goalEnum.vanilla.mimics);
            goals[goalEnum.grubs.peakMimic].Exclusions.Add(goalEnum.vanilla.grubsPeak);
            goals[goalEnum.grubs.peakMimic].Exclusions.Add(goalEnum.vanilla.grimmUpgrade);
            goals[goalEnum.grubs.peakMimic].Exclusions.Add(goalEnum.extended.dirtmouthElevator);
            goals[goalEnum.grubs.crypts].Exclusions.Add(goalEnum.vanilla.flowerQuest);
            goals[goalEnum.grubs.crypts].Exclusions.Add(goalEnum.extended.eaterCatcher);
            goals[goalEnum.grubs.crypts].Exclusions.Add(goalEnum.relics.journalCrypts);
            goals[goalEnum.grubs.crypts].Exclusions.Add(goalEnum.relics.sealCrypts);
            goals[goalEnum.grubs.citySanctumDive].Exclusions.Add(goalEnum.vanilla.grubsCity);
            goals[goalEnum.grubs.citySanctumDive].Exclusions.Add(goalEnum.vanilla.dive);
            goals[goalEnum.grubs.citySanctumDive].Exclusions.Add(goalEnum.vanilla.soulMaster);
            goals[goalEnum.grubs.citySanctumDive].Exclusions.Add(goalEnum.hardsaves.dive);
            goals[goalEnum.grubs.citySanctumDive].Exclusions.Add(goalEnum.relics.sealSanctum);
            goals[goalEnum.grubs.cityBelowSanctum].Exclusions.Add(goalEnum.vanilla.grubsCity);
            goals[goalEnum.grubs.cityBelowSanctum].Exclusions.Add(goalEnum.relics.sealRafters);
            goals[goalEnum.grubs.cityBelowLove].Exclusions.Add(goalEnum.vanilla.grubsCity);
            goals[goalEnum.grubs.cityGuard].Exclusions.Add(goalEnum.vanilla.grubsCity);
            goals[goalEnum.grubs.citySpire].Exclusions.Add(goalEnum.vanilla.grubsCity);
            goals[goalEnum.grubs.waterwaysCenter].Exclusions.Add(goalEnum.vanilla.grubsWaterways);
            goals[goalEnum.grubs.waterwaysCenter].Exclusions.Add(goalEnum.hardsaves.waterways);
            goals[goalEnum.grubs.waterwaysIsma].Exclusions.Add(goalEnum.vanilla.grubsWaterways);
            goals[goalEnum.grubs.waterwaysHwurmp].Exclusions.Add(goalEnum.vanilla.grubsWaterways);
            goals[goalEnum.grubs.fungalBouncy].Exclusions.Add(goalEnum.vanilla.grubsGreen);
            goals[goalEnum.grubs.fungalSpore].Exclusions.Add(goalEnum.vanilla.grubsGreen);
            goals[goalEnum.grubs.fungalSpore].Exclusions.Add(goalEnum.vanilla.thornsBaldurSpore);
            goals[goalEnum.grubs.deepnestMimics].Exclusions.Add(goalEnum.vanilla.mimics);
            goals[goalEnum.grubs.deepnestMimics].Exclusions.Add(goalEnum.vanilla.grubsDeepnest);
            goals[goalEnum.grubs.deepnestNosk].Exclusions.Add(goalEnum.vanilla.nosk);
            goals[goalEnum.grubs.deepnestNosk].Exclusions.Add(goalEnum.vanilla.grubsDeepnest);
            goals[goalEnum.grubs.deepnestSpike].Exclusions.Add(goalEnum.vanilla.grubsDeepnest);
            goals[goalEnum.grubs.deepnestDark].Exclusions.Add(goalEnum.vanilla.grubsDeepnest);
            goals[goalEnum.grubs.deepnestDen].Exclusions.Add(goalEnum.vanilla.herrah);
            goals[goalEnum.grubs.deepnestDen].Exclusions.Add(goalEnum.vanilla.grubsDeepnest);
            goals[goalEnum.grubs.deepnestDen].Exclusions.Add(goalEnum.vanilla.hornetHerrah);
            goals[goalEnum.grubs.archives].Exclusions.Add(goalEnum.vanilla.grubsCross);
            goals[goalEnum.grubs.archives].Exclusions.Add(goalEnum.hardsaves.archives);
            goals[goalEnum.grubs.qgBelowStag].Exclusions.Add(goalEnum.vanilla.grubsQG);
            goals[goalEnum.grubs.qgWhiteLady].Exclusions.Add(goalEnum.vanilla.grubsQG);
            goals[goalEnum.grubs.qgUpper].Exclusions.Add(goalEnum.vanilla.grubsQG);
            goals[goalEnum.grubs.greenpathMossKnight].Exclusions.Add(goalEnum.vanilla.grubsGreen);
            goals[goalEnum.grubs.greenpathMossKnight].Exclusions.Add(goalEnum.hardsaves.greenpathStag);
            goals[goalEnum.grubs.greenpathMossKnight].Exclusions.Add(goalEnum.relics.journalGreenpathStag);
            goals[goalEnum.grubs.greenpathHunter].Exclusions.Add(goalEnum.vanilla.grubsGreen);
            goals[goalEnum.grubs.greenpathCornifer].Exclusions.Add(goalEnum.vanilla.grubsGreen);
            goals[goalEnum.grubs.greenpathVesselFrag].Exclusions.Add(goalEnum.vanilla.grubsGreen);
            goals[goalEnum.grubs.greenpathVesselFrag].Exclusions.Add(goalEnum.extended.greenpathRoot);
            goals[goalEnum.grubs.hiveExternal].Exclusions.Add(goalEnum.extended.grubsHive);
            goals[goalEnum.grubs.hiveInternal].Exclusions.Add(goalEnum.extended.grubsHive);
            goals[goalEnum.grubs.hiveInternal].Exclusions.Add(goalEnum.hardsaves.hive);
            goals[goalEnum.grubs.basinDive].Exclusions.Add(goalEnum.extended.grubsBasin);
            goals[goalEnum.grubs.basinDive].Exclusions.Add(goalEnum.hardsaves.basinToll);
            goals[goalEnum.grubs.basinWings].Exclusions.Add(goalEnum.extended.grubsBasin);
            goals[goalEnum.grubs.basinWings].Exclusions.Add(goalEnum.hardsaves.basinToll);
            goals[goalEnum.grubs.collector].Exclusions.Add(goalEnum.vanilla.collector);
            goals[goalEnum.grubs.keOro].Exclusions.Add(goalEnum.vanilla.quickslash);
            goals[goalEnum.grubs.keOro].Exclusions.Add(goalEnum.hardsaves.oro);
            goals[goalEnum.grubs.keCenter].Exclusions.Add(goalEnum.relics.journalMarkothDive);
        }

        private void setupHogDict(Dictionary<string, BingoGoal> goals) {
            string att = GodhomeMode.levels[0];
            string asc = GodhomeMode.levels[1];
            string rad = GodhomeMode.levels[2];
            foreach(string boss in GodhomeMode.bosses) {
                goals[att+boss].Exclusions.Add(asc+boss);
                goals[att+boss].Exclusions.Add(rad+boss);
                goals[asc+boss].Exclusions.Add(att+boss);
                goals[asc+boss].Exclusions.Add(rad+boss);
                goals[rad+boss].Exclusions.Add(att+boss);
                goals[rad+boss].Exclusions.Add(asc+boss);
            }
        }

        private void setupRelicDict(Dictionary<string, BingoGoal> goals) {
            goals[goalEnum.relics.journalAboveKings].Exclusions.Add(goalEnum.hardsaves.kingsStag);
            goals[goalEnum.relics.journalAboveKings].Exclusions.Add(goalEnum.relics.sealKings);
            goals[goalEnum.relics.journalBelowOgres].Exclusions.Add(goalEnum.relics.sealSporgs);
            goals[goalEnum.relics.journalBelowStoneSanc].Exclusions.Add(goalEnum.extended.stoneSancJournal);
            goals[goalEnum.relics.journalCamp].Exclusions.Add(goalEnum.hardsaves.camp);
            goals[goalEnum.relics.journalCamp].Exclusions.Add(goalEnum.vanilla.hornet2);
            goals[goalEnum.relics.journalCamp].Exclusions.Add(goalEnum.hardsaves.brand);
            goals[goalEnum.relics.journalStorerooms].Exclusions.Add(goalEnum.vanilla.grimmUpgrade);
            goals[goalEnum.relics.journalCrypts].Exclusions.Add(goalEnum.relics.sealCrypts);
            goals[goalEnum.relics.journalCrypts].Exclusions.Add(goalEnum.vanilla.flowerQuest);
            goals[goalEnum.relics.journalCrypts].Exclusions.Add(goalEnum.extended.eaterCatcher);
            goals[goalEnum.relics.journalCrypts].Exclusions.Add(goalEnum.grubs.crypts);
            goals[goalEnum.relics.journalPeakConga].Exclusions.Add(goalEnum.relics.idolPeakCornifer);
            goals[goalEnum.relics.journalGreenpathStag].Exclusions.Add(goalEnum.grubs.greenpathMossKnight);
            goals[goalEnum.relics.journalGreenpathStag].Exclusions.Add(goalEnum.hardsaves.greenpathStag);
            goals[goalEnum.relics.journalCliffs].Exclusions.Add(goalEnum.relics.idolCliffs);
            goals[goalEnum.relics.journalCliffs].Exclusions.Add(goalEnum.vanilla.gwomb);
            goals[goalEnum.relics.journalCliffs].Exclusions.Add(goalEnum.vanilla.grimmUpgrade);
            goals[goalEnum.relics.journalEdgeCornifer].Exclusions.Add(goalEnum.relics.sealKings);
            goals[goalEnum.relics.journalMarkothDive].Exclusions.Add(goalEnum.grubs.keCenter);
            goals[goalEnum.relics.journalPleasureHouse].Exclusions.Add(goalEnum.extended.marissa);
            goals[goalEnum.relics.journalPleasureHouse].Exclusions.Add(goalEnum.hardsaves.pleasureHouse);
            goals[goalEnum.relics.sealEssence].Exclusions.Add(goalEnum.relics.idolGlade);
            goals[goalEnum.relics.sealEssence].Exclusions.Add(goalEnum.vanilla.essence);
            goals[goalEnum.relics.sealEssence].Exclusions.Add(goalEnum.vanilla.xero);
            goals[goalEnum.relics.seal23Grubs].Exclusions.Add(goalEnum.vanilla.grubs20);
            goals[goalEnum.relics.sealDen].Exclusions.Add(goalEnum.hardsaves.denHerrah);
            goals[goalEnum.relics.sealCrypts].Exclusions.Add(goalEnum.relics.journalCrypts);
            goals[goalEnum.relics.sealCrypts].Exclusions.Add(goalEnum.vanilla.flowerQuest);
            goals[goalEnum.relics.sealCrypts].Exclusions.Add(goalEnum.extended.eaterCatcher);
            goals[goalEnum.relics.sealCrypts].Exclusions.Add(goalEnum.grubs.crypts);
            goals[goalEnum.relics.sealKings].Exclusions.Add(goalEnum.relics.journalEdgeCornifer);
            goals[goalEnum.relics.sealKings].Exclusions.Add(goalEnum.vanilla.stagQueensKings);
            goals[goalEnum.relics.sealKings].Exclusions.Add(goalEnum.hardsaves.kingsStag);
            goals[goalEnum.relics.sealKings].Exclusions.Add(goalEnum.relics.journalAboveKings);
            goals[goalEnum.relics.sealMantisLords].Exclusions.Add(goalEnum.vanilla.longnail);
            goals[goalEnum.relics.sealMantisLords].Exclusions.Add(goalEnum.vanilla.mantisLords);
            goals[goalEnum.relics.sealMantisLords].Exclusions.Add(goalEnum.hardsaves.mantisVillage);
            goals[goalEnum.relics.sealQueensStation].Exclusions.Add(goalEnum.extended.willoh);
            goals[goalEnum.relics.sealRafters].Exclusions.Add(goalEnum.grubs.cityBelowSanctum);
            goals[goalEnum.relics.sealRafters].Exclusions.Add(goalEnum.hardsaves.cityGate);
            goals[goalEnum.relics.sealRafters].Exclusions.Add(goalEnum.hardsaves.cityQuirrel);
            goals[goalEnum.relics.sealSanctum].Exclusions.Add(goalEnum.vanilla.dive);
            goals[goalEnum.relics.sealSanctum].Exclusions.Add(goalEnum.vanilla.soulMaster);
            goals[goalEnum.relics.sealSanctum].Exclusions.Add(goalEnum.grubs.citySanctumDive);
            goals[goalEnum.relics.sealSanctum].Exclusions.Add(goalEnum.hardsaves.dive);
            goals[goalEnum.relics.sealSporgs].Exclusions.Add(goalEnum.relics.journalBelowOgres);
            goals[goalEnum.relics.sealSpire].Exclusions.Add(goalEnum.vanilla.watchers);
            goals[goalEnum.relics.sealSpire].Exclusions.Add(goalEnum.extended.telescope);
            goals[goalEnum.relics.sealSpire].Exclusions.Add(goalEnum.hardsaves.spire);
            goals[goalEnum.relics.sealQG].Exclusions.Add(goalEnum.vanilla.marmu);
            goals[goalEnum.relics.sealQG].Exclusions.Add(goalEnum.vanilla.stagQG);
            goals[goalEnum.relics.sealQG].Exclusions.Add(goalEnum.hardsaves.qgStag);
            goals[goalEnum.relics.idolPeakCornifer].Exclusions.Add(goalEnum.relics.journalPeakConga);
            goals[goalEnum.relics.idolPeakCornifer].Exclusions.Add(goalEnum.vanilla.idols);
            goals[goalEnum.relics.idolDeepnestZote].Exclusions.Add(goalEnum.vanilla.idols);
            goals[goalEnum.relics.idolCliffs].Exclusions.Add(goalEnum.relics.journalCliffs);
            goals[goalEnum.relics.idolCliffs].Exclusions.Add(goalEnum.vanilla.idols);
            goals[goalEnum.relics.idolDungDefender].Exclusions.Add(goalEnum.vanilla.idols);
            goals[goalEnum.relics.idolDungDefender].Exclusions.Add(goalEnum.vanilla.ddefender);
            goals[goalEnum.relics.idolGreatHopper].Exclusions.Add(goalEnum.relics.idolPaleLurker);
            goals[goalEnum.relics.idolGreatHopper].Exclusions.Add(goalEnum.vanilla.idols);
            goals[goalEnum.relics.idolPaleLurker].Exclusions.Add(goalEnum.relics.idolGreatHopper);
            goals[goalEnum.relics.idolPaleLurker].Exclusions.Add(goalEnum.vanilla.idols);
            goals[goalEnum.relics.idolPaleLurker].Exclusions.Add(goalEnum.vanilla.paleLurker);
            goals[goalEnum.relics.idolGlade].Exclusions.Add(goalEnum.relics.sealEssence);
            goals[goalEnum.relics.idolGlade].Exclusions.Add(goalEnum.vanilla.idols);
            goals[goalEnum.relics.idolGlade].Exclusions.Add(goalEnum.vanilla.revek);
            goals[goalEnum.relics.eggEssence].Exclusions.Add(goalEnum.vanilla.arcane);
            goals[goalEnum.relics.eggLifebloodCore].Exclusions.Add(goalEnum.vanilla.arcane);
            goals[goalEnum.relics.eggLifebloodCore].Exclusions.Add(goalEnum.vanilla.lifeblood);
            goals[goalEnum.relics.eggLifebloodCore].Exclusions.Add(goalEnum.vanilla.mask1);
            goals[goalEnum.relics.eggLifebloodCore].Exclusions.Add(goalEnum.extended.lifeblood10);
            goals[goalEnum.relics.eggShadeCloak].Exclusions.Add(goalEnum.vanilla.arcane);
            goals[goalEnum.relics.eggShadeCloak].Exclusions.Add(goalEnum.vanilla.shadeCloak);
            goals[goalEnum.relics.eggShadeCloak].Exclusions.Add(goalEnum.vanilla.voidTendrils);
            goals[goalEnum.relics.eggShadeCloak].Exclusions.Add(goalEnum.hardsaves.shadeCloak);
        }
    }
}
