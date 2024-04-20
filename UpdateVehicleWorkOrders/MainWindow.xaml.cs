/* Title:           Update Vehicle Work Orders
 * Date:            10-25-17
 * Author:          Terrance Holmes
 * 
 * Description:     This will take the entries and create multiple work orders */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NewEventLogDLL;
using NewVehicleDLL;
using VehicleProblemsDLL;
using NewEmployeeDLL;
using DataValidationDLL;

namespace UpdateVehicleWorkOrders
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //setting up the classes
        WPFMessagesClass TheMessagesClass = new WPFMessagesClass();
        EventLogClass TheEventLogClass = new EventLogClass();
        VehicleClass TheVehicleClass = new VehicleClass();
        VehicleProblemClass TheVehicleProblemClass = new VehicleProblemClass();
        EmployeeClass TheEmployeeClass = new EmployeeClass();
        DataValidationClass TheDataValidationClass = new DataValidationClass();

        FindActiveVehiclesDataSet TheFindActiveVehiclesDataSet = new FindActiveVehiclesDataSet();
        FindOpenVehicleProblemsByVehicleIDDataSet TheFindOpenVehicleProblemsByVehicleIDDataSet = new FindOpenVehicleProblemsByVehicleIDDataSet();
        FindVehicleProblemUpdateByProblemIDDataSet TheFindVehicleProblemUpdateByProblemIDDataSet = new FindVehicleProblemUpdateByProblemIDDataSet();
        FindVehicleProblemByDateMatchDataSet TheFindVehicleProblemByDateMatchDataSet = new FindVehicleProblemByDateMatchDataSet();
        VerifyLogonDataSet TheVerifyLogonDataSet = new VerifyLogonDataSet();

        //created dataset
        ProblemUpdatesDataSet TheProblemUpdatesDataSet = new ProblemUpdatesDataSet();

        string gstrErrorMessage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            TheMessagesClass.CloseTheProgram();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgrProblemUpdates.Visibility = Visibility.Hidden;
            btnProcess.Visibility = Visibility.Hidden;
        }
        private bool LoadVehicles()
        {
            bool blnFatalError = false;
            int intCounter;
            int intNumberOfRecords;
            int intVehicleID;

            try
            {
                TheFindActiveVehiclesDataSet = TheVehicleClass.FindActiveVehicles();

                TheProblemUpdatesDataSet.problems.Rows.Clear();

                intNumberOfRecords = TheFindActiveVehiclesDataSet.FindActiveVehicles.Rows.Count - 1;

                for(intCounter = 0; intCounter <= intNumberOfRecords; intCounter++)
                {
                    intVehicleID = TheFindActiveVehiclesDataSet.FindActiveVehicles[intCounter].VehicleID;

                    blnFatalError = LoadProblemDataSet(intVehicleID);

                    if (blnFatalError == true)
                        throw new Exception();
                }
            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Update Vehicle Work Orders // Load Vehicles " + Ex.Message);

                gstrErrorMessage = Ex.ToString();

                blnFatalError = true;
            }

            return blnFatalError;
        }
        private bool LoadProblemDataSet(int intVehicleID)
        {
            bool blnFatalError = false;
            int intRecordsReturned;
            int intNumberOfRecords;
            int intCounter;
            int intProblemID;

            try
            {
                TheFindOpenVehicleProblemsByVehicleIDDataSet = TheVehicleProblemClass.FindOpenVehicleProblemsbyVehicleID(intVehicleID);

                intRecordsReturned = TheFindOpenVehicleProblemsByVehicleIDDataSet.FindOpenVehicleProblemsByVehicleID.Rows.Count;

                if (intRecordsReturned == 1)
                {
                    intProblemID = TheFindOpenVehicleProblemsByVehicleIDDataSet.FindOpenVehicleProblemsByVehicleID[0].ProblemID;

                    TheFindVehicleProblemUpdateByProblemIDDataSet = TheVehicleProblemClass.FindVehicleProblemUpdateByProblemID(intProblemID);

                    intNumberOfRecords = TheFindVehicleProblemUpdateByProblemIDDataSet.FindVehicleProblemUpdateByProblemID.Rows.Count - 1;

                    if(intNumberOfRecords > 0)
                    {
                        for (intCounter = 0; intCounter <= intNumberOfRecords; intCounter++)
                        {
                            ProblemUpdatesDataSet.problemsRow NewProblemRow = TheProblemUpdatesDataSet.problems.NewproblemsRow();

                            NewProblemRow.BJCNumber = TheFindVehicleProblemUpdateByProblemIDDataSet.FindVehicleProblemUpdateByProblemID[intCounter].BJCNumber;
                            NewProblemRow.ProblemUpdate = TheFindVehicleProblemUpdateByProblemIDDataSet.FindVehicleProblemUpdateByProblemID[intCounter].ProblemUpdate;
                            NewProblemRow.ImportProblem = false;
                            NewProblemRow.TransactionDate = TheFindVehicleProblemUpdateByProblemIDDataSet.FindVehicleProblemUpdateByProblemID[intCounter].TransactionDate;
                            NewProblemRow.VehicleID = intVehicleID;

                            TheProblemUpdatesDataSet.problems.Rows.Add(NewProblemRow);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Update Vehicle Work Orders // Load Vehicles " + Ex.Message);

                gstrErrorMessage = Ex.ToString();

                blnFatalError = true;
            }

            return blnFatalError;
        }

        private void btnProcess_Click(object sender, RoutedEventArgs e)
        {
            //setting up variables
            int intCounter;
            int intNumberOfRecords;
            int intBJCNumber;
            int intProblemID;
            string strProblem;
            DateTime datTransactionDate;
            bool blnImportProblem;
            bool blnFatalError;
            int intVehicleID;

            try
            {
                intNumberOfRecords = TheProblemUpdatesDataSet.problems.Rows.Count - 1;

                for(intCounter = 0; intCounter <= intNumberOfRecords; intCounter++)
                {
                    intBJCNumber = TheProblemUpdatesDataSet.problems[intCounter].BJCNumber;
                    strProblem = TheProblemUpdatesDataSet.problems[intCounter].ProblemUpdate;
                    datTransactionDate = TheProblemUpdatesDataSet.problems[intCounter].TransactionDate;
                    blnImportProblem = TheProblemUpdatesDataSet.problems[intCounter].ImportProblem;
                    intVehicleID = TheProblemUpdatesDataSet.problems[intCounter].VehicleID;

                    if(blnImportProblem == true)
                    {
                        blnFatalError = TheVehicleProblemClass.InsertVehicleProblem(intVehicleID, datTransactionDate, strProblem);

                        if (blnFatalError == true)
                            throw new Exception();

                        TheFindVehicleProblemByDateMatchDataSet = TheVehicleProblemClass.FindVehicleProblemByDateMatch(datTransactionDate);

                        intProblemID = TheFindVehicleProblemByDateMatchDataSet.FindVehicleProblemByDateMatch[0].ProblemID;

                        blnFatalError = TheVehicleProblemClass.InsertVehicleProblemUpdate(intProblemID, TheVerifyLogonDataSet.VerifyLogon[0].EmployeeID, "IMPORTED FROM ONE WORK ORDER", datTransactionDate);

                        if (blnFatalError == true)
                            throw new Exception();
                    }
                }

                TheMessagesClass.InformationMessage("Vehicles Have Been Updated");

                LoadVehicles();
            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Update Vehicle Work Orders // Process Button " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }
            
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            bool blnFatalError = false;
            int intEmployeeID = 0;
            string strValueForValidation;
            string strLastName;
            bool blnThereIsAProblem = false;
            string strErrorMessage = "";
            int intRecordsReturned;

            //data validation
            strValueForValidation = txtEmployeeID.Text;
            blnThereIsAProblem = TheDataValidationClass.VerifyIntegerData(strValueForValidation);
            if(blnThereIsAProblem == true)
            {
                blnFatalError = true;
                strErrorMessage += "The Employee ID is not an Integer\n";
            }
            else
            {
                intEmployeeID = Convert.ToInt32(strValueForValidation);
            }
            strLastName = txtLastName.Text;
            if(strLastName == "")
            {
                blnFatalError = true;
                strErrorMessage += "The Last Name Was not Entered\n";
            }
            if(blnFatalError == true)
            {
                TheMessagesClass.ErrorMessage(strErrorMessage);
                return;
            }

            TheVerifyLogonDataSet = TheEmployeeClass.VerifyLogon(intEmployeeID, strLastName);

            intRecordsReturned = TheVerifyLogonDataSet.VerifyLogon.Rows.Count;

            if(intRecordsReturned == 0)
            {
                TheMessagesClass.ErrorMessage("Employee Was Not Found");
                return;
            }
            else
            {
                dgrProblemUpdates.Visibility = Visibility.Visible;
                btnProcess.Visibility = Visibility.Visible;
                txtEmployeeID.Text = "";
                txtLastName.Text = "";
            }

            PleaseWait PleaseWait = new PleaseWait();
            PleaseWait.Show();

            LoadVehicles();

            dgrProblemUpdates.ItemsSource = TheProblemUpdatesDataSet.problems;

            PleaseWait.Close();

            if (blnFatalError == true)
            {
                TheMessagesClass.ErrorMessage(gstrErrorMessage);
                btnProcess.IsEnabled = false;
            }
        }
    }
}
