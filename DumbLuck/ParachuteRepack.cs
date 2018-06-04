using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace DumbLuck
{
    public class DumbLuckParachuteRepack : PartModule
    {
        ModuleParachute parachute;
        BaseEvent EVARepairEvent;

        Random _rnd;
        public string EVAKerbalName;
        ProtoCrewMember pcmCached;

        public ProtoCrewMember PCM
        {
            get
            {
                if (pcmCached != null) return pcmCached;
                foreach (ProtoCrewMember pcm in HighLogic.fetch.currentGame.CrewRoster.Crew)
                    if (pcm.name == EVAKerbalName) return pcmCached = pcm;
                return null;
            }
            set
            {
                EVAKerbalName = value.name;
                pcmCached = value;
            }
        }

        /// <summary>
        /// Minimum skill level needed to maintain or repair the part.
        /// </summary>
        [KSPField()]
        public int minimumSkillLevel = 1;

        /// <summary>
        /// Skill required to perform a part repair. 
        /// </summary>
        [KSPField()]
        public string requiredSkill = "RepairSkill";

        /// <summary>
        /// Specifies what percentage chance of the repair going catastofically wrong.
        /// </summary>
        [KSPField()]
        public double repairFailurePercent = 0.05d;  //  1%

        [KSPField()]
        public double nonEngineeraddonPercent = 0.05d;  //  +5%

        [KSPField()]
        public double lowLeveladdonPercent = 0.05d;  //  +5% each level

        [KSPField()]
        public double catastrophicFailureLevel = 0.09d;

        [KSPEvent(guiName = "Repair Part", active = false, guiActiveUnfocused = true, externalToEVAOnly = true, unfocusedRange = 4f)]
        public void EVARepackChute()
        {
            if (this.parachute.deploymentState == ModuleParachute.deploymentStates.CUT)
            {
                _rnd = new Random();
                double adjustodds = new double();

                double currentrandom = _rnd.NextDouble();
                KerbalEVA kerbal = FlightGlobals.ActiveVessel.evaController;
                EVAKerbalName = kerbal.name.Substring(kerbal.name.IndexOf("(") + 1, (kerbal.name.IndexOf(")") - kerbal.name.IndexOf("(")) - 1);

                ProtoCrewMember pcm = PCM;
                double dumbstat = pcm.stupidity;
                double starLevel = pcm.experienceLevel;

                if (pcm.GetEffect(requiredSkill) != null) {
                    print("[Dumb Luck] Correct skill: " + requiredSkill.ToString()); }
                else {
                    print("[Dumb Luck] Wrong Kerbal (" + pcm.experienceTrait.Title.ToString() + ") attempting to repack chute. Adjusting odds by additional " + nonEngineeraddonPercent.ToString("F2"));
                    adjustodds += nonEngineeraddonPercent;
                } // adjusting chance of failure for wrong Kerbal

                if (starLevel < minimumSkillLevel)
                {
                    print("[Dumb Luck] Inexperienced Kerbal (" + starLevel.ToString("F0") + ") attempting to repack chute. Adjusting odds by additional " + (minimumSkillLevel - starLevel).ToString("F0") + "*" + lowLeveladdonPercent.ToString("F2"));
                    adjustodds += (minimumSkillLevel - starLevel) * lowLeveladdonPercent;
                } // adjusting chance of failure for low level Kerbal


                print("[Dumb Luck] <" + (repairFailurePercent + adjustodds).ToString("F2") + " is a failure. This attempt: " + currentrandom.ToString("F3"));

                if ((currentrandom - adjustodds) <= repairFailurePercent)
                {
                    currentrandom = _rnd.NextDouble();
                    print("[Dumb Luck] > " + catastrophicFailureLevel.ToString("F2") + " is catastophic. rnd(" + currentrandom.ToString("F3") + ") dumb(" + dumbstat.ToString("F3") + ") lvl(" + starLevel.ToString("F0") + ") This Attempt: " + ((dumbstat / (starLevel + 1)) * currentrandom).ToString("F3"));
                    if ((dumbstat / (starLevel + 1)) * currentrandom >= catastrophicFailureLevel)
                    {
                        print("[Dumb Luck] Boom. Repack failed");
                        this.part.explosionPotential = 0;
                        this.part.explode();
                        return;
                    }
                }

                pcm = null;
            }
            this.parachute.Repack();
            if (this.parachute.deploymentState == ModuleParachute.deploymentStates.STOWED)
            {
                print("[Dumb Luck] Repack Success");
                this.Events["EVARepackChute"].active = false;
            }

        }


        private void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && this.part != null && this.part.Modules != null && EVARepairEvent != null)
            {
                if (EVARepairEvent.active)
                {
                    EVARepairEvent.active = false; //Disable Stock Repack Chute Event button in PAW
                    this.Events["EVARepackChute"].active = true;
                    this.Events["EVARepackChute"].guiName = EVARepairEvent.guiName;// + "...";
                }
            }
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight && this.part != null && this.part.Modules != null)
            {
                this.parachute = part.Modules.OfType<ModuleParachute>().First();
                foreach (BaseEvent e in this.parachute.Events)
                {
                    if (e.name == "Repack")
                        EVARepairEvent = e;
                }

            }
        }
        public void OnDestroy()
        {
            this.parachute = null;
            this.EVARepairEvent = null;
        }
    }
}
