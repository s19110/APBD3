using Cw3.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.DAL
{
    public class SQLServerDbService : IDbService
    {
      private static string DataSource, InitialCatalog, IntegratedSecurity;

        static SQLServerDbService()
        {
            DataSource = "db-mssql";
            InitialCatalog = "s19110";
            IntegratedSecurity = "true";
        }
        public IEnumerable<Student> GetStudents()
        {
            using (var connection = new SqlConnection($"Data Source={DataSource};Initial Catalog={InitialCatalog};Integrated Security={IntegratedSecurity}"))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;

                //W zadaniu oprócz danych z tabeli Student mamy jeszcze zwrócić nazwę studiów i numer semestru
                command.CommandText = "SELECT FirstName, LastName, IndexNumber,BirthDate, Name, Semester FROM Student JOIN Enrollment ON Student.IdEnrollment = Enrollment.IdEnrollment JOIN Studies ON Studies.IdStudy = Enrollment.IdStudy";
                var studentList = new List<Student>();

                connection.Open();
                var dataReader = command.ExecuteReader();
                
                while (dataReader.Read())
                {
                    var student = new Student();
                 //   student.IdStudent = id++;
                    student.FirstName = dataReader["FirstName"].ToString();
                    student.LastName = dataReader["LastName"].ToString();
                    student.IndexNumber = dataReader["IndexNumber"].ToString();
                    student.BirthDate = DateTime.Parse(dataReader["BirthDate"].ToString());
                   // student.SemesterNumber = int.Parse(dataReader["Semester"].ToString());
                   // student.StudyName = dataReader["Name"].ToString();
                    studentList.Add(student);
                }
                return studentList;
            }        
        }

        public Enrollment GetEnrollment(String idStudenta)
        {
            Student szukany = null;
            foreach(Student s in GetStudents())
            {
                if(s.IndexNumber == idStudenta)
                {
                    szukany = s;
                    break;
                }
            }
            if(szukany == null)
            throw new ArgumentException();
           
            using (var connection = new SqlConnection($"Data Source={DataSource};Initial Catalog={InitialCatalog};Integrated Security={IntegratedSecurity}"))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = "select Enrollment.IdEnrollment ,Semester, Name, StartDate" +
                    " from Enrollment join Student on Student.IdEnrollment = Enrollment.IdEnrollment join Studies on Studies.IdStudy = Enrollment.IdStudy" +
                    " where Student.IndexNumber = @NrIndeksu ";
                command.Parameters.AddWithValue("NrIndeksu", szukany.IndexNumber);

                connection.Open();
                var dataReader = command.ExecuteReader();
                var wpisNaSemestr = new Enrollment();
                while (dataReader.Read())
                {
                    wpisNaSemestr.IdEnrollment = int.Parse(dataReader["IdEnrollment"].ToString());
                    wpisNaSemestr.Semester = int.Parse(dataReader["Semester"].ToString());
                    wpisNaSemestr.StudyName = dataReader["Name"].ToString();
                    wpisNaSemestr.StartDate = DateTime.Parse(dataReader["Startdate"].ToString());
                }
                return wpisNaSemestr;
            }
        }
    }
}
