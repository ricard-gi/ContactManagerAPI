using System.ComponentModel.DataAnnotations;

namespace contacts.Models {


public class User
{
    public int Id { get; set; }
    [Required]
    public string Username { get; set; }
    [Required]
    public string Password { get; set; }
    public bool IsAdmin { get; set; }
    public ICollection<Contact>? Contacts { get; set; }
}


}
