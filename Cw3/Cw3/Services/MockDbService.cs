using Cw3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.DAL
{
    public class MockDbService : IDbService
    {
        private static IEnumerable<Student> _students;

        static MockDbService()
        {
            _students = new List<Student>
            {
                new Student{FirstName="Jan",LastName="Kowalski"},
                new Student{FirstName="Anna",LastName="Malewski"},
                new Student{FirstName="Andrzej",LastName="Andrzejewicz" }
            };
        }

        public Student GetStudent(string IndexNumber)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Student> GetStudents()
        {
            return _students;
        }
    }
}
