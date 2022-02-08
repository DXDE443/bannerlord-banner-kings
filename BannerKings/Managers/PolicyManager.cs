﻿using BannerKings.Managers.Policies;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;
using static BannerKings.Managers.Policies.BKGarrisonPolicy;
using static BannerKings.Managers.Policies.BKMilitiaPolicy;
using static BannerKings.Managers.Policies.BKCriminalPolicy;

namespace BannerKings.Managers
{
    public class PolicyManager
    {
        [SaveableProperty(100)]
        public Dictionary<Settlement, List<DecisionsElement>> DECISIONS { get; set; }
        private Dictionary<Settlement, HashSet<BannerKingsPolicy>> SettlementPolicies { get; set; }

        public PolicyManager(Dictionary<Settlement, List<DecisionsElement>> DECISIONS, Dictionary<Settlement, HashSet<BannerKingsPolicy>> POLICIES)
        {
            this.DECISIONS = DECISIONS;
            this.SettlementPolicies = POLICIES;
        }

        public bool IsSettlementPoliciesSet(Settlement settlement) => DECISIONS.ContainsKey(settlement);
        public List<DecisionsElement> GetDefaultDecisions(Settlement settlement)
        {
            if (!DECISIONS.ContainsKey(settlement))
                InitializeSettlementPolicies(settlement);

            return DECISIONS[settlement];
        }

        public static void InitializeSettlementPolicies(Settlement settlement)
        {
            if (!BannerKingsConfig.Instance.PolicyManager.DECISIONS.ContainsKey(settlement))
            {
                List<DecisionsElement> decisions = new List<DecisionsElement>();
                if (settlement.IsTown)
                {
                    decisions.Add(new DecisionsElement("Allow slaves to be exported", "Slave caravans will be formed when slave population is big", true, PolicyType.EXPORT_SLAVES));
                    decisions.Add(new DecisionsElement("Accelerate population growth", "Use your influence to provide better housing and allow more population growth", false, PolicyType.POP_GROWTH));
                    decisions.Add(new DecisionsElement("Allow settlement to self-invest", "Income generated by the settlement will be reverted back into it's economic development", false, PolicyType.SELF_INVEST));
                    decisions.Add(new DecisionsElement("Conscript the lowmen", "Extensive recruitment will draft serfs into the militia, costing gold and reducing the productive workforce", false, PolicyType.CONSCRIPTION));
                    decisions.Add(new DecisionsElement("Subsidize the militia", "Improve militia quality by subsidizing their equipment and trainning", false, PolicyType.SUBSIDIZE_MILITIA));
                    decisions.Add(new DecisionsElement("Exempt nobles from taxes", "Exempt nobles from taxes, making them vouch in your favor", false, PolicyType.EXEMPTION));
                }
                else if (settlement.IsVillage)
                {
                    decisions.Add(new DecisionsElement("Accelerate population growth", "Population will grow faster at the cost of influence", false, PolicyType.POP_GROWTH));
                    decisions.Add(new DecisionsElement("Allow settlement to self-invest", "Income generated by the settlement will be reverted back into it's growth", false, PolicyType.SELF_INVEST));
                    decisions.Add(new DecisionsElement("Subsidize the militia", "Improve militia quality by subsidizing their equipment and trainning", false, PolicyType.SUBSIDIZE_MILITIA));
                }
                else
                {
                    decisions.Add(new DecisionsElement("Allow slaves to be exported", "Slave caravans will be formed when slave population is big", true, PolicyType.EXPORT_SLAVES));
                    decisions.Add(new DecisionsElement("Accelerate population growth", "Use your influence to provide better housing and allow more population growth", false, PolicyType.POP_GROWTH));
                    decisions.Add(new DecisionsElement("Allow settlement to self-invest", "Income generated by the settlement will be reverted back into it's economic development", false, PolicyType.SELF_INVEST));
                    decisions.Add(new DecisionsElement("Conscript the lowmen", "Extensive recruitment will draft serfs into the militia, costing gold and reducing the productive workforce", false, PolicyType.CONSCRIPTION));
                    decisions.Add(new DecisionsElement("Subsidize the militia", "Improve militia quality by subsidizing their equipment and trainning", false, PolicyType.SUBSIDIZE_MILITIA));
                    decisions.Add(new DecisionsElement("Exempt nobles from taxes", "Exempt nobles from taxes, making them vouch in your favor", false, PolicyType.EXEMPTION));
                }

                BannerKingsConfig.Instance.PolicyManager.DECISIONS.Add(settlement, decisions);
            }  
        }

        public BannerKingsPolicy GetPolicy(Settlement settlement, string policyType)
        {
            BannerKingsPolicy result = null;
            if (SettlementPolicies.ContainsKey(settlement))
            {
                HashSet<BannerKingsPolicy> policies = SettlementPolicies[settlement];
                BannerKingsPolicy policy = policies.FirstOrDefault(x => x.Identifier == policyType);
                if (policy != null) result = policy;
            } else
            {
                result = GeneratePolicy(settlement, policyType);
                HashSet<BannerKingsPolicy> set = new HashSet<BannerKingsPolicy>();
                set.Add(result);
                SettlementPolicies.Add(settlement, set);
            }

            if (result == null) result = GeneratePolicy(settlement, policyType);

            return result;
        }

        public BannerKingsPolicy GeneratePolicy(Settlement settlement, string policyType)
        {
            if (policyType == "garrison")
                return new BKGarrisonPolicy(GarrisonPolicy.Standard, settlement);
            else if (policyType == "militia")
                return new BKMilitiaPolicy(MilitiaPolicy.Balanced, settlement);
            else return new BKCriminalPolicy(CriminalPolicy.Enslavement, settlement);
        }

        private void AddSettlementPolicy(Settlement settlement)
        {
            SettlementPolicies.Add(settlement, new HashSet<BannerKingsPolicy>());
        }

        public void UpdateSettlementPolicy(Settlement settlement, BannerKingsPolicy policy)
        {
            if (SettlementPolicies.ContainsKey(settlement))
            {
                HashSet<BannerKingsPolicy> policies = SettlementPolicies[settlement];
                BannerKingsPolicy target = policies.FirstOrDefault(x => x.Identifier == policy.Identifier);
                if (target != null) policies.Remove(target);
                policies.Add(policy);
            }
            else
            {
                AddSettlementPolicy(settlement);
            }
        }

        /*
        public string GetMilitiaHint(MilitiaPolicy policy)
        {
            if (policy == MilitiaPolicy.Melee)
                return "Focus three fourths of the militia as melee troops";
            else if (policy == MilitiaPolicy.Ranged)
                return "Focus three fourths of the militia as ranged troops";
            else return "Split militia equally between ranged and melee troops";
        }

        public string GetTaxHint(TaxType policy, bool isVillage)
        {

            if (policy == TaxType.High)
            {
                if (!isVillage) return "Yield more tax from the population, but reduce growth";
                else return "Yield more tax from the population, at the cost of decreased loyalty";
            }
            else if (policy == TaxType.Low)
            {
                if (!isVillage) return "Reduce tax burden on the population, diminishing your profit but increasing their support towards you";
                else return "Reduce tax burden on the population, encouraging new settlers";
            }
            else if (policy == TaxType.Exemption)
                return "Fully exempt notables from taxes, improving their attitude towards you";
            else return "Standard tax of the land, with no particular repercussions";
        }

        public string GetCrimeHint(CriminalPolicy policy)
        {
            if (policy == CriminalPolicy.Enslavement)
                return "Prisoners sold in the settlement will be enslaved and join the population. No particular repercussions";
            else if (policy == CriminalPolicy.Execution)
                return "Prisoners will suffer the death penalty. No ransom is paid, but the populace feels at ease knowing there are less threats in their daily lives";
            else return "Forgive criminals and prisoners of war";
        }

        public string GetWorkHint(WorkforcePolicy policy)
        {
            if (policy == WorkforcePolicy.Construction)
                return "Serfs aid in construction for a gold cost, and food production suffers a penalty";
            else if (policy == WorkforcePolicy.Land_Expansion)
                return "Divert slaves and serf workforces to expand the arable land, reducing their outputs while extending usable land";
            else if (policy == WorkforcePolicy.Martial_Law)
                return "Put the militia on active duty, increasing security but costing a food upkeep. Negatively impacts production efficiency";
            else return "No particular policy is implemented";
        }

        public string GetTariffHint(TariffType policy)
        {
            if (policy == TariffType.Standard)
                return "A tariff is paid to the lord by the settlement when items are sold. This tariff is embedded into prices, meaning tariffs make prices higher overall";
            else if (policy == TariffType.Internal_Consumption)
                return "The standard tariff is maintained and a discount is offered to internal consumers (workshops and population). This discount is paid for by the merchants, who won't be happy with it";
            else return "No tariff is charged, reducing prices and possibly attracting more caravans";
        } */

        private DecisionsElement GetPolicyElementFromType(PolicyType type)
        {
            if (type == PolicyType.EXPORT_SLAVES)
                return new DecisionsElement("Allow slaves to be exported", "Slave caravans will be formed when slave population is big", true, PolicyType.EXPORT_SLAVES);
            else if (type == PolicyType.POP_GROWTH)
                return new DecisionsElement("Accelerate population growth", "Population will grow faster at the cost of influence", false, PolicyType.POP_GROWTH);
            else return null;
        }

        public bool IsPolicyEnacted(Settlement settlement, PolicyType policy)
        {
            DecisionsElement element = null;
            if (DECISIONS.ContainsKey(settlement))
                element = DECISIONS[settlement].Find(x => x.type == policy);
            return element != null ? element.isChecked : false;
        }

        public void UpdatePolicy(Settlement settlement, PolicyType policy, bool value)
        {
            DecisionsElement element = DECISIONS[settlement].Find(x => x.type == policy);
            if (element != null) element.isChecked = value;  
            else
            {
                DecisionsElement elementToAdd = GetPolicyElementFromType(policy);
                if (elementToAdd != null)
                {
                    elementToAdd.isChecked = value;
                    DECISIONS[settlement].Add(elementToAdd);
                }
            }
        }

        public class DecisionsElement
        {
            [SaveableProperty(1)]
            public string description { get; set; }

            [SaveableProperty(2)]
            public string hint { get; set; }

            [SaveableProperty(3)]
            public bool isChecked { get; set; }

            [SaveableProperty(4)]
            public PolicyType type { get; set; }

            public DecisionsElement(string description, string hint, bool isChecked, PolicyType type)
            {
                this.description = description;
                this.hint = hint;
                this.isChecked = isChecked;
                this.type = type;
            }
        }

        public enum WorkforcePolicy
        {
            None,
            Land_Expansion,
            Martial_Law,
            Construction
        }

        public enum PolicyType
        {
            EXPORT_SLAVES,
            POP_GROWTH,
            SELF_INVEST,
            CONSCRIPTION,
            EXEMPTION,
            SUBSIDIZE_MILITIA
        }

        public enum TaxType
        {
            High,
            Standard,
            Low,
            Exemption
        }

        public enum TariffType
        {
            Standard,
            Internal_Consumption,
            Exemption
        }

        public enum DraftPolicy
        {
            Standard,
            Enlistment
        }
    }
}
