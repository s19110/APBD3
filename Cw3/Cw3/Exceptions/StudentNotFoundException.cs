using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.Exceptions
{
    public class StudentNotFoundException : Exception
    {
        public StudentNotFoundException(string msg) : base(msg) { }
     
    }
}
