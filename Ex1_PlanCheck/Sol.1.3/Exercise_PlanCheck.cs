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
            // Referene point check routine.
            messageText += "<<STRUCTURE CHECK " + new String(paddingChar, paddingLength) + "\n";
            messageText += CheckStructureFunc(plan);

            messageText += "\n";
            // Referene point check routine.
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

            // Check ImagingDeviceID
            checkName = "Imaging Device ID";
            string deviceName = "CT580W";

            //Retreave Image class
            VMS.TPS.Common.Model.API.Image image = plan.StructureSet.Image;

            if (image.Id == deviceName)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the paramters and text[X] to the string 
                oText += MakeFormatText(false, checkName, image.Id + " --> " + deviceName);
            }

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

            // Check course ID 
            checkName = "Course ID";
            //Set regular expression
            string expressionC = "^C(?<number>[0-9]{1,2}$)";
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

            // Check plan ID 
            checkName = "Plan ID";
            //Set regular expression
            string expressionP = "^(?<number>[0-9]{1,2})-[0-9]{1,2}$";
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
        /// 
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        static string CheckRPFunc(PlanSetup plan)
        {
            string oText = "";

            // TODO : Add here the code 

            return oText;
        }
        /// <summary>
        /// 
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

            // TODO : Add here the code 

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
