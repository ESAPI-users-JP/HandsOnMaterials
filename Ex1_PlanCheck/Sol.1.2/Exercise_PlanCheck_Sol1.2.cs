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
using System.Windows.Controls;

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
            messageText += caption + " was executed on " + datetext + "\n\n"; ;
            // Show patient information.
            messageText += "<<PLAN INFO " + new String(paddingChar, paddingLength) + "\n";
            messageText += GetPlanInfo(plan);
            messageText += "\n";


            /////////////////////////
            // Plan check routine. //
            /////////////////////////
            messageText += "<<PLAN CHECK " + new String(paddingChar, paddingLength) + "\n";
            messageText += CheckPlanFunc(plan);
            messageText += "\n";

            //////////////////////////
            // Field check routine. //
            //////////////////////////
            messageText += "<<FIELD CHECK " + new String(paddingChar, paddingLength) + "\n";
            messageText += CheckFieldFunc(plan);
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
                //If false, add the parameters and text[X] to the string 
                //oText = checkName + ": X \n";
                oText = checkName + ": (" + paraText + ") X \n";
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
            
            //Retrieve Course class
            var course = plan.Course;
            var courseId = course.Id;

            //Set regular expression
            string expressionC = "^C[0-9]{1,2}$";
            Regex regC = new Regex(expressionC);
            Match resultC = regC.Match(courseId);

            if (resultC.Success == true)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, courseId);
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check plan ID 
            checkName = "Plan ID";

            //Retreive plan Id
            var planId = plan.Id;

            //Set regular expression
            string expressionP = "^[0-9]{1,2}-[0-9]{1,2}$";
            Regex regP = new Regex(expressionP);
            Match resultP = regP.Match(planId);

            if (resultP.Success == true)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, planId);
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check plan normalization method.
            checkName = "Plan normalization method";
            
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
                    //If false, add the parameters and text[X] to the string 
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
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, n_method);
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check calculation model.
            //

            // Calculation model names
            string AAA = "AAA";
            string AcurosXB = "AXB";
            // Commissioned calculation models(+version)
            string AAA_version = AAA + "_15.6";
            string AcurosXB_version = AcurosXB + "_15.6";

            //Check calculation models
            checkName = "Photon calculation model";
            string photonCalculationModel = plan.PhotonCalculationModel;
            if (photonCalculationModel != AAA_version && photonCalculationModel != AcurosXB_version)
            {
                oText += MakeFormatText(false, checkName, photonCalculationModel);
            }
            else
            {
                oText += MakeFormatText(true, checkName, "");
            }

            //Check calculation grid sizes
            checkName = "Calculation grid size";
            double XRes_AAA = 2.5;
            double XRes_AcurosXB = 2.0;
            // calculation grid size
            // The dose matrix resolution in X-direction in millimeters
            double XRes = plan.Dose.XRes;
            // The dose matrix resolution in Y-direction in millimeters
            double YRes = plan.Dose.YRes;
            // The dose matrix resolution in Z-direction in millimeters
            double ZRes = plan.Dose.ZRes;

            if (XRes != XRes_AAA && photonCalculationModel.IndexOf(AAA) >= 0)
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
            checkName = "Check treatment machine";
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
                        //add the parameters to the string 
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
            checkName = "Check single isocenter";
            bool isoChkFlg = true;

            // Get 1st field isocenter position
            VVector isoCNT = plan.Beams.ElementAt(0).IsocenterPosition;
            foreach (var beam in plan.Beams)
            {
                // Compare isocenter position of 1st field with others
                if (VVector.Distance(beam.IsocenterPosition, isoCNT) != 0)
                {
                    isoChkFlg = false;
                }
            }

            if (isoChkFlg == true)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, "multiple isocenter");
            }


            ////////////////////////////////////////////////////////////////////////////////
            // Check MU 
            checkName = "Check MU";

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
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, invalidMU);
            }

            ///////////////////////
            //Check Dose Rate
            checkName = "Check Dose Rate";
            int defDoserate = 600;
            bool DRvalidFlag = true;
            string invalidDR = "";
            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField)
                {
                    if (beam.DoseRate != defDoserate)
                    {
                        DRvalidFlag = false;
                        invalidDR += "(" + beam.Id + ":" + string.Format("{0}", beam.DoseRate) + ")";
                    }
                }
            }
            if (DRvalidFlag == true)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, invalidDR);
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check Jaw/MLC Position 
            checkName = "Check Jaw/MLC position";

            var checkJawMLCText = "\n";

            double distJawMLCX = 0.0; // Jawともっとも開いているMLCとの規定距離(X方向)

            foreach (var beam in plan.Beams)
            {
                bool checkJawMLC = true;

                // MLCあり、かつStaticの場合のみ評価
                if (beam.MLC != null && beam.MLCPlanType == 0)
                {

                    var jawPositions = beam.ControlPoints.ElementAt(0).JawPositions;
                    var leafPositions = beam.ControlPoints.ElementAt(0).LeafPositions;
                    var leafPairs = leafPositions.GetLength(1); // Leaf対の数

                    float minX = 200;
                    float maxX = -200;

                    for (int i = 0; i < leafPairs; i++)
                    {
                        if (leafPositions[0, i] != leafPositions[1, i])
                        {
                            minX = (minX > leafPositions[0, i]) ? leafPositions[0, i] : minX;
                            maxX = (maxX < leafPositions[1, i]) ? leafPositions[1, i] : maxX;
                        }
                    }

                    // X Jawの規定位置
                    var jawIdealX1 = minX - distJawMLCX;
                    var jawIdealX2 = maxX + distJawMLCX;


                    // 規定位置と2㎜以上ずれている場合にエラーを出す
                    if (Math.Abs(jawIdealX1 - jawPositions.X1) > 2.0)
                    {
                        checkJawMLCText += string.Format("{0} : X1 jaw should be {1:f1} cm", beam.Id, jawIdealX1 / 10);
                        checkJawMLC = false;
                    }

                    if (Math.Abs(jawIdealX2 - jawPositions.X2) > 2.0)
                    {
                        checkJawMLCText += string.Format("{0} : X2 jaw should be {1:f1} cm", beam.Id, jawIdealX2 / 10);
                        checkJawMLC = false;
                    }

                }

                if (checkJawMLC)
                {
                    oText += MakeFormatText(true, checkName + "(" + beam.Id + ")", "");
                }
                else
                {
                    oText += MakeFormatText(false, checkName, checkJawMLCText);
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
