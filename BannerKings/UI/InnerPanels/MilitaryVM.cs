﻿using BannerKings.Managers.Policies;
using BannerKings.Models;
using BannerKings.Populations;
using BannerKings.UI.Items;
using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static BannerKings.Managers.PolicyManager;

namespace BannerKings.UI
{
    public class MilitaryVM : BannerKingsViewModel
    {
        private MBBindingList<InformationElement> defenseInfo;
        private MBBindingList<InformationElement> manpowerInfo;
        private MBBindingList<InformationElement> siegeInfo;
        private SelectorVM<MilitiaItemVM> militiaSelector;
        private SelectorVM<BKItemVM> garrisonSelector;
        private PopulationOptionVM _conscriptionToogle;
        private PopulationOptionVM _subsidizeMilitiaToogle;
        private BKGarrisonPolicy garrisonItem;
        private Settlement settlement;

        public MilitaryVM(PopulationData data, Settlement _settlement, bool selected) : base(data, selected)
        {
            defenseInfo = new MBBindingList<InformationElement>();
            manpowerInfo = new MBBindingList<InformationElement>();
            siegeInfo = new MBBindingList<InformationElement>();
            this.settlement = _settlement;
            this.RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            DefenseInfo.Clear();
            ManpowerInfo.Clear();
            SiegeInfo.Clear();
            DefenseInfo.Add(new InformationElement("Militia Cap:", new BKMilitiaModel().GetMilitiaLimit(data, settlement.IsCastle).ToString(),
                "The maximum number of militiamen this settlement can support, based on it's population"));
            DefenseInfo.Add(new InformationElement("Militia Quality:", FormatValue(new BKMilitiaModel().CalculateEliteMilitiaSpawnChance(settlement)),
                    "Chance of militiamen being spawned as veterans instead of recruits"));

            ManpowerInfo.Add(new InformationElement("Manpower:", base.data.MilitaryData.Manpower.ToString(),
                    "Manpower"));
            ManpowerInfo.Add(new InformationElement("Noble Manpower:", base.data.MilitaryData.NobleManpower.ToString(),
                   "Manpower"));
            ManpowerInfo.Add(new InformationElement("Peasant Manpower:", base.data.MilitaryData.PeasantManpower.ToString(),
                   "Manpower"));
            ManpowerInfo.Add(new InformationElement("Militarism:", base.FormatValue(base.data.MilitaryData.Militarism.ResultNumber),
                   "Manpower"));
            ManpowerInfo.Add(new InformationElement("Draft Efficiency:", base.FormatValue(base.data.MilitaryData.DraftEfficiency.ResultNumber),
                   "Manpower"));

            SiegeInfo.Add(new InformationElement("Storage Limit:", settlement.Town.FoodStocksUpperLimit().ToString(),
                    "The amount of food this settlement is capable of storing"));
            SiegeInfo.Add(new InformationElement("Estimated Holdout:", string.Format("{0} Days", base.data.MilitaryData.Holdout),
                "How long this settlement will take to start starving in case of a siege"));

            StringBuilder sb = new StringBuilder();
            sb.Append(base.data.MilitaryData.Catapultae);
            sb.Append(" ,");
            sb.Append(base.data.MilitaryData.Catapultae);
            sb.Append(" ,");
            sb.Append(base.data.MilitaryData.Trebuchets);
            sb.Append(" (Ballis., Catap., Treb.)");
            SiegeInfo.Add(new InformationElement("Engines:",  sb.ToString(),
                "How long this settlement will take to start starving in case of a siege"));

            int militiaIndex = 0;
            MilitiaPolicy militiaPolicy = BannerKingsConfig.Instance.PolicyManager.GetMilitiaPolicy(settlement);
            if (militiaPolicy == MilitiaPolicy.Melee)
                militiaIndex = 1;
            else if (militiaPolicy == MilitiaPolicy.Ranged)
                militiaIndex = 2;
            MilitiaSelector = new SelectorVM<MilitiaItemVM>(0, new Action<SelectorVM<MilitiaItemVM>>(this.OnMilitiaChange));
            MilitiaSelector.SetOnChangeAction(null);
            foreach (MilitiaPolicy policy in militiaPolicies)
            {

                MilitiaItemVM item = new MilitiaItemVM(policy, true);
                MilitiaSelector.AddItem(item);
            }
            MilitiaSelector.SetOnChangeAction(OnMilitiaChange);
            MilitiaSelector.SelectedIndex = militiaIndex;

            GarrisonPolicy policyElement = BannerKingsConfig.Instance.PolicyManager.GetGarrisonPolicy(settlement);
            garrisonItem = new BKGarrisonPolicy(policyElement, settlement);
            GarrisonSelector = base.GetSelector(garrisonItem, new Action<SelectorVM<BKItemVM>>(this.garrisonItem.OnChange));


            List<DecisionsElement> elements = BannerKingsConfig.Instance.PolicyManager.GetDefaultDecisions(settlement);
            foreach (DecisionsElement policy in elements)
            {
                PopulationOptionVM vm = new PopulationOptionVM()
                .SetAsBooleanOption(policy.description, policy.isChecked, delegate (bool value)
                {
                    BannerKingsConfig.Instance.PolicyManager.UpdatePolicy(settlement, policy.type, value);
                    this.RefreshValues();

                }, new TextObject(policy.hint));
                switch (policy.type)
                {
                    case PolicyType.CONSCRIPTION:
                        ConscriptionToogle = vm;
                        break;
                    case PolicyType.SUBSIDIZE_MILITIA:
                        SubsidizeToogle = vm;
                        break;
                }
            }
        }

        private void OnMilitiaChange(SelectorVM<MilitiaItemVM> obj)
        {
            if (obj.SelectedItem != null)
            {
                MilitiaItemVM selectedItem = obj.SelectedItem;
                BannerKingsConfig.Instance.PolicyManager.UpdateMilitiaPolicy(settlement, selectedItem.policy);
            }
        }

        private IEnumerable<MilitiaPolicy> militiaPolicies
        {
            get
            {
                yield return MilitiaPolicy.Balanced;
                yield return MilitiaPolicy.Melee;
                yield return MilitiaPolicy.Ranged;
                yield break;
            }
        }

        private IEnumerable<GarrisonPolicy> garrisonPolicies
        {
            get
            {
                yield return GarrisonPolicy.Standard;
                yield return GarrisonPolicy.Enlist_Locals;
                yield return GarrisonPolicy.Enlist_Mercenaries;
                yield break;
            }
        }

        [DataSourceProperty]
        public SelectorVM<BKItemVM> GarrisonSelector
        {
            get
            {
                return this.garrisonSelector;
            }
            set
            {
                if (value != this.garrisonSelector)
                {
                    this.garrisonSelector = value;
                    base.OnPropertyChangedWithValue(value, "GarrisonSelector");
                }
            }
        }

        [DataSourceProperty]
        public SelectorVM<MilitiaItemVM> MilitiaSelector
        {
            get
            {
                return this.militiaSelector;
            }
            set
            {
                if (value != this.militiaSelector)
                {
                    this.militiaSelector = value;
                    base.OnPropertyChangedWithValue(value, "MilitiaSelector");
                }
            }
        }

        [DataSourceProperty]
        public PopulationOptionVM SubsidizeToogle
        {
            get => _subsidizeMilitiaToogle;
            set
            {
                if (value != _subsidizeMilitiaToogle)
                {
                    _subsidizeMilitiaToogle = value;
                    base.OnPropertyChangedWithValue(value, "SubsidizeToogle");
                }
            }
        }

        [DataSourceProperty]
        public PopulationOptionVM ConscriptionToogle
        {
            get => _conscriptionToogle;
            set
            {
                if (value != _conscriptionToogle)
                {
                    _conscriptionToogle = value;
                    base.OnPropertyChangedWithValue(value, "ConscriptionToogle");
                }
            }
        }


        [DataSourceProperty]
        public MBBindingList<InformationElement> DefenseInfo
        {
            get => defenseInfo;
            set
            {
                if (value != defenseInfo)
                {
                    defenseInfo = value;
                    base.OnPropertyChangedWithValue(value, "DefenseInfo");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<InformationElement> ManpowerInfo
        {
            get => manpowerInfo;
            set
            {
                if (value != manpowerInfo)
                {
                    manpowerInfo = value;
                    base.OnPropertyChangedWithValue(value, "ManpowerInfo");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<InformationElement> SiegeInfo
        {
            get => siegeInfo;
            set
            {
                if (value != siegeInfo)
                {
                    siegeInfo = value;
                    base.OnPropertyChangedWithValue(value, "SiegeInfo");
                }
            }
        }
    }
}
