﻿using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace BannerKings.Managers.Decisions
{
    public class BKSubsidizeMilitiaDecision : BKSettlementDecision
    {
        public BKSubsidizeMilitiaDecision(Settlement settlement, bool enabled) : base(settlement, enabled)
        {
        }

        public override string GetHint()
        { return new TextObject("{=!}Improve militia quality by subsidizing their equipment and training").ToString();
        }

        public override string GetIdentifier()
        {
            return "decision_militia_subsidize";
        }

        public override string GetName()
        {
            return new TextObject("{=!}Subsidize the militia").ToString();
        }
    }
}