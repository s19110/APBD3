using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Cw3.DTOs.Requests;
using Cw3.Models;
using Cw3.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cw3.Controllers
{
    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private IStudentDbService _service;

        public EnrollmentsController(IStudentDbService service)
        {
            _service = service;
        }
        [HttpPost]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            try {
            return Ok(_service.EnrollStudent(request));

            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("promotions")]
        public IActionResult PromoteStudent(PromoteStudentRequest request)
        {
            try
            {
                //Mamy zwrócić 201, ale metoda za to odpowiedzialna wymaga adresu url, nie wiem jaki adres podać więc daję adres strony głównej uczelni
                return Created("https://www.pja.edu.pl/",_service.PromoteStudents(request.Semester, request.Studies));
            }
            catch (ArgumentException ex) {
                return NotFound("Nie znaleziono danego wpisu");
            }
        }
    }
}