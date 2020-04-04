using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.Models
{
    public class Enrollment
    {
        public int IdEnrollment { get; set; }
        public int Semester { get; set; }
        public string StudyName { get; set; }
        public DateTime StartDate { get; set; }
    }
}
