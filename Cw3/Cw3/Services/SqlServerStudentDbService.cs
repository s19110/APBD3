using Cw3.DTOs.Requests;
using Cw3.DTOs.Responses;
using Cw3.Other;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cw3.Services
{
    public class SqlServerStudentDbService : IStudentDbService
    {

        private static SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
        

        static SqlServerStudentDbService()
        {
            builder["Data Source"] = "db-mssql";
            builder["Initial Catalog"] = "s19110";
            builder["Integrated Security"] = "true";
            builder["MultipleActiveResultSets"] = "True";
        }

       

        public EnrollStudentResponse EnrollStudent(EnrollStudentRequest request)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
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
            using (var con = new SqlConnection(builder.ConnectionString))
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
        public bool CheckPassword(LoginRequestDTO request)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                // Sprawdzanie haseł sprzed ich zabezpieczenia
                //   com.CommandText = "SELECT 1 FROM Student WHERE IndexNumber=@number AND Password=@Password";              
                //   com.Parameters.AddWithValue("number", request.Login);
                //  com.Parameters.AddWithValue("Password", request.Password);
                
                //    var dr = com.ExecuteReader();
                
                //   return dr.Read();

                com.CommandText = "SELECT Password,Salt FROM Student WHERE IndexNumber=@number";
                com.Parameters.AddWithValue("number", request.Login);

                using var dr = com.ExecuteReader();

                if (dr.Read())
                {
                    return SecurePassword.Validate(request.Password, dr["Salt"].ToString(), dr["Password"].ToString());
                }
                return false; //Nie ma nawet takiego indeksu w bazie danych


            }
        }

        public Claim[] GetClaims(string IndexNumber)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                com.CommandText = "select Student.IndexNumber,FirstName,LastName,Role" +
                    " from Student_Roles Join Roles on Student_Roles.IdRole = Roles.IdRole join Student on Student.IndexNumber = Student_Roles.IndexNumber" +
                    " where Student.IndexNumber=@Index;";
                com.Parameters.AddWithValue("Index", IndexNumber);

                var dr = com.ExecuteReader();

                if (dr.Read())
                {
                    //Na starcie używam listy, bo nie wiem, ile ról ma dany użytkownik
                    var claimList = new List<Claim>();
                    claimList.Add(new Claim(ClaimTypes.NameIdentifier, dr["IndexNumber"].ToString()));
                    claimList.Add(new Claim(ClaimTypes.Name, dr["FirstName"].ToString() + " " + dr["LastName"].ToString())); //Nie wiem czy dawanie imienia i nazwiska w JWT to dobry pomysł, ale nie wiem jakie claimy warto utworzyć
                    claimList.Add(new Claim(ClaimTypes.Role, dr["Role"].ToString()));

                    while (dr.Read())
                    {
                        claimList.Add(new Claim(ClaimTypes.Role, dr["Role"].ToString()));
                    }
                    return claimList.ToArray<Claim>();
                }
                else return null;
                  


            }
        }

        public void SetRefreshToken(string token, string IndexNumber)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                com.CommandText = "UPDATE Student SET RefreshToken=@token, TokenExpirationDate=@expires WHERE IndexNumber=@IndexNumber";
                com.Parameters.AddWithValue("token", token);
                com.Parameters.AddWithValue("expires", DateTime.Now.AddDays(2));
                com.Parameters.AddWithValue("IndexNumber", IndexNumber);

               var dr = com.ExecuteNonQuery();


            }
        }

        public string CheckRefreshToken(string token)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                com.CommandText = "SELECT IndexNumber FROM STUDENT WHERE RefreshToken=@token AND TokenExpirationDate > @expires";
                com.Parameters.AddWithValue("token", token);
                com.Parameters.AddWithValue("expires", DateTime.Now);             

             using var dr = com.ExecuteReader();

                if (dr.Read())
                    return dr["IndexNumber"].ToString();
                else
                    return null;


            }
        }

        public void SetPassword(string password,string IndexNumber)
        {
            using (var con = new SqlConnection(builder.ConnectionString))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                com.CommandText = "Update Student set Password=@Password, Salt=@Salt WHERE IndexNumber=@IndexNumber";
                var salt = SecurePassword.CreateSalt();
                var hashedPassword = SecurePassword.Create(password, salt);
                com.Parameters.AddWithValue("Password", hashedPassword);
                com.Parameters.AddWithValue("Salt", salt);
                com.Parameters.AddWithValue("IndexNumber", IndexNumber);

                var dr = com.ExecuteNonQuery();


            }
        }
    }
}
