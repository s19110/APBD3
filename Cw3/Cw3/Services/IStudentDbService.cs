using Cw3.DTOs.Requests;
using Cw3.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cw3.Services
{
    public interface IStudentDbService
    {
        //zmieniłem typ zwracy względem wykładu bo chcę móc zwrócić użytkownikowi odpowiedź
        EnrollStudentResponse EnrollStudent(EnrollStudentRequest request);
        PromoteStudentResponse PromoteStudents(int semester, string studies);

        public bool CheckPassword(LoginRequestDTO request);

        public Claim[] GetClaims(string IndexNumber);

        public void SetRefreshToken(string token, string indexNumber);
        public string CheckRefreshToken(string token);

        public void SetPassword(string password,string IndexNumber);
    }
}
