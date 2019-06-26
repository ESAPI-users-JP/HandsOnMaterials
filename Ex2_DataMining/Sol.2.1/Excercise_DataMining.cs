using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.IO;
using System.Windows;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]

namespace Excercise_DataMining
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                using (VMS.TPS.Common.Model.API.Application app = VMS.TPS.Common.Model.API.Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }

        static void Execute(VMS.TPS.Common.Model.API.Application app)
        {
            // Open the report file
            string temp = System.Environment.GetEnvironmentVariable("TEMP");
            string dataFilePath = temp + @"\DataMiningOutput.txt";

            PrintStartupMsg(dataFilePath);
            using (StreamWriter reportFile = new StreamWriter(dataFilePath, false))
            {
                // Iterate over all patients in the database.
                foreach (PatientSummary patsum in app.PatientSummaries)
                {
                    // Check if the user has hit a key.
                    if (StopNow())
                        break;
                    // Open a patient, report on the treatment history for that patient, and close the patient.
                    // If there is no treatment history, nothing is reported.
                    Patient pat = app.OpenPatient(patsum);
                    ReportOnePatient(pat, reportFile);
                    app.ClosePatient();
                }
                reportFile.Flush();
            }
            MessageBox.Show("the results to the simple report '" + dataFilePath + "'.");
            // Open the folder with explore.exe.
            System.Diagnostics.Process.Start(temp);

        }
        /// <summary>
        /// ReportOnePatient
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="reportFile"></param>
        static void ReportOnePatient(Patient patient, StreamWriter reportFile)
        {
            if (patient == null)
                return;
            Console.WriteLine("Processing patient " + patient.Id);

            string[] targetVolumeID = { "PTV-PROST", "PTV" };

            var matchPlans = from Course c in patient.Courses
                             from PlanSetup ps in c.PlanSetups
                             where (ps.StructureSet != null && ps.Dose != null && ps.NumberOfFractions.HasValue

                             // Add filter or comment / uncomment Lines.////////////////////////////////////////////////////////////////
                             && ps.DosePerFraction.Dose == 2 //filter by dose per fraction.
                             && ps.NumberOfFractions.Value == 38 // filtered by number of fractions.
                             && c.Id == "C1"    // filtered by course ID.
                             //&& ps.Id == "1-1"  // filtered by plan ID.
                             && targetVolumeID.Contains(ps.TargetVolumeID) // filtered by TargetVolumeID.
                             //&& ps.ApprovalStatus == PlanSetupApprovalStatus.TreatmentApproved  //filtered by only treatmentApproved plan.
                             //&& ps.IsTreated // filtered by treated plan.
                             ////////////////////////////////////////////////////////////////////////////////////////////////////////////
                             )
                             select new
                             {
                                 Patient = patient,
                                 Course = c,
                                 Plan = ps
                             };

            // If there is no match plan, the next process is skipped.
            if (!matchPlans.Any())
                return;
            foreach (var p in matchPlans)
            {
                if (StopNow())
                    break;
                PlanSetup ps = p.Plan;
                Console.WriteLine("Processing plan: " + ps.Id + "/" + ps.Course.Id + "/" + ps.TargetVolumeID);
                ReportOnePlan(patient, ps, reportFile);
            }
        }
        /// <summary>
        /// ReportOnePlan
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="ps"></param>
        /// <param name="reportFile"></param>
        static void ReportOnePlan(Patient patient, PlanSetup ps, StreamWriter reportFile)
        {
            // Change types of presentation for dose values to absolute.
            ps.DoseValuePresentation = DoseValuePresentation.Absolute;

            // Initialize output text
            string msg = "";

            msg += patient.Id + "\t";  // Add patient ID to output text.
            msg += ps.Course.Id + "\t"; // Add course ID to output text.
            msg += ps.Id + "\t"; // Add plan ID to output text.


            // TODO : Add here the code /////////////////////////////////////////////////////////////

            // Get plan informations. ////////////////////////////////////////////////////////////////
            // Get plan normalization method.
            msg += ps.PlanNormalizationMethod + "\t";

            // Get calculation model & options.
            int countFieldX = 0;
            int countFieldE = 0;
            foreach (var b in ps.Beams)
            {
                if (!b.IsSetupField)
                {
                    if (b.EnergyModeDisplayName.IndexOf("X") >= 0)
                        countFieldX++;
                    if (b.EnergyModeDisplayName.IndexOf("E") >= 0)
                        countFieldE++;
                }
            }

            string CalculationModel = "";
            if (countFieldX > 0)
            {
                CalculationModel += "(" + ps.PhotonCalculationModel.ToString();
                foreach (KeyValuePair<string, string> kvp in ps.PhotonCalculationOptions)
                {
                    CalculationModel += "/" + kvp.Key + ":" + kvp.Value;
                }
                CalculationModel += ")";
            }
            if (countFieldE > 0)
            {
                CalculationModel += "(" + ps.ElectronCalculationModel.ToString();
                foreach (KeyValuePair<string, string> kvp in ps.ElectronCalculationOptions)
                {
                    CalculationModel += "/" + kvp.Key + ":" + kvp.Value;
                }
                CalculationModel += ")";
            }
            msg += CalculationModel + "\t";


            // Get field informations. ////////////////////////////////////////////////////////////////
            var energy_List = new List<string>();
            var mu_List = new List<string>();
            int nBeam = 0;
            // Iterate over all beams
            foreach (var b in ps.Beams)
            {
                if (!b.IsSetupField)//exclude SetupField.
                {
                    energy_List.Add(b.EnergyModeDisplayName); // beam energy.
                    mu_List.Add(string.Format("{0:f1}", b.Meterset.Value));  // Get MU at 1st decimal place .
                    nBeam++; //count number of beams.
                }
            }
            msg += "#beam:" + nBeam.ToString() + "\t"; //add number of beams to output text.
            msg += "Energy:" + string.Join("/", energy_List) + "\t"; // Combine all values with "/".
            msg += "MU:" + string.Join("/", mu_List) + "\t"; // Combine all values with "/".
            ///////////////////////////////////////////////////////////////////////////////////////////

            // Get DVH statistic informations. ///////////////////////////////////////////////////////////////////

            // calculate Max/Mean/Min dose.
            // Initialize variables. 
            DVHData dvhStat = null;
            Structure targetStructure = null;
            string[] s_targetIds = {
                "ITV", "PTV", "PTV-PROST", "PTV-SV", "GTV", "CTV",
                "BLADDER", "CORD", "BRAINSTEM", "LUNG", "LIVER", "KIDNEY", "BREAST", "OPTIC", "PAROTID", "SPINE", "RECTUM", "BOWEL", "BRAIN" };

            // Iterate over all structures.
            foreach (var structure in ps.StructureSet.Structures)
            {
                if (ps.Dose != null && ps.StructureSet != null)
                {
                    targetStructure = FindStructure(structure,s_targetIds);
                    if (targetStructure != null)
                    {
                        dvhStat = ps.GetDVHCumulativeData(targetStructure, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.1);
                        if (dvhStat != null)
                        {
                            string doseUnitString = dvhStat.MaxDose.UnitAsString;
                            msg += targetStructure.Id + "[Max/Mean/Min,"+doseUnitString + "]:" +
                                   dvhStat.MaxDose.Dose.ToString() + "/" +
                                   dvhStat.MeanDose.Dose.ToString() + "/" +
                                   dvhStat.MinDose.Dose.ToString() + "\t";
                        }
                    }
                }
            }


            // Calculate Dose Qualiy Parameters (VolumeAtDose,DoseAtVolume,DoseComplement,ComplementVolume).
            var DQPList = new Collection<DQP>();
            // define parameters
            DQPList.Add(new DQP
            {
                strctureName = "Prostate",
                DQPtype = DQPtype.Dose,
                DQPvalue = 95.0,
                InputUnit = IOUnit.Relative,
                OutputUnit = IOUnit.Absolute
            });
            DQPList.Add(new DQP
            {
                strctureName = "Rectum",
                DQPtype = DQPtype.Volume,
                DQPvalue = 70,
                InputUnit = IOUnit.Relative,
                OutputUnit = IOUnit.Relative
            });
            // calculate DQP.
            msg += calcDQP(ps, DQPList);
            ///////////////////////////////////////////////////////////////////////////////////////////

            reportFile.WriteLine(msg);
        }
        
        /// <summary>
        /// FindStructure
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        private static Structure FindStructure(Structure structure,string[] s_targetIds)
        {
            Structure targetStructure = null;
            foreach (string id in s_targetIds)
            {
                if (structure.Id == id)
                {
                    targetStructure = structure;
                }
            }
            return targetStructure;
        }

        static bool stopped = false;

        /// <summary>
        /// StopNow
        /// </summary>
        /// <returns></returns>
        static bool StopNow()
        {
            bool keyPressed = Console.KeyAvailable;
            if (keyPressed)
            {
                Console.ReadKey();
                Console.WriteLine("\n\nPress 'Y' and ENTER if you want to stop now");
                string line = Console.ReadLine();
                stopped = line.Contains('Y') || line.Contains('y');
            }
            return stopped;
        }
        /// <summary>
        /// PrintStartupMsg
        /// </summary>
        /// <param name="dataFile"></param>
        static void PrintStartupMsg(string dataFile)
        {
            Console.WriteLine("This data mining application queries the treatment history for each patient in the Aria database and writes");
            Console.WriteLine("the results to the simple report '" + dataFile + "'.");
            Console.WriteLine("Press any key to pause processing.");
            Console.WriteLine("============================================================================================================");
            System.Threading.Thread.Sleep(2000); // let user read message for 2 seconds.
        }

        /// <summary>
        /// calcDQP
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="DQPList"></param>
        /// <returns></returns>
        static string calcDQP(PlanSetup ps, Collection<DQP> DQPList)
        {
            // Initialize variables.
            DVHData dvhStat = null;
            Structure targetStructure = null;
            string oText = "";
            // Iterate over all DQP in list.
            foreach (var row in DQPList)
            {

                targetStructure = ps.StructureSet.Structures.Where(s => s.Id == row.strctureName).FirstOrDefault();
                
                // Execute if the structure name match.
                if (targetStructure != null)
                {
                    //get string of dose unit [Gy] or [cGy].
                    dvhStat = ps.GetDVHCumulativeData(targetStructure, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.1);
                    string doseUnitString = dvhStat.MaxDose.UnitAsString;

                    // define prefix DQP name.
                    if (row.DQPtype == DQPtype.Dose)
                    {
                        oText += row.strctureName + "-D" + row.DQPvalue.ToString();
                    }
                    else if (row.DQPtype == DQPtype.Volume)
                    {
                        oText += row.strctureName + "-V" + row.DQPvalue.ToString();
                    }
                    else if (row.DQPtype == DQPtype.DoseComplement)
                    {
                        oText += row.strctureName + "-DC" + row.DQPvalue.ToString();
                    }
                    else if (row.DQPtype == DQPtype.ComplementVolume)
                    {
                        oText += row.strctureName + "-CV" + row.DQPvalue.ToString();
                    }

                    // define input unit [Gy/cGy] or [cc] ot [%]
                    if (row.InputUnit == IOUnit.Absolute)
                    {
                        if (row.DQPtype == DQPtype.Dose)
                        {
                            oText += "cc";
                        }
                        else if (row.DQPtype == DQPtype.Volume)
                        {
                            oText += doseUnitString;
                        }
                        else if (row.DQPtype == DQPtype.DoseComplement)
                        {
                            oText += "cc";
                        }
                        else if (row.DQPtype == DQPtype.ComplementVolume)
                        {
                            oText += doseUnitString;
                        }
                    }
                    else
                    {
                        oText += "%";
                    }

                    // define output unit [Gy/cGy] or [cc] ot [%]
                    if (row.OutputUnit == IOUnit.Absolute)
                    {
                        if (row.DQPtype == DQPtype.Dose)
                        {
                            oText += "["+ doseUnitString + "]";
                        }
                        else if (row.DQPtype == DQPtype.Volume)
                        {
                            oText += "[cc]";
                        }
                        else if (row.DQPtype == DQPtype.DoseComplement)
                        {
                            oText += "[" + doseUnitString + "]";
                        }
                        else if (row.DQPtype == DQPtype.ComplementVolume)
                        {
                            oText += "[cc]";
                        }
                    }
                    else
                    {
                        oText += "[%]";
                    }
                    oText += ":";

                    // calculate dose qualiy parameters (VolumeAtDose,DoseAtVolume,DoseComplement,ComplementVolume).
                    if (row.DQPtype == DQPtype.Dose) // calculate DoseAtVolume.
                    {
                        DoseValue doseValue = ps.GetDoseAtVolume(targetStructure, row.DQPvalue,
                            row.InputUnit == IOUnit.Relative ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3,
                            row.OutputUnit == IOUnit.Relative ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute);
                        oText += doseValue.Dose.ToString();
                    }
                    else if (row.DQPtype == DQPtype.Volume) // calculate VolumeAtDose.
                    {                        
                        double voluemeValue = ps.GetVolumeAtDose(targetStructure,
                            row.InputUnit == IOUnit.Relative ?
                            new DoseValue(ps.TotalDose.Dose * (row.DQPvalue * 0.01), dvhStat.MaxDose.Unit) :
                            new DoseValue(row.DQPvalue, dvhStat.MaxDose.Unit),
                             row.OutputUnit == IOUnit.Relative ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3);
                        oText += voluemeValue.ToString();
                    }
                    else if (row.DQPtype == DQPtype.DoseComplement) // calculate DoseComplement.
                    {
                        double subVolume = 0.0;
                        if (row.InputUnit == IOUnit.Relative)
                        {
                            subVolume = targetStructure.Volume - targetStructure.Volume * (row.DQPvalue * 0.01);
                            subVolume = (subVolume / targetStructure.Volume * 100);
                        }
                        else
                        {
                            subVolume = targetStructure.Volume - row.DQPvalue;
                        }
                        DoseValue doseValue = ps.GetDoseAtVolume(targetStructure, subVolume,
                            row.InputUnit == IOUnit.Relative ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3,
                            row.OutputUnit == IOUnit.Relative ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute);
                        oText += doseValue.Dose.ToString();

                    }
                    else if (row.DQPtype == DQPtype.ComplementVolume) // calculate ComplementVolume.
                    {                        
                        double voluemeValue = ps.GetVolumeAtDose(targetStructure,
                           row.InputUnit == IOUnit.Relative ?
                           new DoseValue(ps.TotalDose.Dose * (row.DQPvalue * 0.01), dvhStat.MaxDose.Unit) :
                           new DoseValue(row.DQPvalue, dvhStat.MaxDose.Unit),
                           VolumePresentation.AbsoluteCm3);
                        double CV = targetStructure.Volume - voluemeValue;
                        if (row.OutputUnit == IOUnit.Relative)
                        {
                            CV = (CV / targetStructure.Volume * 100);
                        }
                        oText += CV.ToString();
                    }
                    else
                    {
                        oText += "DQP type not found";
                    }
                    oText += "\t";
                }
            }
            return oText;
        }
    }
    /// <summary>
    /// DQPtype
    /// </summary>
    public enum DQPtype
    {
        Dose,
        Volume,
        DoseComplement,
        ComplementVolume
    }
    /// <summary>
    /// IOUnit
    /// </summary>
    public enum IOUnit
    {
        Relative,
        Absolute
    }
    /// <summary>
    /// DQP
    /// </summary>
    public class DQP
    {
        // Structure name
        public string strctureName { get; set; }
        // DQP type
        public DQPtype DQPtype { get; set; }
        public double DQPvalue { get; set; }
        // IO unit
        public IOUnit InputUnit { get; set; }
        public IOUnit OutputUnit { get; set; }
    }
}
