using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace DumbLuck
{
    public class DumbLuckWheelRepair : PartModule
    {
        ModuleWheels.ModuleWheelDamage wheelDamage;
        ModuleWheelBase wheelBase;
        BaseEvent EVARepairEvent;

        Random _rnd;
        public string EVAKerbalName;
        public string parttype;
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
        public int minimumSkillLevel = 3;

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
        public void EVARepairWheel()
        {
            if (this.wheelDamage.isRepairable)
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
                    print("[Dumb Luck] Wrong Kerbal (" + pcm.experienceTrait.Title.ToString() + ") attempting to repair " + parttype + ". Adjusting odds by additional " + nonEngineeraddonPercent.ToString("F2"));
                    adjustodds += nonEngineeraddonPercent;
                } // adjusting chance of failure for wrong Kerbal

                if (starLevel < minimumSkillLevel)
                {
                    print("[Dumb Luck] Inexperienced Kerbal (" + starLevel.ToString("F0") + ") attempting to repair " + parttype + ". Adjusting odds by additional " + (minimumSkillLevel - starLevel).ToString("F0") + "*" + lowLeveladdonPercent.ToString("F2"));
                    adjustodds += (minimumSkillLevel - starLevel) * lowLeveladdonPercent;
                } // adjusting chance of failure for low level Kerbal


                print("[Dumb Luck] <" + (repairFailurePercent + adjustodds).ToString("F2") + " is a failure. This attempt: " + currentrandom.ToString("F3"));

                if ((currentrandom - adjustodds) <= repairFailurePercent)
                {
                    currentrandom = _rnd.NextDouble();
                    print("[Dumb Luck] > " + catastrophicFailureLevel.ToString("F2") + " is catastophic. rnd(" + currentrandom.ToString("F3") + ") dumb(" + dumbstat.ToString("F3") + ") lvl(" + starLevel.ToString("F0") + ") This Attempt: " + ((dumbstat / (starLevel + 1)) * currentrandom).ToString("F3"));
                    if ((dumbstat / (starLevel + 1)) * currentrandom >= catastrophicFailureLevel)
                    {
                        print("[Dumb Luck] Boom. Repair failed");
                        this.part.explosionPotential = 0;
                        this.part.explode();
                        return;
                    }
                }

                pcm = null;
            }
            this.wheelDamage.EventRepairExternal();
            if (!this.wheelDamage.isDamaged)
            {
                print("[Dumb Luck] Repair Success");
                this.Events["EVARepairWheel"].active = false;
            }

        }


        private void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && this.part != null && this.part.Modules != null && EVARepairEvent != null)
            { 
                if (EVARepairEvent.active || this.wheelDamage.isDamaged)
                {
                    EVARepairEvent.active = false; //Disable Stock Repair Wheel/Leg Event

                    this.Events["EVARepairWheel"].active = true;
                    this.Events["EVARepairWheel"].guiName = EVARepairEvent.guiName;// + "...";
                    if (parttype == "leg")
                        this.Events["EVARepairWheel"].guiName = "Repair Leg";// + "...";
                }
            }
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight && this.part != null && this.part.Modules != null)
            {
                this.wheelDamage = part.Modules.OfType<ModuleWheels.ModuleWheelDamage>().First();

                foreach (BaseEvent e in this.wheelDamage.Events)
                {
                    if (e.name == "EventRepairExternal")
                        EVARepairEvent = e;
                }

                this.wheelBase = part.Modules.OfType<ModuleWheelBase>().First();

                if (this.wheelBase.wheelType == WheelType.LEG)
                {
                    parttype = "leg";
                }
                else
                {
                    parttype = "wheel";
                }
            }
        }
        public void OnDestroy()
        {
            this.wheelDamage = null;
            this.wheelBase = null;
            this.EVARepairEvent = null;
        }
    }
}
