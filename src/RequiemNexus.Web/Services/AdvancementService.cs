using System;
using RequiemNexus.Data.Models;
using RequiemNexus.Data;

namespace RequiemNexus.Web.Services
{
    public class AdvancementService
    {
        private readonly ApplicationDbContext _dbContext;

        public AdvancementService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool TryUpgradeAttribute(Character character, string traitName, int currentRating, int newRating)
        {
            if (newRating <= currentRating || newRating > 5) return false;
            
            // Cost is New Dots x 4
            int totalCost = 0;
            for (int i = currentRating + 1; i <= newRating; i++)
            {
                totalCost += i * 4;
            }

            if (character.ExperiencePoints >= totalCost)
            {
                character.ExperiencePoints -= totalCost;
                UpdateAttribute(character, traitName, newRating);
                return true;
            }

            return false;
        }

        public bool TryUpgradeSkill(Character character, string traitName, int currentRating, int newRating)
        {
            if (newRating <= currentRating || newRating > 5) return false;
            
            // Cost is New Dots x 2
            int totalCost = 0;
            for (int i = currentRating + 1; i <= newRating; i++)
            {
                totalCost += i * 2;
            }

            if (character.ExperiencePoints >= totalCost)
            {
                character.ExperiencePoints -= totalCost;
                UpdateSkill(character, traitName, newRating);
                return true;
            }

            return false;
        }

        public bool TryUpgradeCoreTrait(Character character, string traitName, int currentRating, int newRating)
        {
            string[] attributes = { "Intelligence", "Wits", "Resolve", "Strength", "Dexterity", "Stamina", "Presence", "Manipulation", "Composure" };
            if (Array.Exists(attributes, a => a == traitName))
                return TryUpgradeAttribute(character, traitName, currentRating, newRating);
            else
                return TryUpgradeSkill(character, traitName, currentRating, newRating);
        }

        public void UpdateCoreTrait(Character character, string traitName, int newRating)
        {
            string[] attributes = { "Intelligence", "Wits", "Resolve", "Strength", "Dexterity", "Stamina", "Presence", "Manipulation", "Composure" };
            if (Array.Exists(attributes, a => a == traitName))
                UpdateAttribute(character, traitName, newRating);
            else
                UpdateSkill(character, traitName, newRating);
        }

        private void UpdateAttribute(Character character, string traitName, int newRating)
        {
            switch (traitName)
            {
                case "Intelligence": character.Intelligence = newRating; break;
                case "Wits": character.Wits = newRating; break;
                case "Resolve": character.Resolve = newRating; break;
                case "Strength": character.Strength = newRating; break;
                case "Dexterity": character.Dexterity = newRating; break;
                case "Stamina": character.Stamina = newRating; break;
                case "Presence": character.Presence = newRating; break;
                case "Manipulation": character.Manipulation = newRating; break;
                case "Composure": character.Composure = newRating; break;
            }
        }

        private void UpdateSkill(Character character, string traitName, int newRating)
        {
            switch (traitName)
            {
                // Mental
                case "Academics": character.Academics = newRating; break;
                case "Computer": character.Computer = newRating; break;
                case "Crafts": character.Crafts = newRating; break;
                case "Investigation": character.Investigation = newRating; break;
                case "Medicine": character.Medicine = newRating; break;
                case "Occult": character.Occult = newRating; break;
                case "Politics": character.Politics = newRating; break;
                case "Science": character.Science = newRating; break;
                
                // Physical
                case "Athletics": character.Athletics = newRating; break;
                case "Brawl": character.Brawl = newRating; break;
                case "Drive": character.Drive = newRating; break;
                case "Firearms": character.Firearms = newRating; break;
                case "Larceny": character.Larceny = newRating; break;
                case "Stealth": character.Stealth = newRating; break;
                case "Survival": character.Survival = newRating; break;
                case "Weaponry": character.Weaponry = newRating; break;
                
                // Social
                case "Animal Ken": character.AnimalKen = newRating; break;
                case "Empathy": character.Empathy = newRating; break;
                case "Expression": character.Expression = newRating; break;
                case "Intimidation": character.Intimidation = newRating; break;
                case "Persuasion": character.Persuasion = newRating; break;
                case "Socialize": character.Socialize = newRating; break;
                case "Streetwise": character.Streetwise = newRating; break;
                case "Subterfuge": character.Subterfuge = newRating; break;
            }
        }
    }
}
