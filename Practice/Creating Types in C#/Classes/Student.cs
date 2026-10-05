namespace StudentNamespace
{
    public class Student
    {
        private static int _totalStudents;
        private readonly string _name;
        private int _age;

        public readonly Guid Id;

        public static int TotalStudents
        {
            get { return _totalStudents; }
        }

        public Student (string name, int age)
        {
            _name = name;
            _age = age;
            _totalStudents++;
            Id = Guid.NewGuid();
        }
        public int Age
        {
            get {return _age;}
        }

        public string Name
        {
            get {return _name;}
        }
    }
}
