﻿using BannerKings.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BannerKings.Managers
{
    public class TitleManager
    {
        private Dictionary<FeudalTitle, (Hero, Hero)> TITLES { get; set; }
        public Dictionary<Kingdom, FeudalTitle> KINGDOMS { get; set; }

        public TitleManager(Dictionary<FeudalTitle, (Hero, Hero)> titles, Dictionary<Hero, List<FeudalTitle>> titleHolders, Dictionary<Kingdom, FeudalTitle> kingdoms)
        {
            this.TITLES = titles;
            this.KINGDOMS = kingdoms;
            InitializeTitles();
        }

        public bool IsHeroTitleHolder(Hero hero)
        {
            FeudalTitle result = null;
            foreach (KeyValuePair<FeudalTitle, (Hero, Hero)> pair in TITLES)
                if (pair.Value.Item1 == hero || pair.Value.Item2 == hero)
                {
                    result = pair.Key;
                    break;
                }

            return result != null;
        }
        public FeudalTitle GetTitle(Settlement settlement)
        {
            FeudalTitle result = null;
            foreach (KeyValuePair<FeudalTitle, (Hero, Hero)> pair in TITLES)
                if (pair.Key.fief == settlement)
                {
                    result = pair.Key;
                    break;
                }
                
            return result;
        }

        public Hero GetDeJure(Settlement settlement) => this.GetTitle(settlement).deJure;
        public bool IsHeroKnighted(Hero hero) => hero.IsNoble && IsHeroTitleHolder(hero);
        public FeudalTitle GetImmediateSuzerain(FeudalTitle target)
        {
            FeudalTitle result = null;
            foreach (KeyValuePair<FeudalTitle, (Hero, Hero)> pair in TITLES)
                    if (pair.Key.vassals != null && pair.Key.vassals.Contains(target))
                    {
                        result = pair.Key;
                        break;
                    }

            return result;
        }

        private void ExecuteOwnershipChange(Hero oldOwner, Hero newOwner, FeudalTitle title, bool deJure)
        {
            if (TITLES.ContainsKey(title))
            {
                ValueTuple<Hero, Hero> tuple = TITLES[title];
                if (deJure)
                {
                    title.deJure = newOwner;
                    tuple.Item1 = newOwner;
                }
                else
                {
                    title.deFacto = newOwner;
                    tuple.Item2 = newOwner;
                }
                TITLES[title] = tuple;
            }
        }

        private void ExecuteAddTitle(FeudalTitle title)
        {
            List<FeudalTitle> keys = TITLES.Keys.ToList();
            if (!keys.Contains(title))
                TITLES.Add(title, new (title.deJure, title.deFacto));
        }


        public FeudalTitle CalculateHeroSuzerain(Hero hero)  
        {
            FeudalTitle title = GetHighestTitle(hero);
            if (title == null) return null;
            Kingdom kingdom1 = GetTitleFaction(title);

            if (kingdom1 == null || hero.Clan.Kingdom == null) return null;

            FeudalTitle suzerain = GetImmediateSuzerain(title);
            if (suzerain != null)
            {
                Kingdom kingdom2 = GetTitleFaction(suzerain);
                if (kingdom2 == kingdom1)
                    return suzerain;
                else
                {
                    FeudalTitle factionTitle = GetHighestTitleWithinFaction(hero, kingdom1);
                    if (factionTitle != null)
                    {
                        FeudalTitle suzerainFaction = GetImmediateSuzerain(factionTitle);
                        return suzerainFaction;
                    } else
                        return GetHighestTitle(kingdom1.Leader);
                }
            }

            return null;
        }

        public bool HasSuzerain(Settlement settlement)
        {
            FeudalTitle vassal = GetTitle(settlement);
            if (vassal != null)
            {
                FeudalTitle suzerain = GetImmediateSuzerain(vassal);
                return suzerain != null;
            }
            return false;
        }

        public bool HasSuzerain(FeudalTitle vassal)
        {
            FeudalTitle suzerain = GetImmediateSuzerain(vassal);
            return suzerain != null;
        }

        public void InheritAllTitles(Hero oldOwner, Hero heir)
        {
            if (IsHeroTitleHolder(oldOwner))
            {
                List<FeudalTitle> set = GetAllDeJure(oldOwner);
                List<FeudalTitle> titles = new List<FeudalTitle>(set);
                foreach (FeudalTitle title in titles)
                {
                    if (title.deJure == oldOwner) ExecuteOwnershipChange(oldOwner, heir, title, true);
                    if (title.deFacto == oldOwner) ExecuteOwnershipChange(oldOwner, heir, title, false);
                }
            }
        }

        public void InheritTitle(Hero oldOwner, Hero heir, FeudalTitle title)
        {
            if (IsHeroTitleHolder(oldOwner))
            {
                if (title.deJure == oldOwner) ExecuteOwnershipChange(oldOwner, heir, title, true);
                if (title.deFacto == oldOwner) ExecuteOwnershipChange(oldOwner, heir, title, false);  
            }
        }

        public void UsurpTitle(Hero oldOwner, Hero usurper, FeudalTitle title, UsurpCosts costs)
        {
            ExecuteOwnershipChange(oldOwner, usurper, title, true);
            int impact = new BKUsurpationModel().GetUsurpRelationImpact(title);
            ChangeRelationAction.ApplyPlayerRelation(oldOwner, impact, true, true);
            Kingdom kingdom = oldOwner.Clan.Kingdom;
            if (kingdom != null) 
                foreach(Clan clan in kingdom.Clans) 
                    if (clan != oldOwner.Clan && clan.IsMapFaction && clan.IsKingdomFaction)
                    {
                        int random = MBRandom.RandomInt(1, 100);
                        if (random <= 10)
                            ChangeRelationAction.ApplyPlayerRelation(oldOwner, (int)((float)impact * 0.3f), true, true);
                    }

            if (costs.gold > 0)
                usurper.ChangeHeroGold((int)-costs.gold);
            if (costs.influence > 0)
                usurper.Clan.Influence -= costs.influence;
            if (costs.renown > 0)
                usurper.Clan.Renown -= costs.renown;
            //OwnershipNotification notification = new OwnershipNotification(title, new TextObject(string.Format("You are now the rightful owner to {0}", title.name)));
            //Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(notification);
        }
        
        public List<FeudalTitle> GetAllDeJure(Hero hero)
        {
            List<FeudalTitle> list = new List<FeudalTitle>();
            foreach (KeyValuePair<FeudalTitle, (Hero, Hero)> pair in TITLES)
                if (pair.Value.Item1 == hero)
                    list.Add(pair.Key);
            return list;
        }

        public List<FeudalTitle> GetAllDeJure(Clan clan)
        {
            List<FeudalTitle> list = new List<FeudalTitle>();
            foreach (Hero hero in clan.Heroes)
                foreach (FeudalTitle title in GetAllDeJure(hero))
                    list.Add(title);

            return list;
        }
        public FeudalTitle GetHighestTitle(Hero hero)
        {
            if (IsHeroTitleHolder(hero))
            {
                FeudalTitle highestTitle = null;
                foreach (FeudalTitle title in GetAllDeJure(hero))
                    if (highestTitle == null || title.type < highestTitle.type)
                        highestTitle = title;
                return highestTitle;
            }
            else return null;
        }

        public FeudalTitle GetHighestTitleWithinFaction(Hero hero, Kingdom faction)
        {
            if (IsHeroTitleHolder(hero))
            {
                FeudalTitle highestTitle = null;
                foreach (FeudalTitle title in GetAllDeJure(hero))
                    if ((highestTitle == null || title.type < highestTitle.type) && GetTitleFaction(title) == faction)
                        highestTitle = title;
                return highestTitle;
            }
            else return null;
        }

        public FeudalTitle GetSovereignTitle(Kingdom faction)
        {
            if (KINGDOMS.ContainsKey(faction))
                return KINGDOMS[faction];
            else return null;
        }

        public Kingdom GetFactionFromSettlement(Settlement settlement)
        {
            FeudalTitle title = GetTitle(settlement);
            if (title != null)
            {
                Kingdom faction = KINGDOMS.FirstOrDefault(x => x.Value == title.sovereign).Key;
                return faction;
            }
            return null;
        }

        public FeudalTitle GetSovereignFromSettlement(Settlement settlement)
        {
            FeudalTitle title = GetTitle(settlement);
            if (title != null)
                return title.sovereign;
            
            return null;
        }

        public List<FeudalTitle> GetVassals(TitleType threshold, Hero lord)
        {
            List<FeudalTitle> allTitles = GetAllDeJure(lord);
            List<FeudalTitle> vassals = new List<FeudalTitle>();
            foreach (FeudalTitle title in allTitles)
                if (title.deFacto.MapFaction == lord.MapFaction && (title.deFacto == title.deJure || title.deJure.MapFaction == lord.MapFaction)
                    && (int)title.type <= (int)threshold)
                    vassals.Add(title);
            return vassals;
        }

        public List<FeudalTitle> GetVassals(Hero lord)
        {
            List<FeudalTitle> vassals = new List<FeudalTitle>();
            FeudalTitle highest = this.GetHighestTitle(lord);
            if (highest != null) 
            {
                TitleType threshold = this.GetHighestTitle(lord).type + 1;
                List<FeudalTitle> allTitles = GetAllDeJure(lord);

                foreach (FeudalTitle title in allTitles)
                    if (title.deFacto.MapFaction == lord.MapFaction && (title.deFacto == title.deJure || title.deJure.MapFaction == lord.MapFaction)
                        && (int)title.type >= (int)threshold)
                        vassals.Add(title);
            }
            
            return vassals;
        }


        public Kingdom GetTitleFaction(FeudalTitle title)
        {
            Kingdom faction = null;
            FeudalTitle sovereign = title.sovereign;
            if (sovereign != null)
                faction = KINGDOMS.FirstOrDefault(x => x.Value == sovereign).Key;
            else if (KINGDOMS.ContainsValue(title))
                faction = KINGDOMS.FirstOrDefault(x => x.Value == title).Key;

            return faction;
        }

        public void InitializeTitles()
        {
            XmlDocument doc = BannerKings.Helpers.Helpers.CreateDocumentFromXmlFile(BasePath.Name + "Modules/BannerKings/ModuleData/titles.xml");
            XmlNode titlesNode = doc.ChildNodes[1].ChildNodes[0];
            bool autoGenerate = bool.Parse(titlesNode.Attributes["autoGenerate"].Value);
 
            foreach (XmlNode kingdom in titlesNode)
            {
                if (kingdom.Name != "kingdom") return;

                HashSet<FeudalTitle> vassalsKingdom = new HashSet<FeudalTitle>();
                string factionName = kingdom.Attributes["faction"].Value;
                string deJureNameKingdom = kingdom.Attributes["deJure"].Value;
                Hero deJureKingdom = Hero.AllAliveHeroes.FirstOrDefault(x => x.StringId == deJureNameKingdom);
                Kingdom faction = Kingdom.All.FirstOrDefault(x => x.Name.ToString() == factionName);
                string contractType = kingdom.Attributes["contract"].Value;
                FeudalContract contract = GenerateContract(contractType);

                if (contract == null) return;

                if (kingdom.ChildNodes != null)
                    foreach (XmlNode duchy in kingdom.ChildNodes)
                    {
                        if (duchy.Name != "duchy") return;

                        HashSet<FeudalTitle> vassalsDuchy = new HashSet<FeudalTitle>();
                        string dukedomName = duchy.Attributes["name"].Value;
                        string deJureNameDuchy = duchy.Attributes["deJure"].Value;
                        Hero deJureDuchy = Hero.AllAliveHeroes.FirstOrDefault(x => x.StringId == deJureNameDuchy);

                        if (duchy.ChildNodes != null)
                            foreach (XmlNode county in duchy.ChildNodes)
                            {
                                if (county.Name != "county") return;

                                string settlementNameCounty = county.Attributes["settlement"].Value;
                                string deJureNameCounty = county.Attributes["deJure"].Value;
                                Settlement settlementCounty = Settlement.All.FirstOrDefault(x => x.Name.ToString() == settlementNameCounty);
                                Hero deJureCounty = Hero.AllAliveHeroes.FirstOrDefault(x => x.StringId == deJureNameCounty);
                                HashSet<FeudalTitle> vassalsCounty = new HashSet<FeudalTitle>();

                                if (county.ChildNodes != null)
                                    foreach (XmlNode barony in county.ChildNodes)
                                    {
                                        if (barony.Name != "barony") return;

                                        string settlementNameBarony = barony.Attributes["settlement"].Value;
                                        string deJureIdBarony = barony.Attributes["deJure"].Value;
                                        Settlement settlementBarony = Settlement.All.FirstOrDefault(x => x.Name.ToString() == settlementNameBarony);
                                        Hero deJureBarony = Hero.AllAliveHeroes.FirstOrDefault(x => x.StringId == deJureIdBarony);
                                        if (settlementBarony != null)
                                            vassalsCounty.Add(CreateLandedTitle(settlementBarony, deJureBarony, TitleType.Barony, contract));
                                    }

                                if (settlementCounty != null)
                                    vassalsDuchy.Add(CreateLandedTitle(settlementCounty, deJureCounty, TitleType.County, contract, vassalsCounty));
                            }

                        if (deJureDuchy != null && vassalsDuchy.Count > 0)
                            vassalsKingdom.Add(CreateUnlandedTitle(deJureDuchy, TitleType.Dukedom, vassalsDuchy, dukedomName, contract));
                    }

                if (deJureKingdom != null && vassalsKingdom.Count > 0 && faction != null)
                {
                    FeudalTitle sovereign = CreateKingdom(deJureKingdom, faction, TitleType.Kingdom, vassalsKingdom, contract);
                    foreach (FeudalTitle duchy in vassalsKingdom)
                        duchy.SetSovereign(sovereign);
                }
                    
            }

            if (autoGenerate) 
                foreach (Settlement settlement in Settlement.All)
                {
                    if (settlement.IsVillage) continue;
                    else {
                        if (settlement.OwnerClan != null && settlement.OwnerClan.Leader != null) 
                            CreateLandedTitle(settlement, settlement.Owner, settlement.IsTown ? TitleType.County : TitleType.Barony, GenerateContract("feudal"), null);
                    }
                }         
        }

        public void GrantLordship(FeudalTitle title, Hero giver, Hero receiver)
        {
            ExecuteOwnershipChange(giver, receiver, title, true);
            ExecuteOwnershipChange(giver, receiver, title, false);
            receiver.IsNoble = true;
        }

        public void ApplyOwnerChange(Settlement settlement, Hero newOwner)
        {
            FeudalTitle title = GetTitle(settlement);
            if (title == null) return;

            ExecuteOwnershipChange(settlement.Owner, newOwner, title, false);
            if (!settlement.IsVillage && settlement.BoundVillages != null && settlement.BoundVillages.Count > 0 && title.vassals != null &&
                title.vassals.Count > 0)
                foreach (FeudalTitle lordship in title.vassals.Where(y => y.type == TitleType.Lordship))
                    ExecuteOwnershipChange(settlement.Owner, newOwner, title, false);
        }


        public void DeactivateTitle(FeudalTitle title)
        {
            ExecuteOwnershipChange(title.deJure, null, title, true);
            ExecuteOwnershipChange(title.deFacto, null, title, false);
        }

        public void DeactivateDeJure(FeudalTitle title) => ExecuteOwnershipChange(title.deJure, null, title, true);

        public void ShowContract(Hero lord, string buttonString)
        {
            FeudalTitle title = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(lord);
            string description = BannerKingsConfig.Instance.TitleManager.GetContractText(title);
            InformationManager.ShowInquiry(new InquiryData(string.Format("Enfoeffement Contract for {0}", title.name),
                description, true, false, buttonString, "", null, null), false);
        }

        public FeudalTitle GetDuchy(FeudalTitle title)
        {
            IEnumerable<FeudalTitle> duchies = TITLES.Keys.Where(x => x.type == TitleType.Dukedom && x.sovereign != null && x.sovereign == title.sovereign);
            FeudalTitle result = null;
            foreach (FeudalTitle duchy in duchies)
                if (duchy.vassals.Contains(title))
                    result = duchy;

            if (result == null)
                foreach (FeudalTitle duchy in duchies)
                    foreach (FeudalTitle county in duchy.vassals)
                        if (county.vassals.Contains(title))
                            result = duchy;

            if (result == null)
                foreach (FeudalTitle duchy in duchies)
                    foreach (FeudalTitle county in duchy.vassals)
                        foreach (FeudalTitle barony in county.vassals)
                            if (barony.vassals.Contains(title))
                                result = duchy;

            return result;
        }

        public string GetContractText(FeudalTitle title)
        {
            FeudalContract contract = title.contract;
            StringBuilder sb = new StringBuilder(string.Format("You, {0}, formally accept to be henceforth bound to the {1}, fulfill your duties as well as uphold your rights," +
                " what can not be undone by means other than abdication of all rights and lands associated with the contract, treachery, or death.", Hero.MainHero.Name.ToString(), title.name.ToString()));
            sb.Append(Environment.NewLine);
            sb.Append("   ");
            sb.Append(Environment.NewLine);
            sb.Append("Duties");
            sb.Append(Environment.NewLine);
            foreach (KeyValuePair<FeudalDuties, float> duty in contract.duties)
            {
                if (duty.Key != FeudalDuties.Auxilium) sb.Append(string.Format(this.GetDutyString(duty.Key), duty.Value));
                else sb.Append(this.GetDutyString(duty.Key));
                sb.Append(Environment.NewLine);
            }
            sb.Append(Environment.NewLine);
            sb.Append("   ");
            sb.Append(Environment.NewLine);
            sb.Append("Rights");
            sb.Append(Environment.NewLine);
            foreach (FeudalRights right in contract.rights)
            {
                sb.Append(GetRightString(right));
                sb.Append(Environment.NewLine);
            }
           
            return sb.ToString();
        }

        private string GetDutyString(FeudalDuties duty)
        {
            if (duty == FeudalDuties.Taxation)
                return "You are due {0} of your fief's income to your suzerain.";
            else if (duty == FeudalDuties.Auxilium)
                return "You are obliged to militarily participate in armies.";
            else return "You are obliged to contribute to {0} of your suzerain's ransom.";
        }

        private string GetRightString(FeudalRights right)
        {
            if (right == FeudalRights.Absolute_Land_Rights)
                return "You are entitled to ownership of any conquered lands whose title you own.";
            else if (right == FeudalRights.Enfoeffement_Rights)
                return "You are entitled to be granted land in case you have none, whenever possible.";
            else if (right == FeudalRights.Conquest_Rights)
                return "You are entitle to the ownership of any lands you conquered by yourself.";
            else return "";
        }

        private FeudalContract GenerateContract(string type)
        {
            if (type == "imperial")
                return new FeudalContract(new Dictionary<FeudalDuties, float>() {
                    { FeudalDuties.Ransom, 0.10f },
                    { FeudalDuties.Taxation, 0.4f }
                }, new HashSet<FeudalRights>() {
                    FeudalRights.Assistance_Rights,
                    FeudalRights.Army_Compensation_Rights
                }, GovernmentType.Imperial, SuccessionType.Imperial,
                InheritanceType.Primogeniture, GenderLaw.Agnatic);
            else if (type == "tribal")
                return new FeudalContract(new Dictionary<FeudalDuties, float>() {
                    { FeudalDuties.Taxation, 0.125f },
                    { FeudalDuties.Auxilium, 0.66f }
                }, new HashSet<FeudalRights>() {
                    FeudalRights.Conquest_Rights,
                    FeudalRights.Absolute_Land_Rights
                }, GovernmentType.Tribal, SuccessionType.Elective_Monarchy,
                InheritanceType.Seniority, GenderLaw.Agnatic);
            else if (type == "republic")
                return new FeudalContract(new Dictionary<FeudalDuties, float>() {
                    { FeudalDuties.Ransom, 0.10f },
                    { FeudalDuties.Taxation, 0.4f }
                }, new HashSet<FeudalRights>() {
                    FeudalRights.Assistance_Rights,
                    FeudalRights.Army_Compensation_Rights
                }, GovernmentType.Republic, SuccessionType.Republic,
                InheritanceType.Primogeniture, GenderLaw.Cognatic);
            else return new FeudalContract(new Dictionary<FeudalDuties, float>() {
                    { FeudalDuties.Ransom, 0.20f },
                    { FeudalDuties.Auxilium, 0.4f }
                }, new HashSet<FeudalRights>() {
                    FeudalRights.Absolute_Land_Rights,
                    FeudalRights.Enfoeffement_Rights
                }, GovernmentType.Feudal, SuccessionType.Hereditary_Monarchy,
                InheritanceType.Primogeniture, GenderLaw.Agnatic);
        }

        private FeudalTitle CreateKingdom(Hero deJure, Kingdom faction, TitleType type, HashSet<FeudalTitle> vassals, FeudalContract contract)
        {
            FeudalTitle title = new FeudalTitle(type, null, vassals, deJure, faction.Leader, faction.Name.ToString(), contract);
            ExecuteAddTitle(title);
            KINGDOMS.Add(faction, title);
            return title;
        }

        private FeudalTitle CreateUnlandedTitle(Hero deJure, TitleType type, HashSet<FeudalTitle> vassals, string name, FeudalContract contract)
        {
            FeudalTitle title = new FeudalTitle(type, null, vassals, deJure, deJure, name, contract);
            ExecuteAddTitle(title);
            return title;
        }
            
        private FeudalTitle CreateLandedTitle(Settlement settlement, Hero deJure, TitleType type, FeudalContract contract, HashSet<FeudalTitle> vassals = null)
        {
            Hero deFacto = settlement.OwnerClan.Leader;
            if (deJure == null) deJure = settlement.Owner;
            if (vassals == null) vassals = new HashSet<FeudalTitle>();
            if (settlement.BoundVillages != null)
                foreach (Village lordship in settlement.BoundVillages)
                {
                    FeudalTitle lordshipTitle = CreateLordship(lordship.Settlement, deJure, contract);
                    vassals.Add(lordshipTitle);
                    ExecuteAddTitle(lordshipTitle);
                }
            FeudalTitle title = new FeudalTitle(type, settlement, vassals, deJure, deFacto, settlement.Name.ToString(), contract);
            ExecuteAddTitle(title);
            return title;
        }

        private FeudalTitle CreateLordship(Settlement settlement, Hero deJure, FeudalContract contract) => new FeudalTitle(TitleType.Lordship, settlement, null,
            deJure, settlement.Village.Bound.Owner, settlement.Name.ToString(), contract);

        public class FeudalTitle
        {
            public TitleType type { get; private set; }
            public Settlement fief { get; private set; }
            public HashSet<FeudalTitle> vassals { get; private set; }
            public Hero deJure { get; internal set; }
            public Hero deFacto { get; internal set; }
            public TextObject name { get; private set; }
            public TextObject shortName { get; private set; }
            public float dueTax { get; set; }
            public FeudalTitle sovereign { get; private set; }
            public FeudalContract contract { get; private set;  }

            public override bool Equals(object obj)
            {
                if (obj is FeudalTitle)
                {
                    FeudalTitle target = (FeudalTitle)obj;
                    return this.fief != null ? this.fief == target.fief : this.name == target.name;
                }
                return base.Equals(obj);
            }

            public FeudalTitle(TitleType type, Settlement fief, HashSet<FeudalTitle> vassals, Hero deJure, Hero deFacto, string name, FeudalContract contract)
            {
                this.type = type;
                this.fief = fief;
                this.vassals = vassals;
                this.deJure = deJure;
                this.deFacto = deFacto;
                this.name = new TextObject(BannerKings.Helpers.Helpers.GetTitlePrefix(type) + " of " + name);
                this.shortName = new TextObject(name);
                this.contract = contract;
                dueTax = 0;
            }

            public bool Active => this.deJure != null || this.deFacto != null;

            public void SetSovereign(FeudalTitle sovereign)
            {
                this.sovereign = sovereign;
                if (this.vassals != null && this.vassals.Count > 0)
                    foreach (FeudalTitle vassal in this.vassals)
                        vassal.SetSovereign(sovereign);
            }
        }

        public class FeudalContract
        {
            public Dictionary<FeudalDuties, float> duties { get; private set; }
            public HashSet<FeudalRights> rights { get; private set; }
            public GovernmentType government { get; private set; }
            public SuccessionType succession { get; private set; }
            public InheritanceType inheritance { get; private set; }
            public GenderLaw genderLaw { get; private set; }

            public FeudalContract(Dictionary<FeudalDuties, float> duties, HashSet<FeudalRights> rights, GovernmentType government,
                SuccessionType succession, InheritanceType inheritance, GenderLaw genderLaw)
            {
                this.duties = duties;
                this.rights = rights;
                this.government = government;
                this.succession = succession;
                this.inheritance = inheritance;
                this.genderLaw = genderLaw;
            }
        }

        public enum TitleType
        {
            Empire,
            Kingdom,
            Dukedom,
            County,
            Barony,
            Lordship
        }

        public class UsurpCosts
        {
            public float gold { get; private set; }
            public float influence { get; private set; }
            public float renown { get; private set; }

            public UsurpCosts(float gold, float influence, float renown)
            {
                this.gold = gold;
                this.influence = influence;
                this.renown = renown;
            }
        }

        public enum FeudalDuties
        {
            Ransom,
            Taxation,
            Auxilium
        }

        public enum FeudalRights
        {
            Absolute_Land_Rights,
            Conquest_Rights,
            Enfoeffement_Rights,
            Assistance_Rights,
            Army_Compensation_Rights
        }

        public enum CasusBelli
        {
            None,
            Conquest,
            Provocation,
            Lawful_Claim,
            Imperial_Reconquest
        }

        public enum LegitimacyType
        {
            Lawful,
            Lawful_Foreigner,
            Unlawful,
            Unlawful_Foreigner
        }

        public enum SuccessionType
        {
            Hereditary_Monarchy,
            Elective_Monarchy,
            Imperial,
            Republic
        }

        public enum InheritanceType
        {
            Primogeniture,
            Ultimogeniture,
            Seniority
        }

        public enum GenderLaw
        {
            Agnatic,
            Cognatic
        }

        public enum GovernmentType
        {
            Feudal,
            Tribal,
            Imperial,
            Republic
        }
    }
}
