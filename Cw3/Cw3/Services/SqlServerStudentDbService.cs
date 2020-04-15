using Cw3.DTOs.Requests;
using Cw3.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.Services
{
    public class SqlServerStudentDbService : IStudentDbService
    {


        private static string DataSource, InitialCatalog, IntegratedSecurity;

        static SqlServerStudentDbService()
        {
            DataSource = "db-mssql";
            InitialCatalog = "s19110";
            IntegratedSecurity = "true";
        }

        public EnrollStudentResponse EnrollStudent(EnrollStudentRequest request)
        {
            using (var con = new SqlConnection($"Data Source={DataSource};Initial Catalog={InitialCatalog};Integrated Security={IntegratedSecurity};MultipleActiveResultSets=True;"))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();
                var tran = con.BeginTransaction();
                com.Transaction = tran;
                try
                {


                    //1. Czy studia instnieją?
                    com.CommandText = "SELECT IdStudy FROM studies WHERE name=@name";
                    com.Parameters.AddWithValue("name", request.Studies);
                    int idStudies;
                    using (var reader = com.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            tran.Rollback();
                            throw new ArgumentException("Podane studia nie istnieją");

                        }

                        idStudies = (int)reader["IdStudy"];
                    }

                    //2. Szukanie w tabeli Enrollment
                    com.CommandText = "SELECT * FROM ENROLLMENT WHERE IdStudy=@id AND Semester=1 ORDER BY StartDate desc";
                    com.Parameters.AddWithValue("id", idStudies);



                    var response = new EnrollStudentResponse();
                    int enrollmentId;
                    response.LastName = request.LastName;
                    var dr = com.ExecuteReader();
                    if (dr.Read())
                    {
                        response.Semester = (int)dr["Semester"];
                        response.StartDate = DateTime.Parse(dr["StartDate"].ToString());
                        enrollmentId = (int)dr["IdEnrollment"];
                        dr.Close();
                        dr.Dispose();
                    }
                
                    else
                    {
                        dr.Close();
                        dr.Dispose();
                        //Wstawianie nowego zapisu do bazy danych
                        //Używam zagnieżdżonego selecta zamiast Identity, bo nie korzystałem z niego od początku
                        com.CommandText = "INSERT into Enrollment(IdEnrollment, Semester, StartDate, IdStudy) values ((Select ISNULL(MAX(IdEnrollment),1)+1 From Enrollment),1, SYSDATETIME(),@id)";
                   
                        com.ExecuteNonQuery();

                        
                        response.Semester = 1;
                        com.CommandText = "SELECT SYSDATETIME() \"StartDate\", Max(IdEnrollment) \"IdEnrollment\" From Enrollment";
                         
                        dr = com.ExecuteReader();
                        dr.Read();
                        response.StartDate = DateTime.Parse(dr["StartDate"].ToString());
                        enrollmentId = (int)dr["IdEnrollment"];
                    }


                    //3. Sprawdzenie czy student o takim indeksie już istnieje
                    com.CommandText = "SELECT 1 From Student where IndexNumber=@IndexNumber ";
                    com.Parameters.AddWithValue("IndexNumber", request.IndexNumber);
                    dr.Close();
                    dr.Dispose();

                    dr = com.ExecuteReader();

                    if (dr.Read())
                    {
                        tran.Rollback();
                        throw new ArgumentException("Student o podanym indeksie już znajduje się w bazie danych");
                  
                    }
                    dr.Close();
                    dr.Dispose();


                    //4. Dodanie studenta
                    com.CommandText = "INSERT INTO Student(IndexNumber, FirstName, LastName, BirthDate, IdEnrollment) VALUES(@Index, @Fname, @Lname,@Bdate,@IdEnrollment)";
                    com.Parameters.AddWithValue("Index", request.IndexNumber);
                    com.Parameters.AddWithValue("Fname", request.FirstName);
                    com.Parameters.AddWithValue("Lname", request.LastName);
                    com.Parameters.AddWithValue("Bdate", request.BirthDate);
                    com.Parameters.AddWithValue("IdEnrollment", enrollmentId);
                
                   
                    com.ExecuteNonQuery();

                    tran.Commit();
                    //Muszę w jakiś sposób przekazać obiekt response do Controllera, interfejs narzuca by ta metoda była typu void
                    return response;
                }
                catch (SqlException ex)
                {                
                    tran.Rollback();
                    throw ex;
                }
            }
        }

        public PromoteStudentResponse PromoteStudents(int semester, string studies)
        {
            using (var con = new SqlConnection($"Data Source={DataSource};Initial Catalog={InitialCatalog};Integrated Security={IntegratedSecurity};MultipleActiveResultSets=True;"))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                var tran = con.BeginTransaction();
                com.Transaction = tran;

                try
                {
                    com.CommandText = "SELECT * FROM Enrollment join Studies on Studies.IdStudy = Enrollment.IdStudy where Name=@Name AND semester=@SemesterPar ";
                    com.Parameters.AddWithValue("Name", studies);
                    com.Parameters.AddWithValue("SemesterPar", semester);

                    using (var dr = com.ExecuteReader())
                    {
                        if (!dr.Read())
                            throw new ArgumentException("Nie znaleziono wpisu o podanej wartości");
                    }

                    com.CommandText = "EXEC promoteStudents @Studies=@Name, @semester=@SemesterPar";
                    com.ExecuteNonQuery();
                    tran.Commit();

                    return new PromoteStudentResponse
                    {
                        Semester = semester + 1,
                        Studies = studies

                    };
                }catch(SqlException ex)
                {
                    tran.Rollback();
                    throw new ArgumentException(ex.Message);
                }

            }


                //Zawartość procedury -- nie tworzę jej za każdym razem przy uruchamianiu tej metody

                /*Create Procedure PromoteStudents @Studies nvarchar(100), @Semester INT
     AS BEGIN
     Declare @IndexNumberCurs nvarchar(100), @NameCurs nvarchar(100), @SemesterCurs int, @StudyIdCurs int;
      Declare Studenci cursor for (Select IndexNumber, Name, Semester, Studies.IdStudy From Student
                                     Join Enrollment on Student.IdEnrollment = Enrollment.IdEnrollment join Studies on Studies.IdStudy = Enrollment.IdStudy
                                     where Name = @Studies and Semester = @Semester );

      Open studenci;
      Fetch next From studenci
      into @IndexNumberCurs, @NameCurs, @SemesterCurs, @StudyIdCurs;

      WHILE @@FETCH_STATUS = 0
         BEGIN  
         Declare @newEnrollmentId int = -1;
         Select  @newEnrollmentId = IdEnrollment from Enrollment Join Studies on Studies.IdStudy = Enrollment.IdStudy where Name = @NameCurs AND Semester=@SemesterCurs+1;


         IF @newEnrollmentId = -1
         BEGIN
         Insert Into Enrollment(IdEnrollment,Semester,IdStudy,StartDate) values ((Select Max(IdEnrollment)+1 From Enrollment), @SemesterCurs+1,@StudyIdCurs,SYSDATETIME());
         update Student set IdEnrollment =( Select MAX(IdEnrollment) FROM Enrollment )where IndexNumber=@IndexNumberCurs;
         END
         ELSE
         update Student set IdEnrollment = @newEnrollmentId where IndexNumber=@IndexNumberCurs;
         Fetch next From studenci
         into @IndexNumberCurs, @NameCurs, @SemesterCurs, @StudyIdCurs;
         END

         Close studenci;
         Deallocate studenci;
     END */


            }
    }
}
