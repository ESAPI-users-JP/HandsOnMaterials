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
            // open the report file
            string temp = System.Environment.GetEnvironmentVariable("TEMP");
            string dataFilePath = temp + @"\DataMiningOutput.txt";

            PrintStartupMsg(dataFilePath);
            using (StreamWriter reportFile = new StreamWriter(dataFilePath, false))
            {
                // iterate over all patients in the database
                foreach (PatientSummary patsum in app.PatientSummaries)
                {
                    // check if the user has hit a key
                    if (StopNow())
                        break;
                    // open a patient, report on the treatment history for that patient, and close the patient.
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
                             // Add filter or comment / uncomment Lines.
                             && ps.DosePerFraction.Dose == 2
                             && ps.NumberOfFractions.Value == 38
                             && c.Id == "C1"
                             //&& ps.Id == "1-1"
                             && targetVolumeID.Contains(ps.TargetVolumeID)
                             )
                             select new
                             {
                                 Patient = patient,
                                 Course = c,
                                 Plan = ps
                             };

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
            ps.DoseValuePresentation = DoseValuePresentation.Absolute;

            string msg = "";
            msg += patient.Id + "\t";
            msg += ps.Course.Id + "\t";
            msg += ps.Id + "\t";


            // TODO : Add here the code //




            // Each field parameters
            var energy_List = new List<string>();
            var mu_List = new List<string>();
            foreach (var b in ps.Beams)
            {
                if (!b.IsSetupField)//exclude SetupField
                {
                    energy_List.Add(b.EnergyModeDisplayName);
                    mu_List.Add(string.Format("{0:f1}", b.Meterset.Value));
                }
            }
            msg += "Energy:" + string.Join("/", energy_List) + "\t";
            msg += "MU:" + string.Join("/", mu_List) + "\t";

            //DVH statistics 
            DVHData dvhStat = null;
            Structure targetStructure = null;
            foreach (var structure in ps.StructureSet.Structures)
            {
                if (ps.Dose != null && ps.StructureSet != null)
                {
                    targetStructure = FindStructure(structure);
                    if (targetStructure != null)
                    {
                        dvhStat = ps.GetDVHCumulativeData(targetStructure, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.1);
                        if (dvhStat != null)
                        {
                            msg += targetStructure.Id + "\t" + dvhStat.MaxDose.ToString() + "\t" + dvhStat.MeanDose.ToString() + "\t" + dvhStat.MinDose.ToString() + "\t";
                        }
                        
                    }
                }
            }

            reportFile.WriteLine(msg);
        }

        static string[] s_targetIds = { "ITV", "PTV", "PTV-PROST", "PTV-SV", "GTV", "CTV", "BLADDER", "CORD", "BRAINSTEM", "LUNG", "LIVER", "KIDNEY", "BREAST", "OPTIC", "PAROTID", "SPINE", "RECTUM", "BOWEL", "BRAIN" };
        /// <summary>
        /// FindStructure
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        private static Structure FindStructure(Structure structure)
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
            System.Threading.Thread.Sleep(2000); // let user read message for 2 seconds
        }
    }
}
