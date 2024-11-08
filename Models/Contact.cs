using System.ComponentModel.DataAnnotations;

namespace contacts.Models
{
    public class Contact
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
    }

}