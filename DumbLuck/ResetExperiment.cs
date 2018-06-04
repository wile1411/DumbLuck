using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumbLuck
{
    public class DumbLuckExperimentReset : PartModule
    {
        ModuleScienceExperiment experiment;
        BaseEvent CleanUpEvent;
        BaseEvent ResetEvent;
        Random _rnd;
        public string EVAKerbalName;
        public string experiementname;
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
        /// Skill required to perform an Experiment Reset/Clean. 
        /// </summary>
        [KSPField()]
        public string requiredSkill = "ScienceResetSkill";

        /// <summary>
        /// Specifies what percentage chance of the Reset going catastofically wrong.
        /// Two random numbers generated for checks
        ///  - First Number is to set how often a potential issue occurs
        ///  - Second Number is used in conjuction with Dumb and Skill Level to check for failure
        /// </summary>
        [KSPField()]
        public double repairFailurePercent = 0.85d;  //  5% chance for potential issue

        [KSPField()]
        public double nonScientistaddonPercent = 0.05d;  //  +5% if not correct Trait

        [KSPField()]
        public double catastrophicFailureLevel = 0.02d;

        [KSPEvent(guiName = "Reset Experiement", active = false, guiActiveUnfocused = true, externalToEVAOnly = true, unfocusedRange = 2f)]
        public void EVAResetExperiement()
        {
            if (this.experiment.resettableOnEVA)
            {
                _rnd = new Random();
                double adjustodds = new double();

                double currentrandom = _rnd.NextDouble();
                KerbalEVA kerbal = FlightGlobals.ActiveVessel.evaController;
                EVAKerbalName = kerbal.name.Substring(kerbal.name.IndexOf("(") + 1, (kerbal.name.IndexOf(")") - kerbal.name.IndexOf("(")) - 1);

                ProtoCrewMember pcm = PCM;
                double dumbstat = pcm.stupidity;
                double starLevel = pcm.experienceLevel;

                //print("EVAKerbalName: " + EVAKerbalName.ToString());
                //print("setting dumb: " + dumbstat.ToString());
                //print("setting lvl: " + starLevel.ToString());
                //print("Trait: " + pcm.experienceTrait.Title.ToString());
                if (pcm.GetEffect(requiredSkill) != null)
                {
                }
                else {
                    //print("[Dumb Luck] Wrong Kerbal (" + pcm.experienceTrait.Title.ToString() + ") attempting to reset " + experiementname + ". Adjusting odds by additional " + nonScientistaddonPercent.ToString("F2"));
                    adjustodds += nonScientistaddonPercent;
                } // adjusting chance of failure for wrong Kerbal

                print("[Dumb Luck] <" + (repairFailurePercent + adjustodds).ToString("F2") + " is a failure. This attempt: " + currentrandom.ToString("F3"));

                if ((currentrandom - adjustodds) <= repairFailurePercent)
                {
                    currentrandom = _rnd.NextDouble();
                    print("[Dumb Luck] > " + catastrophicFailureLevel.ToString("F2") + " is catastophic. rnd(" + currentrandom.ToString("F3") + ") dumb(" + dumbstat.ToString("F3") + ") lvl(" + starLevel.ToString("F0") + ") This Attempt: " + ((dumbstat / (starLevel + 1)) * currentrandom).ToString("F3"));
                    if ((dumbstat / (starLevel + 1)) * currentrandom >= catastrophicFailureLevel)
                    {
                        print("[Dumb Luck] Boom. Reset failed");
                        this.part.explosionPotential = 0;
                        this.part.explode();
                        return;
                    }
                }

                pcm = null;
            
                this.Events["EVAResetExperiement"].active = false;
                if (this.experiment.GetScienceCount() > 0)
                    { this.experiment.ResetExperimentExternal(); }
                else
                    { this.experiment.CleanUpExperimentExternal(); }
            }
        }

        private void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && this.part != null && this.part.Modules != null && ResetEvent != null)
            {
                if (ResetEvent.active)
                {
                    ResetEvent.active = false; //Disable Stock Reset Experiment Event button in PAW
                    this.Events["EVAResetExperiement"].active = true;
                    this.Events["EVAResetExperiement"].guiName = ResetEvent.guiName;// + "...";
                }
                if (CleanUpEvent.active)
                {
                    CleanUpEvent.active = false; //Disable Stock Clean Experiment Event button in PAW
                    this.Events["EVAResetExperiement"].active = true;
                    this.Events["EVAResetExperiement"].guiName = CleanUpEvent.guiName;// + "...";
                }
                else if (this.experiment.GetScienceCount() == 0 && !this.experiment.Inoperable && this.Events["EVAResetExperiement"].active)
                {
                    this.Events["EVAResetExperiement"].active = false;
                }
            }
        }

        public void OnVesselModified(Vessel v)
        {
            if (HighLogic.LoadedSceneIsFlight && this.part != null && this.part.Modules != null)
            {
                this.experiment = part.Modules.OfType<ModuleScienceExperiment>().First();
                experiementname = this.experiment.name;

                foreach (BaseEvent e in this.experiment.Events)
                {
                    if (e.name == "ResetExperimentExternal")
                        ResetEvent = e;
                    if (e.name == "CleanUpExperimentExternal")
                        CleanUpEvent = e;
                }

            }
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight && this.part != null && this.part.Modules != null)
            {
                this.experiment = part.Modules.OfType<ModuleScienceExperiment>().First();
                experiementname = this.experiment.name;

                foreach (BaseEvent e in this.experiment.Events)
                {
                    if (e.name == "ResetExperimentExternal")
                        ResetEvent = e;
                    if (e.name == "CleanUpExperimentExternal")
                        CleanUpEvent = e;
                }

            }
        }


        public void OnDestroy()
        {
            this.experiment = null;
            this.CleanUpEvent = null;
            this.ResetEvent = null;
        }
    }
}
