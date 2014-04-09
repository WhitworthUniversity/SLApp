using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SLApp_Beta
{
	/// <summary>
	/// Interaction logic for CreateStudentProfile.xaml
	/// </summary>
	public partial class StudentProfile : Window
    {

        #region Database Methods, Formatting Methods, Members

        private Student student = new Student();
		DatabaseMethods dbMethods = new DatabaseMethods();

        private double myWidth;
        private double myHeight;

        // ComboBox choice listings for autocolumn generation, see
        // StudentLearningExperiences_DataGrid_OnAutoGeneratingColumn
        private string[] semesters = new string[] {"Fall", "Jan", "Spring"};
        private string[] servicelearningtype;

        /*Added to address SLApp bug fix B, upon which SlApp occasionally crashes when 
         * the user creates a student profile which contains no service learning experiences */
        private bool overwriteCheck(Student stud)
        {
            // Prompt the user with a yes/no message box.
            string message =
                "Are you sure you want to change identification information for the following student?\n\nName: "
                + stud.FirstName + " " + stud.LastName + "\nID: " + stud.Student_ID + "\nGrad Year: " 
                + stud.GraduationYear + "\nEmail: " + stud.Email;
            const string caption = "Confirm overwrite?";
            System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(message, caption,
                                         System.Windows.Forms.MessageBoxButtons.YesNo,
                                         System.Windows.Forms.MessageBoxIcon.Question);

            switch (result)
            {
                case System.Windows.Forms.DialogResult.Yes: return true;
                default:  return false;
            }
        }

        /*Added to increase the robustness of SLApp profile creation, such that it will not crash 
         * upon invalid user input in the Student ID box. */
        private bool student_ID_IntCheck(string input)
        {
            int id;
            bool check = Int32.TryParse(input, out id); //if input can be cast as an int32, return true 

            //If the input wasn't an integer, give an error message
            if (!check){
                string message =
                "Student ID must contain only integers (cannot contain letters).\nInformation not saved.";
                const string caption = "Invalid Student ID";
               System.Windows.Forms.MessageBox.Show(message, caption,
                                             System.Windows.Forms.MessageBoxButtons.OK,
                                             System.Windows.Forms.MessageBoxIcon.Error);
            }
            return check;
        }

        /*Added to increase the robustness of SLApp profile creation, such that it will 
         * not crash upon invalid user input in the graduation year box. */
        private bool studentGradYearIntCheck(string input)
        {
            int id;
            bool check = Int32.TryParse(input, out id); //if input can be cast as an int32, return true 

            //If the input wasn't an integer, give an error message
            if (!check)
            {
                string message =
                "Student graduation year must contain only integers (cannot contain letters).\nInformation not saved.";
                const string caption = "Invalid Student Graduation Year";
                System.Windows.Forms.MessageBox.Show(message, caption,
                                              System.Windows.Forms.MessageBoxButtons.OK,
                                              System.Windows.Forms.MessageBoxIcon.Error);
            }
            return check;
        }

        private void expanderCollapsedMinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.Width = myWidth;
            this.Height = myHeight;
        }

        private void expanderExpandedOpenWindow(object sender, RoutedEventArgs e)
        {
            myHeight = this.Height;
            myWidth = this.Width;

            this.Width += 200;
            this.Height += 100;
        }

        #endregion

        public StudentProfile(bool isAdmin)
		{
			InitializeComponent();

            if (isAdmin == false) studentNotes_DataGrid.IsEnabled = false;

            LoadStudentLearningExperiences();

            using (PubsDataContext db = new PubsDataContext())
            {
                servicelearningtype = (from type in db.Service_Learning_Types
                                       select type.Name).AsEnumerable().ToArray();
            }
		}

		public StudentProfile(Student stud, bool isAdmin, bool IsEdit) : this(isAdmin)
		{
			student = stud;

			this.studentFirstName_TB.Text = stud.FirstName;
			this.studentLastName_TB.Text = stud.LastName;
			this.studentID_TB.Text = stud.Student_ID.ToString();
			this.studentemail_TB.Text = stud.Email;
			this.graduationYear_TB.Text = stud.GraduationYear.ToString();

            LoadStudentLearningExperiences();
			
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (dbMethods.CheckDatabaseConnection())
			{
				using (PubsDataContext db = new PubsDataContext())
				{
					var empty = (from exp in db.Learning_Experiences
					             where exp.Student_ID == 0
					             select exp);
					db.Learning_Experiences.DeleteAllOnSubmit(empty);
					db.SubmitChanges();
				}
			}
		}

		#region Student Buttons

			void cancel_BTN_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

		private void save_BTN_Click(object sender, RoutedEventArgs e)
		{
			if (dbMethods.CheckDatabaseConnection())
			{
				using (PubsDataContext db = new PubsDataContext())
				{
					if(studentID_TB.Text.Length > 0 && graduationYear_TB.Text.Length > 0)
					{
                        // if there's a problem with the student ID, exit function
                        if (!student_ID_IntCheck(studentID_TB.Text)) 
                        {
                            return;
                        }
                        // if there's a problem with the student's grad year year, exit function
                        if (!studentGradYearIntCheck(graduationYear_TB.Text))
                        {
                            return;
                        }
						var CheckExists = (from s in db.Students
						                   where s.Student_ID == Convert.ToInt32(studentID_TB.Text)
						                   select s);
						//if the user does not exist, application will create a new user
						if (CheckExists.Count() == 0)
						{


							student.Student_ID = Convert.ToInt32(studentID_TB.Text);
							student.FirstName = studentFirstName_TB.Text;
							student.LastName = studentLastName_TB.Text;
							student.GraduationYear = Convert.ToInt32(graduationYear_TB.Text);
							student.Email = studentemail_TB.Text;


							Learning_Experience exp = new Learning_Experience();
							exp.Student_ID = Convert.ToInt32(studentID_TB.Text);
							db.Students.InsertOnSubmit(student);
							db.Learning_Experiences.InsertOnSubmit(exp);
							db.SubmitChanges();
							LoadStudentLearningExperiences();
						}
						else //if the student ID is found in the database
						{
							//save student info after checking that the user did not accidentally change information
                            Student stud = (from s in db.Students
                                            where s.Student_ID == Convert.ToInt32(studentID_TB.Text)
                                            select s).Single();

                            //Create a shallow copy to see if indentification info changes
                            Student studCopy = new Student();
                            studCopy.Student_ID = stud.Student_ID;
                            studCopy.FirstName = stud.FirstName;
                            studCopy.LastName = stud.LastName;
                            studCopy.GraduationYear = stud.GraduationYear;
                            studCopy.Email = stud.Email;

                            //Update student information using input fields
                            stud.Student_ID = Convert.ToInt32(studentID_TB.Text);
                            stud.FirstName = studentFirstName_TB.Text;
                            stud.LastName = studentLastName_TB.Text;
                            stud.GraduationYear = Convert.ToInt32(graduationYear_TB.Text);
                            stud.Email = studentemail_TB.Text;

                            //If the user has changed the basic information in the student profile, make sure
                            // that was intentional before updating.
                            if (!(studCopy.Student_ID == stud.Student_ID &&
                                studCopy.FirstName == stud.FirstName &&
                                studCopy.LastName == stud.LastName &&
                                studCopy.GraduationYear == stud.GraduationYear &&
                                studCopy.Email == stud.Email))
                            {
                                bool check = overwriteCheck(studCopy);
                                if (!check){  //If it was a mistake, don't save changes
                                    return; 
                                }
                            }
                            student = stud; // fills in this window's student information

                            //saves experience by calling the save experiences button event
                            learningExperienceSave();
                            db.SubmitChanges();
                            LoadStudentLearningExperiences(); // reloads student information into the window


                        
						}

					}
					else
					{
						MessageBox.Show(
							"SLApp apologizes for the inconvenience, but at this time all fields must contain data before saving.",
							"Save Error!", MessageBoxButton.OK, MessageBoxImage.Error);
					}

					//this.Close();
				}
			}
		}

        private void delete_BTN_Click(object sender, RoutedEventArgs e)
        {
            if (dbMethods.CheckDatabaseConnection())
            {
                if (MessageBox.Show("Are you sure you want to delete this student?", "Confirm Delete!", MessageBoxButton.YesNo) ==
                    MessageBoxResult.Yes)
                {
                    using (PubsDataContext db = new PubsDataContext())
                    {
						if (studentID_TB.Text.Length > 0 && graduationYear_TB.Text.Length > 0)
						{
							Student stud = (from s in db.Students
											where s.Student_ID == student.Student_ID
											select s).Single();
							var completionList = new List<Learning_Experience>(from s in db.Learning_Experiences
																			   where s.Student_ID == student.Student_ID
																			   select s);
							db.Students.DeleteOnSubmit(stud);
							db.Learning_Experiences.DeleteAllOnSubmit(completionList);
							db.SubmitChanges();
							this.Close();
						}
						else
						{
							MessageBox.Show(
								"SLApp apologizes for the inconvenience, but you cannot delete an empty profile.",
								"Delete Error!", MessageBoxButton.OK, MessageBoxImage.Error);
						}
                    }
                }
            }
        }

        #endregion

        #region Learning Experiences

		public void LoadStudentLearningExperiences()
		{
			if (dbMethods.CheckDatabaseConnection())
			{
				using (PubsDataContext db = new PubsDataContext())
				{

					var completionList = new List<Learning_Experience>(from s in db.Learning_Experiences
																	   where s.Student_ID == student.Student_ID
																	   select s);
					if (!completionList.Any())
					{
						Learning_Experience exp = new Learning_Experience();
						exp.Student_ID = student.Student_ID;
						db.Learning_Experiences.InsertOnSubmit(exp);
						db.SubmitChanges();
						completionList.Add(exp);
					}
					studentLearningExperiences_DataGrid.DataContext = completionList;

				}
			}
		}

        private bool learningExperienceFieldsCheck(Learning_Experience expROW)
        {
            if (expROW == null)
            {
                //MessageBox.Show("You must first select a valid row before adding, saving, or deleting.",
                //                "Datagrid Row Selection Error", MessageBoxButton.OK,
                //                MessageBoxImage.Exclamation);
                return false;
            }
            else if (expROW.Semester != "Fall" && expROW.Semester != "Jan" && expROW.Semester != "Spring" && expROW.Semester != "")
            {
                MessageBox.Show("Entry in Semester column invalid. Valid entries are blank, 'Fall', 'Jan', or 'Spring'.",
                                "Datagrid Row Error", MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
                return false;
            }
			else if (expROW.Section.Equals(null))
			{
				MessageBox.Show("Must enter course section!", "Datagrid Row Error", MessageBoxButton.OK,
				                MessageBoxImage.Exclamation);
			}
            return true;
        }

        private void learningExperienceSave()
        {
            Learning_Experience expROW = studentLearningExperiences_DataGrid.SelectedItem as Learning_Experience;
                if (learningExperienceFieldsCheck(expROW))
                {
                    if (dbMethods.CheckDatabaseConnection())
                    {
                        using (PubsDataContext db = new PubsDataContext())
                        {
                            //Learning_Experience exp = (from s in db.Learning_Experiences
                            //                           where s.Student_ID == expROW.Student_ID
                            //                           select s);
                            var completionList = new List<Learning_Experience>(from s in db.Learning_Experiences
                                                                               where s.ID == expROW.ID
                                                                               select s);
                            if (completionList.Count > 0)
                            {

                                var completion = completionList.First();
                                completion.Student_ID = student.Student_ID;
                                completion.ConfirmedHours = expROW.ConfirmedHours;
                                completion.CourseNumber = expROW.CourseNumber;
                                completion.LiabilityWaiver = expROW.LiabilityWaiver;
                                completion.ProjectAgreement = expROW.ProjectAgreement;
                                completion.Semester = expROW.Semester;
                                completion.Year = expROW.Year;
                                completion.TimeLog = expROW.TimeLog;
                                completion.TotalHours = expROW.TotalHours;
                                completion.TypeofLearning = expROW.TypeofLearning;
	                            completion.Section = expROW.Section;
	                            completion.Professor = expROW.Professor;
	                            completion.CourseName = expROW.CourseName;

                                db.SubmitChanges();
                                LoadStudentLearningExperiences();
                            }
                            else
                            {
                                Learning_Experience exp = new Learning_Experience();

                                exp.Student_ID = student.Student_ID;
                                exp.ConfirmedHours = expROW.ConfirmedHours;
                                exp.CourseNumber = expROW.CourseNumber;
                                exp.LiabilityWaiver = expROW.LiabilityWaiver;
                                exp.ProjectAgreement = expROW.ProjectAgreement;
                                exp.Semester = expROW.Semester;
                                exp.Year = expROW.Year;
                                exp.TimeLog = expROW.TimeLog;
                                exp.TotalHours = expROW.TotalHours;
                                exp.TypeofLearning = expROW.TypeofLearning;
								
								//The bottom three object variables were missing in this else statemenet == Fixes the problem of the second entry not saving properly
								exp.Section = expROW.Section;
								exp.Professor = expROW.Professor;
								exp.CourseName = expROW.CourseName;
                               
								db.Learning_Experiences.InsertOnSubmit(exp);
                                db.SubmitChanges();	
                                LoadStudentLearningExperiences();
							
                            }
                        }
                    }
                }
        }


        private void learningExperienceDelete()
        {
            if (dbMethods.CheckDatabaseConnection())
            {
                if (MessageBox.Show("Are you sure you want to delete this learning experience?", "Confirm Delete!", MessageBoxButton.YesNo) ==
                    MessageBoxResult.Yes)
                {
                    using (PubsDataContext db = new PubsDataContext())
                    {
                        Learning_Experience expROW = studentLearningExperiences_DataGrid.SelectedItem as Learning_Experience;

	                        var completionList = new List<Learning_Experience>(from s in db.Learning_Experiences
	                                                                           where s.ID == expROW.ID
	                                                                           select s);
                        if (expROW != null && completionList.Any())
                        {
                            var completion = completionList.First();
                            db.Learning_Experiences.DeleteOnSubmit(completion);
                            db.SubmitChanges();
                            LoadStudentLearningExperiences();
                        }
                        else
                        {
                            LoadStudentLearningExperiences();
                        }
                    }
                }
            }
        }

        private void Delete_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            learningExperienceDelete();
        }

        #endregion

        // Event which runs as columns in the main grid are auto-generated
		private void StudentLearningExperiences_DataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
            // don't show the ID column
            if (e.PropertyName == "ID") {
                e.Cancel = true;
            // Use ComboBox instead of text for Semester
            } else if (e.PropertyName == "Semester") {
                DataGridComboBoxColumn Combo = new DataGridComboBoxColumn();
                Combo.TextBinding = new Binding(e.PropertyName);
                Combo.ItemsSource = semesters;
                Combo.Header = "Semester";
                e.Column = Combo;
            // Use ComboBox instead of text for Type of Learning
            } else if (e.PropertyName == "TypeofLearning") {
                DataGridComboBoxColumn Combo = new DataGridComboBoxColumn();
                Combo.TextBinding = new Binding(e.PropertyName);
                Combo.ItemsSource = servicelearningtype;
                Combo.Header = "Type of Learning";
                e.Column = Combo;
            }
#if Demo
			if (e.PropertyName == "Student_ID") e.Cancel = true;
#endif
		}

        // When the user edits a row, make sure that the student ID column is auto-populated with the current student id
        private void studentLearningExperiences_DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            Learning_Experience x = e.Row.Item as Learning_Experience;
            x.Student_ID = student.Student_ID;
        }

        #region Notes

        

        #endregion

    }
}
