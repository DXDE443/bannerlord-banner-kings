﻿using BannerKings.Managers.Institutions.Religions;
using BannerKings.Managers.Institutions.Religions.Faiths;
using BannerKings.Managers.Institutions.Religions.Leaderships;
using BannerKings.Models;
using BannerKings.Models.BKModels;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace BannerKings.Managers
{
    public class ReligionsManager
    {
        private Dictionary<Religion, Dictionary<Hero, float>> Religions { get; set; }

        public ReligionsManager()
        {
            this.Religions = new Dictionary<Religion, Dictionary<Hero, float>>();
            InitializeReligions();
        }

        public void InitializeReligions()
        {
            CultureObject aserai = Utils.Helpers.GetCulture("aserai");
            CultureObject khuzait = Utils.Helpers.GetCulture("khuzait");
            CultureObject imperial = Utils.Helpers.GetCulture("imperial");
            CultureObject battania = Utils.Helpers.GetCulture("battania");

            Religion aseraiReligion = new Religion(Settlement.All.First(x => x.StringId == "town_A1"), 
                DefaultFaiths.Instance.AseraCode, new KinshipLeadership(),
                new List<CultureObject> { aserai, khuzait, imperial },
                new List<string>());

            Religion battaniaReligion = new Religion(null,
                DefaultFaiths.Instance.AmraOllahm, new AutonomousLeadership(),
                new List<CultureObject> { battania },
                new List<string>() { "druidism", "animism" });

            Religions.Add(aseraiReligion, new Dictionary<Hero, float>());
            Religions.Add(battaniaReligion, new Dictionary<Hero, float>());
            InitializeFaithfulHeroes(aseraiReligion, aserai);
            InitializeFaithfulHeroes(battaniaReligion, battania);
        }

        public void InitializeFaithfulHeroes(Religion rel, CultureObject culture)
        {
            foreach (Hero hero in Hero.AllAliveHeroes)
                if (!hero.IsDisabled && (hero.IsNoble || hero.IsNotable || hero.IsWanderer) && hero.Culture == culture
                    && !hero.IsChild)
                    Religions[rel].Add(hero, 50f);
        }

        public void InitializePresets()
        {
            foreach (Religion rel in Religions.Keys.ToList())
            {
                string id = rel.Faith.GetId();
                List<CharacterObject> presets = CharacterObject.All.ToList().FindAll(x => x.Occupation == Occupation.Preacher
                && x.Culture == rel.MainCulture && x.IsTemplate && x.StringId.Contains("bannerkings") && x.StringId.Contains(id));
                foreach (CharacterObject preset in presets)
                {
                    int number = int.Parse(preset.StringId[preset.StringId.Length - 1].ToString());
                    rel.Faith.AddPreset(number, preset);
                }
            }
        }

        public List<Religion> GetReligions()
        {
            List<Religion> religions = new List<Religion>();
            foreach (Religion rel in Religions.Keys)
                religions.Add(rel);

            return religions;
        }

        public Religion GetHeroReligion(Hero hero) => Religions.FirstOrDefault(pair => pair.Value.ContainsKey(hero)).Key;

        public List<Hero> GetFaithfulHeroes(Religion religion)
        {
            List<Hero> heroes = new List<Hero>();
            if (Religions.ContainsKey(religion))
                foreach (Hero hero in Religions[religion].Keys.ToList())
                    heroes.Add(hero);

            return heroes;
        }

        public Religion GetIdealReligion(CultureObject culture)
        {
            foreach (Religion rel in Religions.Keys.ToList())
                if (rel.MainCulture == culture)
                    return rel;

            return null;
        }

        public bool IsReligionMember(Hero hero, Religion religion)
        {
            if (Religions.ContainsKey(religion))
                if (Religions[religion].ContainsKey(hero))
                    return true;
            return false;
        }

        public void AddPiety(Religion rel, Hero hero, float piety)
        {
            if (rel == null || hero == null) return;
            if (Religions[rel].ContainsKey(hero))
                Religions[rel][hero] += piety;
        }

        public float GetPiety(Religion rel, Hero hero)
        {
            if (rel == null || hero == null) return 0f;
            float piety = 0f;
            if (Religions[rel].ContainsKey(hero))
                piety = Religions[rel][hero];

            return MBMath.ClampFloat(piety, -1000f, 1000f);
        }

        public bool IsPreacher(Hero hero)
        {
            foreach (Religion rel in Religions.Keys.ToList())
                foreach (Clergyman clergy in rel.Clergy.Values.ToList())
                    if (clergy.Hero == hero)
                        return true;

            return false;
        }

        public Clergyman GetClergymanFromHeroHero(Hero hero)
        {
            foreach (Religion rel in this.Religions.Keys.ToList())
                foreach (Clergyman clergy in rel.Clergy.Values.ToList())
                    if (clergy.Hero == hero)
                        return clergy;

            return null;
        }

        public Religion GetClergymanReligion(Clergyman clergyman)
        {
            foreach (Religion rel in this.Religions.Keys.ToList())
                foreach (Clergyman clergy in rel.Clergy.Values.ToList())
                    if (clergy == clergyman)
                        return rel;
            return null;
        }
    }
}
