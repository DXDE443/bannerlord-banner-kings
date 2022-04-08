﻿using HarmonyLib;
using BannerKings.Behaviors;
using BannerKings.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.VillageBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using static BannerKings.Managers.TitleManager;
using static BannerKings.Managers.PopulationManager;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using System.Reflection;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;
using BannerKings.Managers.Helpers;
using BannerKings.Populations;
using BannerKings.Models.Vanilla;
using BannerKings.Behaviours;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;

namespace BannerKings
{
    public class Main : MBSubModuleBase
    {
        public static Harmony patcher = new Harmony("Patcher");
        //private readonly UIExtender xtender = new UIExtender("BannerKings");

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (game.GameType is Campaign)
            {
                try
                {
                    CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;
                    campaignStarter.AddBehavior(new BKSettlementBehavior());
                    campaignStarter.AddBehavior(new BKCompanionBehavior());
                    campaignStarter.AddBehavior(new BKTournamentBehavior());
                    campaignStarter.AddBehavior(new BKRepublicBehavior());
                    campaignStarter.AddBehavior(new BKPartyBehavior());
                    campaignStarter.AddBehavior(new BKClanBehavior());
                    campaignStarter.AddBehavior(new BKArmyBehavior());
                    campaignStarter.AddBehavior(new BKRansomBehavior());
                    campaignStarter.AddBehavior(new BKTitleBehavior());

                    campaignStarter.AddModel(new BKCompanionPrices());
                    campaignStarter.AddModel(new BKProsperityModel());
                    campaignStarter.AddModel(new BKTaxModel());
                    campaignStarter.AddModel(new BKFoodModel());
                    campaignStarter.AddModel(new BKConstructionModel());
                    campaignStarter.AddModel(new BKMilitiaModel());
                    campaignStarter.AddModel(new BKInfluenceModel());
                    campaignStarter.AddModel(new BKLoyaltyModel());
                    campaignStarter.AddModel(new BKVillageProductionModel());
                    campaignStarter.AddModel(new BKSecurityModel());
                    campaignStarter.AddModel(new BKPartyLimitModel());
                    campaignStarter.AddModel(new BKEconomyModel());
                    //campaignStarter.AddModel(new BKPriceFactorModel());
                    campaignStarter.AddModel(new BKWorkshopModel());
                    //campaignStarter.AddModel(new BKClanFinanceModel());
                    campaignStarter.AddModel(new BKArmyManagementModel());
                    campaignStarter.AddModel(new BKSiegeEventModel());
                    campaignStarter.AddModel(new BKTournamentModel());
                    campaignStarter.AddModel(new BKRaidModel());
                    campaignStarter.AddModel(new BKVolunteerModel());
                    campaignStarter.AddModel(new BKNotableModel());
                    campaignStarter.AddModel(new BKGarrisonModel());
                    campaignStarter.AddModel(new BKRansomModel());
                    campaignStarter.AddModel(new BKClanTierModel());
                } catch (Exception e)
                {
                }
            }

            //xtender.Register(typeof(Main).Assembly);
            //xtender.Enable();
        }

        protected override void OnSubModuleLoad()
        {
            new Harmony("BannerKings").PatchAll();
            base.OnSubModuleLoad();
        }
    }

    namespace Patches
    {
        namespace Recruitment
        {
            [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "ApplyInternal")]
            class RecruitmentApplyInternalPatch
            {
                static void Postfix(MobileParty side1Party, Settlement settlement, Hero individual, CharacterObject troop, int number, int bitCode, RecruitmentCampaignBehavior.RecruitingDetail detail)
                {

                    if (settlement == null) return;
                    if (BannerKingsConfig.Instance.PopulationManager != null && BannerKingsConfig.Instance.PopulationManager.IsSettlementPopulated(settlement))
                    {
                        PopulationData data = BannerKingsConfig.Instance.PopulationManager.GetPopData(settlement);
                        data.MilitaryData.DeduceManpower(data, number, troop);
                    }
                }
            }
        }

        namespace Fixes
        {
            // Fix crash on wanderer same gender child born
            [HarmonyPatch(typeof(NameGenerator), "GenerateHeroFullName")]
            class NameGeneratorPatch
            {
                static bool Prefix(ref TextObject __result, Hero hero, TextObject heroFirstName, bool useDeterministicValues = true)
                {

                    Hero parent = hero.IsFemale ? hero.Mother : hero.Father;
                    if (parent == null) return true;
                    if (BannerKingsConfig.Instance.TitleManager.IsHeroKnighted(parent) && hero.IsWanderer)
                    {
                        TextObject textObject = heroFirstName;
                        textObject.SetTextVariable("FEMALE", hero.IsFemale ? 1 : 0);
                        textObject.SetTextVariable("IMPERIAL", (hero.Culture.StringId == "empire") ? 1 : 0);
                        textObject.SetTextVariable("COASTAL", (hero.Culture.StringId == "empire" || hero.Culture.StringId == "vlandia") ? 1 : 0);
                        textObject.SetTextVariable("NORTHERN", (hero.Culture.StringId == "battania" || hero.Culture.StringId == "sturgia") ? 1 : 0);
                        textObject.SetCharacterProperties("HERO", hero.CharacterObject, false);
                        textObject.SetTextVariable("FIRSTNAME", heroFirstName);
                        __result = textObject;
                        return false;
                    }
                    return true;
                }
            }
        }


        namespace Government
        {

            [HarmonyPatch(typeof(KingdomPolicyDecision), "IsAllowed")]
            class PolicyIsAllowedPatch
            {
                static bool Prefix(ref bool __result, KingdomPolicyDecision __instance)
                {
                    if (BannerKingsConfig.Instance.TitleManager != null)
                    {
                        FeudalTitle sovereign = BannerKingsConfig.Instance.TitleManager.GetSovereignTitle(__instance.Kingdom);
                        if (sovereign != null)
                        {
                            __result = !PolicyHelper.GetForbiddenGovernmentPolicies(sovereign.contract.Government).Contains(__instance.Policy);
                            return false;
                        }
                    }
                    return true;
                }
            }

            [HarmonyPatch(typeof(KillCharacterAction), "ApplyInternal")]
            class KillCharacterActionPatch
            {
                static bool Prefix(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail actionDetail, bool showNotification, bool isForced = false)
                {
                    if (!victim.CanDie(actionDetail) && !isForced)
                        return false;

                    if (!victim.IsAlive)
                    {
                        Debug.FailedAssert("Victim: " + victim.Name + " is already dead!", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\KillCharacterAction.cs", "ApplyInternal", 35);
                        return false;
                    }
                    if (victim.IsNotable)
                    {
                        IssueBase issue = victim.Issue;
                        if (((issue != null) ? issue.IssueQuest : null) != null)
                            Debug.FailedAssert("Trying to kill a notable that has quest!", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\KillCharacterAction.cs", "ApplyInternal", 42);

                    }
                    MobileParty partyBelongedTo = victim.PartyBelongedTo;
                    if (((partyBelongedTo != null) ? partyBelongedTo.MapEvent : null) == null)
                    {
                        MobileParty partyBelongedTo2 = victim.PartyBelongedTo;
                        if (((partyBelongedTo2 != null) ? partyBelongedTo2.SiegeEvent : null) == null)
                            goto IL_E2;

                    }
                    if (victim.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
                    {
                        victim.AddDeathMark(killer, actionDetail);
                        return false;
                    }
                IL_E2:
                    CampaignEventDispatcher.Instance.OnBeforeHeroKilled(victim, killer, actionDetail, showNotification);
                    if (victim.IsHumanPlayerCharacter && victim.DeathMark == KillCharacterAction.KillCharacterActionDetail.None && actionDetail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
                    {
                        victim.MakeWounded(killer, actionDetail);
                        return false;
                    }
                    if (victim.IsHumanPlayerCharacter && !isForced)
                    {
                        victim.MakeWounded(killer, actionDetail);
                        CampaignEventDispatcher.Instance.OnBeforeMainCharacterDied();
                        return false;
                    }

                    victim.EncyclopediaText = (TextObject)Type.GetType("TaleWorlds.CampaignSystem.Actions.KillCharacterAction, TaleWorlds.CampaignSystem").GetMethod("CreateObituary", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { victim, actionDetail });
                    if (victim.Clan != null && (victim.Clan.Leader == victim || victim == Hero.MainHero))
                    {
                        Kingdom kingdom = victim.Clan.Kingdom;
                        FeudalTitle title = null;
                        if (BannerKingsConfig.Instance.TitleManager != null && BannerKingsConfig.Instance.TitleManager.IsHeroTitleHolder(victim))
                        {
                            if (kingdom != null) title = BannerKingsConfig.Instance.TitleManager.GetHighestTitleWithinFaction(victim, victim.Clan.Kingdom);
                            else title = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(victim);
                        }

                        if (victim != Hero.MainHero && victim.Clan.Heroes.Any((Hero x) => !x.IsChild && x != victim && x.IsAlive && (x.IsNoble || x.IsMinorFactionHero)))
                            InheritanceHelper.ApplyInheritance(title, victim);

                        if (kingdom != null)
                        {
                            if (victim.Clan.Kingdom.RulingClan == victim.Clan)
                            {
                                List<Clan> list = (from t in victim.Clan.Kingdom.Clans
                                                   where !t.IsEliminated && t.Leader != victim && !t.IsUnderMercenaryService
                                                   select t).ToList<Clan>();

                                if (list.IsEmpty<Clan>())
                                    DestroyKingdomAction.Apply(victim.Clan.Kingdom);
                                else SuccessionHelper.ApplySuccession(title, list, victim, kingdom);
                            }
                        }
                    }

                    if (victim.PartyBelongedTo != null && (victim.PartyBelongedTo.LeaderHero == victim || victim == Hero.MainHero))
                    {
                        if (victim.PartyBelongedTo.Army != null)
                        {
                            if (victim.PartyBelongedTo.Army.LeaderParty == victim.PartyBelongedTo)
                                victim.PartyBelongedTo.Army.DisperseArmy(Army.ArmyDispersionReason.ArmyLeaderIsDead);

                            else victim.PartyBelongedTo.Army = null;

                        }
                        if (victim.PartyBelongedTo != MobileParty.MainParty)
                        {
                            victim.PartyBelongedTo.SetMoveModeHold();
                            if (victim.Clan != null && victim.Clan.IsRebelClan)
                                DestroyPartyAction.Apply(null, victim.PartyBelongedTo);

                        }
                    }
                    Type.GetType("TaleWorlds.CampaignSystem.Actions.KillCharacterAction, TaleWorlds.CampaignSystem").GetMethod("MakeDead", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { victim, true });
                    if (victim.GovernorOf != null)
                        ChangeGovernorAction.ApplyByGiveUpCurrent(victim);

                    if (actionDetail == KillCharacterAction.KillCharacterActionDetail.Executed && killer == Hero.MainHero && victim.Clan != null && !victim.Clan.IsNeutralClan)
                    {
                        if (victim.GetTraitLevel(DefaultTraits.Honor) >= 0)
                            TraitLevelingHelper.OnLordExecuted();

                        foreach (Clan clan in Clan.All)
                        {
                            if (!clan.IsEliminated && !clan.IsBanditFaction && clan != Clan.PlayerClan && clan != CampaignData.NeutralFaction)
                            {
                                bool affectRelatives;
                                int relationChangeForExecutingHero = Campaign.Current.Models.ExecutionRelationModel.GetRelationChangeForExecutingHero(victim, clan.Leader, out affectRelatives);
                                if (relationChangeForExecutingHero != 0)
                                    ChangeRelationAction.ApplyPlayerRelation(clan.Leader, relationChangeForExecutingHero, affectRelatives, true);

                            }
                        }
                    }
                    if (victim.Clan != null && !victim.Clan.IsEliminated && !victim.Clan.IsBanditFaction && !victim.Clan.IsNeutralClan && (victim.Clan.Leader == victim || victim.Clan.Leader == null))
                        DestroyClanAction.Apply(victim.Clan);

                    CampaignEventDispatcher.Instance.OnHeroKilled(victim, killer, actionDetail, showNotification);
                    if (victim.Spouse != null)
                        victim.Spouse = null;

                    if (victim.CompanionOf != null)
                        RemoveCompanionAction.ApplyByDeath(victim.CompanionOf, victim);

                    if (victim.CurrentSettlement != null)
                    {
                        if (victim.CurrentSettlement == Settlement.CurrentSettlement)
                        {
                            LocationComplex locationComplex = LocationComplex.Current;
                            if (locationComplex != null)
                                locationComplex.RemoveCharacterIfExists(victim);

                        }
                        if (victim.StayingInSettlement != null)
                            victim.StayingInSettlement = null;

                    }
                    return false;
                }
            }
        }


        namespace Economy
        {


            [HarmonyPatch(typeof(DefaultClanFinanceModel), "AddIncomeFromKingdomBudget")]
            class AddIncomeFromKingdomBudgetPatch
            {
                static bool Prefix(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
                {
                    if (BannerKingsConfig.Instance.TitleManager != null)
                    {
                        FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(clan.Leader);
                        return title != null && title.contract != null && title.contract.Rights.Contains(FeudalRights.Assistance_Rights);
                    }
                    return true;
                }
            }

            [HarmonyPatch(typeof(DefaultClanFinanceModel), "AddVillagesIncome")]
            class AddVillagesIncomePatch
            {
                static bool Prefix(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
                {
                    if (BannerKingsConfig.Instance.TitleManager != null)
                    {
                        List<FeudalTitle> lordships = BannerKingsConfig.Instance.TitleManager
                            .GetAllDeJure(clan)
                            .FindAll(x => x.type == TitleType.Lordship);
                        foreach (Village village in clan.Villages)
                        {
                            FeudalTitle title = lordships.FirstOrDefault(x => x.fief.Village == village);
                            if (title == null) title = BannerKingsConfig.Instance.TitleManager.GetTitle(village.Settlement);
                            else lordships.Remove(title);
                            int result = CalculateVillageIncome(ref goldChange, village, clan, applyWithdrawals);

                            if (title != null)
                            {
                                Hero deJure = title.deJure;
                                bool knightOwned = title.deJure != clan.Leader && title.deJure.Clan == clan;
                                if (knightOwned)
                                {
                                    deJure.Gold += result;
                                    continue;
                                }
                                else if (deJure.Clan.Kingdom == clan.Kingdom)
                                    continue;
                            }

                            goldChange.Add((float)result, new TextObject("{=!}{A0}", null), village.Name);
                        }

                        foreach (FeudalTitle lordship in lordships)
                        {
                            Village village = lordship.fief.Village;
                            Clan ownerClan = village.Settlement.OwnerClan;
                            if (ownerClan.Kingdom == clan.Kingdom)
                            {
                                int result = CalculateVillageIncome(ref goldChange, village, clan, applyWithdrawals);
                                bool leaderOwned = lordship.deJure == clan.Leader;
                                if (!leaderOwned)
                                {
                                    Hero deJure = lordship.deJure;
                                    deJure.Gold += result;
                                }
                                else goldChange.Add((float)result, new TextObject("{=!}{A0}", null), village.Name);
                            }
                        }
                        return false;
                    }
                    return true;
                }

                private static int CalculateVillageIncome(ref ExplainedNumber goldChange, Village village, Clan clan, bool applyWithdrawals)
                {
                    int total = (village.VillageState == Village.VillageStates.Looted || village.VillageState == Village.VillageStates.BeingRaided) ? 0 : ((int)((float)village.TradeTaxAccumulated / 5f));
                    int num2 = total;
                    if (clan.Kingdom != null && clan.Kingdom.RulingClan != clan && clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LandTax))
                    {
                        total += (int)((-(float)total) * 0.05f);
                    }     

                    if (village.Bound.Town != null && village.Bound.Town.Governor != null && village.Bound.Town.Governor.GetPerkValue(DefaultPerks.Scouting.ForestKin))
                        total += MathF.Round((float)total * DefaultPerks.Scouting.ForestKin.SecondaryBonus * 0.01f);

                    Settlement bound = village.Bound;
                    bool flag;
                    if (bound == null)
                        flag = (null != null);
                    else
                    {
                        Town town = bound.Town;
                        flag = (((town != null) ? town.Governor : null) != null);
                    }
                    if (flag && village.Bound.Town.Governor.GetPerkValue(DefaultPerks.Steward.Logistician))
                        total += MathF.Round((float)total * DefaultPerks.Steward.Logistician.SecondaryBonus * 0.01f);

                    if (applyWithdrawals)
                        village.TradeTaxAccumulated -= num2;

                    if (clan.Kingdom != null && clan.Kingdom.RulingClan == clan && clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LandTax))
                    {
                        if (!village.IsOwnerUnassigned && village.Settlement.OwnerClan != clan)
                        {
                            int policyTotal = (village.VillageState == Village.VillageStates.Looted || village.VillageState == Village.VillageStates.BeingRaided) ? 0 : ((int)((float)village.TradeTaxAccumulated / 5f));
                            total += (int)((float)policyTotal * 0.05f);
                        }
                    }

                    return total;
                }
            }

            [HarmonyPatch(typeof(CaravansCampaignBehavior), "GetTradeScoreForTown")]
            class GetTradeScoreForTownPatch
            {
                static void Postfix(ref float __result, MobileParty caravanParty, Town town, CampaignTime lastHomeVisitTimeOfCaravan, 
                    float caravanFullness, bool distanceCut)
                {
                    if (BannerKingsConfig.Instance.PopulationManager != null && BannerKingsConfig.Instance.PopulationManager.IsSettlementPopulated(town.Settlement))
                    {
                        PopulationData data = BannerKingsConfig.Instance.PopulationManager.GetPopData(town.Settlement);
                        __result *= data.EconomicData.CaravanAttraction.ResultNumber;
                    }
                }
            }

            [HarmonyPatch(typeof(DefaultItemCategories), "InitializeAll")]
            class InitializeCategoriesPatch
            {
                private static ItemCategory _itemCategoryBread;
                static void Postfix()
                {
                    _itemCategoryBread = Game.Current.ObjectManager.RegisterPresumedObject<ItemCategory>(new ItemCategory("bread"));
                    _itemCategoryBread.InitializeObject(true, 50, 20, ItemCategory.Property.BonusToFoodStores, null, 0f, false, true);
                }
            }

            //Mules
            [HarmonyPatch(typeof(VillagerCampaignBehavior), "MoveItemsToVillagerParty")]
            class VillagerMoveItemsPatch
            {
                static bool Prefix(Village village, MobileParty villagerParty)
                {
                    ItemObject mule = MBObjectManager.Instance.GetObject<ItemObject>(x => x.StringId == "mule");
                    int muleCount = (int)((float)villagerParty.MemberRoster.TotalManCount * 0.1f);
                    villagerParty.ItemRoster.AddToCounts(mule, muleCount);
                    ItemRoster itemRoster = village.Settlement.ItemRoster;
                    float num = (float)villagerParty.InventoryCapacity - villagerParty.ItemRoster.TotalWeight;
                    for (int i = 0; i < 4; i++)
                    {
                        foreach (ItemRosterElement itemRosterElement in itemRoster)
                        {
                            ItemObject item = itemRosterElement.EquipmentElement.Item;
                            int num2 = MBRandom.RoundRandomized((float)itemRosterElement.Amount * 0.2f);
                            if (num2 > 0)
                            {
                                if (!item.HasHorseComponent && item.Weight * (float)num2 > num)
                                {
                                    num2 = MathF.Ceiling(num / item.Weight);
                                    if (num2 <= 0)
                                    {
                                        continue;
                                    }
                                }
                                if (!item.HasHorseComponent)
                                {
                                    num -= (float)num2 * item.Weight;
                                }
                                villagerParty.Party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, num2);
                                itemRoster.AddToCounts(itemRosterElement.EquipmentElement, -num2);
                            }
                        }
                    }
                    return false;
                }
             }


            //Add gold to village and consume some of it, do not reset gold
            [HarmonyPatch(typeof(VillagerCampaignBehavior), "OnSettlementEntered")]
            class VillagerSettlementEnterPatch
            {
                static bool Prefix(ref Dictionary<MobileParty, List<Settlement>> ____previouslyChangedVillagerTargetsDueToEnemyOnWay, MobileParty mobileParty, Settlement settlement, Hero hero)
                {
                    if (BannerKingsConfig.Instance.PopulationManager != null && BannerKingsConfig.Instance.PopulationManager.IsSettlementPopulated(settlement))
                    {

                        if (mobileParty != null && mobileParty.IsActive && mobileParty.IsVillager)
                        {
                            ____previouslyChangedVillagerTargetsDueToEnemyOnWay[mobileParty].Clear();
                            if (settlement.IsTown)
                                SellGoodsForTradeAction.ApplyByVillagerTrade(settlement, mobileParty);

                            if (settlement.IsVillage)
                            {
                                int tax = Campaign.Current.Models.SettlementTaxModel.CalculateVillageTaxFromIncome(mobileParty.HomeSettlement.Village, mobileParty.PartyTradeGold);
                                float remainder = mobileParty.PartyTradeGold - tax;
                                mobileParty.HomeSettlement.Village.ChangeGold((int)(remainder * 0.5f));
                                mobileParty.PartyTradeGold = 0;
                                mobileParty.HomeSettlement.Village.TradeTaxAccumulated += tax;
                            }
                            if (settlement.IsTown && settlement.Town.Governor != null && settlement.Town.Governor.GetPerkValue(DefaultPerks.Trade.DistributedGoods))
                                settlement.Town.TradeTaxAccumulated += MathF.Round(DefaultPerks.Trade.DistributedGoods.SecondaryBonus);
                        }
                        return false;
                    }
                    else return true;
                }
            }


            // Pass on settlement party as parameter
            [HarmonyPatch(typeof(Town))]
            class TownItemPricePatch
            {

                [HarmonyPrefix]
                [HarmonyPatch("GetItemPrice", new Type[] { typeof(ItemObject), typeof(MobileParty), typeof(bool) })]
                static bool Prefix1(Town __instance, ref int __result, ItemObject item, MobileParty tradingParty = null, bool isSelling = false)
                {
                    if (__instance != null && __instance.MarketData != null && __instance.GarrisonParty != null && __instance.GarrisonParty.Party != null)
                    {
                        __result = __instance.MarketData.GetPrice(item, tradingParty, isSelling, __instance.GarrisonParty.Party);
                        return false;
                    }
                    else return true;
                }

                
                [HarmonyPrefix]
                [HarmonyPatch("GetItemPrice", new Type[] { typeof(EquipmentElement), typeof(MobileParty), typeof(bool) })]
                static bool Prefix2(Town __instance, ref int __result, EquipmentElement itemRosterElement, MobileParty tradingParty = null, bool isSelling = false)
                {
                    if (__instance != null && __instance.MarketData != null && __instance.GarrisonParty != null && __instance.GarrisonParty.Party != null)
                    {
                        __result = __instance.MarketData.GetPrice(itemRosterElement, tradingParty, isSelling, __instance.GarrisonParty.Party);
                        return false;
                    }
                    else return true;
                }
            }

            // Impact prosperity
            [HarmonyPatch(typeof(ChangeOwnerOfWorkshopAction), "ApplyInternal")]
            class BankruptcyPatch
            {
                static void Postfix(Workshop workshop, Hero newOwner, WorkshopType workshopType, int capital, bool upgradable, int cost, TextObject customName, ChangeOwnerOfWorkshopAction.ChangeOwnerOfWorkshopDetail detail)
                {
                    Settlement settlement = workshop.Settlement;
                    settlement.Prosperity -= 50f;
                }
            }

            // Added productions
            [HarmonyPatch(typeof(VillageGoodProductionCampaignBehavior), "TickGoodProduction")]
            class TickGoodProductionPatch
            {
                static bool Prefix(Village village, bool initialProductionForTowns)
                {

                    if (BannerKingsConfig.Instance.PopulationManager != null && BannerKingsConfig.Instance.PopulationManager.IsSettlementPopulated(village.Settlement))
                    {

                        List<(ItemObject, float)> productions = BannerKingsConfig.Instance.PopulationManager.GetProductions(BannerKingsConfig.Instance.PopulationManager.GetPopData(village.Settlement).VillageData);
                        foreach (ValueTuple<ItemObject, float> valueTuple in productions)
                        {
                            ItemObject item = valueTuple.Item1;
                            int num = MBRandom.RoundRandomized(Campaign.Current.Models.VillageProductionCalculatorModel.CalculateDailyProductionAmount(village, valueTuple.Item1));
                            if (num > 0)
                            {
                                if (!initialProductionForTowns)
                                {
                                    village.Owner.ItemRoster.AddToCounts(item, num);
                                    CampaignEventDispatcher.Instance.OnItemProduced(item, village.Owner.Settlement, num);
                                }
                                else
                                    village.TradeBound.ItemRoster.AddToCounts(item, num);

                            }
                        }
                        return false;
                    }
                    else return true; 
                }
            }


            // Retain behavior of original while updating satisfaction parameters
            [HarmonyPatch(typeof(ItemConsumptionBehavior), "MakeConsumption")]
            class ItemConsumptionPatch
            {
                static bool Prefix(Town town, Dictionary<ItemCategory, float> categoryDemand, Dictionary<ItemCategory, int> saleLog)
                {
                    if (BannerKingsConfig.Instance.PopulationManager != null && BannerKingsConfig.Instance.PopulationManager.IsSettlementPopulated(town.Settlement))
                    {
                        saleLog.Clear();
                        TownMarketData marketData = town.MarketData;
                        ItemRoster itemRoster = town.Owner.ItemRoster;
                        PopulationData popData = BannerKingsConfig.Instance.PopulationManager.GetPopData(town.Settlement);
                        for (int i = itemRoster.Count - 1; i >= 0; i--)
                        {
                            ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
                            ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
                            int amount = elementCopyAtIndex.Amount;
                            ItemCategory itemCategory = item.GetItemCategory();
                            float demand = categoryDemand[itemCategory];

                            IEnumerable<ItemConsumptionBehavior> behaviors = Campaign.Current.GetCampaignBehaviors<ItemConsumptionBehavior>();
                            MethodInfo dynMethod = behaviors.First().GetType().GetMethod("CalculateBudget", BindingFlags.NonPublic | BindingFlags.Static);
                            float num = (float)dynMethod.Invoke(null, new object[] { town, demand, itemCategory });
                            if (num > 0.01f)
                            {
                                int price = marketData.GetPrice(item, null, false, null);
                                float desiredAmount = num / (float)price;
                                if (desiredAmount > (float)amount)
                                    desiredAmount = (float)amount;


                                if (item.IsFood && town.FoodStocks <= (float)town.FoodStocksUpperLimit() * 0.1f)
                                {
                                    float requiredFood = town.FoodChange * -1f;
                                    if (amount > requiredFood)
                                        desiredAmount += requiredFood + 1f;
                                    else desiredAmount += amount;
                                }

                                int finalAmount = MBRandom.RoundRandomized(desiredAmount);
                                ConsumptionType type = Helpers.Helpers.GetTradeGoodConsumptionType(item);
                                if (finalAmount > amount)
                                {
                                    finalAmount = amount;
                                    if (type != ConsumptionType.None) popData.EconomicData.UpdateSatisfaction(type, -0.0015f);
                                }
                                else if (type != ConsumptionType.None) popData.EconomicData.UpdateSatisfaction(type, 0.001f);
                                
                                itemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement, -finalAmount);
                                categoryDemand[itemCategory] = num - desiredAmount * (float)price;
                                town.ChangeGold(finalAmount * price);
                                int num4 = 0;
                                saleLog.TryGetValue(itemCategory, out num4);
                                saleLog[itemCategory] = num4 + finalAmount;
                            }
                        }

                        if (town.FoodStocks <= (float)town.FoodStocksUpperLimit() * 0.05f && town.Settlement.Stash != null)
                        {
                            List<ItemRosterElement> elements = new List<ItemRosterElement>();
                            foreach (ItemRosterElement element in town.Settlement.Stash)
                                if (element.EquipmentElement.Item != null && element.EquipmentElement.Item.ItemCategory.Properties == ItemCategory.Property.BonusToFoodStores)
                                    elements.Add(element);

                            foreach (ItemRosterElement element in elements)
                            {
                                ItemCategory category = element.EquipmentElement.Item.ItemCategory;
                                if (saleLog.ContainsKey(category))
                                    saleLog[category] += element.Amount;
                                else saleLog.Add(category, element.Amount);
                                town.Settlement.Stash.Remove(element);
                            }
                        }


                        List<Town.SellLog> list = new List<Town.SellLog>();
                        foreach (KeyValuePair<ItemCategory, int> keyValuePair in saleLog)
                            if (keyValuePair.Value > 0)
                                list.Add(new Town.SellLog(keyValuePair.Key, keyValuePair.Value));
 
                        town.SetSoldItems(list);
                        return false;
                    }
                    else return true;
                }
            }
        }     
    }
}
