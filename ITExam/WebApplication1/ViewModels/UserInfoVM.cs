using ITExam.ViewModels.Class;
using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels
{
    public class UserInfoVM
    {
        public int UserId { get; set; }
        public string HoTen { get; set; }
        public string? Lop { get; set; }
        public string? Email { get; set; }
        public string? Khoa { get; set; }

        public List<ClassExamVM> DanhSachDeThi { get; set; }

    }
}
