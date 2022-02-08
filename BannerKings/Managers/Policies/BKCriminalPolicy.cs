﻿using BannerKings.UI.Items;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection;
using static BannerKings.Managers.PolicyManager;

namespace BannerKings.Managers.Policies
{
    class BKCriminalPolicy : BannerKingsPolicy
    {

        CriminalPolicy policy;
        public BKCriminalPolicy(CriminalPolicy policy, Settlement settlement) : base(settlement, (int)policy, "criminal")
        {
            this.policy = policy;
        }
        public override string GetHint()
        {
            if (policy == CriminalPolicy.Execution)
                return "";
            else if (policy == CriminalPolicy.Forgiveness)
                return "";
            else return "";
        }

        public override void OnChange(SelectorVM<BKItemVM> obj)
        {
            if (obj.SelectedItem != null)
            {
                BKItemVM vm = obj.GetCurrentItem();
                this.policy = (CriminalPolicy)vm.value;
                BannerKingsConfig.Instance.PolicyManager.UpdateSettlementPolicy(settlement, this);
            }
        }

        public enum CriminalPolicy
        {
            Enslavement,
            Execution,
            Forgiveness
        }

        public override IEnumerable<Enum> GetPolicies()
        {
            yield return MilitiaPolicy.Balanced;
            yield return MilitiaPolicy.Melee;
            yield return MilitiaPolicy.Ranged;
            yield break;
        }
    }
}
