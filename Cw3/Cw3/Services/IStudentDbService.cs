using Cw3.DTOs.Requests;
using Cw3.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.Services
{
    public interface IStudentDbService
    {
        //zmieniłem typ zwracy względem wykładu bo chcę móc zwrócić użytkownikowi odpowiedź
        EnrollStudentResponse EnrollStudent(EnrollStudentRequest request);
        PromoteStudentResponse PromoteStudents(int semester, string studies);
    }
}
