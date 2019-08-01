using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.IO;
using System.Text.RegularExpressions;

[assembly: AssemblyVersion("1.0.0.1")]

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context /*, System.Windows.Window window, ScriptEnvironment environment*/)
        {
            // TODO : Add here the code that is called when the script is launched from Eclipse.
            
            //Retrieve PlanSetup class
            PlanSetup plan = context.PlanSetup;
            if (plan == null)
            {
                MessageBox.Show("Warning: plan is not found.");
                return;
            }

            //Set the caption of the message box.
            string caption = "Plan Check Script";

            //Displays the [Yes] button and [No] button in the messagebox.
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            //Display a symbol consisting of lowercase i in a circle in the message box.
            MessageBoxImage icon = MessageBoxImage.Information;
            //Set default to [No] button.
            MessageBoxResult defaultResult = MessageBoxResult.No;
            //The message box text is right - aligned.
            MessageBoxOptions options = MessageBoxOptions.RightAlign;

            //Initializes the variables to pass to the MessageBox.Show method.
            string messageText = "";
            int paddingLength = 30;
            char paddingChar = '>';

            DateTime dt = DateTime.Now;
            string datetext = dt.ToString("yyyy/MM/dd HH:mm:ss");
            messageText += caption + " was executed on " + datetext + "\n"; ;
            // Show patient information.
            messageText += "<<PLAN INFO " + new String(paddingChar, paddingLength) + "\n";
            messageText += GetPlanInfo(plan);
            messageText += "\n";

            // Image check routine.
            messageText += "<<IMAGE CHECK " + new String(paddingChar, paddingLength) + "\n";
            messageText += CheckImageFunc(plan);

            messageText += "\n";
            // Plan check routine.
            messageText += "<<PLAN CHECK " + new String(paddingChar, paddingLength) + "\n";
            messageText += CheckPlanFunc(plan);

            messageText += "\n";
            // Field check routine.
            messageText += "<<FIELD CHECK " + new String(paddingChar, paddingLength) + "\n";
            messageText += CheckFieldFunc(plan);

            messageText += "\n";
            // Referene point check routine.
            messageText += "<<REFPOINT CHECK " + new String(paddingChar, paddingLength) + "\n";
            messageText += CheckRPFunc(plan);

            messageText += "\n";
            // Structure check routine.
            messageText += "<<STRUCTURE CHECK " + new String(paddingChar, paddingLength) + "\n";
            messageText += CheckStructureFunc(plan);

            messageText += "\n";
            // Dose check routine.
            messageText += "<<DOSE CHECK " + new String(paddingChar, paddingLength) + "\n";
            messageText += CheckDoseFunc(plan);

            messageText += "\n";
            //Add export messageText.
            messageText += "Do you export to text file ?";

            // show MessageBox 
            MessageBoxResult result = MessageBox.Show(messageText, caption, buttons, icon, defaultResult, options);

            // When the OK button is pressed, the data writing processing is executed.
            if (result == MessageBoxResult.Yes)
            {
                string temp = System.Environment.GetEnvironmentVariable("TEMP");
                string dataFilePath = temp + @"\PlanCheckResult.txt";

                using (StreamWriter sw = new StreamWriter(dataFilePath))
                {
                    // Execute text writing process.
                    sw.WriteLine(messageText);
                }
                MessageBox.Show("the results to the simple report '" + dataFilePath + "'.");
                // Open the folder with explore.exe.
                System.Diagnostics.Process.Start(temp);
            }
        }
        /// <summary>
        /// makeFormatText
        /// </summary>
        /// <param name="judge"></param>
        /// <param name="checkName"></param>
        /// <param name="paraText"></param>
        /// <returns></returns>
        static string MakeFormatText(bool judge, string checkName, string paraText)
        {
            //Initializes the variables
            string oText = "";

            if (judge == true)
            {
                // If true, add text[O] to the string 
                oText = checkName + ": O \n";
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                //oText = checkName + ": X \n";
                oText = checkName + ": (" + paraText + ") X \n";
            }
            return oText;
        }
        /// <summary>
        /// checkImageFunc
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        static string CheckImageFunc(PlanSetup plan)
        {
            //Initializes the variables
            string oText = "";
            string checkName = "";

            ////////////////////////////////////////////////////////////////////////////////
            // Check ImagingDeviceID
            checkName = "Imaging Device ID";
            string deviceName = "CT580W";

            //Retreave Image class
            VMS.TPS.Common.Model.API.Image image = plan.StructureSet.Image;

            if (image.Series.ImagingDeviceId == deviceName)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, image.Id + " --> " + deviceName);
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check PatientOrientation
            checkName = "PatientOrientation";
            string PatientOrientation = "HeadFirstSupine";

            if (image.ImagingOrientation.ToString() == PatientOrientation)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, image.ImagingOrientation.ToString() + " --> " + PatientOrientation);
            }

            // Matching between ImagingOrientation and TreatmentOrientation
            checkName = "MatchOrientation(Image-Plan)";
            if (image.ImagingOrientation.ToString() == plan.TreatmentOrientation.ToString())
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, image.ImagingOrientation.ToString() + " <-> " + plan.TreatmentOrientation.ToString());
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check imaging date of CT images
            // CT images must be newest.
            checkName = "ImageDate";
            //Get open image creation date
            DateTime cImgDateTime = image.CreationDateTime.Value;
            DateTime datetime = DateTime.Now;
            DateTime newestImgDate = cImgDateTime;
            foreach(var study in plan.Course.Patient.Studies)
            {
                // Exclude QA(phantom) images
                if(study.Comment != "ARIA RadOnc Study")
                {
                    foreach(var series in study.Series)
                    {
                        // Exclude kVCBCT images
                        if(series.ImagingDeviceId !="")
                        {
                            foreach(var im in series.Images)
                            {
                                datetime = im.CreationDateTime.Value;
                                if(datetime.Date>cImgDateTime.Date)
                                {
                                    newestImgDate = datetime;
                                }
                            }
                        }
                    }
                }
            }

             if (newestImgDate.Date == cImgDateTime.Date)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, cImgDateTime.ToString("yyyyMMdd") + " --> newest:" +  newestImgDate.ToString("yyyyMMdd"));
            }



            return oText;
        }
        /// <summary>
        /// checkPlanFunc
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        static string CheckPlanFunc(PlanSetup plan)
        {
            //Initializes the variables
            string oText = "";
            string checkName = "";

            ////////////////////////////////////////////////////////////////////////////////
            // Check course ID 
            checkName = "Course ID";
            //Set regular expression
            string expressionC = "^C[0-9]{1,2}$";
            Regex regC = new Regex(expressionC);
            Match resultC = regC.Match(plan.Course.Id);

            if (resultC.Success == true)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, plan.Course.Id);
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check plan ID 
            checkName = "Plan ID";
            //Set regular expression
            string expressionP = "^[0-9]{1,2}-[0-9]{1,2}$";
            Regex regP = new Regex(expressionP);
            Match resultP = regP.Match(plan.Id);

            if (resultP.Success == true)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, plan.Id);
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check plan normalization method.
            checkName = "plan normalization method";
            string n_method = plan.PlanNormalizationMethod;
            //if ((n_method.IndexOf("No plan normalization") >= 0) ||
            //(n_method.IndexOf("Plan Normalization Value") >= 0))

            if (n_method.IndexOf("Primary") >= 0)// set primary reference point
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else if (n_method.IndexOf("covers") >= 0)
            {
                if ((((n_method.IndexOf("95.00% of") >= 0) || (n_method.IndexOf("50.00% of") >= 0))
                    && (n_method.IndexOf("100.00% covers") >= 0)) == false)
                {
                    //If false, add the paramters and text[X] to the string 
                    oText += MakeFormatText(false, checkName, n_method);
                }
                else // D95% or D50% 
                {
                    // If true, add text[O] to the string 
                    oText += MakeFormatText(true, checkName, "");
                }
            }
            else // other method (No plan normalization or Plan Normalization Value)
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, n_method);
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check calculation model.
            //

            // Calculation model names
            string AAA = "AAA";
            string AcurosXB = "AEB";
            // Commissioned calculation models(+version)
            string AAA_version = AAA + "_15.6.03";        
            string AcurosXB_version = AcurosXB + "_15.6.03";

            //Check calculation models
            checkName = "photon calculation model";
            string photonCalculationModel = plan.PhotonCalculationModel;
            if (photonCalculationModel != AAA_version)
            {
                oText += MakeFormatText(false, checkName, photonCalculationModel);
            }
            else if (photonCalculationModel != AcurosXB_version) 
            {
                oText += MakeFormatText(false, checkName, photonCalculationModel);
            }
            else
            {
                oText += MakeFormatText(true, checkName, "");
            }

            //Check calculation grid sizes
            checkName = "calculation grid size";
            double XRes_AAA = 2.5;
            double XRes_AcurosXB = 2.0;
            // calculation grid size
            // The dose matrix resolution in X-direction in millimeters
            double XRes = plan.Dose.XRes;
            // The dose matrix resolution in Y-direction in millimeters
            double YRes = plan.Dose.YRes;
            // The dose matrix resolution in Z-direction in millimeters
            double ZRes = plan.Dose.ZRes;

            if (XRes != XRes_AAA && photonCalculationModel.IndexOf(AAA) >=0 )
            {
                oText += MakeFormatText(false, checkName, string.Format("{0:f1}", XRes) + " -> " + string.Format("{0:f1}", XRes_AAA));
            }
            else if (XRes != XRes_AcurosXB && photonCalculationModel.IndexOf(AcurosXB) >= 0)
            {
                oText += MakeFormatText(false, checkName, string.Format("{0:f1}", XRes) + " -> " + string.Format("{0:f1}", XRes_AcurosXB));
            }
            else
            {
                oText += MakeFormatText(true, checkName, "");
            }

            return oText;
        }
        /// <summary>
        /// checkFieldFunc
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        static string CheckFieldFunc(PlanSetup plan)
        {
            //Initializes the variables
            string oText = "";
            string checkName = "";

            ////////////////////////////////////////////////////////////////////////////////
            // Check Treatment Machine
            //
            checkName = "check treatment machine";
            bool machineChkFlag = true;

            //TreatmentMachineName (the 1st field)
            string machine = plan.Beams.ElementAt(0).TreatmentUnit.Id;

            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField)
                {
                    if (machine != beam.TreatmentUnit.Id)
                    {
                        //If false
                        //flag -> false 
                        machineChkFlag = false;
                        //add the paramters to the string 
                        oText += MakeFormatText(false, checkName, beam.Id + ": " + beam.TreatmentUnit.Id + " -> " + machine);
                    }                 
                }
            }

            // If true
            if (machineChkFlag == true)
            {    
                //add OK to the string 
                oText += MakeFormatText(true, checkName, "");
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check isocenter
            // Plan isocenter must be single point
            checkName = "check single isocenter";
            bool isoChkFlg = true;

            // Get 1st field isocenter position
            VVector isoCNT = plan.Beams.ElementAt(0).IsocenterPosition;
            foreach (var beam in plan.Beams)
            {
                // Compare isocenter position of 1st field with others
                if(VVector.Distance(beam.IsocenterPosition, isoCNT) != 0)
                {
                    isoChkFlg=false;
                }
            }

            if (isoChkFlg == true)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, "multiple isocenter");
            }


            ////////////////////////////////////////////////////////////////////////////////
            // Check MU 
            checkName = "check MU";

            double minMU = 5.0;
            bool validFlag = true;
            string invalidMU = "";

            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField)
                {
                    if (beam.Meterset.Value < minMU)
                    {
                        validFlag = false;
                        invalidMU += "(" + beam.Id + ":" + string.Format("{0:f1}", beam.Meterset.Value) + ")";
                    }
                }
            }
            if (validFlag == true)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, invalidMU);
            }
            return oText;
        }
        /// <summary>
        /// checkRPFunc
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        static string CheckRPFunc(PlanSetup plan)
        {
            string oText = "";
            string checkName = "";

            ////////////////////////////////////////////////////////////////////////////////
            // Check primary reference point ID
            // Primary reference point ID and plan ID must be the same
            checkName ="check primary ref. point ID";

            if (plan.PrimaryReferencePoint.Id == plan.Id)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, "primary ref. point ID:"+plan.PrimaryReferencePoint.Id+",plan ID:"+plan.Id);
            }
            // TODO : Add here the code 

            return oText;
        }
        /// <summary>
        /// CheckStructureFunc
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        static string CheckStructureFunc(PlanSetup plan)
        {
            string oText = "";

            // TODO : Add here the code 

            return oText;

        }
        /// <summary>
        /// checkDoseFunc
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        static string CheckDoseFunc(PlanSetup plan)
        {
            string oText = "";

            ////////////////////////////////////////////////////////////////////////////////
            // Check DVH parameters
            //

            // scaling factor: Gy to cGy
            double scaling_dose_unit = 100.0;
            if (plan.TotalDose.UnitAsString == "Gy")
            {
                scaling_dose_unit = 1.0;
            }

            // Bin width of DVH curves
            double binWidth = 0.001;

            oText += " ----- Target volumes -------------------------- \n";

            foreach (var structure in plan.StructureSet.Structures)
            {
                // CTV 
                if (structure.Id.IndexOf("CTV") >= 0 && structure.DicomType == "CTV")
                {
                    // D98%
                    DVHData dvhData = plan.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, binWidth);
                    DoseValue d_98 = plan.DoseAtVolume(dvhData, 98.0);
                    oText += "(" + structure.Id + ") D98%:" + string.Format("{0:f2}", d_98.Dose) + " " + plan.TotalDose.UnitAsString + "\n";
                }

                // PTV
                if (structure.Id.IndexOf("PTV") >= 0 && structure.DicomType == "PTV")
                {
                    
                    // D95%
                    DVHData dvhData = plan.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, binWidth);
                    DoseValue d_95 = plan.DoseAtVolume(dvhData, 95.0);
                    oText += "(" + structure.Id + ") D95%:" + string.Format("{0:f2}", d_95.Dose) + " " + plan.TotalDose.UnitAsString + "\n";

                    // HI 
                    // Dmax/Dmin
                    double maxDose = dvhData.MaxDose.Dose;
                    double minDose = dvhData.MinDose.Dose;
                    double homogeneityIndex = maxDose/minDose;
                    oText += "(" + structure.Id + ") Homogeneity Index:" + string.Format("{0:f2}", homogeneityIndex) + "\n";


                    // HI 
                    // (D2% - D98%) / D50% 
                    DoseValue d_2 = plan.DoseAtVolume(dvhData, 2.0);
                    DoseValue d_98 = plan.DoseAtVolume(dvhData, 98.0);
                    DoseValue d_50 = plan.DoseAtVolume(dvhData, 50.0);
                    double homogeneityIndex_ICRU83 = (d_2.Dose - d_98.Dose)/d_50.Dose;
                    oText += "(" + structure.Id + ") Homogeneity Index(ICRU 83):" + string.Format("{0:f2}", homogeneityIndex_ICRU83) + "\n";

                }
            }

            oText += " ----- OARs -------------------------- \n";

            foreach (var structure in plan.StructureSet.Structures)
            {
                // Change uppercase letters to lowercase
                String str_lowercase = structure.Id.ToLower();

                // Lung
                // V20Gy
                if (str_lowercase.IndexOf("lung") >= 0)
                {
                    DVHData dvhData = plan.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, binWidth);
                    double dose_index = 20.0 * scaling_dose_unit;
                    double v_20 = plan.VolumeAtDose(dvhData, dose_index);
                    if(v_20 < 30.0)
                    {
                        oText += "(" + structure.Id + ") V20Gy:" + string.Format("{0:f2}", v_20) + " %" + "\n";
                    }
                    else
                    {
                        oText += "WARNING!! (" + structure.Id + ") V20Gy:" + string.Format("{0:f2}", v_20) + " % (QUANTEC: < 30.0)" + "\n";
                    }
                }

                // Brainstem
                // D1cc
                if (str_lowercase.IndexOf("brain") >= 0 && structure.Id.IndexOf("stem") >= 0)
                {
                    DVHData dvhData = plan.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, binWidth);
                    DoseValue d_1cc = plan.DoseAtVolume(dvhData, 1.0);
                    if(d_1cc < 59.0)
                    {
                        oText += "(" + structure.Id + ") D1cc:" + string.Format("{0:f2}", d_1cc.Dose) + " " + plan.TotalDose.UnitAsString + "\n";
                    }
                    else
                    {
                        oText += "WARNING!! (" + structure.Id + ") D1cc:" + string.Format("{0:f2}", d_1cc.Dose) + " " + plan.TotalDose.UnitAsString + " (QUANTEC: < 59.0)\n";
                    }
                }
            } 

            return oText;

        }

        /// <summary>
        /// getPatientInfo
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        static string GetPlanInfo(PlanSetup plan)
        {
            //Initializes the variables
            string oText = "";
            // Retrieve Patient class
            Patient patient = plan.Course.Patient;
            var patientID = patient.Id;
            var patientName = patient.LastName + " " + patient.FirstName;
            oText += string.Format("ID:{0}, Name:{1}\n", patientID, patientName);
            oText += string.Format("Course ID:{0}, Plan ID:{1}\n", plan.Course.Id, plan.Id);
            oText += string.Format("Approval status:{0}, Approval date ID:{1}\n", plan.ApprovalStatus.ToString(), plan.PlanningApprovalDate);
            return oText;
        }
    }
}
