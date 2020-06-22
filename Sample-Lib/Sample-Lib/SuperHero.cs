using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleLib.SuperHeroes
{
    public enum UniverseType
    {
        DC = 0,
        Marvel = 1
    }

    public enum SuperPower
    {
        Fly,
        RunFast,
        LaserEyes,
        Teleportatioin,
        SuperStrength,
        LooksGreatInTights,
        EatLead, 
        Invisibility
    }

    public enum Alignment
    {
        Neutral,
        Good,
        Evil,
        Confused
    }

    public class SuperHero
    {
        public string SuperHeroName { get; set; }
        public string Alias { get; set; }
        public string PlanetofOrigin { get; set; }
        public List<SuperPower> SuperPowers { get; set; }
        public int SocialSecurityNumber { get; set; }
        public UniverseType Universe { get; set; }
        public SuperHero Nemesis { get; set; }
        public Alignment alighnment { get; set; }
        public int NumberofChickensInBackyard { get; set; }
        public long BadDeedsCommitted { get; set; }
        public int Age { get; set; }
        public int NumberofChickens { get; set; }

        public SuperHero(string superheroname, string alias, string planetoforigin, int socialsecuritynumber, UniverseType universe)
        {
            SuperHeroName = superheroname;
            PlanetofOrigin = planetoforigin;
            SocialSecurityNumber = socialsecuritynumber;
            Alias = alias;
            Universe = universe;
        }
    }
}
