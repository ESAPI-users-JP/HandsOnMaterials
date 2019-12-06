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
                //If false, add the parameters and text[X] to the string 
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
                //If false, add the parameters and text[X] to the string 
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
                //If false, add the parameters and text[X] to the string 
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
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, image.ImagingOrientation.ToString() + " <-> " + plan.TreatmentOrientation.ToString());
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check imaging date of CT images
            // CT images must be newest.
            checkName = "ImageDate";
            //Get open image creation date
            DateTime cImgDateTime = image.CreationDateTime.Value;
            DateTime datetime;
            DateTime newestImgDate = cImgDateTime;
            foreach (var study in plan.Course.Patient.Studies)
            {
                // Exclude QA(phantom) images
                if (study.Comment != "ARIA RadOnc Study")

                {
                    foreach (var series in study.Series)
                    {
                        // Exclude kVCBCT images
                        if (series.ImagingDeviceId != "")
                        {
                            foreach (var im in series.Images)
                            {
                                datetime = im.CreationDateTime.Value;
                                if (datetime.Date > cImgDateTime.Date)
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
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, cImgDateTime.ToString("yyyyMMdd") + " --> newest:" + newestImgDate.ToString("yyyyMMdd"));
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
                //If false, add the parameters and text[X] to the string 
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
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, plan.Id);
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
                if (((n_method.IndexOf("100.00% covers") >= 0)
                 && ((n_method.IndexOf("95.00% of") >= 0) || (n_method.IndexOf("50.00% of") >= 0))) == true)
                {
                    // If true, add text[O] to the string 
                    oText += MakeFormatText(true, checkName, "");
                }
                else
                {
                    //If false, add the parameters and text[X] to the string 
                    oText += MakeFormatText(false, checkName, n_method);
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
                        checkJawMLCText += string.Format("{0} : X1 jaw should be {1:f1} cm", beam.Id, jawIdealX1/10);
                        checkJawMLC = false;
                    }

                    if (Math.Abs(jawIdealX2 - jawPositions.X2) > 2.0)
                    {
                        checkJawMLCText += string.Format("{0} : X2 jaw should be {1:f1} cm", beam.Id, jawIdealX2/10);
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

            ////////////////////////////////////////////////////////////////////////////////
            // Collision Check 
            checkName = "Check Collision";

            string structureName = "BODY";
            // radius of the bottom of gantry head in cm
            double radiusCm = 50;
            // clearance of the gantry head in cm
            double clearanceCm = 50;

            var structure = GetStructureFromId(structureName, plan);
            if (structure == null)
            {
                oText += MakeFormatText(false, checkName, String.Format("No structure: {0}", structureName));
            }
            else
            {
                //radius in mm
                var radius = radiusCm * 10;

                //clearance in mm
                var clearance = clearanceCm * 10;

                var image = plan.StructureSet.Image;
                double zOrigin = image.Origin.z;
                int zSize = image.ZSize;
                double zRes = image.ZRes;
                double xIso = plan.Beams.First().IsocenterPosition.x;
                double yIso = plan.Beams.First().IsocenterPosition.y;
                double zIso = plan.Beams.First().IsocenterPosition.z;
                var zRange = GetGridRange1d(radius, zIso, zOrigin, zSize, zRes);

                var zCollisions = new List<double>();
                for (int i = zRange[0]; i <= zRange[1]; i++)
                {
                    double z = zOrigin + i * zRes;

                    // if i goes out of image, the boundary image is used
                    VVector[][] contours;
                    if (i < 0)
                    {
                        contours = structure.GetContoursOnImagePlane(0);
                    }
                    else if (i >= zSize)
                    {
                        contours = structure.GetContoursOnImagePlane(zSize - 1);
                    }
                    else
                    {
                        contours = structure.GetContoursOnImagePlane(i);
                    }

                    bool doesCollide = false;
                    foreach (var contour in contours)
                    {
                        if (doesCollide)
                        {
                            break;
                        }
                        foreach (var point in contour)
                        {
                            double r = Math.Sqrt(Math.Pow(point.x - xIso, 2) + Math.Pow(point.y - yIso, 2));
                            if (r >= clearance)
                            {
                                doesCollide = true;
                                zCollisions.Add(z);
                                break;
                            }
                        }
                    }
                }

                if (zCollisions.Count == 0)
                {
                    oText += MakeFormatText(true, checkName, "");
                    //   return "No collision";
                }
                else
                {
                    // string collisionResult = "Collision check:";

                    double x = image.Origin.x;
                    double y = image.Origin.y;
                    double zFirst = zCollisions.First();
                    double zLast = zCollisions.Last();

                    var zFirstUcs = (image.DicomToUser(new VVector(x, y, zFirst), plan)).z;
                    var zLastUcs = (image.DicomToUser(new VVector(x, y, zLast), plan)).z;
                    string collisionResult = string.Format("\n{0} collides with gantry between z = {1:0.00} and {2:0.00} cm", structureName, zFirstUcs / 10, zLastUcs / 10);

                    oText += MakeFormatText(false, checkName, collisionResult);
                }
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
            checkName = "Check primary ref. point ID";

            if (plan.PrimaryReferencePoint.Id == plan.Id)
            {
                // If true, add text[O] to the string 
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                //If false, add the parameters and text[X] to the string 
                oText += MakeFormatText(false, checkName, "primary ref. point ID:" + plan.PrimaryReferencePoint.Id + ",plan ID:" + plan.Id);
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Reference Point と体表面の距離を算出
            checkName = "Distance between BODY and Primary Ref. Point";

            double tolDist = 5.0;  // ReferencePointと体表面距離の許容値 (mm)

            // Reference Point座標の取得
            if (plan.PrimaryReferencePoint.HasLocation(plan))
            {
                var refPointLocation = plan.PrimaryReferencePoint.GetReferencePointLocation(plan);

                // 体輪郭の取得
                var ss = plan.StructureSet;
                var body = ss.Structures.First(s => s.DicomType == "EXTERNAL");

                var z = ss.Image.ZSize;
                double minDist = 10000;
                for (int i = 0; i < z; i++)
                {
                    var contours = body.GetContoursOnImagePlane(i);
                    if (contours.Length != 0)
                    {
                        foreach (var contour in contours)
                        {
                            foreach (var point in contour)
                            {
                                var dist = VVector.Distance(refPointLocation, point);
                                minDist = (minDist > dist) ? dist : minDist;
                            }
                        }
                    }
                }
                if (minDist > tolDist)
                {
                    oText += MakeFormatText(true, checkName, "");
                }
                else
                {
                    oText += MakeFormatText(false, checkName, string.Format("Distance between ref. point and Body is {0:f1} mm", minDist));
                }

            }
            else
            {
                oText += MakeFormatText(false, checkName, "Primary Reference Point has no location.");
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Check primary reference point Dose limit
            checkName = "Check Total Dose Limit";
            if (plan.PrimaryReferencePoint.TotalDoseLimit == plan.TotalDose)
            {
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                oText += MakeFormatText(false, checkName, "Total Dose Limit: " + plan.PrimaryReferencePoint.TotalDoseLimit + ",Planed Dose: " + plan.TotalDose);
            }
            //Check Session Dose Limit
            checkName = "Check Session Dose Limit";
            if (plan.PrimaryReferencePoint.SessionDoseLimit == plan.DosePerFraction)
            {
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                oText += MakeFormatText(false, checkName, " Session Dose Limit: " + plan.PrimaryReferencePoint.SessionDoseLimit + ",Planed Dose: " + plan.DosePerFraction);
            }
            //Check Daily Dose Limit
            checkName = "Check Daily Dose Limit";
            if (plan.PrimaryReferencePoint.DailyDoseLimit == plan.PrimaryReferencePoint.SessionDoseLimit)
            {
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                oText += MakeFormatText(false, checkName, " Daily DoseLimit: " + plan.PrimaryReferencePoint.DailyDoseLimit + "is differ form Session Dose Limit: " + plan.PrimaryReferencePoint.SessionDoseLimit);
            }


            ////////////////////////////////////////////////////////////////////////////////
            // Check Isocenter HU
            oText += CheckIsocenterHu(plan, -300, 1000);

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
            string checkName = "";

            var ss = plan.StructureSet;

            ////////////////////////////////////////////////////////////////////////////////
            // 輪郭の命名規則のチェック
            // Volume TypeがPTVの場合に輪郭名がPTVから始まっているかチェック
            checkName = "Check structure ID";
            string checkStructureText = "\n";
            bool checkStructureId = true;

            foreach (var s in ss.Structures)
            {
                if (s.DicomType == "PTV")
                {
                    if (!s.Id.StartsWith("PTV"))
                    {
                        checkStructureText += string.Format("{0} should start with PTV\n", s.Id);
                        checkStructureId = false;
                    }
                }
            }

            if (checkStructureId)
            {
                oText += MakeFormatText(true, checkName, "");
            }
            else
            {
                oText += MakeFormatText(false, checkName, checkStructureText);
            }

            /////////////////////////////////
            //Check HU Assigned Structure

            checkName = "Check HU Assigned Structure";
            bool AssignedFlag = false;

            string HUAssigned = "";
            foreach (Structure st in ss.Structures)
            {
                double AssignedHU = 0;
                bool isAssigned = st.GetAssignedHU(out AssignedHU);
                if (isAssigned)
                {
                    AssignedFlag = true;
                    HUAssigned += " (" + st.Id + ": " + string.Format("{0}", AssignedHU) + "HU" + ")";
                }
            }
            if (AssignedFlag == false)
            {
                oText += "No Structure HU Overridden";
            }
            else
            {
                oText += MakeFormatText(false, checkName, HUAssigned);
            }

            oText = "\n";
            
            /////////////////////////////////
            //Check Small segments
            oText += CheckSmallSegments(plan);

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

            // Bin width of DVH curves
            double binWidth = 0.001;

            oText += " ----- Target volumes -------------------------- \n";

            foreach (var structure in plan.StructureSet.Structures)
            {
                // CTV 
                if (structure.Id.IndexOf("CTV") >= 0 && structure.DicomType == "CTV")
                {
                    // D98%
                    DoseValue d_98 = plan.GetDoseAtVolume(structure, 98.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                    oText += "(" + structure.Id + ") D98%:" + string.Format("{0:f2}", d_98.Dose) + " " + plan.TotalDose.UnitAsString + "\n";
                }

                // PTV
                if (structure.Id.IndexOf("PTV") >= 0 && structure.DicomType == "PTV")
                {
                    
                    // D95%
                    DoseValue d_95 = plan.GetDoseAtVolume(structure, 95.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                    oText += "(" + structure.Id + ") D95%:" + string.Format("{0:f2}", d_95.Dose) + " " + plan.TotalDose.UnitAsString + "\n";
                    
                    // Dmean
                    DoseValue d_mean = plan.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, binWidth).MeanDose;
                    oText += "(" + structure.Id + ") Dmean:" + string.Format("{0:f2}", d_mean.Dose) + " " + plan.TotalDose.UnitAsString + "\n";

                    // HI 
                    // Dmax/Dmin
                    DVHData dvhData = plan.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, binWidth);
                    double maxDose = dvhData.MaxDose.Dose;
                    double minDose = dvhData.MinDose.Dose;
                    double homogeneityIndex = maxDose/minDose;
                    oText += "(" + structure.Id + ") Homogeneity Index:" + string.Format("{0:f2}", homogeneityIndex) + "\n";


                    // HI 
                    // (D2% - D98%) / D50% 
                    DoseValue d_2 = plan.GetDoseAtVolume(structure, 2.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                    DoseValue d_98 = plan.GetDoseAtVolume(structure, 98.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                    DoseValue d_50 = plan.GetDoseAtVolume(structure, 50.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
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
                    DoseValue dose_index = new DoseValue(2000.0, "cGy");
                    double v_20 = plan.GetVolumeAtDose(structure, dose_index, VolumePresentation.Relative);
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
                if (str_lowercase.IndexOf("brain") >= 0 && str_lowercase.IndexOf("stem") >= 0)
                {
                    DoseValue d_1cc = plan.GetDoseAtVolume(structure, 1.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                    if(d_1cc.Dose < 59.0)
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



        public static string CheckIsocenterHu(PlanSetup planSetup, double HuLowerThreshold, double HuUpperThreshold)
        {
            var isocenter = planSetup.Beams.First().IsocenterPosition;
            var isocenterPosition = new double[] { isocenter.x, isocenter.y, isocenter.z };
            var image = planSetup.StructureSet.Image;

            var gridOrigin = new double[] { image.Origin.x, image.Origin.y, image.Origin.z };
            var gridReses = new double[] { image.XRes, image.YRes, image.ZRes };
            var gridSizes = new int[] { image.XSize, image.YSize, image.ZSize };

            // radius of the sphere ROI in mm
            double radius = 5;


            var gridRanges = GetGridRangesForSphere(radius, isocenterPosition, gridOrigin, gridReses, gridSizes);

            var voxels = new double[(gridRanges[3] - gridRanges[0] + 1) * (gridRanges[4] - gridRanges[1] + 1) * (gridRanges[5] - gridRanges[2] + 1)];

            int numVoxels = 0;
            double sum = 0;
            var zImage = new int[image.XSize, image.YSize];
            // Z
            for (int i = gridRanges[2]; i <= gridRanges[5]; i++)
            {
                double z = image.Origin.z + i * image.ZRes;
                image.GetVoxels(i, zImage);
                // Y
                for (int j = gridRanges[1]; j <= gridRanges[4]; j++)
                {
                    double y = image.Origin.y + j * image.YRes;
                    // X
                    for (int k = gridRanges[0]; k <= gridRanges[3]; k++)
                    {
                        double x = image.Origin.x + k * image.XRes;

                        double r = Math.Sqrt(Math.Pow(isocenterPosition[0] - x, 2) + Math.Pow(isocenterPosition[1] - y, 2) + Math.Pow(isocenterPosition[2] - z, 2));
                        if (r > radius)
                        {
                            continue;
                        }

                        var value = image.VoxelToDisplayValue(zImage[k, j]);
                        voxels[numVoxels] = value;
                        sum += value;
                        numVoxels += 1;
                    }
                }
            }


            var average = sum / numVoxels;
            //MessageBox.Show(String.Format("numVoxels, sum, average: {0}, {1}, {2:0.0}", numVoxels, sum, average));
            string result = String.Format("Averaged HU within the {0} mm sphere at Isocenter: {1:0.0}", radius, average);
            
            if (HuLowerThreshold < average && average < HuUpperThreshold)
            {
                return MakeFormatText(true, "Check Isocenter HU", "");
            }
            else
            {
                return MakeFormatText(false, "Check Isocenter HU", result);
            }

        }

        public static string CheckSmallSegments(PlanSetup planSetup)
        {
            double defaultMinimumSegmentAreaCm2 = 0.5;

            // Threshold area in mm2
            double thresholdArea;

            //If doesAskThreholdArea is true, input window for minimum segment area pops up
            bool doesAskThreholdArea = true;
            if (!doesAskThreholdArea)
            {
                thresholdArea = defaultMinimumSegmentAreaCm2 * 100;
            }
            else
            {
                var inputWindow = new InputWindow("Minimum segment area", "Minimum segment area in cm2", defaultMinimumSegmentAreaCm2.ToString("0.00"));
                inputWindow.Window.ShowDialog();

                if ((!inputWindow.IsOk) || string.IsNullOrWhiteSpace(inputWindow.InputText))
                {
                    return "Small segment area check is canceled\n";
                }

                double thresholdAreaCm2;
                if (double.TryParse(inputWindow.InputText, out thresholdAreaCm2))
                {
                    // cm2 to mm2
                    thresholdArea = thresholdAreaCm2 * 100;
                }
                else
                {
                    return string.Format("Invalid input for segment area: {0}\n", inputWindow.InputText);
                }
            }

            string structureName = "BODY";
            var query = planSetup.StructureSet.Structures.Where(s => s.Id == structureName);
            if (query.Count() != 1)
            {
                MessageBox.Show(String.Format("No structure: {0}", structureName));
                return String.Format("No structure: {0}\n", structureName);
            }
            var structure = query.Single();

            //MessageBox.Show(String.Format("{0}", structure.GetNumberOfSeparateParts()));

            var image = planSetup.StructureSet.Image;
            var smallAreas = new List<double[]>();
            for (int i = 0; i < image.ZSize; i++)
            {
                var z = image.Origin.z + i * image.ZRes;
                var contours = structure.GetContoursOnImagePlane(i);
                if (contours.Length > 0)
                {
                    //Console.WriteLine("z index: {0}, Number of contours: {1}", i, contours.Length);
                    for (int j = 0; j < contours.Length; j++)
                    {
                        var area = AreaOfPolygon(contours[j]);
                        //Console.WriteLine("\tindex: {0}, Area: {1}", j, area);

                        if (area <= thresholdArea)
                        {
                            var centerOfMass = CenterOfMassOfPolygon(contours[j]);
                            var smallArea = new double[] { centerOfMass[0], centerOfMass[1], centerOfMass[2], area };
                            smallAreas.Add(smallArea);
                        }
                    }
                }
            }

            if (smallAreas.Count == 0)
            {
                string oText = string.Format("No small segment less than {0} cm2 in {1}", thresholdArea * 0.01, structureName);
                return MakeFormatText(true, "Check small segments", oText);
            }

            string smallAreaResult = string.Format("Small segments (<= {0} cm2 ) in {1}:\n", thresholdArea * 0.01, structureName);
            foreach (var smallArea in smallAreas)
            {
                var x = smallArea[0];
                var y = smallArea[1];
                var z = smallArea[2];
                var area = smallArea[3];

                var centerOfMassUcs = image.DicomToUser(new VVector(x, y, z), planSetup);

                string result = String.Format("\n({0:0.00}, {1:0.00}, {2:0.00}): {3:0.000} cm2\n",
                    centerOfMassUcs.x / 10, centerOfMassUcs.y / 10, centerOfMassUcs.z / 10, area * 0.01);
                smallAreaResult += result;
            }

            return MakeFormatText(false, "Check small segments", smallAreaResult);
        }


        /// <summary>
        /// Area of polygon
        /// Reference : https://imagingsolution.net/math/calc_n_point_area/
        /// </summary>
        /// <remarks>
        /// All points are assumed in the same z plane 
        /// </remarks>
        /// <param name="points"> Array of VVectors of vertices of polygon </param>
        /// <returns> Area of polygon </returns>
        public static double AreaOfPolygon(VVector[] points)
        {

            int numberOfPoints = points.Length;

            // a point or line 
            if (numberOfPoints < 3)
            {
                return 0;
            }

            // Calculating area using outer product
            double sum = 0;
            for (int i = 0; i < numberOfPoints - 1; i++)
            {
                sum += points[i].x * points[i + 1].y - points[i].y * points[i + 1].x;
            }

            sum += points[numberOfPoints - 1].x * points[0].y - points[numberOfPoints - 1].y * points[0].x;

            return 0.5 * Math.Abs(sum);
        }

        /// <summary>
        /// Center of mass coordinate in the z plane
        /// </summary>
        /// <remarks>
        /// z coordinates are ignored.
        /// </remarks>
        /// <param name="points"> Array of VVector of the point coordinates </param>
        /// <returns> Center of mass coordinate </returns>
        public static double[] CenterOfMassOfPolygon(VVector[] points)
        {
            int numberOfPoints = points.Length;

            double xSum = 0;
            double ySum = 0;

            for (int i = 0; i < numberOfPoints; i++)
            {
                xSum += points[i].x;
                ySum += points[i].y;
            }

            return new double[] { xSum / numberOfPoints, ySum / numberOfPoints, points[0].z };
        }

        public static int[] GetGridRangesForSphere(double radius, double[] center, double[] gridOrigin, double[] gridReses, int[] gridSizes)
        {
            var gridRanges = new int[6];

            int iXCenter = (int)((center[0] - gridOrigin[0]) / gridReses[0]);
            int iYCenter = (int)((center[1] - gridOrigin[1]) / gridReses[1]);
            int iZCenter = (int)((center[2] - gridOrigin[2]) / gridReses[2]);

            int xRange = (int)(Math.Ceiling(radius / gridReses[0]));
            int yRange = (int)(Math.Ceiling(radius / gridReses[1]));
            int zRange = (int)(Math.Ceiling(radius / gridReses[2]));

            // Lower limits
            gridRanges[0] = iXCenter - xRange;
            if (center[0] - (gridOrigin[0] + gridRanges[0] * gridReses[0]) < radius)
            {
                gridRanges[0] -= 1;
            }
            gridRanges[1] = iYCenter - yRange;
            if (center[1] - (gridOrigin[1] + gridRanges[1] * gridReses[1]) < radius)
            {
                gridRanges[1] -= 1;
            }
            gridRanges[2] = iZCenter - zRange;
            if (center[2] - (gridOrigin[2] + gridRanges[2] * gridReses[2]) < radius)
            {
                gridRanges[2] -= 1;
            }

            // check if gridRanges is out of lower boundary
            if (gridRanges[0] < 0)
            {
                gridRanges[0] = 0;
            }
            if (gridRanges[1] < 0)
            {
                gridRanges[1] = 0;
            }
            if (gridRanges[2] < 0)
            {
                gridRanges[2] = 0;
            }

            // Upper limits
            gridRanges[3] = iXCenter + xRange;
            if ((gridOrigin[0] + gridRanges[3] * gridReses[0] - center[0]) < radius)
            {
                gridRanges[3] += 1;
            }
            gridRanges[4] = iYCenter + yRange;
            if ((gridOrigin[1] + gridRanges[4] * gridReses[1] - center[1]) < radius)
            {
                gridRanges[4] += 1;
            }
            gridRanges[5] = iZCenter + zRange;
            if ((gridOrigin[2] + gridRanges[5] * gridReses[2] - center[2]) < radius)
            {
                gridRanges[5] += 1;
            }

            // check if gridRanges is out of upper boundary
            if (gridRanges[3] >= gridSizes[0])
            {
                gridRanges[3] = gridSizes[0] - 1;
            }
            if (gridRanges[4] >= gridSizes[1])
            {
                gridRanges[4] = gridSizes[1] - 1;
            }
            if (gridRanges[5] >= gridSizes[2])
            {
                gridRanges[5] = gridSizes[2] - 1;
            }

            return gridRanges;
        }

        public static int[] GetGridRange1d(double radius, double center, double origin, double size, double res)
        {
            var gridRanges = new int[2];

            int iCenter = (int)((center - origin) / res);
            int range = (int)(Math.Ceiling(radius / res));

            gridRanges[0] = iCenter - range;
            if (center - (origin + gridRanges[0] * res) < radius)
            {
                gridRanges[0] -= 1;
            }

            gridRanges[1] = iCenter + range;
            if (origin + gridRanges[1] * res - center < radius)
            {
                gridRanges[1] += 1;
            }

            return gridRanges;
        }

        public static Course GetCourse(Patient patient, string courseId)
        {
            var res = patient.Courses.Where(c => c.Id == courseId);
            if (res.Any())
            {
                return res.Single();
            }
            else
            {
                throw new ArgumentException("No corresponding Course", courseId);
            }
        }
        public static PlanSetup GetPlanSetup(Course course, string planId)
        {
            var res = course.PlanSetups.Where(p => p.Id == planId);
            if (res.Any())
            {
                return res.Single();
            }
            {
                throw new InvalidOperationException("No corresponding PlanSetup");
            }
        }

        public static Structure GetStructureFromId(string id, PlanSetup planSetup)
        {
            string structureId = id;
            var query = planSetup.StructureSet.Structures.Where(s => s.Id == structureId);
            if (query.Count() != 1)
            {
                return null;
            }
            return query.Single();
        }
    }

    /// <summary>
    /// Window for Input using TextBox and ComboBox
    /// Reference: https://stackoverflow.com/questions/8103743/wpf-c-sharp-inputbox
    /// </summary>
    public class InputWindow
    {
        public Window Window;
        public bool IsOk = false;
        public string WindowTitle;

        public string InputText;
        public string InputBoxTitle;
        public string DefaultInputBoxValue;
        private TextBox InputBox;

        //public string SelectedItem;
        //public string ComboBoxTitle;
        //private ComboBox ComboBox;

        public InputWindow(string windowTitle, string inputBoxTitle, string defaultInputBoxValue)
        {
            WindowTitle = windowTitle;
            InputBoxTitle = inputBoxTitle;
            DefaultInputBoxValue = defaultInputBoxValue;

            Window = new Window
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                Title = WindowTitle,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var InputBoxLabel = new Label();
            InputBoxLabel.Content = InputBoxTitle;

            InputBox = new TextBox();
            InputBox.MinWidth = 120;

            if (!string.IsNullOrEmpty(DefaultInputBoxValue))
            {
                InputBox.Text = DefaultInputBoxValue;
            }

            //var ComboBoxLabel = new Label();
            //ComboBoxLabel.Content = ComboBoxLabel;
            //ComboBox = new ComboBox();
            //ComboBox.ItemsSource = new List<string> { "PTV", "CTV" };
            //ComboBox.MinWidth = 120;

            var OkButton = new Button
            {
                Content = "OK",
                Margin = new Thickness(3),
                Width = 72
            };

            OkButton.Click += OkButton_Click;

            var CancelButton = new Button
            {
                Content = "Cancel",
                Margin = new Thickness(3),
                Width = 72
            };

            CancelButton.Click += CancelButton_Click;

            var stackPanelForButtons = new StackPanel();
            stackPanelForButtons.Orientation = Orientation.Horizontal;
            stackPanelForButtons.Children.Add(OkButton);
            stackPanelForButtons.Children.Add(CancelButton);

            var stackPanel = new StackPanel();
            Window.Content = stackPanel;

            stackPanel.Children.Add(InputBoxLabel);
            stackPanel.Children.Add(InputBox);
            //stackPanel.Children.Add(ComboBox);
            stackPanel.Children.Add(stackPanelForButtons);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            IsOk = true;

            //SelectedItem = ComboBox.SelectedItem.ToString();
            InputText = InputBox.Text;

            Window.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Window.Close();
        }
    }
}
